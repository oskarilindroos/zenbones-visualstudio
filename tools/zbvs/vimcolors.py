"""Parse the shipwright-compiled Vim colorschemes in zenbones.nvim.

Each ``colors/<name>.vim`` file contains one block per background
(``" light start`` ... ``" light end``) holding resolved hex values for
every highlight group plus the 16 terminal colors. Parsing these avoids
having to reimplement lush's HSLuv derivations.
"""

from __future__ import annotations

import json
import re
from dataclasses import dataclass, field
from pathlib import Path

_BLOCK = re.compile(r'^\s*" (light|dark) start$(.*?)^\s*" \1 end$', re.M | re.S)
_HIGHLIGHT = re.compile(
    r"^\s*highlight (\S+) guifg=(\S+) guibg=(\S+) guisp=(\S+) gui=(\S+)", re.M
)
_LINK = re.compile(r"^\s*highlight! link (\S+) (\S+)", re.M)
_TERM = re.compile(r"^\s*let g:terminal_color_(\d+) = '(#[0-9A-Fa-f]{6})'", re.M)


@dataclass(frozen=True)
class Group:
    fg: str | None = None
    bg: str | None = None
    sp: str | None = None
    styles: frozenset[str] = frozenset()


@dataclass
class Variant:
    """One concrete colorscheme, e.g. zenbones with background=dark."""

    scheme: str
    background: str
    description: str
    groups: dict[str, Group] = field(default_factory=dict)
    links: dict[str, str] = field(default_factory=dict)
    terminal: list[str] = field(default_factory=list)

    @property
    def id(self) -> str:
        return f"{self.scheme}_{self.background}"

    def group(self, name: str) -> Group:
        """Resolve a group through links; unknown groups are empty."""
        seen = set()
        while name in self.links and name not in seen:
            seen.add(name)
            name = self.links[name]
        return self.groups.get(name, Group())

    def fg(self, *names: str) -> str | None:
        return self._first("fg", names)

    def bg(self, *names: str) -> str | None:
        return self._first("bg", names)

    def _first(self, attr: str, names: tuple[str, ...]) -> str | None:
        for name in names:
            value = getattr(self.group(name), attr)
            if value:
                return value
        return None


def _color(value: str) -> str | None:
    return None if value == "NONE" else value.upper()


def _parse_block(body: str, variant: Variant) -> None:
    for name, fg, bg, sp, gui in _HIGHLIGHT.findall(body):
        styles = frozenset() if gui == "NONE" else frozenset(gui.split(","))
        variant.groups[name] = Group(_color(fg), _color(bg), _color(sp), styles)
    for name, target in _LINK.findall(body):
        variant.links[name] = target
    terminal = sorted(_TERM.findall(body), key=lambda t: int(t[0]))
    variant.terminal = [c.upper() for _, c in terminal]


# Groups every theme is built from. If the compiled colorschemes change format
# and stop matching the regexes above, these go missing and generation fails,
# instead of every color quietly falling back to Normal.
REQUIRED_GROUPS = (
    "Normal", "Comment", "Constant", "String", "Statement", "Type", "Function",
    "Identifier", "Special", "Delimiter", "PreProc", "Visual", "CursorLine",
    "LineNr", "Search", "StatusLine", "NormalFloat", "PmenuSel", "DiffAdd", "DiffDelete",
)


def _validate(variant: Variant) -> None:
    missing = [g for g in REQUIRED_GROUPS if g not in variant.groups and g not in variant.links]
    if missing:
        raise ValueError(f"{variant.id}: no highlight for {', '.join(missing)}")
    if len(variant.terminal) != 16:
        raise ValueError(f"{variant.id}: expected 16 terminal colors, found {len(variant.terminal)}")


def load_variants(zenbones_dir: Path) -> list[Variant]:
    """Return every light/dark variant of every scheme, in registry order."""
    registry = json.loads((zenbones_dir / "zenbones.json").read_text(encoding="utf-8"))
    variants = []
    for entry in registry:
        text = (zenbones_dir / "colors" / f"{entry['name']}.vim").read_text(encoding="utf-8")
        for background, body in _BLOCK.findall(text):
            if entry.get("background", background) != background:
                continue
            variant = Variant(entry["name"], background, entry["description"])
            _parse_block(body, variant)
            _validate(variant)
            variants.append(variant)
    if not variants:
        raise ValueError(f"no colorschemes found in {zenbones_dir}")
    return variants
