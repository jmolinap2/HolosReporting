using System.Net;
using System.Text.RegularExpressions;

namespace Majorsilence.Reporting.Rdl
{
    /// <summary>
    /// The text of an html textbox without its markup, for the outputs that have no html: Word, Excel and CSV.
    /// Line breaks and the end of a block become new lines; entities are decoded.
    /// </summary>
    internal static class HtmlPlainText
    {
        private static readonly Regex Break = new Regex(@"<\s*(br|/p|/div|/li|/tr|/h[1-6])\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex Tag = new Regex(@"<[^>]*>", RegexOptions.Compiled);

        public static string Convert(string html)
        {
            if (string.IsNullOrEmpty(html))
                return html;
            string s = Break.Replace(html, "\n");
            s = Tag.Replace(s, "");
            s = WebUtility.HtmlDecode(s);
            return s.TrimEnd('\n', '\r');
        }
    }
}
