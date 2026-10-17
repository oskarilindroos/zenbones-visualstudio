"""Visual Studio tokens set directly from roles.

These are applied on top of the converter-derived tokens: editor
classifications (which the VS Code scope table only partially covers),
fixes for converter mappings that don't suit zenbones, and the Fluent
`Shell` / `ShellInternal` categories that Visual Studio 2026 reads first.
"""

from __future__ import annotations

from .colors import alpha, compound, mix
from .theme import Roles
from .vsmap import Tokens

TEXT_MANAGER = "Text Editor Text Manager Items"
LANGUAGE = "Text Editor Language Service Items"
MEF = "Text Editor MEF Items"
MARKER = "Text Editor Text Marker Items"
SHELL = "Shell"
SHELL_INTERNAL = "ShellInternal"

# Categories the converter's CategoryGuid.json doesn't know about.
EXTRA_CATEGORY_GUIDS = {
    SHELL: "{73708ded-2d56-4aad-b8eb-73b20d3f4bff}",
    SHELL_INTERNAL: "{5af241b7-5627-4d12-bfb1-2b67d11127d7}",
}

FG = "Foreground"
BG = "Background"


# Classification name -> role, for the Text Editor MEF Items.
SYNTAX = {
    # language service basics
    "keyword": "keyword",
    "keyword - control": "keyword",
    "operator": "delimiter",
    "operator - overloaded": "delimiter",
    "punctuation": "delimiter",
    "number": "number",
    "string": "string",
    "string - verbatim": "string",
    "string - escape character": "special",
    "Literal": "constant",
    "identifier": "identifier",
    "preprocessor keyword": "preproc",
    "preprocessor text": "preproc",
    "excluded code": "comment",
    "comment": "comment",
    # Roslyn (C#, VB)
    "class name": "type",
    "record class name": "type",
    "record struct name": "type",
    "struct name": "type",
    "interface name": "type",
    "enum name": "type",
    "delegate name": "type",
    "module name": "type",
    "type parameter name": "type",
    "namespace name": "fg",
    "method name": "function",
    "extension method name": "function",
    "field name": "identifier",
    "property name": "identifier",
    "event name": "identifier",
    "local name": "identifier",
    "parameter name": "identifier",
    "constant name": "constant",
    "enum member name": "constant",
    "label name": "special",
    "xml doc comment - attribute name": "comment",
    "xml doc comment - attribute quotes": "comment",
    "xml doc comment - attribute value": "comment",
    "xml doc comment - cdata section": "comment",
    "xml doc comment - comment": "comment",
    "xml doc comment - delimiter": "comment",
    "xml doc comment - entity reference": "comment",
    "xml doc comment - name": "comment",
    "xml doc comment - processing instruction": "comment",
    "xml doc comment - text": "comment",
    # C++
    "cppMacro": "preproc",
    "cppEnumerator": "constant",
    "cppGlobalVariable": "identifier",
    "cppLocalVariable": "identifier",
    "cppParameter": "identifier",
    "cppMemberField": "identifier",
    "cppStaticMemberField": "identifier",
    "cppProperty": "identifier",
    "cppEvent": "identifier",
    "cppType": "type",
    "cppRefType": "type",
    "cppValueType": "type",
    "cppClassTemplate": "type",
    "cppGenericType": "type",
    "cppFunction": "function",
    "cppMemberFunction": "function",
    "cppStaticMemberFunction": "function",
    "cppFunctionTemplate": "function",
    "cppNamespace": "fg",
    "cppLabel": "special",
    # markup
    # TextMate-colored markup (HTML etc. without the web workload installed)
    "Markup Node": "keyword",
    "Markup Attribute": "special",
    "Markup Attribute Value": "string",
    # HTML editor (ASP.NET and web development workload)
    "HTML Element Name": "keyword",
    "HTML Attribute": "special",
    "HTML Server-Side Script": "special",
    "HTML Attribute Value": "string",
    "HTML Tag Delimiter": "delimiter",
    "HTML Operator": "delimiter",
    "HTML Comment": "comment",
    "HTML Entity": "special",
    "CSS Selector": "keyword",
    "CSS Keyword": "keyword",
    "CSS String Value": "string",
    "CSS Property Name": "identifier",
    "CSS Property Value": "constant",
    "CSS Comment": "comment",
    "RazorTagHelperElement": "keyword",
    "RazorTagHelperAttribute": "special",
    "RazorComponentElement": "keyword",
    "RazorComponentAttribute": "special",
    "urlformat": "link",
}


LANGUAGE_SERVICE = {
    "Comment": "comment",
    "Keyword": "keyword",
    "Identifier": "identifier",
    "String": "string",
    "Number": "number",
    "Operator": "delimiter",
    "Text": "fg",
    "Function": "function",
    "Error": "error",
    "XML Attribute": "special",
    "XML Attribute Quotes": "delimiter",
    "XML Attribute Value": "string",
    "XML CData Section": "string",
    "XML Comment": "comment",
    "XML Delimiter": "delimiter",
    "XML Keyword": "keyword",
    "XML Name": "keyword",
    "XML Processing Instruction": "preproc",
    "XML Text": "fg",
    "XAML Attribute": "special",
    "XAML Attribute Quotes": "delimiter",
    "XAML Attribute Value": "string",
    "XAML CData Section": "string",
    "XAML Comment": "comment",
    "XAML Delimiter": "delimiter",
    "XAML Keyword": "keyword",
    "XAML Markup Extension Class": "type",
    "XAML Markup Extension Parameter Name": "special",
    "XAML Markup Extension Parameter Value": "string",
    "XAML Name": "keyword",
    "XAML Processing Instruction": "preproc",
    "XAML Text": "fg",
    "XSLT Keyword": "keyword",
    "SQL Operator": "delimiter",
    "SQL String": "string",
    "SQL Stored Procedure": "function",
    "SQL System Function": "function",
    "SQL System Table": "type",
}


def _text_views(r: Roles, put) -> None:
    """Output, Find Results and friends: plain editor-like text views."""
    selection = compound(r.selection, r.bg, vs_opacity=0.4)
    for category in (
        "Output Window",
        "Find Results",
        "Immediate Window",
        "Command Window",
        "Package Manager Console",
    ):
        put(category, "Plain Text", FG, r.fg)
        put(category, "Plain Text", BG, r.bg)
        put(category, "Selected Text", BG, selection)
        put(category, "Inactive Selected Text", BG, selection)
        put(category, "Current List Location", FG, r.fg)
        put(category, "Current List Location", BG, r.cursorline)
    put("Output Window", "OutputError", FG, r.error)
    put("Output Window", "OutputHeading", FG, r.keyword)
    put("Output Window", "OutputVerbose", FG, r.fg_muted)
    put("Output Window", "urlformat", FG, r.link)
    put("Find Results", "Selected Text", FG, r.fg)
    put("Editor Tooltip", "Plain Text", FG, r.fg)
    put("Editor Tooltip", "Plain Text", BG, r.float)


def _editor(r: Roles, put) -> None:
    for name, role in SYNTAX.items():
        put(MEF, name, FG, getattr(r, role))
    for name, role in LANGUAGE_SERVICE.items():
        put(LANGUAGE, name, FG, getattr(r, role))

    put(TEXT_MANAGER, "Plain Text", FG, r.fg)
    put(TEXT_MANAGER, "Plain Text", BG, r.bg)
    put(TEXT_MANAGER, "Visible Whitespace", FG, r.whitespace)
    put(TEXT_MANAGER, "Indicator Margin", BG, r.bg)

    put(MEF, "Line Number", FG, r.line_nr)
    put(MEF, "Line Number", BG, r.bg)
    put(MEF, "MarkerFormatDefinition/FindHighlight", BG, r.search)
    put(MEF, "MarkerFormatDefinition/FindHighlight", FG, r.search)
    put(MEF, "MarkerFormatDefinition/ScopeHighlight", BG, r.cursorline)
    put(MEF, "brace matching", FG, r.match_paren)
    put(MEF, "outlining.collapsehintadornment", FG, r.folded_fg)
    put(MEF, "outlining.collapsehintadornment", BG, r.folded_bg)
    put(MEF, "outlining.square", FG, r.line_nr)
    put(MEF, "outlining.square", BG, r.bg)
    put(MEF, "outlining.verticalrule", FG, r.border_subtle)
    put(MEF, "Block Structure Adornments", FG, r.border_subtle)
    put(MEF, "Peek Background", BG, r.float)
    put(MEF, "Peek Background Unfocused", BG, r.chrome)
    put(MEF, "Peek Focused Border", BG, r.accent)
    put(MEF, "Peek Label Text", FG, r.fg)
    put(MEF, "Peek History Selected", BG, r.float_sel)
    put(MEF, "Peek History Hovered", BG, mix(r.float, r.float_sel, 0.5))
    put(MEF, "deltadiff.add.line", BG, r.diff_add)
    put(MEF, "deltadiff.remove.line", BG, r.diff_delete)
    put(MEF, "deltadiff.add.word", BG, mix(r.diff_add, r.ok, 0.25))
    put(MEF, "deltadiff.remove.word", BG, mix(r.diff_delete, r.error, 0.25))
    put(MEF, "Track reverted changes", BG, r.info)
    put(MEF, "Track reverted changes", FG, r.info)
    put(MEF, "OverviewMarginBackground", BG, r.bg)
    put(MEF, "OverviewMarginCaret", FG, r.fg)
    put(MEF, "Suggestion ellipses (...)", FG, r.hint)
    put(MEF, "RenameTrackingTag", FG, r.hint)
    put(MEF, "RoslynActiveStatementTag", BG, r.warn_bg)

    # Squiggles, breakpoints and other markers.
    put(MARKER, "syntax error", FG, r.error)
    put(MARKER, "compiler error", FG, r.error)
    put(MARKER, "other error", FG, r.hint)
    put(MARKER, "compiler warning", FG, r.warn)
    put(MARKER, "hinted suggestion", FG, r.hint)
    put(MARKER, "Error Message", FG, r.error)
    put(MARKER, "Brace Matching (Rectangle)", BG, r.match_paren)
    put(MARKER, "Brace Matching (Highlight)", BG, r.match_paren)
    put(MARKER, "Collapsible Text (Collapsed)", FG, r.folded_fg)
    put(MARKER, "Collapsible Text (Expanded)", FG, r.line_nr)
    put(MARKER, "Current Statement", BG, r.warn_bg)
    put(MARKER, "Current Statement", FG, r.fg)
    put(MARKER, "Call Return", BG, r.hint_bg)
    put(MARKER, "Call Return", FG, r.fg)
    for state in ("Enabled", "Error", "Warning"):
        for kind in ("Breakpoint", "Breakpoint - Advanced", "Breakpoint - Mapped"):
            put(MARKER, f"{kind} ({state})", BG, r.error_bg)
            put(MARKER, f"{kind} ({state})", FG, r.fg)
    put(MARKER, "Bookmark", BG, r.info)
    put(MARKER, "Bookmark", FG, r.bg)
    put(MARKER, "Code Snippet Field", BG, r.reference)
    put(MARKER, "Code Snippet Dependent Field", FG, r.fg_muted)
    put(MARKER, "Refactoring Background", BG, r.reference)
    put(MARKER, "Refactoring Current Field", BG, r.search)
    put(MARKER, "Refactoring Dependent Field", FG, r.fg_muted)
    put(MARKER, "Track Changes before save", BG, r.warn)
    put(MARKER, "Track Changes before save", FG, r.warn)
    put(MARKER, "Track Changes after save", BG, r.ok)
    put(MARKER, "Track Changes after save", FG, r.ok)
    put(MARKER, "Definition Window Background", BG, r.float)
    put(MARKER, "Definition Window Current Match", BG, r.search)
    put(MARKER, "Read-Only Region", BG, r.chrome)

    put("CodeSense", "IndicatorText", FG, r.fg_faint)
    put("CodeSense", "IndicatorTextHovered", FG, r.fg)
    put("CodeSense", "IndicatorTextDisabled", FG, alpha(r.fg_faint, 0.6))
    put("CodeSense", "IndicatorSeparator", FG, r.border_subtle)


def _fluent(r: Roles, put) -> None:
    """Visual Studio 2026 semantic tokens (Shell / ShellInternal)."""

    def raised(amount: float) -> str:
        # Fluent controls sit slightly lighter than their surface in both modes.
        return mix(r.bg, r.fg if r.dark else "#FFFFFF", amount)

    disabled = mix(r.bg, r.fg, 0.38)
    shadow = "#000000"
    shell = {
        # accent
        "AccentFillDefault": r.accent,
        "AccentFillSecondary": alpha(r.accent, 0.9),
        "AccentFillTertiary": alpha(r.accent, 0.8),
        "AccentFillAlt": mix(r.accent, r.fg, 0.3),
        "AccentFillDisabled": alpha(r.fg, 0.2),
        "AccentFillSenary": alpha(r.accent, 0.12),
        "AccentFillSelectedTextBackground": r.selection,
        "AccentFillSelectedTextBackgroundSubtle": alpha(r.selection, 0.7),
        "AccentTextFillPrimary": r.accent,
        "AccentTextFillSecondary": mix(r.accent, r.fg, 0.25),
        "AccentTextFillTertiary": mix(r.accent, r.bg, 0.15),
        "AccentTextFillDisabled": disabled,
        # cards
        "CardBackgroundFillDefault": raised(0.04),
        "CardBackgroundFillSecondary": raised(0.06),
        "CardBackgroundFillTertiary": raised(0.02),
        "CardStrokeDefault": r.border_subtle,
        "CardStrokeDefaultSolid": r.border_subtle,
        "CardStrokeDefaultSolidAlt": r.border,
        # controls
        "ControlFillDefault": raised(0.06),
        "ControlFillSecondary": raised(0.1),
        "ControlFillTertiary": raised(0.03),
        "ControlFillQuaternary": raised(0.06),
        "ControlFillDisabled": raised(0.03),
        "ControlFillActiveInput": r.bg,
        "ControlFillTransparent": alpha(r.bg, 0),
        "ControlSolidFillDefault": raised(0.2),
        "ControlAltFillTransparent": alpha(r.bg, 0),
        "ControlAltFillDisabled": alpha(r.bg, 0),
        "ControlAltFillSecondary": alpha(r.fg, 0.04),
        "ControlAltFillTertiary": alpha(r.fg, 0.07),
        "ControlAltFillQuaternary": alpha(r.fg, 0.1),
        "ControlOnImageFillDefault": alpha(r.float, 0.8),
        "ControlOnImageFillSecondary": r.float,
        "ControlOnImageFillTertiary": r.float_sel,
        "ControlOnImageFillDisabled": alpha(r.float, 0),
        "ControlStrokeDefault": r.border_subtle,
        "ControlStrokeSecondary": r.border_subtle,
        "ControlStrokeTransparent": alpha(r.bg, 0),
        "ControlStrokeForStrongFillWhenOnImage": alpha(r.bg, 0.4),
        "ControlStrokeOnAccentDefault": alpha(r.accent_fg, 0.08),
        "ControlStrokeOnAccentSecondary": alpha(shadow, 0.3),
        "ControlStrokeOnAccentTertiary": alpha(shadow, 0.2),
        "ControlStrokeOnAccentDisabled": alpha(shadow, 0.1),
        "ControlStrongFillDefault": r.fg_faint,
        "ControlStrongFillDisabled": disabled,
        "ControlStrongStrokeDefault": r.border,
        "ControlStrongStrokeDisabled": r.border_subtle,
        # misc strokes
        "DividerStrokeDefault": r.border_subtle,
        "FocusStrokeOuter": r.fg,
        "FocusStrokeInner": r.bg,
        "SurfaceStrokeDefault": r.border_subtle,
        "SurfaceStrokeFlyout": r.float_border,
        # hyperlinks
        "HyperlinkFillPrimary": r.link,
        "HyperlinkFillSecondary": mix(r.link, r.fg, 0.25),
        "HyperlinkFillTertiary": alpha(r.link, 0.9),
        "HyperlinkFillDisabled": disabled,
        # layers and surfaces
        "LayerFillDefault": alpha(r.chrome, 0.5),
        "LayerFillAlt": r.bg,
        "SolidBackgroundFillBase": r.chrome,
        "SolidBackgroundFillBaseAlt": mix(r.chrome, shadow, 0.15),
        "SolidBackgroundFillSecondary": r.chrome,
        "SolidBackgroundFillTertiary": r.bg,
        "SolidBackgroundFillQuaternary": r.float,
        "SolidBackgroundFillQuinary": mix(r.float, r.float_sel, 0.5),
        "SolidBackgroundFillSenary": r.float_sel,
        "SurfaceBackgroundFillDefault": r.float,
        # shadow and smoke
        "ShadowFlyout": alpha(shadow, 0.26 if r.dark else 0.14),
        "SmokeFillDefault": alpha(shadow, 0.3),
        "SmokeFillInverse": alpha(r.float, 0.8),
        # subtle
        "SubtleFillTransparent": alpha(r.bg, 0),
        "SubtleFillDisabled": alpha(r.bg, 0),
        "SubtleFillSecondary": alpha(r.fg, 0.08),
        "SubtleFillTertiary": alpha(r.fg, 0.05),
        # system status
        "SystemFillAttention": r.info,
        "SystemFillAttentionBackground": r.info_bg,
        "SystemFillSolidAttentionBackground": r.float,
        "SystemFillCaution": r.warn,
        "SystemFillCautionBackground": r.warn_bg,
        "SystemFillCritical": r.error,
        "SystemFillCriticalBackground": r.error_bg,
        "SystemFillSuccess": r.ok,
        "SystemFillSuccessBackground": r.ok_bg,
        "SystemFillNeutral": r.fg_faint,
        "SystemFillNeutralBackground": alpha(r.fg, 0.04),
        "SystemFillSolidNeutral": r.fg_faint,
        "SystemFillSolidNeutralBackground": r.float,
        # text
        "TextFillPrimary": r.fg,
        "TextFillSecondary": r.fg_muted,
        "TextFillTertiary": r.fg_faint,
        "TextFillDisabled": disabled,
        "TextOnAccentFillPrimary": r.accent_fg,
        "TextOnAccentFillSecondary": alpha(r.accent_fg, 0.7),
        "TextOnAccentFillDisabled": alpha(r.accent_fg, 0.5),
        "TextOnAccentFillSelectedText": r.fg,
    }
    internal = {
        "EnvironmentBackground": r.chrome,
        "EnvironmentBody": r.chrome,
        "EnvironmentBodyText": r.fg,
        "EnvironmentBadge": r.float,
        "EnvironmentBorder": r.border_subtle,
        "EnvironmentBorderInactive": r.border_subtle,
        "EnvironmentHeader": r.bg,
        "EnvironmentHeaderInactive": r.chrome,
        "EnvironmentTab": r.bg,
        "EnvironmentTabInactive": r.chrome,
        "EnvironmentIndicator": r.border,
        "EnvironmentLayeredBackground": r.float,
        "EnvironmentLayeredBorder": r.border_subtle,
        "EnvironmentLogo": r.accent,
        "CaptionControlCloseFillPrimary": r.error,
        "CaptionControlCloseFillSecondary": alpha(r.error, 0.9),
        "CaptionControlCloseTextFillPrimary": r.bg,
        "CaptionControlCloseTextFillSecondary": alpha(r.bg, 0.7),
        "StatusBarBackgroundFillRest": r.status_bg,
        "StatusBarTextFillRest": r.status_fg,
        "StatusBarBackgroundFillBuilding": r.accent,
        "StatusBarTextFillBuilding": r.accent_fg,
        "StatusBarBackgroundFillDebugging": r.warn,
        "StatusBarTextFillDebugging": r.bg,
        "StatusBarBackgroundFillSolutionLoading": r.info,
        "StatusBarTextFillSolutionLoading": r.bg,
        "StatusBarControlFillSecondary": alpha(r.status_fg, 0.12),
        "StatusBarTextFillDisabled": alpha(r.status_fg, 0.5),
    }
    # Fluent tokens are single-aspect; the docs list them as Background.
    for name, color in shell.items():
        put(SHELL, name, BG, color)
    for name, color in internal.items():
        put(SHELL_INTERNAL, name, BG, color)


def apply(r: Roles, tokens: Tokens) -> None:
    def put(category: str, name: str, aspect: str, color: str) -> None:
        tokens.setdefault((category, name), {})[aspect] = color

    _text_views(r, put)
    _editor(r, put)
    _fluent(r, put)
