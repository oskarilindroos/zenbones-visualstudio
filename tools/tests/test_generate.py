import re
import sys
import unittest
import uuid
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

import generate  # noqa: E402
from zbvs import pkgdef  # noqa: E402
from zbvs.colors import compound, contrast  # noqa: E402
from zbvs.theme import derive_roles  # noqa: E402
from zbvs.vimcolors import load_variants  # noqa: E402

DATA = re.compile(r'"Data"=hex:([0-9a-f,]+)')


def blobs(text: str) -> list[bytes]:
    return [bytes(int(h, 16) for h in m.split(",")) for m in DATA.findall(text)]


class PkgdefFormat(unittest.TestCase):
    def test_matches_vsix_color_compiler(self):
        # Same record layout and color bytes ("Python class" = #4EC9B0 ->
        # 4e,c9,b0,ff) as microsoft/PTVS Microsoft.PythonTools.theme.v17.0.pkgdef
        # for the VS Dark theme, trimmed to a single color.
        expected = bytes.fromhex(
            "360000000b00000001000000"
            "8556a075a800ed4dbae5e7a50bfa929a01000000"
            "0c000000507974686f6e20636c617373" "00" "014ec9b0ff"
        )
        category = pkgdef.Category(
            "Text Editor MEF Items",
            uuid.UUID("{75a05685-00a8-4ded-bae5-e7a50bfa929a}"),
            {"Python class": {"Foreground": "#4EC9B0"}},
        )
        self.assertEqual(pkgdef.encode_category(category), expected)

    def test_round_trip_with_alpha(self):
        category = pkgdef.Category(
            "Shell",
            uuid.uuid4(),
            {"A": {"Background": "#11223380"}, "B": {"Foreground": "#ABCDEF"}, "C": {}},
        )
        decoded = pkgdef.decode_category(pkgdef.encode_category(category))
        self.assertEqual(decoded.colors, category.colors)


class Generated(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.variants = load_variants(generate.ZENBONES)

    def test_all_variants_present(self):
        self.assertEqual(len(self.variants), 19)
        names = [generate.display_name(v, self.variants) for v in self.variants]
        self.assertEqual(len(set(names)), len(names))
        self.assertIn("Zenbones Dark", names)
        self.assertIn("Kanagawabones", names)

    def test_theme_guids_are_stable(self):
        # Changing these would orphan users' theme selection after an update.
        theme = generate.build_theme(self.variants[0], self.variants)
        self.assertEqual(str(theme.guid), "686f1e84-634e-5b99-ac9d-183780754f00")

    def test_every_blob_decodes(self):
        for v in self.variants:
            text = pkgdef.render(generate.build_theme(v, self.variants))
            for blob in blobs(text):
                category = pkgdef.decode_category(blob)
                self.assertEqual(pkgdef.encode_category(category), blob)

    def test_fluent_categories_last(self):
        theme = generate.build_theme(self.variants[0], self.variants)
        self.assertEqual([c.name for c in theme.categories[-2:]], ["Shell", "ShellInternal"])

    def test_plain_text_is_readable(self):
        for v in self.variants:
            r = derive_roles(v)
            self.assertGreaterEqual(contrast(r.fg, r.bg), 4.5, v.id)
            self.assertGreaterEqual(contrast(r.comment, r.bg), 2.5, v.id)

    def test_selection_is_compensated_for_vs_opacity(self):
        # VS paints "Selected Text" at 40% opacity; the stored color must
        # land on zenbones' Visual color once blended over the background.
        for v in self.variants:
            r = derive_roles(v)
            stored = compound(r.selection, r.bg, vs_opacity=0.4)
            drawn = compound(stored, r.bg, vsc_opacity=0.4)
            for a, b in zip(bytes.fromhex(drawn[1:7]), bytes.fromhex(r.selection[1:])):
                self.assertLessEqual(abs(a - b), 2, v.id)


if __name__ == "__main__":
    unittest.main()
