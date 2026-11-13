using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using UglyToad.PdfPig;

namespace ReportTests
{
    /// <summary>
    /// A textbox that may not grow draws only the lines that fit inside its height, and by default a
    /// line fits only when all of it does. SQL Server ReportViewer also draws a last line that
    /// overflows the box by a fraction (two 5.2pt lines in a 0.40cm box are 0.45pt too tall), and
    /// forms laid out for it rely on that. RdlEngineConfig.PdfMinimumVisibleLineFraction lets a host
    /// choose how much of the line must fit; the default, 1, keeps the original behavior.
    /// </summary>
    [TestFixture]
    [NonParallelizable]     // The fraction is a process-wide setting.
    public class PdfMinimumVisibleLineFractionTests
    {
        // 0.40cm = 11.3pt; two lines of 5.2pt Arial need ~11.8pt, so the second line overflows by ~0.5pt.
        private const string Rdl = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>3in</Width>
  <Body>
    <Height>1in</Height>
    <ReportItems>
      <Textbox Name=""Label"">
        <Top>0.2in</Top>
        <Left>0.1in</Left>
        <Height>0.40cm</Height>
        <Width>1.6283cm</Width>
        <CanGrow>false</CanGrow>
        <Value>9 ENF. NEUROLOGICA</Value>
        <Style>
          <FontFamily>Arial</FontFamily>
          <FontSize>5.2pt</FontSize>
          <PaddingLeft>0pt</PaddingLeft>
          <PaddingRight>0pt</PaddingRight>
          <PaddingTop>0pt</PaddingTop>
          <PaddingBottom>0pt</PaddingBottom>
        </Style>
      </Textbox>
    </ReportItems>
  </Body>
  <PageHeight>11in</PageHeight>
  <PageWidth>8.5in</PageWidth>
</Report>";

        private float _initial;
        private float _saved;

        [OneTimeSetUp]
        public void RememberInitialValue() => _initial = RdlEngineConfig.PdfMinimumVisibleLineFraction;

        [SetUp]
        public void Prepare()
        {
            _saved = RdlEngineConfig.PdfMinimumVisibleLineFraction;
            RdlEngineConfig.RdlEngineConfigInit();
        }

        [TearDown]
        public void Restore() => RdlEngineConfig.PdfMinimumVisibleLineFraction = _saved;

        [Test]
        public void DefaultIsWholeLinesOnly()
        {
            Assert.That(_initial, Is.EqualTo(1f), "the setting is opt-in: unless a host changes it, only whole lines are drawn");
        }

        [Test]
        public async Task Default_DropsALastLineThatOverflowsTheBox()
        {
            RdlEngineConfig.PdfMinimumVisibleLineFraction = 1f;

            string drawn = await RenderLetters();

            Assert.That(drawn, Does.Contain("ENF"), "the first line is drawn");
            Assert.That(drawn, Does.Not.Contain("NEUROLOGICA"), "the second line does not fit entirely, so it is not drawn");
        }

        [Test]
        public async Task LowerFraction_DrawsALastLineThatOverflowsOnlyAHair()
        {
            RdlEngineConfig.PdfMinimumVisibleLineFraction = 0.25f;

            string drawn = await RenderLetters();

            Assert.That(drawn, Does.Contain("ENF"));
            Assert.That(drawn, Does.Contain("NEUROLOGICA"), "most of the second line lies inside the box, so it is drawn");
        }

        [Test]
        public async Task OutOfRangeValues_AreClamped()
        {
            RdlEngineConfig.PdfMinimumVisibleLineFraction = 5f;     // treated as 1: whole lines only
            Assert.That(await RenderLetters(), Does.Not.Contain("NEUROLOGICA"));

            RdlEngineConfig.PdfMinimumVisibleLineFraction = 0f;     // treated as the 0.1 floor: still draws a line that overflows a hair
            Assert.That(await RenderLetters(), Does.Contain("NEUROLOGICA"));
        }

        private static async Task<string> RenderLetters()
        {
            var parser = new RDLParser(Rdl) { Folder = AppContext.BaseDirectory };
            using Report report = await parser.Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4), string.Join(" | ", (report.ErrorItems ?? new System.Collections.Generic.List<string>()).Cast<object>()));
            await report.RunGetData(null);

            using var stream = new MemoryStreamGen();
            await report.RunRender(stream, OutputPresentationType.PDF);
            byte[] bytes = ((MemoryStream)stream.GetStream()).ToArray();

            using var pdf = PdfDocument.Open(bytes);
            return new string(pdf.GetPages().Single().Letters.Select(l => l.Value).SelectMany(v => v).Where(char.IsLetter).ToArray());
        }
    }
}
