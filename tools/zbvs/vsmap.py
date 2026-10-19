"""Fan VS Code-style colors out to Visual Studio tokens.

A Python port of the translation step in microsoft/theme-converter
(Converter.GroupColorsByCategory), driven by its vendored mapping tables
in ./data (MIT, see data/LICENSE.theme-converter).
"""

from __future__ import annotations

import json
import re
from dataclasses import dataclass
from functools import cache
from pathlib import Path

from .colors import compound

DATA = Path(__file__).parent / "data"

# (category name, color name) -> {"Background": "#..", "Foreground": "#.."}
Tokens = dict[tuple[str, str], dict[str, str]]


@dataclass(frozen=True)
class ColorKey:
    category: str
    name: str
    aspect: str = "Foreground"
    vsc_opacity: float | None = None
    vsc_background: str | None = None

    @classmethod
    def parse(cls, text: str) -> "ColorKey":
        parts = text.split("&")
        if len(parts) == 2:
            return cls(*parts)
        if len(parts) == 3:
            return cls(*parts)
        if len(parts) == 4:
            return cls(parts[0], parts[1], "Foreground", float(parts[2]), parts[3])
        if len(parts) == 5:
            return cls(parts[0], parts[1], parts[2], float(parts[3]), parts[4])
        raise ValueError(f"invalid mapping {text!r}")


def _strip_comment(line: str) -> str:
    """`line` without a trailing // comment, ignoring // inside strings."""
    in_string = escaped = False
    for i, ch in enumerate(line):
        if escaped:
            escaped = False
        elif in_string and ch == "\\":
            escaped = True
        elif ch == '"':
            in_string = not in_string
        elif not in_string and line.startswith("//", i):
            return line[:i]
    return line


def _load_jsonc(path: Path):
    """json.loads that tolerates // comments and trailing commas."""
    lines = [_strip_comment(line) for line in path.read_text(encoding="utf-8-sig").splitlines()]
    return json.loads(re.sub(r",(\s*[\]}])", r"\1", "\n".join(lines)))


@cache
def scope_mappings() -> dict[str, tuple[ColorKey, ...]]:
    rows = _load_jsonc(DATA / "TokenMappings.json")["tokenColors"]
    return {
        row["VSC Token"]: tuple(ColorKey.parse(t) for t in row["VS Token"]) for row in rows
    }


@cache
def category_guids() -> dict[str, str]:
    return {k: v.lower() for k, v in _load_jsonc(DATA / "CategoryGuid.json").items()}


@cache
def _fallbacks() -> dict[str, str]:
    return _load_jsonc(DATA / "VSCTokenFallback.json")


@cache
def _overlays() -> dict[str, tuple[float, str]]:
    raw = _load_jsonc(DATA / "OverlayMapping.json")
    return {k: (v["Item1"], v["Item2"]) for k, v in raw.items()}


def _lookup(colors: dict[str, str], key: str) -> str | None:
    seen = set()
    while key not in colors and key in _fallbacks() and key not in seen:
        seen.add(key)
        key = _fallbacks()[key]
    return colors.get(key)


def _best_rule(token: str, rules: list[tuple[str, str]]) -> str | None:
    """Foreground of the most specific rule whose scope prefixes `token`."""
    matches = [(scope, fg) for scope, fg in rules if scope and token.startswith(scope)]
    return max(matches, key=lambda m: len(m[0]))[1] if matches else None


def translate(colors: dict[str, str], rules: list[tuple[str, str]]) -> Tokens:
    tokens: Tokens = {}

    def put(key: ColorKey, color: str) -> None:
        tokens.setdefault((key.category, key.name), {})[key.aspect] = color

    # Syntax: TextMate scopes, falling back per VSCTokenFallback.json.
    for token, keys in scope_mappings().items():
        fg = _best_rule(token, rules)
        if fg is None and token in _fallbacks():
            fallback = _fallbacks()[token]
            fg = colors.get("foreground") if fallback == "foreground" else _best_rule(fallback, rules)
        if fg is not None:
            for key in keys:
                put(key, fg)

    # Shell: workbench colors, pre-compensated where VS draws translucently.
    for name in colors:
        keys = scope_mappings().get(name)
        value = _lookup(colors, name)
        if not keys or value is None:
            continue
        if name in _overlays():
            opacity, base_name = _overlays()[name]
            base = _lookup(colors, base_name)
            if base is not None:
                value = compound(value, base, vs_opacity=opacity)
        for key in keys:
            color = value
            if key.vsc_opacity is not None and key.vsc_background:
                base = _lookup(colors, key.vsc_background)
                if base is not None:
                    color = compound(value, base, vsc_opacity=key.vsc_opacity)
            put(key, color)
    return tokens
