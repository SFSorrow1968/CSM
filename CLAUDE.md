# Agent Instructions

When making changes in this repo:
- Always build (Release and Nomad).
- If any UI/options change, regenerate `MENU_MOCK.xlsx` using `_agent/build_menu_mock_xlsx.py` (file is in `_design/`).
- If new UI text/labels are added or translations need updating, consult `_translations/TRANSLATIONS.md` for the full workflow.
- Always commit your changes.
- `DEVELOPMENT.md` contains platform/ModOptions notes; consult it when needed.
- Quick refs: `DEVELOPMENT.md` sections "Platform Differences", "ModOptions System", "EventManager Events".
- User does not want learning-oriented content; optimize for agent clarity over human prose (except MENU_MOCK, PUBLISH, and build steps).
- When adding/renaming presets or ModOption labels, follow `_docs/PRESET_CHANGE_CHECKLIST.md`.
- Build artifacts: `bin/Release/PCVR/CSM/` and `bin/Release/Nomad/CSM/`.
- `_design/MENU_MOCK.xlsx` is the current UI reference (preset guide removed).
- Check `References/` for new logs/screenshots before starting; screenshots live in `References/Screenshots/`.
- Common edit points: UI options in `Configuration/CSMModOptions.cs`, UI sync/tooltips in `Core/CSMModOptionVisibility.cs`, runtime logic in `Core/CSMManager.cs`.
- Only add new pointers to these agent files if a redundant task cost significant time and the note would materially help onboarding agents avoid it.
- Only add new pointers to these agent files if a redundant task cost significant time and the note would materially help onboarding agents avoid it.
- If the user says "publish", follow `_docs/PUBLISH.md`.
