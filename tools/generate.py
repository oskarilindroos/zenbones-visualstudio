#!/usr/bin/env python3
"""Generate the Visual Studio theme .pkgdef files from zenbones.nvim.

    python3 tools/generate.py          # write src/ZenbonesTheme/Themes/*.pkgdef
    python3 tools/generate.py --check  # fail if the committed files are stale
"""

from __future__ import annotations

import argparse
import re
import sys
import uuid
from pathlib import Path

from zbvs import fontstyles, pkgdef, vsdirect, vsmap
from zbvs.theme import derive_roles, token_rules, vscode_colors
from zbvs.vimcolors import Variant, load_variants

ROOT = Path(__file__).resolve().parent.parent
ZENBONES = ROOT / "vendor" / "zenbones.nvim"
PROJECT = ROOT / "src" / "ZenbonesTheme"
THEMES = PROJECT / "Themes"
MANIFEST = PROJECT / "source.extension.vsixmanifest"
CSPROJ = PROJECT / "ZenbonesTheme.csproj"
FONT_STYLES = PROJECT / "FontStyles.g.cs"

# Theme GUIDs must never change once published, or users lose their selection.
NAMESPACE = uuid.UUID("8b3c5d0e-2f4a-4e6b-9c1d-7a5e3f2b1c09")


def display_name(v: Variant, variants: list[Variant]) -> str:
    name = v.scheme.capitalize()
    both = sum(1 for o in variants if o.scheme == v.scheme) > 1
    return f"{name} {v.background.capitalize()}" if both else name


def build_theme(v: Variant, variants: list[Variant]) -> pkgdef.Theme:
    roles = derive_roles(v)
    tokens = vsmap.translate(vscode_colors(roles), token_rules(roles))
    vsdirect.apply(roles, tokens)

    guids = {**vsmap.category_guids(), **vsdirect.EXTRA_CATEGORY_GUIDS}
    # Several category names share a GUID (e.g. the Roslyn and C++ editor
    # items are all "Text Editor MEF Items"); merge them under the first name.
    canonical: dict[str, pkgdef.Category] = {}
    by_name: dict[str, pkgdef.Category] = {}
    for name, guid in guids.items():
        category = canonical.setdefault(guid, pkgdef.Category(name, uuid.UUID(guid)))
        by_name[name] = category
    for (category_name, color_name), aspects in tokens.items():
        if category_name not in by_name:
            raise KeyError(f"no GUID for category {category_name!r}")
        by_name[category_name].colors.setdefault(color_name, {}).update(aspects)

    # Fluent categories go last, per the VS 2026 migration guide.
    fluent = [c for c in canonical.values() if c.name in vsdirect.EXTRA_CATEGORY_GUIDS]
    legacy = sorted((c for c in canonical.values() if c not in fluent), key=lambda c: c.name)
    return pkgdef.Theme(
        name=display_name(v, variants),
        guid=uuid.uuid5(NAMESPACE, v.id),
        fallback=pkgdef.DARK_THEME if roles.dark else pkgdef.LIGHT_THEME,
        categories=legacy + fluent,
    )


def render_assets(ids: list[str]) -> str:
    assets = "".join(
        f'\n        <Asset Type="Microsoft.VisualStudio.VsPackage" d:Source="File" Path="Themes\\{i}.pkgdef" />'
        for i in ids
    )
    return assets + "\n        "


def render_content(ids: list[str]) -> str:
    items = "".join(
        f'\n    <Content Include="Themes\\{i}.pkgdef">'
        "\n      <CopyToOutputDirectory>Always</CopyToOutputDirectory>"
        "\n      <IncludeInVSIX>true</IncludeInVSIX>"
        "\n    </Content>"
        for i in ids
    )
    return items + "\n    "


def replace_block(text: str, body: str) -> str:
    return re.sub(
        r"(<!-- themes:start -->).*?(<!-- themes:end -->)",
        lambda m: m.group(1) + body + m.group(2),
        text,
        flags=re.S,
    )


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--check", action="store_true", help="verify instead of writing")
    args = parser.parse_args()

    if not (ZENBONES / "zenbones.json").exists():
        sys.exit("vendor/zenbones.nvim is missing; run: git submodule update --init")

    variants = load_variants(ZENBONES)
    outputs = {
        THEMES / f"{v.id}.pkgdef": pkgdef.render(build_theme(v, variants)) for v in variants
    }

    ids = [v.id for v in variants]
    outputs[MANIFEST] = replace_block(MANIFEST.read_text(encoding="utf-8"), render_assets(ids))
    outputs[CSPROJ] = replace_block(CSPROJ.read_text(encoding="utf-8"), render_content(ids))

    outputs[FONT_STYLES] = fontstyles.render_csharp(variants)

    # The VSIX packager can't rename linked files, so keep a copy beside the project.
    outputs[PROJECT / "LICENSE.txt"] = _read(ROOT / "LICENSE")

    stale = [p for p, text in outputs.items() if not p.exists() or _read(p) != text]
    extra = [p for p in THEMES.glob("*.pkgdef") if p not in outputs]
    if args.check:
        for p in stale + extra:
            print(f"out of date: {p.relative_to(ROOT)}")
        return 1 if stale or extra else 0

    THEMES.mkdir(parents=True, exist_ok=True)
    for p in stale:
        p.write_bytes(outputs[p].encode("utf-8"))
        print(f"wrote {p.relative_to(ROOT)}")
    for p in extra:
        p.unlink()
        print(f"removed {p.relative_to(ROOT)}")
    print(f"{len(variants)} themes up to date")
    return 0


def _read(path: Path) -> str:
    return path.read_bytes().decode("utf-8")


if __name__ == "__main__":
    raise SystemExit(main())
