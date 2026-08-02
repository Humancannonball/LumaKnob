# Third-party notices

LumaKnob's original code and artwork use the MIT License in `LICENSE`. No
third-party visual assets, fonts, or copied application artwork are bundled.

The Windows executable contains the following restored NuGet dependency
families. Versions are pinned in `Directory.Packages.props` and were audited on
2026-08-02.

| Component | Version | License / copyright |
| --- | --- | --- |
| Avalonia, Avalonia Win32, Skia, HarfBuzz, themes, and support packages | 12.1.1 | MIT; Copyright 2013–2026 The AvaloniaUI Project |
| Avalonia ANGLE Windows native binaries | 2.1.27548.20260419 | BSD 3-Clause; Copyright 2018 The ANGLE Project Authors |
| CommunityToolkit.Mvvm | 8.4.2 | MIT; .NET Foundation and contributors |
| HarfBuzzSharp and Windows native assets | 8.3.1.3 | MIT; Microsoft and upstream HarfBuzz contributors |
| MicroCom.Runtime | 0.11.6 | MIT; Nikita Tsukanov |
| SkiaSharp and Windows native assets | 3.119.4 | MIT; Microsoft, with upstream notices |
| System.CodeDom and System.Management | 10.0.10 | MIT; Microsoft Corporation |

The release executable embeds this file, LumaKnob's MIT license, and the exact
restored SkiaSharp/HarfBuzzSharp and ANGLE native notices. They remain part of
the single-file distribution.

## Common MIT license text

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

## Reference implementation review

The MIT-licensed [Twinkle Tray](https://github.com/xanderfrangos/twinkle-tray),
copyright (c) 2019 Xander Frangos, was reviewed as a product and Windows
display-control reference. No Twinkle Tray source code or visual assets are
bundled in LumaKnob. If a substantial implementation is ported later, its
copyright and permission notice must accompany that code and distribution.
