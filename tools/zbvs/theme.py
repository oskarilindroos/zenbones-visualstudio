"""Derive semantic roles from a zenbones variant.

`Roles` is the single source of truth for every color the Visual Studio
theme uses. `vscode_colors` / `token_rules` re-express the roles with
VS Code theme keys so Microsoft's theme-converter mapping tables can fan
them out to the ~1500 legacy Visual Studio shell tokens.
"""

from __future__ import annotations

from dataclasses import dataclass

from .colors import mix
from .vimcolors import Variant


@dataclass(frozen=True)
class Roles:
    dark: bool

    # surfaces
    bg: str
    chrome: str
    float: str
    float_sel: str
    float_border: str
    status_bg: str
    status_fg: str
    cursorline: str
    selection: str
    border: str
    border_subtle: str
    scrollbar: str
    scrollbar_hover: str

    # text
    fg: str
    fg_muted: str
    fg_faint: str
    line_nr: str
    line_nr_active: str
    whitespace: str

    # accents
    accent: str
    accent_fg: str
    link: str
    search: str
    search_fg: str
    cursearch: str
    cursearch_fg: str
    match_paren: str
    reference: str
    folded_bg: str
    folded_fg: str
    cursor: str

    # syntax
    comment: str
    constant: str
    number: str
    boolean: str
    string: str
    keyword: str
    type: str
    function: str
    identifier: str
    special: str
    delimiter: str
    preproc: str

    # diagnostics
    error: str
    warn: str
    info: str
    hint: str
    ok: str
    error_bg: str
    warn_bg: str
    info_bg: str
    hint_bg: str
    ok_bg: str

    # diff
    diff_add: str
    diff_change: str
    diff_delete: str
    diff_text: str

    terminal: tuple[str, ...]


def derive_roles(v: Variant) -> Roles:
    bg = v.bg("Normal")
    fg = v.fg("Normal")
    if not (bg and fg):
        raise ValueError(f"{v.id}: Normal has no colors")

    def fg_of(*names: str) -> str:
        return v.fg(*names) or fg

    def bg_of(*names: str) -> str:
        return v.bg(*names) or bg

    chrome = bg_of("TabLineFill", "StatusLineNC")
    border = fg_of("WinSeparator", "VertSplit")
    return Roles(
        dark=v.background == "dark",
        bg=bg,
        chrome=chrome,
        float=bg_of("NormalFloat", "Pmenu"),
        float_sel=bg_of("PmenuSel"),
        float_border=fg_of("FloatBorder"),
        status_bg=bg_of("StatusLine"),
        status_fg=fg_of("StatusLine"),
        cursorline=bg_of("CursorLine"),
        selection=bg_of("Visual"),
        border=border,
        border_subtle=mix(chrome, border, 0.45),
        scrollbar=bg_of("PmenuSbar"),
        scrollbar_hover=bg_of("PmenuThumb"),
        fg=fg,
        fg_muted=mix(fg, bg, 0.3),
        fg_faint=fg_of("LineNr"),
        line_nr=fg_of("LineNr"),
        line_nr_active=fg_of("CursorLineNr"),
        whitespace=fg_of("Whitespace", "NonText"),
        accent=bg_of("WildMenu"),
        accent_fg=fg_of("WildMenu"),
        link=v.fg("CocMarkdownLink") or v.terminal[6],
        search=bg_of("Search"),
        search_fg=fg_of("Search"),
        cursearch=bg_of("IncSearch", "CurSearch"),
        cursearch_fg=fg_of("IncSearch", "CurSearch"),
        match_paren=bg_of("MatchParen"),
        reference=bg_of("LspReferenceText", "ColorColumn"),
        folded_bg=bg_of("Folded"),
        folded_fg=fg_of("Folded"),
        cursor=bg_of("Cursor"),
        comment=fg_of("Comment"),
        constant=fg_of("Constant"),
        number=fg_of("Number", "Constant"),
        boolean=fg_of("Boolean", "Constant"),
        string=fg_of("String", "Constant"),
        keyword=fg_of("Statement"),
        type=fg_of("Type"),
        function=fg_of("Function"),
        identifier=fg_of("Identifier"),
        special=fg_of("Special"),
        delimiter=fg_of("Delimiter"),
        preproc=fg_of("PreProc"),
        error=fg_of("Error", "DiagnosticError"),
        warn=fg_of("DiagnosticWarn", "WarningMsg"),
        info=fg_of("DiagnosticInfo"),
        hint=fg_of("DiagnosticHint"),
        ok=fg_of("DiagnosticOk"),
        error_bg=bg_of("DiagnosticVirtualTextError"),
        warn_bg=bg_of("DiagnosticVirtualTextWarn"),
        info_bg=bg_of("DiagnosticVirtualTextInfo"),
        hint_bg=bg_of("DiagnosticVirtualTextHint"),
        ok_bg=bg_of("DiagnosticVirtualTextOk"),
        diff_add=bg_of("DiffAdd"),
        diff_change=bg_of("DiffChange"),
        diff_delete=bg_of("DiffDelete"),
        diff_text=bg_of("DiffText"),
        terminal=tuple(v.terminal),
    )


# Syntax role -> the zenbones group whose font style (bold/italic) it takes.
ROLE_GROUPS = {
    "fg": "Normal",
    "comment": "Comment",
    "constant": "Constant",
    "number": "Number",
    "boolean": "Boolean",
    "string": "String",
    "keyword": "Statement",
    "type": "Type",
    "function": "Function",
    "identifier": "Identifier",
    "special": "Special",
    "delimiter": "Delimiter",
    "preproc": "PreProc",
    "error": "Error",
}


def role_styles(v: Variant) -> dict[str, frozenset[str]]:
    """Bold/italic per syntax role; Visual Studio can't do the other styles."""
    return {
        role: v.group(group).styles & {"bold", "italic"} for role, group in ROLE_GROUPS.items()
    }


def vscode_colors(r: Roles) -> dict[str, str]:
    """Roles expressed as VS Code workbench color keys."""
    hover = mix(r.bg, r.float_sel, 0.5)
    return {
        "foreground": r.fg,
        "descriptionForeground": r.fg_muted,
        "errorForeground": r.error,
        "focusBorder": r.accent,
        "icon.foreground": r.fg_muted,
        "selection.background": r.selection,
        "widget.shadow": r.chrome,
        "textLink.foreground": r.link,
        "textLink.activeForeground": r.link,
        "progressBar.background": r.accent,
        # title bar, menus
        "titleBar.activeBackground": r.chrome,
        "titleBar.activeForeground": r.fg,
        "titleBar.inactiveBackground": r.chrome,
        "titleBar.inactiveForeground": r.fg_muted,
        "menubar.selectionBackground": r.float_sel,
        "menubar.selectionForeground": r.fg,
        "menubar.selectionBorder": r.float_sel,
        "menu.background": r.float,
        "menu.foreground": r.fg,
        "menu.border": r.float_border,
        "menu.selectionBackground": r.float_sel,
        "menu.selectionForeground": r.fg,
        "menu.selectionBorder": r.float_sel,
        "menu.separatorBackground": r.border_subtle,
        # tool windows
        "sideBar.background": r.bg,
        "sideBar.foreground": r.fg,
        "sideBar.border": r.border_subtle,
        "sideBarTitle.foreground": r.fg,
        "sideBarSectionHeader.background": r.chrome,
        "sideBarSectionHeader.foreground": r.fg,
        "panel.background": r.bg,
        "panel.border": r.border_subtle,
        "panelTitle.activeForeground": r.fg,
        "panelTitle.inactiveForeground": r.fg_muted,
        "panelSectionHeader.background": r.chrome,
        # lists and trees
        "list.activeSelectionBackground": r.float_sel,
        "list.activeSelectionForeground": r.fg,
        "list.inactiveSelectionBackground": r.cursorline,
        "list.inactiveSelectionForeground": r.fg,
        "list.hoverBackground": hover,
        "list.hoverForeground": r.fg,
        "list.focusBackground": r.float_sel,
        "list.focusForeground": r.fg,
        "list.focusOutline": r.accent,
        "list.dropBackground": r.selection,
        # tabs
        "editorGroupHeader.tabsBackground": r.chrome,
        "editorGroupHeader.noTabsBackground": r.chrome,
        "tab.activeBackground": r.bg,
        "tab.activeForeground": r.fg,
        "tab.activeBorderTop": r.accent,
        "tab.inactiveBackground": r.chrome,
        "tab.inactiveForeground": r.fg_muted,
        "tab.hoverBackground": hover,
        "tab.hoverForeground": r.fg,
        "tab.unfocusedHoverBackground": hover,
        "tab.unfocusedInactiveForeground": r.fg_muted,
        "tab.border": r.chrome,
        "tab.hoverBorder": r.chrome,
        # status bar
        "statusBar.background": r.status_bg,
        "statusBar.foreground": r.status_fg,
        "statusBar.debuggingBackground": r.warn,
        "statusBar.debuggingForeground": r.bg,
        "statusBar.noFolderBackground": r.status_bg,
        "statusBar.noFolderForeground": r.status_fg,
        "statusBarItem.hoverBackground": r.float_sel,
        # inputs and buttons
        "input.background": r.bg,
        "input.foreground": r.fg,
        "input.border": r.border_subtle,
        "inputOption.activeBackground": r.float_sel,
        "inputOption.activeBorder": r.accent,
        "inputValidation.errorBorder": r.error,
        "inputValidation.warningBorder": r.warn,
        "dropdown.background": r.float,
        "dropdown.foreground": r.fg,
        "dropdown.border": r.border_subtle,
        "dropdown.listBackground": r.float,
        "button.background": r.accent,
        "button.foreground": r.accent_fg,
        "button.border": r.accent,
        "button.hoverBackground": mix(r.accent, r.fg, 0.15),
        "button.secondaryBackground": r.float,
        "button.secondaryForeground": r.fg,
        "button.secondaryHoverBackground": r.float_sel,
        "checkbox.background": r.bg,
        "checkbox.border": r.border,
        "checkbox.foreground": r.fg,
        "settings.dropdownBackground": r.float,
        "banner.background": r.float,
        "banner.foreground": r.fg,
        "notifications.background": r.float,
        "notifications.foreground": r.fg,
        "badge.background": r.accent,
        "badge.foreground": r.accent_fg,
        # scrollbars and minimap
        "scrollbar.shadow": r.chrome,
        "scrollbarSlider.background": r.scrollbar,
        "scrollbarSlider.hoverBackground": r.scrollbar_hover,
        "scrollbarSlider.activeBackground": r.scrollbar_hover,
        "minimap.background": r.bg,
        "minimap.selectionHighlight": r.selection,
        "minimapSlider.background": r.cursorline,
        "minimapSlider.hoverBackground": r.float_sel,
        "minimapSlider.activeBackground": r.float_sel,
        # editor
        "editor.background": r.bg,
        "editor.foreground": r.fg,
        "editor.lineHighlightBackground": r.cursorline,
        "editor.lineHighlightBorder": r.cursorline,
        "editor.selectionBackground": r.selection,
        "editor.inactiveSelectionBackground": mix(r.bg, r.selection, 0.6),
        "editor.selectionForeground": r.folded_fg,
        "editor.foldBackground": r.folded_bg,
        "editor.wordHighlightBackground": r.reference,
        "editor.wordHighlightBorder": r.reference,
        "editor.wordHighlightStrongBackground": r.reference,
        "editor.wordHighlightStrongBorder": r.reference,
        "editorBracketMatch.background": r.match_paren,
        "editorBracketMatch.border": r.match_paren,
        "editorLineNumber.foreground": r.line_nr,
        "editorGutter.background": r.bg,
        "editorWhitespace.foreground": r.whitespace,
        "editorRuler.foreground": r.border_subtle,
        "editorHoverWidget.background": r.float,
        "editorHoverWidget.foreground": r.fg,
        "editorHoverWidget.border": r.float_border,
        "terminal.background": r.bg,
        "terminal.foreground": r.fg,
        "terminal.selectionBackground": r.selection,
        "welcomePage.background": r.bg,
        "welcomePage.progress.foreground": r.accent,
    }


def token_rules(r: Roles) -> list[tuple[str, str]]:
    """(TextMate scope, foreground) pairs, most general first."""
    return [
        ("comment", r.comment),
        ("string", r.string),
        ("constant", r.constant),
        ("constant.numeric", r.number),
        ("keyword", r.keyword),
        ("keyword.operator", r.delimiter),
        ("keyword.preprocessor", r.preproc),
        ("entity.name.function", r.function),
        ("entity.name.type", r.type),
        ("entity.name.tag", r.keyword),
        ("entity.name.label", r.special),
        ("entity.name.variable", r.identifier),
        ("entity.name.variable.field", r.identifier),
        ("entity.name.variable.enum-member", r.constant),
        ("entity.name.class", r.type),
        ("entity.other.attribute-name", r.special),
        ("variable", r.identifier),
        ("support.constant", r.constant),
        ("punctuation", r.delimiter),
    ]
