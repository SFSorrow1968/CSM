# Publish Checklist

Use this when the user says "publish".

1) Confirm version
- Update `Configuration/CSMModOptions.cs` VERSION (and any other version fields if present).
- Ensure the Git tag/release name matches the version.

2) Build + artifacts
- `dotnet build CSM.csproj -c Release`
- `dotnet build CSM.csproj -c Nomad`
- Copy artifacts:
  - `bin/Release/CSM.dll` -> `builds/CSM-PCVR/CSM/CSM.dll`
  - `bin/Nomad/CSM.dll` -> `builds/CSM-Nomad/CSM/CSM.dll`
- Log results in `_agent/verification_results.md`.

3) Documentation updates
- If UI/options changed, regenerate `MENU_MOCK.xlsx` using `_agent/build_menu_mock_xlsx.py`.
- Update `Description.md` overview/detailed text if needed.

4) Commit + push
- Commit all changes and push to `main`.

5) GitHub release
- Create a GitHub release with the version tag.
- Attach build artifacts (zip `builds/CSM-PCVR` and `builds/CSM-Nomad`).
- Release notes: concise summary of changes + any known issues.
