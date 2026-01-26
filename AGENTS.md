# Agent Instructions

When making changes in this repo:
- Always build (Release and Nomad) and log results in `_agent/verification_results.md`.
- If any UI/options change, regenerate `MENU_MOCK.xlsx` using `_agent/build_menu_mock_xlsx.py`.
- Always commit your changes.
- `DEVELOPMENT.md` contains platform/ModOptions notes; consult it when needed.
- Quick refs: `DEVELOPMENT.md` sections "Platform Differences", "ModOptions System", "EventManager Events".
- Build artifacts live at `builds/CSM-PCVR/CSM/CSM.dll` and `builds/CSM-Nomad/CSM/CSM.dll`.
- `MENU_MOCK.xlsx` is the current UI reference (preset guide removed).
- Check `References/` for new logs/screenshots before starting; screenshots live in `References/Screenshots/`.
- Common edit points: UI options in `Configuration/CSMModOptions.cs`, UI sync/tooltips in `Core/CSMModOptionVisibility.cs`, runtime logic in `Core/CSMManager.cs` and `Core/CSMKillcam.cs`.
