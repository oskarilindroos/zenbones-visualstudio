# Zenbones Themes

The [zenbones](https://github.com/zenbones-theme/zenbones.nvim) collection of
contrast-based color themes for Visual Studio 2022 and 2026, plus a small
editor extension for what a Visual Studio color theme can't do on its own.
Zenbones relies on contrast and font style rather than lots of hues, so code
stays calm and readable.

A VS theme can only set colors, and only per classification. So besides the
19 themes, the extension:

- **adds zenbones' bold and italics** (italic comments and strings, bold
  keywords, …) while a zenbones theme is active, and removes them when you
  switch away
- **can color C# modifiers as types**, the way zenbones does in Neovim and VS
  Code, instead of as keywords (opt-in)
- adds a **Tools → Options → Zenbones** page to turn each of these on or off

![Zenbones Dark](https://raw.githubusercontent.com/oskarilindroos/zenbones-visualstudio/main/marketplace/images/zenbones-dark.png)

![Zenbones Light](https://raw.githubusercontent.com/oskarilindroos/zenbones-visualstudio/main/marketplace/images/zenbones-light.png)

All 19 variants are generated from the zenbones Neovim colorschemes, so the
colors match the originals exactly.

| Scheme | Variants |
| --- | --- |
| Zenbones | Light, Dark |
| Zenwritten | Light, Dark |
| Neobones | Light, Dark |
| Forestbones | Light, Dark |
| Rosebones | Light, Dark |
| Tokyobones | Light, Dark |
| Seoulbones | Light, Dark |
| Vimbones | Light |
| Nordbones, Duckbones, Zenburned, Kanagawabones | Dark |

## Getting started

Pick a theme under **Tools → Options → Environment → General → Color Theme**.

## Bold and italics

Zenbones uses font style as much as color: italic comments, strings and
constants, bold keywords. Visual Studio themes can only carry colors, so the
extension applies the styles itself while a zenbones theme is active, and
removes them when you switch to another theme. Bold or italics you set
yourself in Fonts and Colors are left alone.

Turn italics or bold off under **Tools → Options → Zenbones → General**.
Italics look best with a font that has a real italic, such as Cascadia Code.

## C# modifiers

Like zenbones in Neovim and VS Code, modifiers and declaration keywords
(`public`, `static`, `async`, `class`, `namespace`, …) can be colored as types
rather than as keywords. It's off by default to keep the extension light; turn
it on under **Tools → Options → Zenbones → General → Color C# modifiers**, and
adjust the color under **Fonts and Colors → Keyword - Modifier (Zenbones)**.

## Credits

Zenbones was created by [Michael Chris Lopez](https://github.com/mcchrish);
all credit for the color design goes to
[zenbones.nvim](https://github.com/zenbones-theme/zenbones.nvim). This is an
unofficial port, built with [Claude Code](https://claude.com/claude-code).

Issues and source: [GitHub](https://github.com/oskarilindroos/zenbones-visualstudio).
Free and open source under the MIT license.
