from __future__ import annotations
import re
import zipfile
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(r"D:\Documents\Projects\repos\CSM")
CSM_OPTIONS = ROOT / "Configuration" / "CSMModOptions.cs"
OUTPUT = ROOT / "MENU_MOCK.xlsx"

text = CSM_OPTIONS.read_text(encoding="utf-8")

NS_MAIN = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
NS_REL = "http://schemas.openxmlformats.org/officeDocument/2006/relationships"


def extract_method_block(source: str, signature_regex: str) -> str:
    m = re.search(signature_regex, source)
    if not m:
        raise ValueError(f"Method signature not found: {signature_regex}")
    idx = m.end()
    brace_start = source.find("{", idx)
    if brace_start == -1:
        raise ValueError(f"No opening brace for method: {signature_regex}")
    depth = 0
    for i in range(brace_start, len(source)):
        ch = source[i]
        if ch == "{":
            depth += 1
        elif ch == "}":
            depth -= 1
            if depth == 0:
                return source[brace_start + 1 : i]
    raise ValueError(f"No matching closing brace for method: {signature_regex}")


def strip_quotes(value: str) -> str:
    value = value.strip()
    if len(value) >= 2 and value[0] == '"' and value[-1] == '"':
        return value[1:-1]
    return value


def parse_provider_values(source: str) -> dict[str, list[str]]:
    providers: dict[str, list[str]] = {}
    for match in re.finditer(r"public\s+static\s+ModOption(?:String|Float|Int)\[\]\s+(\w+)\s*\(", source):
        name = match.group(1)
        block = extract_method_block(source, rf"public\s+static\s+ModOption(?:String|Float|Int)\[\]\s+{re.escape(name)}\s*\(")
        values = re.findall(r'new\s+ModOption(?:String|Float|Int)\("([^"]+)"', block)
        providers[name] = values
    return providers


def split_args(arg_text: str) -> list[str]:
    parts: list[str] = []
    current = []
    depth = 0
    in_str = False
    escape = False
    for ch in arg_text:
        if ch == '"' and not escape:
            in_str = not in_str
        if not in_str:
            if ch == "(":
                depth += 1
            elif ch == ")":
                depth -= 1
            elif ch == "," and depth == 0:
                part = "".join(current).strip()
                if part:
                    parts.append(part)
                current = []
                continue
        if escape:
            escape = False
        elif ch == "\\":
            escape = True
        current.append(ch)
    tail = "".join(current).strip()
    if tail:
        parts.append(tail)
    return parts


def parse_attr_kv(attr_text: str) -> dict[str, str]:
    kv: dict[str, str] = {}
    for part in split_args(attr_text):
        if "=" not in part:
            continue
        key, value = part.split("=", 1)
        kv[key.strip()] = value.strip()
    return kv


def normalize_default(value: str, field_type: str) -> str:
    value = value.strip()
    if field_type == "bool":
        return "On" if value.lower() == "true" else "Off"
    if field_type == "string":
        return strip_quotes(value)
    if field_type in ("float", "double"):
        value = value.rstrip("f")
        return value
    return value


def parse_modoptions(source: str) -> list[dict[str, str]]:
    results: list[dict[str, str]] = []
    idx = 0
    while True:
        start = source.find("[ModOption(", idx)
        if start == -1:
            break
        i = start + len("[ModOption(")
        depth = 1
        in_str = False
        escape = False
        end = -1
        while i < len(source):
            ch = source[i]
            if ch == '"' and not escape:
                in_str = not in_str
            if not in_str:
                if ch == "(":
                    depth += 1
                elif ch == ")":
                    depth -= 1
                    if depth == 0:
                        end = i
                        break
            if escape:
                escape = False
            elif ch == "\\":
                escape = True
            i += 1
        if end == -1:
            break
        attr_text = source[start + len("[ModOption(") : end]
        close = source.find("]", end)
        if close == -1:
            break
        field_match = re.search(
            r"public\s+static\s+([^\s]+)\s+([^\s=]+)\s*=\s*([^;]+);",
            source[close + 1 :],
        )
        if not field_match:
            idx = close + 1
            continue
        field_type, field_name, field_value = field_match.group(1, 2, 3)
        idx = close + 1 + field_match.end()
        kv = parse_attr_kv(attr_text)
        results.append(
            {
                "name": strip_quotes(kv.get("name", "")),
                "category": strip_quotes(kv.get("category", "")),
                "tooltip": strip_quotes(kv.get("tooltip", "")),
                "valueSourceName": strip_quotes(kv.get("valueSourceName", "")),
                "defaultValueIndex": kv.get("defaultValueIndex", ""),
                "interactionType": kv.get("interactionType", ""),
                "order": kv.get("order", ""),
                "categoryOrder": kv.get("categoryOrder", ""),
                "fieldType": field_type,
                "fieldName": field_name,
                "fieldValue": field_value.strip(),
            }
        )
    return results


providers = parse_provider_values(text)
options = parse_modoptions(text)


def parse_int(value: str) -> int | None:
    value = value.strip()
    if not value:
        return None
    try:
        return int(value)
    except ValueError:
        return None


# Preserve ModOptions order (first occurrence in file), but respect explicit order values
category_order: list[str] = []
category_order_values: dict[str, int] = {}
by_category: dict[str, list[dict[str, str]]] = {}
for idx, option in enumerate(options):
    option["index"] = idx
    category = option["category"] or ""
    if category not in by_category:
        by_category[category] = []
        category_order.append(category)
    by_category[category].append(option)
    category_order_val = parse_int(option.get("categoryOrder", ""))
    if category_order_val is not None:
        if category not in category_order_values:
            category_order_values[category] = category_order_val
        else:
            category_order_values[category] = min(category_order_values[category], category_order_val)


def category_sort_key(category: str) -> tuple[int, int]:
    if category in category_order_values:
        return (0, category_order_values[category])
    return (1, category_order.index(category))


def option_sort_key(option: dict[str, str]) -> tuple[int, int]:
    order_val = parse_int(option.get("order", ""))
    if order_val is not None:
        return (0, order_val)
    return (1, option["index"])


def control_type(option: dict[str, str]) -> str:
    if option["fieldType"] == "bool":
        return "Toggle"
    if option["valueSourceName"]:
        interaction = option.get("interactionType", "")
        if "2" in interaction:
            return "Slider"
        return "Dropdown"
    return "Input"


def default_display(option: dict[str, str]) -> str:
    default_value = normalize_default(option["fieldValue"], option["fieldType"])
    src = option["valueSourceName"]
    if src:
        try:
            idx = int(option["defaultValueIndex"])
        except ValueError:
            idx = -1
        values = providers.get(src, [])
        if 0 <= idx < len(values):
            return values[idx]
    return default_value


def provider_values_display(name: str) -> str:
    values = providers.get(name, [])
    if not values:
        return ""
    return " | ".join(values)


# Color scheme - RGB hex colors
COLORS = {
    "title_bg": "FF2F5496",        # Dark blue
    "title_fg": "FFFFFFFF",        # White
    "section_bg": "FF4472C4",      # Medium blue
    "section_fg": "FFFFFFFF",      # White
    "header_bg": "FFD6DCE5",       # Light gray-blue
    "header_fg": "FF000000",       # Black
    "data_bg": "FFFFFFFF",         # White
    "data_alt_bg": "FFF2F2F2",     # Light gray alternating
    "toggle_bg": "FFE2EFDA",       # Light green
    "slider_bg": "FFFFF2CC",       # Light yellow
    "dropdown_bg": "FFFCE4D6",     # Light orange
    "preset_bg": "FFE2E2F4",       # Light purple for preset categories
    "custom_bg": "FFDCE6F1",       # Light blue for custom trigger categories
    "killcam_bg": "FFEBE5D9",      # Light tan for killcam
    "triggers_bg": "FFD9EAD3",     # Light green for triggers
    "advanced_bg": "FFF4CCCC",     # Light red for advanced
    "intensity_bg": "FFDAE3F3",    # Blue tint for intensity
    "duration_bg": "FFD9EAD3",     # Green tint for duration
    "cooldown_bg": "FFFCE4D6",     # Orange tint for cooldown
    "chance_bg": "FFE4DFEC",       # Purple tint for chance
    "fade_bg": "FFF2F2F2",         # Gray for fade
    "multiplier_bg": "FFFFF2CC",   # Yellow for multiplier row
}

# Category to color mapping
CATEGORY_COLORS = {
    "Preset Selection": "preset_bg",
    "Optional Overrides": "preset_bg",
    "CSM Triggers": "triggers_bg",
    "CSM Killcam": "killcam_bg",
    "CSM Advanced": "advanced_bg",
    "CSM Statistics": "advanced_bg",
    "Custom: Basic Kill": "custom_bg",
    "Custom: Critical Kill": "custom_bg",
    "Custom: Dismemberment": "custom_bg",
    "Custom: Decapitation": "custom_bg",
    "Custom: Last Enemy": "custom_bg",
    "Custom: Last Stand": "custom_bg",
    "Custom: Parry": "custom_bg",
}


# Build styles XML with all our colors
def build_styles_xml():
    # Build fills
    fills = [
        ("none", None),
        ("gray125", None),
    ]
    fill_map = {}
    for color_name, color_val in COLORS.items():
        fill_idx = len(fills)
        fills.append(("solid", color_val))
        fill_map[color_name] = fill_idx

    fills_xml = '<fills count="{}">\n'.format(len(fills))
    for pattern, color in fills:
        if pattern == "none":
            fills_xml += '    <fill><patternFill patternType="none"/></fill>\n'
        elif pattern == "gray125":
            fills_xml += '    <fill><patternFill patternType="gray125"/></fill>\n'
        else:
            fills_xml += f'    <fill><patternFill patternType="solid"><fgColor rgb="{color}"/><bgColor indexed="64"/></patternFill></fill>\n'
    fills_xml += '  </fills>'

    # Fonts
    fonts_xml = '''<fonts count="4">
    <font><sz val="11"/><color theme="1"/><name val="Calibri"/><family val="2"/></font>
    <font><b/><sz val="14"/><color rgb="FFFFFFFF"/><name val="Calibri"/><family val="2"/></font>
    <font><b/><sz val="12"/><color rgb="FFFFFFFF"/><name val="Calibri"/><family val="2"/></font>
    <font><b/><sz val="11"/><color theme="1"/><name val="Calibri"/><family val="2"/></font>
  </fonts>'''

    # Build cellXfs - each style combination
    # Style indices:
    # 0 = default
    # 1 = title (font 1, title_bg)
    # 2 = section (font 2, section_bg)
    # 3 = header (font 3, header_bg)
    # 4+ = data styles with different backgrounds

    style_map = {
        "default": 0,
        "title": 1,
        "section": 2,
        "header": 3,
    }

    xf_entries = [
        (0, 0),  # 0: default
        (1, fill_map["title_bg"]),  # 1: title
        (2, fill_map["section_bg"]),  # 2: section
        (3, fill_map["header_bg"]),  # 3: header
    ]

    # Add data styles for each background color
    for color_name in ["data_bg", "data_alt_bg", "toggle_bg", "slider_bg", "dropdown_bg",
                       "preset_bg", "custom_bg", "killcam_bg", "triggers_bg", "advanced_bg",
                       "intensity_bg", "duration_bg", "cooldown_bg", "chance_bg", "fade_bg",
                       "multiplier_bg"]:
        style_idx = len(xf_entries)
        xf_entries.append((0, fill_map[color_name]))
        style_map[color_name] = style_idx

    xfs_xml = f'<cellXfs count="{len(xf_entries)}">\n'
    for font_id, fill_id in xf_entries:
        if font_id == 0 and fill_id == 0:
            xfs_xml += '    <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>\n'
        else:
            xfs_xml += f'    <xf numFmtId="0" fontId="{font_id}" fillId="{fill_id}" borderId="0" xfId="0" applyFont="1" applyFill="1"/>\n'
    xfs_xml += '  </cellXfs>'

    styles_xml = f'''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<styleSheet xmlns="{NS_MAIN}">
  {fonts_xml}
  {fills_xml}
  <borders count="1">
    <border><left/><right/><top/><bottom/><diagonal/></border>
  </borders>
  <cellStyleXfs count="1">
    <xf numFmtId="0" fontId="0" fillId="0" borderId="0"/>
  </cellStyleXfs>
  {xfs_xml}
  <cellStyles count="1">
    <cellStyle name="Normal" xfId="0" builtinId="0"/>
  </cellStyles>
</styleSheet>
'''
    return styles_xml.encode("utf-8"), style_map


styles_xml, STYLE_MAP = build_styles_xml()


def col_letter(idx: int) -> str:
    letters = []
    while idx:
        idx, rem = divmod(idx - 1, 26)
        letters.append(chr(65 + rem))
    return "".join(reversed(letters))


def is_number(value) -> bool:
    if value is None:
        return False
    if isinstance(value, (int, float)):
        return True
    if not isinstance(value, str):
        return False
    text = value.strip()
    if not text:
        return False
    if any(ch in text for ch in ("%", "x", "s", "m")):
        return False
    try:
        float(text)
        return True
    except ValueError:
        return False


def compute_col_widths(rows: list[list]) -> list[float]:
    widths: list[int] = []
    for row in rows:
        if not isinstance(row, (list, tuple)):
            continue
        for idx, cell in enumerate(row):
            if isinstance(cell, tuple):
                value = cell[0]
            else:
                value = cell
            text = "" if value is None else str(value)
            if idx >= len(widths):
                widths.append(len(text))
            else:
                widths[idx] = max(widths[idx], len(text))
    result: list[float] = []
    for length in widths:
        width = max(10, min(60, length + 3))
        result.append(float(width))
    return result


def build_sheet_xml(rows: list, col_widths: list[float] = None):
    """Build sheet XML. Rows can contain:
    - list of values (uses row-level style)
    - list of (value, style_name) tuples for per-cell styling
    - ("row_style", style_name) as first element to set row default
    """
    root = ET.Element(f"{{{NS_MAIN}}}worksheet")

    if col_widths is None:
        col_widths = compute_col_widths(rows)

    if col_widths:
        cols = ET.SubElement(root, f"{{{NS_MAIN}}}cols")
        for idx, width in enumerate(col_widths, start=1):
            ET.SubElement(
                cols,
                f"{{{NS_MAIN}}}col",
                {"min": str(idx), "max": str(idx), "width": f"{width:.2f}", "customWidth": "1"},
            )

    sheet_data = ET.SubElement(root, f"{{{NS_MAIN}}}sheetData")
    max_cols = max((len(r) for r in rows if isinstance(r, (list, tuple))), default=0)

    for r_idx, row in enumerate(rows, start=1):
        if not isinstance(row, (list, tuple)):
            continue

        row_el = ET.SubElement(sheet_data, f"{{{NS_MAIN}}}row", {"r": str(r_idx)})

        # Check for row-level style marker
        row_style = "default"
        start_idx = 0
        if row and isinstance(row[0], tuple) and row[0][0] == "row_style":
            row_style = row[0][1]
            start_idx = 1

        cells = list(row[start_idx:])
        while len(cells) < max_cols - start_idx:
            cells.append("")

        for c_idx, cell in enumerate(cells, start=1):
            cell_ref = f"{col_letter(c_idx)}{r_idx}"

            # Determine value and style
            if isinstance(cell, tuple):
                value, cell_style = cell
            else:
                value, cell_style = cell, row_style

            style_idx = STYLE_MAP.get(cell_style, STYLE_MAP["default"])

            if value is None or value == "":
                continue

            if is_number(value):
                c_el = ET.SubElement(row_el, f"{{{NS_MAIN}}}c", {"r": cell_ref, "s": str(style_idx)})
                v_el = ET.SubElement(c_el, f"{{{NS_MAIN}}}v")
                v_el.text = str(value).strip()
            else:
                c_el = ET.SubElement(
                    row_el, f"{{{NS_MAIN}}}c", {"r": cell_ref, "t": "inlineStr", "s": str(style_idx)}
                )
                is_el = ET.SubElement(c_el, f"{{{NS_MAIN}}}is")
                t_el = ET.SubElement(is_el, f"{{{NS_MAIN}}}t")
                t_el.text = str(value)

    return ET.tostring(root, encoding="utf-8", xml_declaration=True)


# ============ BUILD MENU MOCK SHEET ============
menu_rows = []

# Title
menu_rows.append([("row_style", "title"), "CSM Menu Mock - v1.5.0", "", "", "", "", "", ""])

# Blank row
menu_rows.append([])

# Header row
menu_rows.append([("row_style", "header"), "Category", "Option", "Control", "Default", "Values", "Tooltip"])

# Process categories
for category in sorted(category_order, key=category_sort_key):
    # Section header
    menu_rows.append([("row_style", "section"), category or "Global", "", "", "", "", ""])

    # Get category color
    cat_color = CATEGORY_COLORS.get(category, "data_bg")

    for opt_idx, option in enumerate(sorted(by_category[category], key=option_sort_key)):
        ctrl = control_type(option)
        default = default_display(option)
        src = option["valueSourceName"]
        values = provider_values_display(src)
        tooltip = option["tooltip"]

        # Determine cell color based on control type
        if ctrl == "Toggle":
            ctrl_color = "toggle_bg"
        elif ctrl == "Slider":
            ctrl_color = "slider_bg"
        elif ctrl == "Dropdown":
            ctrl_color = "dropdown_bg"
        else:
            ctrl_color = "data_bg"

        menu_rows.append([
            ("row_style", cat_color),
            (category, cat_color),
            (option["name"], cat_color),
            (ctrl, ctrl_color),
            (default, cat_color),
            (values, cat_color),
            (tooltip, cat_color),
        ])

    # Blank row after category
    menu_rows.append([])

# Remove trailing blank rows
while menu_rows and (not menu_rows[-1] or menu_rows[-1] == []):
    menu_rows.pop()


# ============ BUILD PRESET REFERENCE SHEET ============
preset_rows = []

# Title
preset_rows.append([("row_style", "title"), "Preset Reference", "", "", "", "", "", ""])
preset_rows.append([])

# ---- INTENSITY PRESET ----
preset_rows.append([("row_style", "section"), "INTENSITY PRESET - Time Scale", "", "", "", "", "", ""])
preset_rows.append([("row_style", "multiplier_bg"), "Multiplier:", "", "1.5x", "1.0x", "0.8x", "0.5x", "0.3x"])
preset_rows.append([("row_style", "header"), "Trigger", "Base", "Subtle", "Standard", "Dramatic", "Cinematic", "Epic"])

intensity_data = [
    ("Parry", 0.34, 0.51, 0.34, 0.27, 0.17, 0.10),
    ("Dismemberment", 0.30, 0.45, 0.30, 0.24, 0.15, 0.09),
    ("Basic Kill", 0.28, 0.42, 0.28, 0.22, 0.14, 0.08),
    ("Last Enemy", 0.26, 0.39, 0.26, 0.21, 0.13, 0.08),
    ("Critical", 0.25, 0.38, 0.25, 0.20, 0.13, 0.08),
    ("Decapitation", 0.23, 0.35, 0.23, 0.18, 0.12, 0.07),
    ("Last Stand", 0.30, 0.45, 0.30, 0.24, 0.15, 0.09),
]
for row in intensity_data:
    preset_rows.append([("row_style", "intensity_bg")] + list(row))

preset_rows.append([])

# ---- DURATION PRESET ----
preset_rows.append([("row_style", "section"), "DURATION PRESET - Seconds", "", "", "", "", "", ""])
preset_rows.append([("row_style", "multiplier_bg"), "Multiplier:", "", "0.35x", "0.7x", "1.0x", "1.35x", "1.7x"])
preset_rows.append([("row_style", "header"), "Trigger", "Base", "Very Short", "Short", "Standard", "Long", "Extended"])

duration_data = [
    ("Parry", 1.5, 0.53, 1.05, 1.5, 2.03, 2.55),
    ("Dismemberment", 2.0, 0.70, 1.40, 2.0, 2.70, 3.40),
    ("Basic Kill", 2.5, 0.88, 1.75, 2.5, 3.38, 4.25),
    ("Last Enemy", 2.75, 0.96, 1.93, 2.75, 3.71, 4.68),
    ("Critical", 3.0, 1.05, 2.10, 3.0, 4.05, 5.10),
    ("Decapitation", 3.25, 1.14, 2.28, 3.25, 4.39, 5.53),
    ("Last Stand", 4.0, 1.40, 2.80, 4.0, 5.40, 6.80),
]
for row in duration_data:
    preset_rows.append([("row_style", "duration_bg")] + list(row))

preset_rows.append([])

# ---- COOLDOWN PRESET ----
preset_rows.append([("row_style", "section"), "COOLDOWN PRESET - Seconds", "", "", "", "", "", ""])
preset_rows.append([("row_style", "multiplier_bg"), "Multiplier:", "", "0x", "0.6x", "1.0x", "2.0x", "3.0x"])
preset_rows.append([("row_style", "header"), "Trigger", "Base", "Off", "Short", "Standard", "Long", "Extended"])

cooldown_data = [
    ("Parry", 5, 0, 3, 5, 10, 15),
    ("Basic Kill", 10, 0, 6, 10, 20, 30),
    ("Dismemberment", 10, 0, 6, 10, 20, 30),
    ("Critical", 10, 0, 6, 10, 20, 30),
    ("Decapitation", 10, 0, 6, 10, 20, 30),
    ("Last Enemy", 30, 0, 18, 30, 60, 90),
    ("Last Stand", 90, 0, 54, 90, 180, 270),
]
for row in cooldown_data:
    preset_rows.append([("row_style", "cooldown_bg")] + list(row))

preset_rows.append([])

# ---- CHANCE PRESET ----
preset_rows.append([("row_style", "section"), "CHANCE PRESET - Percentage (Off = 100%)", "", "", "", "", "", ""])
preset_rows.append([("row_style", "multiplier_bg"), "Multiplier:", "", "100%", "0.5x", "0.6x", "1.0x", "1.4x"])
preset_rows.append([("row_style", "header"), "Trigger", "Base", "Off", "Very Rare", "Rare", "Standard", "Frequent"])

chance_data = [
    ("Basic Kill", "25%", "100%", "13%", "15%", "25%", "35%"),
    ("Parry", "50%", "100%", "25%", "30%", "50%", "70%"),
    ("Dismemberment", "30%", "100%", "15%", "18%", "30%", "42%"),
    ("Critical", "75%", "100%", "38%", "45%", "75%", "100%"),
    ("Decapitation", "90%", "100%", "45%", "54%", "90%", "100%"),
    ("Last Enemy", "100%", "100%", "50%", "60%", "100%", "100%"),
    ("Last Stand", "100%", "100%", "50%", "60%", "100%", "100%"),
]
for row in chance_data:
    preset_rows.append([("row_style", "chance_bg")] + list(row))

preset_rows.append([])

# ---- FADE PRESETS ----
preset_rows.append([("row_style", "section"), "FADE PRESETS - Smoothing Percentage", "", "", "", "", "", ""])
preset_rows.append([("row_style", "header"), "Preset", "Smoothing %", "Description", "", "", "", ""])

fade_data = [
    ("Instant", "0%", "No transition, immediate"),
    ("Default", "10%", "Natural B&S feel"),
    ("Quick Fade", "15%", "Quick transition"),
    ("Medium Fade", "20%", "Moderate transition"),
    ("Long Fade", "30%", "Slow transition"),
    ("Very Long Fade", "40%", "Very gradual transition"),
]
for row in fade_data:
    preset_rows.append([("row_style", "fade_bg")] + list(row) + ["", "", "", ""])


# ============ BUILD PROVIDERS SHEET ============
providers_rows = []
providers_rows.append([("row_style", "title"), "Value Providers", "", ""])
providers_rows.append([])
providers_rows.append([("row_style", "header"), "Provider", "Value Count", "Values"])

for name in sorted(providers.keys()):
    values = providers.get(name, [])
    providers_rows.append([
        ("row_style", "data_bg"),
        name,
        len(values),
        provider_values_display(name)
    ])


# ============ BUILD XLSX ============
def sanitize(name: str) -> str:
    invalid = set(r'[]:*?/\\')
    cleaned = "".join(ch for ch in name if ch not in invalid)
    return cleaned[:31] if len(cleaned) > 31 else cleaned


sheets = [
    ("Menu Mock", menu_rows),
    ("Preset Reference", preset_rows),
    ("Providers", providers_rows),
]

# workbook.xml
workbook = ET.Element(f"{{{NS_MAIN}}}workbook", {f"{{{NS_REL}}}r": NS_REL})
sheets_el = ET.SubElement(workbook, f"{{{NS_MAIN}}}sheets")

sheet_names = []
name_counts = {}
for name, _ in sheets:
    base = sanitize(name)
    count = name_counts.get(base, 0) + 1
    name_counts[base] = count
    if count > 1:
        suffix = f" ({count})"
        trimmed = base[: 31 - len(suffix)]
        sheet_name = trimmed + suffix
    else:
        sheet_name = base
    sheet_names.append(sheet_name)

for idx, sheet_name in enumerate(sheet_names, start=1):
    ET.SubElement(
        sheets_el,
        f"{{{NS_MAIN}}}sheet",
        {"name": sheet_name, "sheetId": str(idx), f"{{{NS_REL}}}id": f"rId{idx}"},
    )
workbook_xml = ET.tostring(workbook, encoding="utf-8", xml_declaration=True)

# workbook rels
rels_root = ET.Element("Relationships", xmlns="http://schemas.openxmlformats.org/package/2006/relationships")
for idx in range(1, len(sheets) + 1):
    ET.SubElement(
        rels_root,
        "Relationship",
        {
            "Id": f"rId{idx}",
            "Type": "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet",
            "Target": f"worksheets/sheet{idx}.xml",
        },
    )
ET.SubElement(
    rels_root,
    "Relationship",
    {
        "Id": f"rId{len(sheets) + 1}",
        "Type": "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles",
        "Target": "styles.xml",
    },
)
workbook_rels_xml = ET.tostring(rels_root, encoding="utf-8", xml_declaration=True)

# root rels
root_rels = ET.Element("Relationships", xmlns="http://schemas.openxmlformats.org/package/2006/relationships")
ET.SubElement(
    root_rels,
    "Relationship",
    {"Id": "rId1", "Type": "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument", "Target": "xl/workbook.xml"},
)
root_rels_xml = ET.tostring(root_rels, encoding="utf-8", xml_declaration=True)

# [Content_Types].xml
sheet_overrides = "\n".join(
    f'  <Override PartName="/xl/worksheets/sheet{i}.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>'
    for i in range(1, len(sheets) + 1)
)
content_types = f"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
  <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
{sheet_overrides}
</Types>
""".encode("utf-8")

# Build sheets
sheet_xmls = []
for _, rows in sheets:
    sheet_xmls.append(build_sheet_xml(rows))

with zipfile.ZipFile(OUTPUT, "w", compression=zipfile.ZIP_DEFLATED) as zf:
    zf.writestr("[Content_Types].xml", content_types)
    zf.writestr("_rels/.rels", root_rels_xml)
    zf.writestr("xl/workbook.xml", workbook_xml)
    zf.writestr("xl/_rels/workbook.xml.rels", workbook_rels_xml)
    zf.writestr("xl/styles.xml", styles_xml)
    for idx, xml_bytes in enumerate(sheet_xmls, start=1):
        zf.writestr(f"xl/worksheets/sheet{idx}.xml", xml_bytes)

print(f"Wrote {OUTPUT}")
print("Color scheme:")
print("  - Title: Dark blue with white text")
print("  - Section headers: Medium blue with white text")
print("  - Column headers: Light gray-blue")
print("  - Categories: Color-coded by type (presets=purple, triggers=green, killcam=tan, custom=blue, advanced=red)")
print("  - Controls: Color-coded (Toggle=green, Slider=yellow, Dropdown=orange)")
print("  - Preset tables: Color-coded (Intensity=blue, Duration=green, Cooldown=orange, Chance=purple, Fade=gray)")
