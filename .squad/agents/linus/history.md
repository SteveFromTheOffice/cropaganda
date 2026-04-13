# Project Context

- **Owner:** Michael Scott
- **Project:** cropaganda — Windows desktop app for fast batch photo cropping to 4:5 (Instagram) format
- **Stack:** C# / WPF, Windows
- **Created:** 2026-04-13

## Learnings
- Image library chosen: WPF built-in (System.Windows.Media.Imaging), no extra NuGet packages. Project structure: src/Cropaganda (WPF app), src/Cropaganda.Tests (xUnit). ICropService interface at src/Cropaganda/Services/ICropService.cs is the abstraction layer.

<!-- Append new learnings below. Each entry is something lasting about the project. -->
