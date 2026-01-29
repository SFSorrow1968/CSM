from __future__ import annotations
import zipfile
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(r"D:\Documents\Projects\repos\CSM")
OUTPUT = ROOT / "Presets.xlsx"

NS_MAIN = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
NS_REL = "http://schemas.openxmlformats.org/officeDocument/2006/relationships"

STYLE_DEFAULT = 0
STYLE_SECTION = 2
STYLE_HEADER = 4


def col_letter(idx: int) -> str:
    """Convert 1-based column index to Excel letter (1=A, 27=AA)"""
    letters = []
    while idx:
        idx, rem = divmod(idx - 1, 26)
        letters.append(chr(65 + rem))
    return "".join(reversed(letters))


def build_sheet_xml(rows, row_types, formulas):
    """Build Excel sheet XML with support for formulas"""
    root = ET.Element(f"{{{NS_MAIN}}}worksheet")

    # Column widths
    cols = ET.SubElement(root, f"{{{NS_MAIN}}}cols")
    col_widths = [15, 8, 10, 10, 10, 10, 10, 10]
    for idx, width in enumerate(col_widths, start=1):
        ET.SubElement(cols, f"{{{NS_MAIN}}}col",
            {"min": str(idx), "max": str(idx), "width": str(width), "customWidth": "1"})

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

            # Check if this cell has a formula
            formula_key = (r_idx, c_idx)
            if formula_key in formulas:
                c_el = ET.SubElement(row_el, f"{{{NS_MAIN}}}c",
                    {"r": cell_ref, "s": str(style_idx)})
                f_el = ET.SubElement(c_el, f"{{{NS_MAIN}}}f")
                f_el.text = formulas[formula_key]
            elif value is None or value == "":
                continue
            elif isinstance(value, (int, float)):
                c_el = ET.SubElement(row_el, f"{{{NS_MAIN}}}c",
                    {"r": cell_ref, "s": str(style_idx)})
                v_el = ET.SubElement(c_el, f"{{{NS_MAIN}}}v")
                v_el.text = str(value)
            else:
                c_el = ET.SubElement(row_el, f"{{{NS_MAIN}}}c",
                    {"r": cell_ref, "t": "inlineStr", "s": str(style_idx)})
                is_el = ET.SubElement(c_el, f"{{{NS_MAIN}}}is")
                t_el = ET.SubElement(is_el, f"{{{NS_MAIN}}}t")
                t_el.text = str(value)

    return ET.tostring(root, encoding="utf-8", xml_declaration=True)


# Build the sheet data
rows = []
row_types = []
formulas = {}

# ============ INTENSITY PRESET ============
rows.append(["INTENSITY PRESET - Time Scale"])
row_types.append("section")
rows.append([])
row_types.append("blank")

# Multiplier row (values directly above preset names)
rows.append(["", "", 1.35, 1.0, 0.85, 0.70, 0.55])
row_types.append("data")
intensity_mult_row = len(rows)

# Header row with preset names
rows.append(["Trigger", "Base", "Subtle", "Standard", "Dramatic", "Cinematic", "Epic"])
row_types.append("header")

# Intensity base values (Standard preset values)
intensity_bases = [
    ("Parry", 0.34),
    ("Dismemberment", 0.30),
    ("Basic Kill", 0.28),
    ("Last Enemy", 0.26),
    ("Critical", 0.25),
    ("Decapitation", 0.23),
    ("Last Stand", 0.21),
]

for trigger, base in intensity_bases:
    current_row = len(rows) + 1
    rows.append([trigger, base, "", "", "", "", ""])
    row_types.append("data")
    # Add formulas for columns C-G (multiply base by multiplier)
    for col_offset in range(5):
        col_idx = col_offset + 3  # C=3, D=4, E=5, F=6, G=7
        mult_col = col_letter(col_idx)
        formulas[(current_row, col_idx)] = f"B{current_row}*{mult_col}${intensity_mult_row}"

rows.append([])
row_types.append("blank")
rows.append([])
row_types.append("blank")

# ============ DURATION PRESET ============
rows.append(["DURATION PRESET"])
row_types.append("section")
rows.append([])
row_types.append("blank")

# Multiplier row (values directly above preset names)
rows.append(["", "", 0.5, 0.7, 1.0, 1.3, 1.5])
row_types.append("data")
duration_mult_row = len(rows)

# Header row with preset names
rows.append(["Trigger", "Base", "Very Short", "Short", "Standard", "Long", "Extended"])
row_types.append("header")

# Duration base values
duration_bases = [
    ("Parry", 1.5),
    ("Dismemberment", 2.0),
    ("Basic Kill", 2.5),
    ("Last Enemy", 2.75),
    ("Critical", 3.0),
    ("Decapitation", 3.25),
    ("Last Stand", 3.5),
]

for trigger, base in duration_bases:
    current_row = len(rows) + 1
    rows.append([trigger, base, "", "", "", "", ""])
    row_types.append("data")
    for col_offset in range(5):
        col_idx = col_offset + 3
        mult_col = col_letter(col_idx)
        formulas[(current_row, col_idx)] = f"B{current_row}*{mult_col}${duration_mult_row}"

rows.append([])
row_types.append("blank")
rows.append([])
row_types.append("blank")

# ============ COOLDOWN PRESET ============
rows.append(["COOLDOWN PRESET"])
row_types.append("section")
rows.append([])
row_types.append("blank")

# Multiplier row (values directly above preset names)
rows.append(["", "", 0, 0.6, 1.0, 2.0, 3.0])
row_types.append("data")
cooldown_mult_row = len(rows)

# Header row with preset names
rows.append(["Trigger", "Base", "Off", "Short", "Standard", "Long", "Extended"])
row_types.append("header")

cooldown_bases = [
    ("Parry", 5),
    ("Basic Kill", 10),
    ("Dismemberment", 10),
    ("Critical", 10),
    ("Decapitation", 10),
    ("Last Enemy", 20),
    ("Last Stand", 60),
]

for trigger, base in cooldown_bases:
    current_row = len(rows) + 1
    rows.append([trigger, base, "", "", "", "", ""])
    row_types.append("data")
    for col_offset in range(5):
        col_idx = col_offset + 3
        mult_col = col_letter(col_idx)
        formulas[(current_row, col_idx)] = f"B{current_row}*{mult_col}${cooldown_mult_row}"

rows.append([])
row_types.append("blank")
rows.append([])
row_types.append("blank")

# ============ CHANCE PRESET ============
rows.append(["CHANCE PRESET (Off = always 100%)"])
row_types.append("section")
rows.append([])
row_types.append("blank")

# Multiplier row (values directly above preset names)
rows.append(["", "", 1.0, 0.5, 0.6, 1.0, 1.4])  # Off=1.0 means 100%
row_types.append("data")
chance_mult_row = len(rows)

# Header row with preset names
rows.append(["Trigger", "Base", "Off", "Very Rare", "Rare", "Standard", "Frequent"])
row_types.append("header")

chance_bases = [
    ("Basic Kill", 0.25),
    ("Parry", 0.50),
    ("Dismemberment", 0.60),
    ("Critical", 0.75),
    ("Decapitation", 0.90),
    ("Last Enemy", 1.00),
    ("Last Stand", 1.00),
]

for trigger, base in chance_bases:
    current_row = len(rows) + 1
    rows.append([trigger, base, "", "", "", "", ""])
    row_types.append("data")
    # Off column (C) is special - always 1.0 (100%)
    formulas[(current_row, 3)] = f"1"  # Off = 100%
    # Other columns use formula with MIN to cap at 1.0
    for col_offset in range(1, 5):
        col_idx = col_offset + 3  # D=4, E=5, F=6, G=7
        mult_col = col_letter(col_idx)
        formulas[(current_row, col_idx)] = f"MIN(B{current_row}*{mult_col}${chance_mult_row},1)"

rows.append([])
row_types.append("blank")
rows.append([])
row_types.append("blank")

# ============ FADE PRESETS ============
rows.append(["FADE PRESETS - Smoothing Percentages"])
row_types.append("section")
rows.append([])
row_types.append("blank")
rows.append(["Preset", "Smoothing %", "Description"])
row_types.append("header")

fade_data = [
    ["Instant", 0.00, "No transition, immediate"],
    ["Default", 0.10, "Natural B&S feel"],
    ["Quick Fade", 0.15, "Quick transition"],
    ["Medium Fade", 0.20, "Moderate transition"],
    ["Long Fade", 0.30, "Slow transition"],
    ["Very Long Fade", 0.40, "Very gradual transition"],
]
for row in fade_data:
    rows.append(row)
    row_types.append("data")


# ============ BUILD XLSX ============
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

workbook = ET.Element(f"{{{NS_MAIN}}}workbook", {f"{{{NS_REL}}}r": NS_REL})
sheets_el = ET.SubElement(workbook, f"{{{NS_MAIN}}}sheets")
ET.SubElement(sheets_el, f"{{{NS_MAIN}}}sheet", {"name": "Presets", "sheetId": "1", f"{{{NS_REL}}}id": "rId1"})
workbook_xml = ET.tostring(workbook, encoding="utf-8", xml_declaration=True)

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

root_rels = ET.Element("Relationships", xmlns="http://schemas.openxmlformats.org/package/2006/relationships")
ET.SubElement(root_rels, "Relationship", {
    "Id": "rId1",
    "Type": "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument",
    "Target": "xl/workbook.xml"
})
root_rels_xml = ET.tostring(root_rels, encoding="utf-8", xml_declaration=True)

content_types = f"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
  <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
</Types>
""".encode("utf-8")

sheet_xml = build_sheet_xml(rows, row_types, formulas)

with zipfile.ZipFile(OUTPUT, "w", compression=zipfile.ZIP_DEFLATED) as zf:
    zf.writestr("[Content_Types].xml", content_types)
    zf.writestr("_rels/.rels", root_rels_xml)
    zf.writestr("xl/workbook.xml", workbook_xml)
    zf.writestr("xl/_rels/workbook.xml.rels", workbook_rels_xml)
    zf.writestr("xl/styles.xml", styles_xml)
    zf.writestr("xl/worksheets/sheet1.xml", sheet_xml)

print(f"Wrote {OUTPUT}")
print("Edit the multiplier rows to see automatic calculations!")
