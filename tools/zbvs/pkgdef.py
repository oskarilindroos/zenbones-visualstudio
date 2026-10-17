"""Write Visual Studio theme .pkgdef files.

Port of microsoft/theme-converter's ColorCompiler (PkgDefWriter and
friends). Each theme category is stored as a registry binary value:

    int32  total length (including this field)
    int32  version (11)
    int32  category count (1)
      guid   category id (little-endian layout, as Guid.ToByteArray)
      int32  color count
        int32 name length, utf-8 name
        byte  background type [uint32 background]
        byte  foreground type [uint32 foreground]

Colors are CT_RAW (1) with bytes R, G, B, A; absent aspects are
CT_INVALID (0) with no value.
"""

from __future__ import annotations

import struct
import uuid
from dataclasses import dataclass, field

from .colors import rgba

VERSION = 11
CT_INVALID = 0
CT_RAW = 1

DARK_THEME = uuid.UUID("{1ded0138-47ce-435e-84ef-9ec1f439b749}")
LIGHT_THEME = uuid.UUID("{de3dbbcd-f642-433c-8353-8f1df4370aba}")


@dataclass
class Category:
    name: str
    guid: uuid.UUID
    # color name -> {"Background": "#..", "Foreground": "#.."}
    colors: dict[str, dict[str, str]] = field(default_factory=dict)


@dataclass
class Theme:
    name: str
    guid: uuid.UUID
    fallback: uuid.UUID
    categories: list[Category] = field(default_factory=list)


def _aspect(colors: dict[str, str], aspect: str) -> bytes:
    if aspect not in colors:
        return bytes([CT_INVALID])
    r, g, b, a = rgba(colors[aspect])
    return bytes([CT_RAW, r, g, b, a])


def encode_category(category: Category) -> bytes:
    body = struct.pack("<i", 1) + category.guid.bytes_le
    body += struct.pack("<i", len(category.colors))
    for name, colors in category.colors.items():
        encoded = name.encode("utf-8")
        body += struct.pack("<i", len(encoded)) + encoded
        body += _aspect(colors, "Background") + _aspect(colors, "Foreground")
    return struct.pack("<ii", 8 + len(body), VERSION) + body


def decode_category(blob: bytes) -> Category:
    """Inverse of encode_category, for tests and debugging."""
    total, version, count = struct.unpack_from("<iii", blob)
    if total != len(blob) or version != VERSION or count != 1:
        raise ValueError(f"unexpected header {total=} {version=} {count=}")
    category = Category("", uuid.UUID(bytes_le=blob[12:28]))
    (colors,) = struct.unpack_from("<i", blob, 28)
    offset = 32
    for _ in range(colors):
        (length,) = struct.unpack_from("<i", blob, offset)
        name = blob[offset + 4 : offset + 4 + length].decode("utf-8")
        offset += 4 + length
        aspects = {}
        for aspect in ("Background", "Foreground"):
            kind = blob[offset]
            offset += 1
            if kind != CT_INVALID:
                r, g, b, a = blob[offset : offset + 4]
                offset += 4
                aspects[aspect] = f"#{r:02X}{g:02X}{b:02X}" + ("" if a == 255 else f"{a:02X}")
        category.colors[name] = aspects
    if offset != len(blob):
        raise ValueError("trailing bytes")
    return category


def _guid(value: uuid.UUID) -> str:
    return "{" + str(value) + "}"


def render(theme: Theme) -> str:
    root = rf"$RootKey$\Themes\{_guid(theme.guid)}"
    lines = [
        f"[{root}]",
        f'@="{theme.name}"',
        f'"Name"="{theme.name}"',
        f'"FallbackId"="{_guid(theme.fallback)}"',
    ]
    for category in theme.categories:
        if not category.colors:
            continue
        data = ",".join(f"{b:02x}" for b in encode_category(category))
        lines += ["", f"[{root}\\{category.name}]", f'"Data"=hex:{data}']
    # Matches the converter's StreamWriter output: UTF-8 BOM, CRLF.
    return "﻿" + "\r\n".join(lines) + "\r\n"
