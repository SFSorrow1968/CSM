from __future__ import annotations
import zipfile
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(r"D:\Documents\Projects\repos\CSM")
OUTPUT = ROOT / "Presets.xlsx"

# Intensity Preset - per-trigger time scale values
intensity_data = [
    ["INTENSITY PRESET - Time Scale Values"],
    [],
    ["Trigger", "Subtle", "Standard", "Dramatic", "Cinematic", "Epic"],
    ["Parry", "46%", "34%", "29%", "24%", "19%"],
    ["Dismemberment", "42%", "30%", "25%", "20%", "15%"],
    ["Basic Kill", "40%", "28%", "23%", "18%", "13%"],
    ["Last Enemy", "38%", "26%", "21%", "16%", "11%"],
    ["Critical", "37%", "25%", "20%", "15%", "10%"],
    ["Decapitation", "35%", "23%", "18%", "13%", "8%"],
    ["Last Stand", "33%", "21%", "16%", "11%", "8%"],
]

# Duration base values and multipliers
duration_multipliers = {"Very Short": 0.5, "Short": 0.7, "Standard": 1.0, "Long": 1.3, "Extended": 1.5}
duration_base = {
    "Parry": 1.5,
    "Dismemberment": 2.0,
    "Basic Kill": 2.5,
    "Last Enemy": 2.75,
    "Critical": 3.0,
    "Decapitation": 3.25,
    "Last Stand": 3.5,
}

duration_data = [
    ["DURATION PRESET - Complete Values"],
    [],
    ["Multipliers: Very Short 0.5x, Short 0.7x, Standard 1.0x, Long 1.3x, Extended 1.5x"],
    [],
    ["Trigger", "Base", "Very Short", "Short", "Standard", "Long", "Extended"],
]
for trigger, base in duration_base.items():
    row = [trigger, f"{base}s"]
    for preset, mult in duration_multipliers.items():
        val = base * mult
        row.append(f"{val:.2f}s")
    duration_data.append(row)

# Cooldown base values and multipliers
cooldown_multipliers = {"Off": 0, "Short": 0.6, "Standard": 1.0, "Long": 2.0, "Extended": 3.0}
cooldown_base = {
    "Parry": 5,
    "Basic Kill": 10,
    "Dismemberment": 10,
    "Critical": 10,
    "Decapitation": 10,
    "Last Enemy": 20,
    "Last Stand": 60,
}

cooldown_data = [
    ["COOLDOWN PRESET - Complete Values"],
    [],
    ["Multipliers: Off 0x, Short 0.6x, Standard 1.0x, Long 2.0x, Extended 3.0x"],
    [],
    ["Trigger", "Base", "Off", "Short", "Standard", "Long", "Extended"],
]
for trigger, base in cooldown_base.items():
    row = [trigger, f"{base}s"]
    for preset, mult in cooldown_multipliers.items():
        val = base * mult
        row.append(f"{val:.0f}s")
    cooldown_data.append(row)

# Chance base values and multipliers
chance_multipliers = {"Off": None, "Very Rare": 0.5, "Rare": 0.6, "Standard": 1.0, "Frequent": 1.4}
chance_base = {
    "Basic Kill": 0.25,
    "Parry": 0.50,
    "Dismemberment": 0.60,
    "Critical": 0.75,
    "Decapitation": 0.90,
    "Last Enemy": 1.00,
    "Last Stand": 1.00,
}

chance_data = [
    ["CHANCE PRESET - Complete Values"],
    [],
    ["Multipliers: Off = 100%, Very Rare 0.5x, Rare 0.6x, Standard 1.0x, Frequent 1.4x"],
    [],
    ["Trigger", "Base", "Off", "Very Rare", "Rare", "Standard", "Frequent"],
]
for trigger, base in chance_base.items():
    row = [trigger, f"{base*100:.0f}%"]
    for preset, mult in chance_multipliers.items():
        if mult is None:
            row.append("100%")
        else:
            val = min(base * mult, 1.0)
            row.append(f"{val*100:.1f}%")
    chance_data.append(row)

# Fade presets
fade_data = [
    ["FADE PRESETS - Smoothing Percentages"],
    [],
    ["Preset", "Smoothing %", "Description"],
    ["Instant", "0%", "No transition, immediate"],
    ["Default", "10%", "Natural B&S feel"],
    ["Quick Fade", "15%", "Quick transition"],
    ["Medium Fade", "20%", "Moderate transition"],
    ["Long Fade", "30%", "Slow transition"],
    ["Very Long Fade", "40%", "Very gradual transition"],
]

# Build all sheets
all_rows = []
all_rows.extend(intensity_data)
all_rows.append([])
all_rows.append([])
all_rows.extend(duration_data)
all_rows.append([])
all_rows.append([])
all_rows.extend(cooldown_data)
all_rows.append([])
all_rows.append([])
all_rows.extend(chance_data)
all_rows.append([])
all_rows.append([])
all_rows.extend(fade_data)

# Row types for styling
row_types = []
for row in all_rows:
    if not row:
        row_types.append("blank")
    elif len(row) == 1 and row[0].startswith(("INTENSITY", "DURATION", "COOLDOWN", "CHANCE", "FADE", "Multipliers")):
        row_types.append("section")
    elif row[0] == "Trigger" or row[0] == "Preset":
        row_types.append("header")
    else:
        row_types.append("data")

# Excel generation (minimal xlsx)
NS_MAIN = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
NS_REL = "http://schemas.openxmlformats.org/officeDocument/2006/relationships"

STYLE_DEFAULT = 0
STYLE_TITLE = 1
STYLE_SECTION = 2
STYLE_HEADER = 4


def col_letter(idx: int) -> str:
    letters = []
    while idx:
        idx, rem = divmod(idx - 1, 26)
        letters.append(chr(65 + rem))
    return "".join(reversed(letters))


def compute_col_widths(rows):
    widths = []
    for row in rows:
        for idx, value in enumerate(row):
            text = "" if value is None else str(value)
            if idx >= len(widths):
                widths.append(len(text))
            else:
                widths[idx] = max(widths[idx], len(text))
    return [max(10, min(20, w + 2)) for w in widths]


def build_sheet_xml(rows, row_types):
    root = ET.Element(f"{{{NS_MAIN}}}worksheet")
    col_widths = compute_col_widths(rows)
    if col_widths:
        cols = ET.SubElement(root, f"{{{NS_MAIN}}}cols")
        for idx, width in enumerate(col_widths, start=1):
            ET.SubElement(cols, f"{{{NS_MAIN}}}col",
                {"min": str(idx), "max": str(idx), "width": f"{width:.2f}", "customWidth": "1"})

    sheet_data = ET.SubElement(root, f"{{{NS_MAIN}}}sheetData")
    max_cols = max((len(r) for r in rows if r), default=0)

    for r_idx, row in enumerate(rows, start=1):
        row_type = row_types[r_idx - 1] if r_idx - 1 < len(row_types) else "data"
        if not row:
            row = [""] * max_cols
        elif len(row) < max_cols:
            row = row + [""] * (max_cols - len(row))

        row_el = ET.SubElement(sheet_data, f"{{{NS_MAIN}}}row", {"r": str(r_idx)})

        if row_type == "section":
            style_idx = STYLE_SECTION
        elif row_type == "header":
            style_idx = STYLE_HEADER
        else:
            style_idx = STYLE_DEFAULT

        for c_idx, value in enumerate(row, start=1):
            cell_ref = f"{col_letter(c_idx)}{r_idx}"
            if value is None:
                value = ""
            c_el = ET.SubElement(row_el, f"{{{NS_MAIN}}}c",
                {"r": cell_ref, "t": "inlineStr", "s": str(style_idx)})
            is_el = ET.SubElement(c_el, f"{{{NS_MAIN}}}is")
            t_el = ET.SubElement(is_el, f"{{{NS_MAIN}}}t")
            t_el.text = str(value)

    return ET.tostring(root, encoding="utf-8", xml_declaration=True)


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

# Build workbook
workbook = ET.Element(f"{{{NS_MAIN}}}workbook", {f"{{{NS_REL}}}r": NS_REL})
sheets_el = ET.SubElement(workbook, f"{{{NS_MAIN}}}sheets")
ET.SubElement(sheets_el, f"{{{NS_MAIN}}}sheet", {"name": "Presets", "sheetId": "1", f"{{{NS_REL}}}id": "rId1"})
workbook_xml = ET.tostring(workbook, encoding="utf-8", xml_declaration=True)

# Workbook rels
rels_root = ET.Element("Relationships", xmlns="http://schemas.openxmlformats.org/package/2006/relationships")
ET.SubElement(rels_root, "Relationship", {
    "Id": "rId1",
    "Type": "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet",
    "Target": "worksheets/sheet1.xml"
})
ET.SubElement(rels_root, "Relationship", {
    "Id": "rId2",
    "Type": "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles",
    "Target": "styles.xml"
})
workbook_rels_xml = ET.tostring(rels_root, encoding="utf-8", xml_declaration=True)

# Root rels
root_rels = ET.Element("Relationships", xmlns="http://schemas.openxmlformats.org/package/2006/relationships")
ET.SubElement(root_rels, "Relationship", {
    "Id": "rId1",
    "Type": "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument",
    "Target": "xl/workbook.xml"
})
root_rels_xml = ET.tostring(root_rels, encoding="utf-8", xml_declaration=True)

# Content types
content_types = f"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
  <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
</Types>
""".encode("utf-8")

# Build sheet
sheet_xml = build_sheet_xml(all_rows, row_types)

# Write xlsx
with zipfile.ZipFile(OUTPUT, "w", compression=zipfile.ZIP_DEFLATED) as zf:
    zf.writestr("[Content_Types].xml", content_types)
    zf.writestr("_rels/.rels", root_rels_xml)
    zf.writestr("xl/workbook.xml", workbook_xml)
    zf.writestr("xl/_rels/workbook.xml.rels", workbook_rels_xml)
    zf.writestr("xl/styles.xml", styles_xml)
    zf.writestr("xl/worksheets/sheet1.xml", sheet_xml)

print(f"Wrote {OUTPUT}")
