from __future__ import annotations
import re
import zipfile
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(r"C:\Users\dkatz\Documents\Projects\CSM")
CSM_MANAGER = ROOT / "Core" / "CSMManager.cs"
CSM_OPTIONS = ROOT / "Configuration" / "CSMModOptions.cs"
TRIGGER_TYPE = ROOT / "Configuration" / "TriggerType.cs"
OUTPUT = ROOT / "PRESET_GUIDE_ORGANIZED.xlsx"

manager_text = CSM_MANAGER.read_text(encoding="utf-8")
options_text = CSM_OPTIONS.read_text(encoding="utf-8")
trigger_text = TRIGGER_TYPE.read_text(encoding="utf-8")


def extract_method_block(text: str, signature_regex: str) -> str:
    m = re.search(signature_regex, text)
    if not m:
        raise ValueError(f"Method signature not found: {signature_regex}")
    idx = m.end()
    brace_start = text.find('{', idx)
    if brace_start == -1:
        raise ValueError(f"No opening brace for method: {signature_regex}")
    depth = 0
    for i in range(brace_start, len(text)):
        ch = text[i]
        if ch == '{':
            depth += 1
        elif ch == '}':
            depth -= 1
            if depth == 0:
                return text[brace_start + 1:i]
    raise ValueError(f"No matching closing brace for method: {signature_regex}")


def parse_enum(text: str, enum_name: str) -> list[str]:
    pattern = re.compile(rf"public enum {re.escape(enum_name)}\s*\{{(.*?)\}}", re.S)
    match = pattern.search(text)
    if not match:
        return []
    body = match.group(1)
    names = []
    for line in body.splitlines():
        line = line.strip()
        if not line or line.startswith("//"):
            continue
        line = line.split('//', 1)[0].strip()
        if not line:
            continue
        token = line.rstrip(',')
        token = token.split('=')[0].strip()
        if token:
            names.append(token)
    return names


def parse_modoption_strings(text: str, method_name: str) -> dict[str, str]:
    block = extract_method_block(text, rf"public\s+static\s+ModOptionString\[\]\s+{re.escape(method_name)}\s*\(")
    mapping: dict[str, str] = {}
    for m in re.finditer(r'new\s+ModOptionString\("([^"]+)",\s*"([^"]+)"\)', block):
        mapping[m.group(2)] = m.group(1)
    return mapping


def pick_display(mapping: dict[str, str], fallback: str, *keys: str) -> str:
    for key in keys:
        value = mapping.get(key)
        if value:
            return value
    return fallback


def display_trigger(name: str) -> str:
    mapping = {
        "BasicKill": "Basic Kill",
        "LastEnemy": "Last Enemy",
        "LastStand": "Last Stand",
    }
    return mapping.get(name, name)


def format_percent(value: float) -> str:
    pct = value * 100.0
    if abs(pct - round(pct)) < 1e-6:
        return f"{int(round(pct))}%"
    return f"{pct:.1f}%"


def format_timescale(value: float) -> str:
    return f"{value:.2f}".rstrip('0').rstrip('.') + "x"


def format_number(value: float) -> str:
    if abs(value - round(value)) < 1e-6:
        return str(int(round(value)))
    return f"{value:.2f}".rstrip('0').rstrip('.')


def clamp_percent(value: float) -> tuple[str, str]:
    raw_pct = value * 100.0
    if raw_pct <= 100.0 + 1e-6:
        return format_percent(value), ""
    clamped = "100%"
    raw = f"{raw_pct:.1f}%" if abs(raw_pct - round(raw_pct)) > 1e-6 else f"{int(round(raw_pct))}%"
    return clamped, f"clamped from {raw}"


# Trigger order
trigger_enum = parse_enum(trigger_text, "TriggerType")

# Parse GetPresetValues
preset_block = extract_method_block(manager_text, r"public\s+static\s+void\s+GetPresetValues\s*\(")
trigger_case_pattern = re.compile(r"case\s+TriggerType\.(\w+)\s*:")
trigger_matches = list(trigger_case_pattern.finditer(preset_block))

intensity_values: dict[str, dict[str, dict[str, object]]] = {}
trigger_order: list[str] = []

for idx, match in enumerate(trigger_matches):
    trigger = match.group(1)
    start = match.end()
    end = trigger_matches[idx + 1].start() if idx + 1 < len(trigger_matches) else len(preset_block)
    block = preset_block[start:end]

    trigger_order.append(trigger)
    intensity_values[trigger] = {}

    defaults: dict[str, object] = {}
    pre_switch = block.split("switch (preset)", 1)[0]
    for var in ["chance", "timeScale", "duration", "cooldown", "smoothing"]:
        m_value = re.search(rf"{var}\s*=\s*([0-9.]+)f;", pre_switch)
        if m_value:
            defaults[var] = float(m_value.group(1))
    m_third = re.search(r"thirdPerson\s*=\s*(true|false);", pre_switch)
    if m_third:
        defaults["thirdPerson"] = m_third.group(1) == "true"

    preset_case_pattern = re.compile(r"case\s+CSMModOptions\.Preset\.(\w+)\s*:(.*?)(?=case\s+CSMModOptions\.Preset|\Z)", re.S)
    for preset_match in preset_case_pattern.finditer(block):
        preset = preset_match.group(1)
        case_block = preset_match.group(2)
        values: dict[str, object] = {}
        for var in ["chance", "timeScale", "duration", "cooldown", "smoothing"]:
            m = re.search(rf"{var}\s*=\s*([0-9.]+)f;", case_block)
            if m:
                values[var] = float(m.group(1))
        m_third = re.search(r"thirdPerson\s*=\s*(true|false);", case_block)
        if m_third:
            values["thirdPerson"] = m_third.group(1) == "true"
        for key, default_value in defaults.items():
            if key not in values:
                values[key] = default_value
        intensity_values[trigger][preset] = values

if not trigger_order:
    trigger_order = trigger_enum

third_person_allowed = {t for t in trigger_order if t not in ("Parry", "LastStand")}

# Preset order
preset_order_all = parse_enum(options_text, "Preset")
preset_order = [p for p in preset_order_all if p != "Custom"]
if not preset_order:
    for trigger in trigger_order:
        if trigger in intensity_values:
            preset_order = list(intensity_values[trigger].keys())
            break

# Killcam base chance
base_killcam_block = extract_method_block(options_text, r"private\s+static\s+float\s+GetKillcamBaseChance\s*\(")
base_killcam: dict[str, float] = {}
for m in re.finditer(r"case\s+TriggerType\.(\w+)\s*:\s*return\s*([0-9.]+)f;", base_killcam_block):
    base_killcam[m.group(1)] = float(m.group(2))

# Camera distribution multipliers
camera_dist_block = extract_method_block(options_text, r"public\s+static\s+float\s+GetCameraDistributionMultiplier\s*\(")
camera_distribution: dict[str, float] = {}
for m in re.finditer(r"case\s+CameraDistributionPreset\.(\w+)\s*:\s*return\s*([0-9.]+)f;", camera_dist_block):
    camera_distribution[m.group(1)] = float(m.group(2))

camera_distribution_display = parse_modoption_strings(options_text, "CameraDistributionProvider")
camera_distribution_labels = {
    "FirstPersonOnly": pick_display(camera_distribution_display, "First Person Only", "First Person Only"),
    "MostlyFirstPerson": pick_display(
        camera_distribution_display,
        "Mostly First Person",
        "Mixed (Rare Third Person)",
        "Mostly First Person",
        "Rare",
    ),
    "Mixed": pick_display(camera_distribution_display, "Mixed", "Mixed", "Balanced"),
    "MostlyThirdPerson": pick_display(camera_distribution_display, "Mostly Third Person", "Mostly Third Person", "Frequent"),
    "ThirdPersonOnly": pick_display(camera_distribution_display, "Third Person Only", "Third Person Only", "Always"),
}

# Chance presets
chance_block = extract_method_block(options_text, r"public\s+static\s+void\s+ApplyChancePreset\s*\(")
chance_presets: dict[str, float] = {}
for case in re.finditer(r"case\s+ChancePreset\.(\w+)\s*:(.*?)(?=case\s+ChancePreset|default|\Z)", chance_block, re.S):
    name = case.group(1)
    body = case.group(2)
    m_chance = re.search(r"chanceMultiplier\s*=\s*([0-9.]+)f;", body)
    if m_chance:
        chance_presets[name] = float(m_chance.group(1))

# Cooldown presets
cooldown_block = extract_method_block(options_text, r"public\s+static\s+void\s+ApplyCooldownPreset\s*\(")
cooldown_presets: dict[str, float] = {}
for case in re.finditer(r"case\s+CooldownPreset\.(\w+)\s*:(.*?)(?=case\s+CooldownPreset|default|\Z)", cooldown_block, re.S):
    name = case.group(1)
    body = case.group(2)
    m_cooldown = re.search(r"cooldownMultiplier\s*=\s*([0-9.]+)f;", body)
    if m_cooldown:
        cooldown_presets[name] = float(m_cooldown.group(1))

# Duration presets
duration_block = extract_method_block(options_text, r"public\s+static\s+void\s+ApplyDurationPreset\s*\(")
duration_presets: dict[str, float] = {}
for case in re.finditer(r"case\s+DurationPreset\.(\w+)\s*:(.*?)(?=case\s+DurationPreset|default|\Z)", duration_block, re.S):
    name = case.group(1)
    body = case.group(2)
    m_mult = re.search(r"durationMultiplier\s*=\s*([0-9.]+)f;", body)
    if m_mult:
        duration_presets[name] = float(m_mult.group(1))

# Smoothness presets
smoothness_block = extract_method_block(options_text, r"public\s+static\s+void\s+ApplySmoothnessPreset\s*\(")
smoothness_presets: dict[str, float] = {}
for case in re.finditer(r"case\s+SmoothnessPreset\.(\w+)\s*:(.*?)(?=case\s+SmoothnessPreset|default|\Z)", smoothness_block, re.S):
    name = case.group(1)
    body = case.group(2)
    m_mult = re.search(r"smoothingMultiplier\s*=\s*([0-9.]+)f;", body)
    if m_mult:
        smoothness_presets[name] = float(m_mult.group(1))

# Trigger profile mapping
profile_triggers: dict[str, list[str]] = {
    "All": list(trigger_order),
    "KillsOnly": ["BasicKill", "Critical", "Dismemberment", "Decapitation", "LastEnemy"],
    "Highlights": ["Critical", "Decapitation", "LastEnemy"],
    "LastEnemyOnly": ["LastEnemy"],
}
for key, triggers in list(profile_triggers.items()):
    profile_triggers[key] = [t for t in triggers if t in trigger_order]

# Enum orders
chance_order = parse_enum(options_text, "ChancePreset")
cooldown_order = parse_enum(options_text, "CooldownPreset")
duration_order = parse_enum(options_text, "DurationPreset")
smoothness_order = parse_enum(options_text, "SmoothnessPreset")
profile_order = parse_enum(options_text, "TriggerProfilePreset")

base_balanced = {t: intensity_values.get(t, {}).get("Balanced", {}) for t in trigger_order}


def compute_chance_value(base_chance: float, preset_name: str) -> str:
    if preset_name in ("Off", "Always"):
        return "100%"
    mult = chance_presets.get(preset_name, 1.0)
    return format_percent(min(1.0, base_chance * mult))


def compute_cooldown_value(base_cooldown: float, preset_name: str) -> str:
    if preset_name == "Off":
        return "0"
    mult = cooldown_presets.get(preset_name, 1.0)
    return format_number(max(0.0, base_cooldown * mult))


# Sheet building helpers
STYLE_DEFAULT = 0
STYLE_TITLE = 1
STYLE_SECTION = 2
STYLE_PRESET = 3
STYLE_HEADER = 4
STYLE_NOTE = 5
STYLE_SECTION_INTENSITY = 6
STYLE_SECTION_FREQUENCY = 7
STYLE_SECTION_DURATION = 8
STYLE_SECTION_SMOOTHNESS = 9
STYLE_SECTION_CAMERA = 10
STYLE_SECTION_KILLCAM = 11
STYLE_SECTION_PROFILE = 12


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
    return ''.join(reversed(letters))


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
        width = max(8, min(45, length + 2))
        result.append(float(width))
    return result


def build_sheet_xml(rows: list[list[str]], row_types: list[str]):
    root = ET.Element(f"{{{NS_MAIN}}}worksheet")

    col_widths = compute_col_widths(rows)
    if col_widths:
        cols = ET.SubElement(root, f"{{{NS_MAIN}}}cols")
        for idx, width in enumerate(col_widths, start=1):
            ET.SubElement(cols, f"{{{NS_MAIN}}}col", {
                "min": str(idx),
                "max": str(idx),
                "width": f"{width:.2f}",
                "customWidth": "1",
            })

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
        elif row_type == "section_intensity":
            style_idx = STYLE_SECTION_INTENSITY
        elif row_type == "section_frequency":
            style_idx = STYLE_SECTION_FREQUENCY
        elif row_type == "section_duration":
            style_idx = STYLE_SECTION_DURATION
        elif row_type == "section_smoothness":
            style_idx = STYLE_SECTION_SMOOTHNESS
        elif row_type == "section_camera":
            style_idx = STYLE_SECTION_CAMERA
        elif row_type == "section_killcam":
            style_idx = STYLE_SECTION_KILLCAM
        elif row_type == "section_profile":
            style_idx = STYLE_SECTION_PROFILE
        elif row_type == "preset":
            style_idx = STYLE_PRESET
        elif row_type == "header":
            style_idx = STYLE_HEADER
        elif row_type == "note":
            style_idx = STYLE_NOTE
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
                c_el = ET.SubElement(row_el, f"{{{NS_MAIN}}}c", {"r": cell_ref, "t": "inlineStr", "s": str(style_idx)})
                is_el = ET.SubElement(c_el, f"{{{NS_MAIN}}}is")
                t_el = ET.SubElement(is_el, f"{{{NS_MAIN}}}t")
                t_el.text = str(value)
    return ET.tostring(root, encoding="utf-8", xml_declaration=True)


def section_row_type(header: str) -> str:
    if header.startswith("Intensity"):
        return "section_intensity"
    if header.startswith("Frequency") or header.startswith("Chance Preset") or header.startswith("Cooldown Preset"):
        return "section_frequency"
    if header.startswith("Duration"):
        return "section_duration"
    if header.startswith("Smoothness"):
        return "section_smoothness"
    if header.startswith("Camera Distribution") or header.startswith("Third Person Distribution"):
        return "section_camera"
    if "Killcam" in header:
        return "section_killcam"
    if header.startswith("Profile") or header.startswith("Trigger Profiles"):
        return "section_profile"
    return "section"


def make_sheet(title: str, notes: list[str], blocks: list[tuple[str, list[list[str]]]]) -> tuple[str, list[list[str]], list[str]]:
    rows: list[list[str]] = []
    row_types: list[str] = []
    rows.append([title])
    row_types.append("title")
    for note in notes:
        rows.append(["Note", note])
        row_types.append("note")
    rows.append([])
    row_types.append("blank")
    for header, table_rows in blocks:
        rows.append([header])
        row_types.append(section_row_type(header))
        if table_rows:
            rows.append(table_rows[0])
            row_types.append("header")
            for row in table_rows[1:]:
                rows.append(row)
                row_types.append("data")
        rows.append([])
        row_types.append("blank")
    while rows and not rows[-1]:
        rows.pop()
        row_types.pop()
    return title, rows, row_types


chance_preset_note = "Chance Preset Off disables chance rolls (cooldown only). Always forces 100%."
cooldown_preset_note = "Cooldown Preset Off disables per-trigger cooldowns."

# Overview sheet
overview_blocks: list[tuple[str, list[list[str]]]] = []

# Third Person Distribution blocks
for preset, mult in camera_distribution.items():
    label = camera_distribution_labels.get(preset, preset)
    table = [["Trigger", "Killcam Chance", "Eligible"]]
    for trigger in trigger_order:
        base_chance = base_killcam.get(trigger, 0.0)
        allow = trigger in third_person_allowed and mult > 0.0
        eligible = "Yes" if allow else "No"
        chance, _ = clamp_percent(base_chance * mult if allow else 0.0)
        table.append([display_trigger(trigger), chance, eligible])
    overview_blocks.append((f"Third Person Distribution: {label} (Killcam x{format_number(mult)})", table))

# Intensity blocks (TimeScale only)
for preset in preset_order:
    table = [["Trigger", "TimeScale"]]
    for trigger in trigger_order:
        values = intensity_values.get(trigger, {}).get(preset, {})
        table.append([
            display_trigger(trigger),
            format_timescale(float(values.get("timeScale", 0.0))),
        ])
    overview_blocks.append((f"Intensity Preset: {preset}", table))

# Chance preset blocks
for preset in chance_order:
    if preset == "Off":
        header = "Chance Preset: Off (Cooldown Only)"
    elif preset == "Always":
        header = "Chance Preset: Always (100%)"
    else:
        mult = chance_presets.get(preset, 1.0)
        header = f"Chance Preset: {preset} (Chance x{format_number(mult)})"
    table = [["Trigger", "Chance"]]
    for trigger in trigger_order:
        base = base_balanced.get(trigger, {})
        base_chance = float(base.get("chance", 0.0))
        table.append([display_trigger(trigger), compute_chance_value(base_chance, preset)])
    overview_blocks.append((header, table))

# Cooldown preset blocks
for preset in cooldown_order:
    table = [["Trigger", "Cooldown (s)"]]
    for trigger in trigger_order:
        base = base_balanced.get(trigger, {})
        base_cooldown = float(base.get("cooldown", 0.0))
        table.append([display_trigger(trigger), compute_cooldown_value(base_cooldown, preset)])
    if preset == "Off":
        header = "Cooldown Preset: Off (Disabled)"
    else:
        mult = cooldown_presets.get(preset, 1.0)
        header = f"Cooldown Preset: {preset} (Cooldown x{format_number(mult)})"
    overview_blocks.append((header, table))

# Duration blocks
for preset in duration_order:
    if preset not in duration_presets:
        continue
    mult = duration_presets[preset]
    table = [["Trigger", "Duration (s)"]]
    for trigger in trigger_order:
        base = base_balanced.get(trigger, {})
        duration = max(0.05, float(base.get("duration", 0.0)) * mult)
        table.append([display_trigger(trigger), format_number(duration)])
    overview_blocks.append((f"Duration Preset: {preset} (Duration x{format_number(mult)})", table))

# Smoothness blocks
for preset in smoothness_order:
    if preset not in smoothness_presets:
        continue
    mult = smoothness_presets[preset]
    table = [["Trigger", "Smoothing"]]
    for trigger in trigger_order:
        base = base_balanced.get(trigger, {})
        smoothing = max(0.0, float(base.get("smoothing", 0.0)) * mult)
        table.append([display_trigger(trigger), format_number(smoothing)])
    overview_blocks.append((f"Smoothness Preset: {preset} (Smoothing x{format_number(mult)})", table))

overview_sheet = make_sheet(
    "Overview (Preset-First)",
    [
        "Intensity tab shows only TimeScale.",
        "Chance/Cooldown/Duration/Smoothness tables are derived from Intensity = Balanced.",
        chance_preset_note,
        cooldown_preset_note,
        "Killcam chance = Base Chance x Third Person Distribution.",
        "Killcam tables assume Camera Mode = Default.",
    ],
    overview_blocks,
)

# Trigger Profile sheet
profile_blocks: list[tuple[str, list[list[str]]]] = []
for profile in profile_order:
    table = [["Trigger", "Enabled"]]
    for trigger in trigger_order:
        if profile == "All":
            enabled = "Yes"
        else:
            enabled = "Yes" if trigger in profile_triggers.get(profile, []) else "No"
        table.append([display_trigger(trigger), enabled])
    profile_blocks.append((f"Profile: {profile}", table))

profile_sheet = make_sheet(
    "Trigger Profiles",
    ["Selecting a profile updates the per-trigger toggles."],
    profile_blocks,
)

# Trigger-specific sheets
trigger_sheets: list[tuple[str, list[list[str]], list[str]]] = []
for trigger in trigger_order:
    trigger_name = display_trigger(trigger)
    blocks: list[tuple[str, list[list[str]]]] = []

    # Intensity table (TimeScale only)
    table = [["Preset", "TimeScale"]]
    for preset in preset_order:
        values = intensity_values.get(trigger, {}).get(preset, {})
        table.append([
            preset,
            format_timescale(float(values.get("timeScale", 0.0))),
        ])
    blocks.append(("Intensity Presets", table))

    # Chance preset table
    table = [["Preset", "Chance"]]
    base = base_balanced.get(trigger, {})
    for preset in chance_order:
        base_chance = float(base.get("chance", 0.0))
        table.append([preset, compute_chance_value(base_chance, preset)])
    blocks.append(("Chance Presets (from Intensity Balanced)", table))

    # Cooldown preset table
    table = [["Preset", "Cooldown (s)"]]
    for preset in cooldown_order:
        base_cooldown = float(base.get("cooldown", 0.0))
        table.append([preset, compute_cooldown_value(base_cooldown, preset)])
    blocks.append(("Cooldown Presets (from Intensity Balanced)", table))

    # Duration table
    table = [["Preset", "Duration (s)"]]
    for preset in duration_order:
        if preset not in duration_presets:
            continue
        mult = duration_presets[preset]
        duration = max(0.05, float(base.get("duration", 0.0)) * mult)
        table.append([preset, format_number(duration)])
    blocks.append(("Duration Presets (from Intensity Balanced)", table))

    # Smoothness table
    table = [["Preset", "Smoothing"]]
    for preset in smoothness_order:
        if preset not in smoothness_presets:
            continue
        mult = smoothness_presets[preset]
        smoothing = max(0.0, float(base.get("smoothing", 0.0)) * mult)
        table.append([preset, format_number(smoothing)])
    blocks.append(("Smoothness Presets (from Intensity Balanced)", table))

    # Trigger profiles table
    table = [["Profile", "Enabled"]]
    for profile in profile_order:
        if profile == "All":
            enabled = "Yes"
        else:
            enabled = "Yes" if trigger in profile_triggers.get(profile, []) else "No"
        table.append([profile, enabled])
    blocks.append(("Trigger Profiles", table))

    # Killcam tables
    base_chance = base_killcam.get(trigger, 0.0)
    table = [["Base Killcam Chance", format_percent(base_chance)]]
    blocks.append(("Killcam Base Chance", table))

    table = [["Third Person Distribution", "Killcam Chance", "Eligible"]]
    for preset, mult in camera_distribution.items():
        chance, _ = clamp_percent(base_chance * mult)
        eligible = "Yes" if mult > 0.0 else "No"
        label = camera_distribution_labels.get(preset, preset)
        table.append([label, chance, eligible])
    blocks.append(("Killcam by Third Person Distribution", table))

    trigger_sheets.append(make_sheet(
        f"{trigger_name}",
        [
            "Intensity table shows only TimeScale.",
            "Chance/Cooldown/Duration/Smoothness derived from Intensity = Balanced.",
            chance_preset_note,
            "Killcam chance = Base Chance x Third Person Distribution.",
            "Killcam only triggers if eligible unless camera mode forces third person.",
        ],
        blocks,
    ))

# Collect sheets
sheets: list[tuple[str, list[list[str]], list[str]]] = [overview_sheet, profile_sheet] + trigger_sheets


# Workbook writer
NS_MAIN = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
NS_REL = "http://schemas.openxmlformats.org/officeDocument/2006/relationships"
ET.register_namespace('', NS_MAIN)
ET.register_namespace('r', NS_REL)

invalid = set('[]:*?/\\')

def sanitize(name: str) -> str:
    cleaned = ''.join('_' if c in invalid else c for c in name).strip()
    if not cleaned:
        cleaned = "Sheet"
    return cleaned[:31]

# styles.xml
styles_xml = f"""<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>
<styleSheet xmlns=\"{NS_MAIN}\">
  <fonts count=\"5\">
    <font><sz val=\"11\"/><color theme=\"1\"/><name val=\"Calibri\"/><family val=\"2\"/></font>
    <font><b/><sz val=\"15\"/><color theme=\"1\"/><name val=\"Calibri\"/><family val=\"2\"/></font>
    <font><b/><sz val=\"11\"/><color theme=\"1\"/><name val=\"Calibri\"/><family val=\"2\"/></font>
    <font><b/><sz val=\"11\"/><color theme=\"1\"/><name val=\"Calibri\"/><family val=\"2\"/></font>
    <font><i/><sz val=\"11\"/><color theme=\"1\"/><name val=\"Calibri\"/><family val=\"2\"/></font>
  </fonts>
  <fills count=\"10\">
    <fill><patternFill patternType=\"none\"/></fill>
    <fill><patternFill patternType=\"gray125\"/></fill>
    <fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFD9D9D9\"/><bgColor indexed=\"64\"/></patternFill></fill>
    <fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFDCE6F1\"/><bgColor indexed=\"64\"/></patternFill></fill>
    <fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFFFF2CC\"/><bgColor indexed=\"64\"/></patternFill></fill>
    <fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFF2F2F2\"/><bgColor indexed=\"64\"/></patternFill></fill>
    <fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFE2F0D9\"/><bgColor indexed=\"64\"/></patternFill></fill>
    <fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFFCE4D6\"/><bgColor indexed=\"64\"/></patternFill></fill>
    <fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFCFE2F3\"/><bgColor indexed=\"64\"/></patternFill></fill>
    <fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFF4CCCC\"/><bgColor indexed=\"64\"/></patternFill></fill>
  </fills>
  <borders count=\"1\">
    <border><left/><right/><top/><bottom/><diagonal/></border>
  </borders>
  <cellStyleXfs count=\"1\">
    <xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/>
  </cellStyleXfs>
  <cellXfs count=\"13\">
    <xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>
    <xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/>
    <xf numFmtId=\"0\" fontId=\"2\" fillId=\"3\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\"/>
    <xf numFmtId=\"0\" fontId=\"2\" fillId=\"4\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\"/>
    <xf numFmtId=\"0\" fontId=\"2\" fillId=\"2\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\"/>
    <xf numFmtId=\"0\" fontId=\"4\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyAlignment=\"1\"><alignment wrapText=\"1\"/></xf>
    <xf numFmtId=\"0\" fontId=\"2\" fillId=\"3\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\"/>
    <xf numFmtId=\"0\" fontId=\"2\" fillId=\"6\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\"/>
    <xf numFmtId=\"0\" fontId=\"2\" fillId=\"4\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\"/>
    <xf numFmtId=\"0\" fontId=\"2\" fillId=\"7\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\"/>
    <xf numFmtId=\"0\" fontId=\"2\" fillId=\"8\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\"/>
    <xf numFmtId=\"0\" fontId=\"2\" fillId=\"9\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\"/>
    <xf numFmtId=\"0\" fontId=\"2\" fillId=\"5\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\"/>
  </cellXfs>
  <cellStyles count=\"1\">
    <cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/>
  </cellStyles>
</styleSheet>
""".encode("utf-8")

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
        trimmed = base[:31 - len(suffix)]
        sheet_name = trimmed + suffix
    else:
        sheet_name = base
    sheet_names.append(sheet_name)

for idx, sheet_name in enumerate(sheet_names, start=1):
    ET.SubElement(sheets_el, f"{{{NS_MAIN}}}sheet", {
        "name": sheet_name,
        "sheetId": str(idx),
        f"{{{NS_REL}}}id": f"rId{idx}",
    })
workbook_xml = ET.tostring(workbook, encoding="utf-8", xml_declaration=True)

# workbook rels
rels_root = ET.Element("Relationships", xmlns="http://schemas.openxmlformats.org/package/2006/relationships")
for idx in range(1, len(sheets) + 1):
    ET.SubElement(rels_root, "Relationship", {
        "Id": f"rId{idx}",
        "Type": "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet",
        "Target": f"worksheets/sheet{idx}.xml",
    })
ET.SubElement(rels_root, "Relationship", {
    "Id": f"rId{len(sheets) + 1}",
    "Type": "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles",
    "Target": "styles.xml",
})
workbook_rels_xml = ET.tostring(rels_root, encoding="utf-8", xml_declaration=True)

# root rels
root_rels = ET.Element("Relationships", xmlns="http://schemas.openxmlformats.org/package/2006/relationships")
ET.SubElement(root_rels, "Relationship", {
    "Id": "rId1",
    "Type": "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument",
    "Target": "xl/workbook.xml",
})
root_rels_xml = ET.tostring(root_rels, encoding="utf-8", xml_declaration=True)

# [Content_Types].xml
sheet_overrides = "\n".join(
    f"  <Override PartName=\"/xl/worksheets/sheet{i}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>"
    for i in range(1, len(sheets) + 1)
)
content_types = f"""<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>
<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">
  <Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>
  <Default Extension=\"xml\" ContentType=\"application/xml\"/>
  <Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>
  <Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>
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
