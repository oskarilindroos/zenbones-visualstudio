# Zenbones for Visual Studio

The [zenbones](https://github.com/zenbones-theme/zenbones.nvim) color themes
for **Visual Studio 2022 and 2026**, plus an editor extension for what a color
theme can't do on its own:

- **Bold and italics** where zenbones uses them, while a zenbones theme is active.
- **C# modifiers** (`public`, `static`, `class`, …) colored as types, as in
  zenbones for Neovim and VS Code. Off by default.

Both can be turned on or off under **Tools → Options → Zenbones**.

![Zenbones Dark in Visual Studio](marketplace/images/zenbones-dark.png)

![Zenbones Light in Visual Studio](marketplace/images/zenbones-light.png)

This is an unofficial port. Zenbones was created by
[Michael Chris Lopez (@mcchrish)](https://github.com/mcchrish), and all credit
for the design goes to the original project. The port was built with
[Claude Code](https://claude.com/claude-code).

## Install

Install **Zenbones Themes** from the
[Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=OskariLindroos.ZenbonesThemes)
or under **Extensions → Manage Extensions** in Visual Studio. The `.vsix` is
also attached to each [GitHub release](https://github.com/oskarilindroos/zenbones-visualstudio/releases).

Then pick a theme under **Tools → Options → Environment → General → Color Theme**:

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

## Notes

- Italics look best with a font that has a real italic, such as Cascadia Code.
- Bold or italics you set yourself in Fonts and Colors are kept.
- The C# modifier color can be changed in Fonts and Colors under
  **Keyword - Modifier (Zenbones)**. C# in Razor files isn't affected.
- So far only tested on Visual Studio 2022.

## Development

The themes are generated from the Neovim colorschemes in `vendor/zenbones.nvim`
(a submodule) by `tools/generate.py`, which needs only Python 3.10+.

```sh
git clone --recurse-submodules https://github.com/oskarilindroos/zenbones-visualstudio
cd zenbones-visualstudio
python3 tools/generate.py                     # regenerate the themes
python3 -m unittest discover -s tools/tests   # generator tests
```

Generated files are committed, and CI fails if they're out of date. The C#
tests in `src/ZenbonesTheme.Tests` need Windows. To try the extension, open
`ZenbonesTheme.sln` in Visual Studio (with the **Visual Studio extension
development** workload) and press **F5**.

To update zenbones:

```sh
git -C vendor/zenbones.nvim pull origin main
python3 tools/generate.py
```

## Credits

- Theme design and colors: [zenbones.nvim](https://github.com/zenbones-theme/zenbones.nvim)
  by [Michael Chris Lopez](https://github.com/mcchrish) (MIT).
- Token mapping data in `tools/zbvs/data` from
  [microsoft/theme-converter](https://github.com/microsoft/theme-converter)
  (MIT, see `tools/zbvs/data/LICENSE.theme-converter`).

## License

[MIT](LICENSE)
