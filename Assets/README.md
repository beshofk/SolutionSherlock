# Assets

Place the official `SolutionSherlock` logo here as:

```
https://raw.githubusercontent.com/beshofk/SolutionSherlock/main/Assets/SolutionSherlock-logo-dark.png
```

This is the dark-mode logo (detective silhouette, magnifying glass, Dataverse
solution/document concept, colorful component puzzle blocks, blue/cyan accent
lighting) referenced by the [README](../README.md).

## Wiring it into the XrmToolBox plugin tile

XrmToolBox reads a plugin's tile icons from two Base64-encoded PNG strings on
the `[ExportMetadata]` attributes in
[`src/SolutionSherlockPlugin.cs`](../src/SolutionSherlockPlugin.cs):

- `SmallImageBase64` — 32x32 PNG
- `BigImageBase64` — 80x80 PNG

Once this logo file is added, generate the two required sizes and Base64
strings (for example with `[Convert]::ToBase64String([IO.File]::ReadAllBytes('path'))`
in PowerShell) and paste them into those attributes in place of `null`.
