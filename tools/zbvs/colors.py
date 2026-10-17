"""Small color helpers. Colors are '#RRGGBB' or '#RRGGBBAA' strings."""

from __future__ import annotations


def rgba(color: str) -> tuple[int, int, int, int]:
    h = color.lstrip("#")
    if len(h) == 6:
        h += "FF"
    if len(h) != 8:
        raise ValueError(f"bad color {color!r}")
    return tuple(int(h[i : i + 2], 16) for i in range(0, 8, 2))  # type: ignore[return-value]


def to_hex(r: float, g: float, b: float, a: float = 255) -> str:
    parts = [max(0, min(255, round(v))) for v in (r, g, b, a)]
    text = "#" + "".join(f"{v:02X}" for v in parts[:3])
    return text if parts[3] == 255 else text + f"{parts[3]:02X}"


def mix(base: str, over: str, amount: float) -> str:
    """Blend `over` onto `base`; amount=0 gives base, 1 gives over."""
    br, bg, bb, _ = rgba(base)
    orr, og, ob, _ = rgba(over)
    return to_hex(
        br + (orr - br) * amount,
        bg + (og - bg) * amount,
        bb + (ob - bb) * amount,
    )


def alpha(color: str, amount: float) -> str:
    r, g, b, _ = rgba(color)
    return to_hex(r, g, b, amount * 255)


def luminance(color: str) -> float:
    def channel(v: int) -> float:
        c = v / 255
        return c / 12.92 if c <= 0.03928 else ((c + 0.055) / 1.055) ** 2.4

    r, g, b, _ = rgba(color)
    return 0.2126 * channel(r) + 0.7152 * channel(g) + 0.0722 * channel(b)


def contrast(a: str, b: str) -> float:
    la, lb = sorted((luminance(a), luminance(b)), reverse=True)
    return (la + 0.05) / (lb + 0.05)


def compound(over: str, base: str, vs_opacity: float = 1.0, vsc_opacity: float = 1.0) -> str:
    """Port of theme-converter's GetCompoundColor.

    Pre-compensates a color VS will itself draw at `vs_opacity` over `base`,
    and/or flattens a VS Code color drawn at `vsc_opacity`. Result is opaque.
    """
    orr, og, ob, oa = rgba(over)
    br, bg, bb, ba = rgba(base)
    a = oa * vsc_opacity / 255 / vs_opacity
    base_a = ba / 255
    return to_hex(
        a * orr + (1 - a) * base_a * br,
        a * og + (1 - a) * base_a * bg,
        a * ob + (1 - a) * base_a * bb,
    )
