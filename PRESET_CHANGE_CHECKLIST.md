# Preset/Feature Change Checklist

Use this when adding or renaming preset labels, enum options, or ModOption strings.

- If a label differs from its stored value (e.g., "Off (No Cooldown)" -> "Off"), add the label to the matching `Get*Preset()` switch so it never falls back unexpectedly.
- If you change or add a `ModOptionString` label/value, update any parsing in `CSMModOptions` and any UI sync in `CSMModOptionVisibility`.
- If you rename option labels in custom sections, ensure UI sync keys still resolve (category + name) so presets can push values.
- If you add/rename presets, update provider arrays, enum options, default indices, and any mappings in `CSMManager.GetPresetValues()`.
- If UI/options change: regenerate `MENU_MOCK.xlsx`.
- Always build Release + Nomad and copy outputs to `builds/CSM-PCVR/CSM/CSM.dll` and `builds/CSM-Nomad/CSM/CSM.dll`, then commit.
