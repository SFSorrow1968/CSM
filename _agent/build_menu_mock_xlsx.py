from __future__ import annotations
import re
import zipfile
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(r"C:\Users\dkatz\Documents\Projects\CSM")
CSM_OPTIONS = ROOT / "Configuration" / "CSMModOptions.cs"
OUTPUT = ROOT / "MENU_MOCK.xlsx"

text = CSM_OPTIONS.read_text(encoding="utf-8")


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
                "fieldType": field_type,
                "fieldName": field_name,
                "fieldValue": field_value.strip(),
            }
        )
    return results


providers = parse_provider_values(text)
options = parse_modoptions(text)

# Preserve category order
category_order: list[str] = []
by_category: dict[str, list[dict[str, str]]] = {}
for option in options:
    category = option["category"] or "Uncategorized"
    if category not in by_category:
        by_category[category] = []
        category_order.append(category)
    by_category[category].append(option)


def control_type(option: dict[str, str]) -> str:
    if option["fieldType"] == "bool":
        return "Toggle"
    if option["valueSourceName"]:
        return "Select"
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


# Build sheet rows
menu_rows: list[list[str]] = []
menu_row_types: list[str] = []
menu_rows.append(["CSM Menu Mock"])
menu_row_types.append("title")
menu_rows.append([])
menu_row_types.append("blank")
menu_rows.append(["Category", "Option", "Control", "Default", "Values", "Source", "Tooltip"])
menu_row_types.append("header")

for category in category_order:
    menu_rows.append([category])
    menu_row_types.append("section")
    for option in by_category[category]:
        src = option["valueSourceName"]
        menu_rows.append(
            [
                category,
                option["name"],
                control_type(option),
                default_display(option),
                provider_values_display(src),
                src,
                option["tooltip"],
            ]
        )
        menu_row_types.append("data")
    menu_rows.append([])
    menu_row_types.append("blank")

while menu_rows and not menu_rows[-1]:
    menu_rows.pop()
    menu_row_types.pop()

# Providers sheet
providers_rows: list[list[str]] = []
providers_row_types: list[str] = []
providers_rows.append(["Value Providers"])
providers_row_types.append("title")
providers_rows.append([])
providers_row_types.append("blank")
providers_rows.append(["Provider", "Values"])
providers_row_types.append("header")
for name in sorted(providers.keys()):
    providers_rows.append([name, provider_values_display(name)])
    providers_row_types.append("data")

# Minimal styling
STYLE_DEFAULT = 0
STYLE_TITLE = 1
STYLE_SECTION = 2
STYLE_HEADER = 4

NS_MAIN = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
NS_REL = "http://schemas.openxmlformats.org/officeDocument/2006/relationships"


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
    if any(ch in text for ch in ("%", "x")):
        return False
    try:
        float(text)
        return True
    except ValueError:
        return False


def col_letter(idx: int) -> str:
    letters = []
    while idx:
        idx, rem = divmod(idx - 1, 26)
        letters.append(chr(65 + rem))
    return "".join(reversed(letters))


def compute_col_widths(rows: list[list[str]]) -> list[float]:
    widths: list[int] = []
    for row in rows:
        for idx, value in enumerate(row):
            text = "" if value is None else str(value)
            if idx >= len(widths):
                widths.append(len(text))
            else:
                widths[idx] = max(widths[idx], len(text))
    result: list[float] = []
    for length in widths:
        width = max(8, min(60, length + 2))
        result.append(float(width))
    return result


def build_sheet_xml(rows: list[list[str]], row_types: list[str]):
    root = ET.Element(f"{{{NS_MAIN}}}worksheet")
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
    max_cols = max((len(r) for r in rows), default=0)
    for r_idx, row in enumerate(rows, start=1):
        row_type = row_types[r_idx - 1] if r_idx - 1 < len(row_types) else "data"
        if len(row) < max_cols:
            row = row + [""] * (max_cols - len(row))
        row_el = ET.SubElement(sheet_data, f"{{{NS_MAIN}}}row", {"r": str(r_idx)})
        if row_type == "title":
            style_idx = STYLE_TITLE
        elif row_type == "section":
            style_idx = STYLE_SECTION
        elif row_type == "header":
            style_idx = STYLE_HEADER
        else:
            style_idx = STYLE_DEFAULT
        for c_idx, value in enumerate(row, start=1):
            cell_ref = f"{col_letter(c_idx)}{r_idx}"
            if value is None:
                value = ""
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


def sanitize(name: str) -> str:
    invalid = set(r'[]:*?/\\')
    cleaned = "".join(ch for ch in name if ch not in invalid)
    return cleaned[:31] if len(cleaned) > 31 else cleaned


# Minimal styles borrowed from preset guide script
styles_xml = f"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<styleSheet xmlns="{NS_MAIN}">
  <fonts count="3">
    <font><sz val="11"/><color theme="1"/><name val="Calibri"/><family val="2"/></font>
    <font><b/><sz val="14"/><color theme="1"/><name val="Calibri"/><family val="2"/></font>
    <font><b/><sz val="11"/><color theme="1"/><name val="Calibri"/><family val="2"/></font>
  </fonts>
  <fills count="3">
    <fill><patternFill patternType="none"/></fill>
    <fill><patternFill patternType="gray125"/></fill>
    <fill><patternFill patternType="solid"><fgColor rgb="FFD9E2F3"/><bgColor indexed="64"/></patternFill></fill>
  </fills>
  <borders count="1">
    <border><left/><right/><top/><bottom/><diagonal/></border>
  </borders>
  <cellStyleXfs count="1">
    <xf numFmtId="0" fontId="0" fillId="0" borderId="0"/>
  </cellStyleXfs>
  <cellXfs count="5">
    <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>
    <xf numFmtId="0" fontId="1" fillId="0" borderId="0" xfId="0" applyFont="1"/>
    <xf numFmtId="0" fontId="2" fillId="2" borderId="0" xfId="0" applyFont="1" applyFill="1"/>
    <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>
    <xf numFmtId="0" fontId="2" fillId="2" borderId="0" xfId="0" applyFont="1" applyFill="1"/>
  </cellXfs>
  <cellStyles count="1">
    <cellStyle name="Normal" xfId="0" builtinId="0"/>
  </cellStyles>
</styleSheet>
""".encode("utf-8")


sheets = [
    ("Menu Mock", menu_rows, menu_row_types),
    ("Providers", providers_rows, providers_row_types),
]

# workbook.xml
workbook = ET.Element(f"{{{NS_MAIN}}}workbook", {f"{{{NS_REL}}}r": NS_REL})
sheets_el = ET.SubElement(workbook, f"{{{NS_MAIN}}}sheets")

sheet_names = []
name_counts = {}
for name, _, _ in sheets:
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
    f"  <Override PartName=\"/xl/worksheets/sheet{i}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>"
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
for _, rows, row_types in sheets:
    sheet_xmls.append(build_sheet_xml(rows, row_types))

with zipfile.ZipFile(OUTPUT, "w", compression=zipfile.ZIP_DEFLATED) as zf:
    zf.writestr("[Content_Types].xml", content_types)
    zf.writestr("_rels/.rels", root_rels_xml)
    zf.writestr("xl/workbook.xml", workbook_xml)
    zf.writestr("xl/_rels/workbook.xml.rels", workbook_rels_xml)
    zf.writestr("xl/styles.xml", styles_xml)
    for idx, xml_bytes in enumerate(sheet_xmls, start=1):
        zf.writestr(f"xl/worksheets/sheet{idx}.xml", xml_bytes)

print(f"Wrote {OUTPUT}")
