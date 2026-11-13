# HolosReporting (fork)

Fork of [majorsilence/Reporting](https://github.com/majorsilence/Reporting) used by HolosCoreOps to render its
RDLC templates without Wine. The libraries are published to nuget.org under their own ids so that the upstream
`Majorsilence.*` packages are not shadowed:

| Fork package | Upstream package |
| --- | --- |
| `Holos.Reporting.RdlEngine` | `Majorsilence.Reporting.RdlEngine` |
| `Holos.Reporting.DataProviders` | `Majorsilence.Reporting.DataProviders` |
| `Holos.Reporting.Spreadsheet` | `Majorsilence.Spreadsheet` |
| `Holos.Reporting.Pdf` | `Majorsilence.Pdf` |
| `Holos.Reporting.Pdf.Security` | `Majorsilence.Pdf.Security` |

Namespaces and assembly names are the same as upstream.

## Differences from upstream

1. `Rdl2008Normalizer` keeps per-run formatting (a bold label and a normal value in one box) by laying such boxes out
   as html, with the data escaped. Proposed upstream from branch `pr/rich-text-runs-as-html`.
2. `RdlEngineConfig.PdfMinimumVisibleLineFraction` (default 1, unchanged behavior) draws a last line that overflows a
   no-grow box by a fraction, as SQL Server ReportViewer does. Proposed upstream from
   `pr/pdf-minimum-visible-line-fraction`.
3. `Directory.Build.targets` renames the package ids when packing with `-p:HolosPackaging=true`, and
   `.github/workflows/holos-publish.yml` publishes them. Fork-only.

When both changes are in a released upstream version, HolosCoreOps can use the upstream packages and this fork can be retired.

## Release

```bash
git tag holos-v27.0.0-holos.2 && git push origin holos-v27.0.0-holos.2
```

The tag sets the version. See the header of `.github/workflows/holos-publish.yml` for the one-time nuget.org setup.
