# -*- coding: utf-8 -*-
"""Generate all language JSON files from _translations.csv with new localization group ID."""
import json
import csv
from pathlib import Path

BASE = Path(__file__).parent
INPUT = BASE / '_translations.csv'
TEXTS_DIR = BASE / 'Texts'
GROUP_ID = 'CSM_Options'

LANG_MAP = {
    'English': 'English',
    'French': 'French',
    'German': 'German',
    'Spanish': 'Spanish',
    'Italian': 'Italian',
    'Portuguese': 'Portuguese',
    'Japanese': 'Japanese',
    'Korean': 'Korean',
    'Chinese_Simplified': 'ChineseSimplified',
    'Chinese_Traditional': 'ChineseTraditional',
    'Thai': 'Thai',
}

# Map old IDs to new localization IDs
ID_MAP = {
    'CameraFirstPersonOnly': 'CameraFirstPersonOnly',
    'CameraMixed': 'CameraMixed',
    'CameraMostlyFirstPerson': 'CameraMixedRare',
    'CameraMostlyThirdPerson': 'CameraMostlyThirdPerson',
    'CameraThirdPersonOnly': 'CameraThirdPersonOnly',
    'CategoryAdvanced': 'CategoryAdvanced',
    'CategoryCustomBasic': 'CategoryCustomBasic',
    'CategoryCustomCritical': 'CategoryCustomCritical',
    'CategoryCustomDecapitation': 'CategoryCustomDecapitation',
    'CategoryCustomDismemberment': 'CategoryCustomDismemberment',
    'CategoryCustomLastEnemy': 'CategoryCustomLastEnemy',
    'CategoryCustomLastStand': 'CategoryCustomLastStand',
    'CategoryCustomParry': 'CategoryCustomParry',
    'CategoryDamageMultipliers': 'CategoryDamageMultipliers',
    'CategoryKillcam': 'CategoryKillcam',
    'CategoryPresetSelection': 'CategoryPresetSelection',
    'CategoryTriggers': 'CategoryTriggers',
    'ChanceDefault': 'ChanceDefault',
    'ChanceFrequent': 'ChanceFrequent',
    'ChanceOff': 'ChanceOff',
    'ChanceRare': 'ChanceRare',
    'ChanceVeryRare': 'ChanceVeryRare',
    'CooldownDefault': 'CooldownDefault',
    'CooldownExtended': 'CooldownExtended',
    'CooldownLong': 'CooldownLong',
    'CooldownOff': 'CooldownOff',
    'CooldownShort': 'CooldownShort',
    'DurationDefault': 'DurationDefault',
    'DurationExtended': 'DurationExtended',
    'DurationLong': 'DurationLong',
    'DurationShort': 'DurationShort',
    'DurationVeryShort': 'DurationVeryShort',
    'PresetCinematic': 'PresetCinematic',
    'PresetDefault': 'PresetDefault',
    'PresetDramatic': 'PresetDramatic',
    'PresetEpic': 'PresetEpic',
    'PresetSubtle': 'PresetSubtle',
    'TransitionLinear': 'TransitionLinear',
    'TransitionOff': 'TransitionOff',
    'TransitionSmoothstep': 'TransitionSmoothstep',
    'TriggerProfileAll': 'TriggerProfileAll',
    'TriggerProfileHighlights': 'TriggerProfileHighlights',
    'TriggerProfileKillsOnly': 'TriggerProfileKillsOnly',
    'TriggerProfileLastEnemyOnly': 'TriggerProfileLastEnemyOnly',
    'TriggerProfileParryOnly': 'TriggerProfileParryOnly',
}

# Text overrides to remove emojis from categories
TEXT_OVERRIDES = {
    'CategoryAdvanced': 'Advanced',
    'CategoryCustomBasic': 'Basic Kill',
    'CategoryCustomCritical': 'Critical Kill',
    'CategoryCustomDecapitation': 'Decapitation',
    'CategoryCustomDismemberment': 'Dismemberment',
    'CategoryCustomLastEnemy': 'Last Enemy',
    'CategoryCustomLastStand': 'Last Stand',
    'CategoryCustomParry': 'Parry',
    'CategoryDamageMultipliers': 'Damage Modifiers',
    'CategoryKillcam': 'Killcam',
    'CategoryPresetSelection': 'Quick Setup',
    'CategoryTriggers': 'Triggers',
}

def strip_emoji(text):
    """Remove leading emoji and space from text."""
    if not text:
        return text
    # Common emoji prefixes used in the old translations
    prefixes = ['🔧 ', '► ', '⚖ ', '📷 ', '⚙ ', '⚡ ']
    for prefix in prefixes:
        if text.startswith(prefix):
            return text[len(prefix):]
    return text

def main():
    TEXTS_DIR.mkdir(exist_ok=True)

    with open(INPUT, 'r', encoding='utf-8') as f:
        rows = list(csv.DictReader(f))

    # Build translation dict: old_id -> {lang: text}
    translations = {}
    for row in rows:
        old_id = row.get('Text_ID', '')
        if not old_id:
            continue
        translations[old_id] = {}
        for csv_col in LANG_MAP.keys():
            val = row.get(csv_col, '')
            if val and not val.startswith('=GOOGLETRANSLATE'):
                translations[old_id][csv_col] = strip_emoji(val)

    # Generate JSON for each language
    for csv_col, json_suffix in LANG_MAP.items():
        text_list = []

        # Add translations for IDs that exist in our ID_MAP
        for old_id, new_suffix in ID_MAP.items():
            new_id = f"{GROUP_ID}.{new_suffix}"
            if old_id in translations and csv_col in translations[old_id]:
                text = translations[old_id][csv_col]
                # Apply text overrides for English
                if csv_col == 'English' and old_id in TEXT_OVERRIDES:
                    text = TEXT_OVERRIDES[old_id]
                text_list.append({"id": new_id, "text": text})

        # Add other translations that don't need mapping (option names, etc.)
        for old_id in translations:
            if old_id not in ID_MAP:
                new_id = f"{GROUP_ID}.{old_id}"
                if csv_col in translations[old_id]:
                    text = strip_emoji(translations[old_id][csv_col])
                    text_list.append({"id": new_id, "text": text})

        # Create the JSON structure
        data = {
            "$type": "ThunderRoad.TextData, ThunderRoad",
            "id": GROUP_ID,
            "sensitiveContent": "None",
            "sensitiveFilterBehaviour": "None",
            "groupId": GROUP_ID,
            "textList": text_list
        }

        json_path = TEXTS_DIR / f'Text_{json_suffix}.json'
        with open(json_path, 'w', encoding='utf-8') as f:
            json.dump(data, f, indent=2, ensure_ascii=False)
        print(f"Generated {json_path.name}: {len(text_list)} entries")

if __name__ == '__main__':
    main()
