- Command: dotnet build -c Nomad
- Result: success
- Notes: Build succeeded (0 warnings, 0 errors).

- Command: dotnet build -c Release
- Result: success
- Notes: Build succeeded (0 warnings, 0 errors).

- Command: (not run)
- Result: skipped
- Notes: CSV documentation update only.

- Command: (not run)
- Result: skipped
- Notes: Documentation conversion to XLSX only.

- Command: python _agent/build_preset_xlsx.py
- Result: success
- Notes: Generated PRESET_GUIDE.xlsx from PRESET_GUIDE.csv.

- Command: python _agent/build_preset_xlsx.py
- Result: success
- Notes: Generated PRESET_GUIDE.xlsx from code (CSMManager/CSMModOptions).

- Command: python _agent/build_preset_xlsx.py
- Result: success
- Notes: Regenerated PRESET_GUIDE.xlsx with styling (headers, notes, freeze panes, column widths).

- Command: python _agent/build_preset_xlsx.py
- Result: success
- Notes: Regenerated PRESET_GUIDE.xlsx as a single-sheet layout with section/preset blocks and styling.

- Command: python _agent/build_preset_xlsx.py
- Result: success
- Notes: Regenerated PRESET_GUIDE.xlsx with one tab per preset section (code-derived).

- Command: python _agent/build_preset_compact_csv.py
- Result: success
- Notes: Generated PRESET_GUIDE_COMPACT.csv from code (single-sheet, compact guide).

- Command: python _agent/build_preset_compact_csv.py
- Result: success
- Notes: Regenerated PRESET_GUIDE_COMPACT.csv with correct method parsing (non-zero values).

- Command: python _agent/build_compact_xlsx.py
- Result: success
- Notes: Generated PRESET_GUIDE_COMPACT.xlsx from PRESET_GUIDE_COMPACT.csv (single sheet, formatted).

- Command: python _agent/build_preset_organized_xlsx.py
- Result: success
- Notes: Generated PRESET_GUIDE_ORGANIZED.xlsx with overview + trigger-based tabs (code-derived).

- Command: python _agent/build_preset_organized_xlsx.py
- Result: success
- Notes: Regenerated PRESET_GUIDE_ORGANIZED.xlsx with intensity tables limited to TimeScale/Third Person.

- Command: python _agent/build_preset_organized_xlsx.py
- Result: success
- Notes: Regenerated PRESET_GUIDE_ORGANIZED.xlsx to include Frequency Mode notes.

- Command: python _agent/build_preset_organized_xlsx.py
- Result: success
- Notes: Regenerated PRESET_GUIDE_ORGANIZED.xlsx with per-mode frequency columns and section color coding.

- Command: dotnet build C:\Users\dkatz\Documents\Projects\CSM\CSM.csproj -c Release
- Result: success
- Notes: Build succeeded (0 warnings, 0 errors).

- Command: python C:\Users\dkatz\Documents\Projects\CSM\_agent\build_preset_organized_xlsx.py
- Result: success
- Notes: Regenerated PRESET_GUIDE_ORGANIZED.xlsx after killcam distribution/intensity updates.

- Command: dotnet build C:\Users\dkatz\Documents\Projects\CSM\CSM.csproj -c Release
- Result: success
- Notes: Build succeeded (0 warnings, 0 errors).

- Command: python C:\Users\dkatz\Documents\Projects\CSM\_agent\build_preset_organized_xlsx.py
- Result: success
- Notes: Regenerated PRESET_GUIDE_ORGANIZED.xlsx after preset and chance/cooldown split updates.

- Command: dotnet build C:\Users\dkatz\Documents\Projects\CSM\CSM.csproj -c Release
- Result: success
- Notes: Build succeeded (0 warnings, 0 errors).

- Command: dotnet build CSM.csproj -c Release
- Result: success
- Notes: Build succeeded (0 warnings, 0 errors).

- Command: python _agent/build_preset_organized_xlsx.py
- Result: success
- Notes: Regenerated PRESET_GUIDE_ORGANIZED.xlsx after preset/dynamic custom sync updates.


- Command: dotnet build CSM.csproj -c Release
- Result: success
- Notes: Build succeeded (0 warnings, 0 errors).

- Command: python _agent/build_preset_organized_xlsx.py
- Result: success
- Notes: Regenerated PRESET_GUIDE_ORGANIZED.xlsx after third-person eligibility and preset renames.


- Command: dotnet build CSM.csproj -c Release
- Result: success
- Notes: Build succeeded (0 warnings, 0 errors).

- Command: python _agent/build_preset_organized_xlsx.py
- Result: success
- Notes: Regenerated PRESET_GUIDE_ORGANIZED.xlsx after cooldown/smoothness/preset-gating updates.


- Command: python _agent/build_menu_mock_xlsx.py
- Result: success
- Notes: Generated MENU_MOCK.xlsx from CSMModOptions menu metadata.


- Command: dotnet build CSM.csproj -c Release
- Result: success
- Notes: Build succeeded (0 warnings, 0 errors).

- Command: python _agent/build_preset_organized_xlsx.py
- Result: success
- Notes: Regenerated PRESET_GUIDE_ORGANIZED.xlsx after chance preset and category updates.

- Command: python _agent/build_menu_mock_xlsx.py
- Result: success
- Notes: Regenerated MENU_MOCK.xlsx after category and preset updates.


- Command: python _agent/build_menu_mock_xlsx.py
- Result: success
- Notes: Regenerated MENU_MOCK.xlsx after moving Last Stand Threshold into CSM Triggers.


- Command: dotnet build CSM.csproj -c Release
- Result: success
- Notes: Build succeeded (0 warnings, 0 errors).

- Command: python _agent/build_preset_organized_xlsx.py
- Result: success
- Notes: Regenerated PRESET_GUIDE_ORGANIZED.xlsx after preset renames and killcam changes.

- Command: python _agent/build_menu_mock_xlsx.py
- Result: success
- Notes: Regenerated MENU_MOCK.xlsx after menu restructuring.


- Command: dotnet build CSM.csproj -c Release
- Result: success
- Notes: Build succeeded (0 warnings, 0 errors).

- Command: dotnet build CSM.csproj -c Nomad
- Result: success
- Notes: Build succeeded (0 warnings, 0 errors).

- Command: python _agent/build_preset_organized_xlsx.py
- Result: success
- Notes: Regenerated PRESET_GUIDE_ORGANIZED.xlsx after dynamic intensity and menu changes.

- Command: python _agent/build_menu_mock_xlsx.py
- Result: success
- Notes: Regenerated MENU_MOCK.xlsx after menu changes.

- Command: Copy-Item bin/Release/CSM.dll -> builds/CSM-PCVR/CSM/CSM.dll
- Result: success
- Notes: Updated PCVR build artifact.

- Command: Copy-Item bin/Nomad/CSM.dll -> builds/CSM-Nomad/CSM/CSM.dll
- Result: success
- Notes: Updated Nomad build artifact.


- Command: dotnet build CSM.csproj -c Release
- Result: success
- Notes: Build succeeded (0 warnings, 0 errors).

- Command: dotnet build CSM.csproj -c Nomad
- Result: success
- Notes: Build succeeded (0 warnings, 0 errors).

- Command: python _agent/build_preset_organized_xlsx.py
- Result: success
- Notes: Regenerated PRESET_GUIDE_ORGANIZED.xlsx after killcam randomization and smoothing units.

- Command: python _agent/build_menu_mock_xlsx.py
- Result: success
- Notes: Regenerated MENU_MOCK.xlsx after killcam randomization and menu changes.

- Command: Copy-Item bin/Release/CSM.dll -> builds/CSM-PCVR/CSM/CSM.dll
- Result: success
- Notes: Updated PCVR build artifact.

- Command: Copy-Item bin/Nomad/CSM.dll -> builds/CSM-Nomad/CSM/CSM.dll
- Result: success
- Notes: Updated Nomad build artifact.


- Command: dotnet build CSM.csproj -c Release
- Result: success
- Notes: Build succeeded (0 warnings, 0 errors).

- Command: dotnet build CSM.csproj -c Nomad
- Result: success
- Notes: Build succeeded (0 warnings, 0 errors).

- Command: python _agent/build_preset_organized_xlsx.py
- Result: success
- Notes: Regenerated PRESET_GUIDE_ORGANIZED.xlsx after menu control changes.

- Command: python _agent/build_menu_mock_xlsx.py
- Result: success
- Notes: Regenerated MENU_MOCK.xlsx with sorted order and control types.

- Command: Copy-Item bin/Release/CSM.dll -> builds/CSM-PCVR/CSM/CSM.dll
- Result: success
- Notes: Updated PCVR build artifact.

- Command: Copy-Item bin/Nomad/CSM.dll -> builds/CSM-Nomad/CSM/CSM.dll
- Result: success
- Notes: Updated Nomad build artifact.


- Command: dotnet build CSM.csproj -c Release
- Result: success
- Notes: Build succeeded (0 warnings, 0 errors).

- Command: dotnet build CSM.csproj -c Nomad
- Result: success
- Notes: Build succeeded (0 warnings, 0 errors).

- Command: python _agent/build_preset_organized_xlsx.py
- Result: success
- Notes: Regenerated PRESET_GUIDE_ORGANIZED.xlsx after menu ordering changes.

- Command: python _agent/build_menu_mock_xlsx.py
- Result: success
- Notes: Regenerated MENU_MOCK.xlsx with ModOptions file order.

- Command: Copy-Item bin/Release/CSM.dll -> builds/CSM-PCVR/CSM/CSM.dll
- Result: success
- Notes: Updated PCVR build artifact.

- Command: Copy-Item bin/Nomad/CSM.dll -> builds/CSM-Nomad/CSM/CSM.dll
- Result: success
- Notes: Updated Nomad build artifact.

- Command: dotnet build CSM.csproj -c Release
  Result: Success
  Notes: Build complete: bin\Release\CSM.dll
- Command: dotnet build CSM.csproj -c Nomad
  Result: Success
  Notes: Build complete: bin\Nomad\CSM.dll
- Command: Copy-Item -Force "bin/Release/CSM.dll" "builds/CSM-PCVR/CSM/CSM.dll"; Copy-Item -Force "bin/Nomad/CSM.dll" "builds/CSM-Nomad/CSM/CSM.dll"
  Result: Success
  Notes: Updated build artifacts
- Command: dotnet build CSM.csproj -c Release
  Result: Success
  Notes: Build complete: bin\Release\CSM.dll
- Command: dotnet build CSM.csproj -c Nomad
  Result: Success
  Notes: Build complete: bin\Nomad\CSM.dll
- Command: Copy-Item -Force "bin/Release/CSM.dll" "builds/CSM-PCVR/CSM/CSM.dll"; Copy-Item -Force "bin/Nomad/CSM.dll" "builds/CSM-Nomad/CSM/CSM.dll"
  Result: Success
  Notes: Updated build artifacts
- Command: python _agent/build_menu_mock_xlsx.py
  Result: Success
  Notes: Updated MENU_MOCK.xlsx
- Command: dotnet build CSM.csproj -c Release
  Result: Success
  Notes: Build complete: bin\Release\CSM.dll
- Command: dotnet build CSM.csproj -c Nomad
  Result: Success
  Notes: Build complete: bin\Nomad\CSM.dll
- Command: Copy-Item -Force "bin/Release/CSM.dll" "builds/CSM-PCVR/CSM/CSM.dll"; Copy-Item -Force "bin/Nomad/CSM.dll" "builds/CSM-Nomad/CSM/CSM.dll"
  Result: Success
  Notes: Updated build artifacts
- Command: python _agent/build_menu_mock_xlsx.py
  Result: Success
  Notes: Updated MENU_MOCK.xlsx (category/order sync)
- Command: dotnet build CSM.csproj -c Release
  Result: Success
  Notes: Build complete: bin\Release\CSM.dll
- Command: dotnet build CSM.csproj -c Nomad
  Result: Success
  Notes: Build complete: bin\Nomad\CSM.dll
- Command: Copy-Item -Force "bin/Release/CSM.dll" "builds/CSM-PCVR/CSM/CSM.dll"; Copy-Item -Force "bin/Nomad/CSM.dll" "builds/CSM-Nomad/CSM/CSM.dll"
  Result: Success
  Notes: Updated build artifacts
- Command: dotnet build CSM.csproj -c Release
  Result: Success
  Notes: Build complete: bin\Release\CSM.dll
- Command: dotnet build CSM.csproj -c Nomad
  Result: Success
  Notes: Build complete: bin\Nomad\CSM.dll
- Command: Copy-Item -Force "bin/Release/CSM.dll" "builds/CSM-PCVR/CSM/CSM.dll"; Copy-Item -Force "bin/Nomad/CSM.dll" "builds/CSM-Nomad/CSM/CSM.dll"
  Result: Success
  Notes: Updated build artifacts
- Command: python _agent/build_menu_mock_xlsx.py
  Result: Success
  Notes: Updated MENU_MOCK.xlsx
- Command: python _agent/build_menu_mock_xlsx.py
  Result: Success
  Notes: Updated MENU_MOCK.xlsx (advanced trimmed)
- Command: dotnet build CSM.csproj -c Release
  Result: Success
  Notes: Build complete: bin\Release\CSM.dll
- Command: dotnet build CSM.csproj -c Nomad
  Result: Success
  Notes: Build complete: bin\Nomad\CSM.dll
- Command: Copy-Item -Force "bin/Release/CSM.dll" "builds/CSM-PCVR/CSM/CSM.dll"; Copy-Item -Force "bin/Nomad/CSM.dll" "builds/CSM-Nomad/CSM/CSM.dll"
  Result: Success
  Notes: Updated build artifacts
- Command: dotnet build CSM.csproj -c Release
- Result: success
- Notes: Build complete: bin\Release\CSM.dll

- Command: dotnet build CSM.csproj -c Nomad
- Result: success
- Notes: Build complete: bin\Nomad\CSM.dll

- Command: Copy-Item -Force "bin/Release/CSM.dll" "builds/CSM-PCVR/CSM/CSM.dll"; Copy-Item -Force "bin/Nomad/CSM.dll" "builds/CSM-Nomad/CSM/CSM.dll"
- Result: success
- Notes: Updated build artifacts.

- Command: python _agent/build_menu_mock_xlsx.py
- Result: success
- Notes: Updated MENU_MOCK.xlsx.
- Command: dotnet build CSM.csproj -c Release
- Result: success
- Notes: Build complete: bin\\Release\\CSM.dll

- Command: dotnet build CSM.csproj -c Nomad
- Result: success
- Notes: Build complete: bin\\Nomad\\CSM.dll

- Command: Copy-Item -Force "bin/Release/CSM.dll" "builds/CSM-PCVR/CSM/CSM.dll"; Copy-Item -Force "bin/Nomad/CSM.dll" "builds/CSM-Nomad/CSM/CSM.dll"
- Result: success
- Notes: Updated build artifacts.
- Command: dotnet build CSM.csproj -c Release
- Result: success
- Notes: Build complete: bin\\Release\\CSM.dll

- Command: dotnet build CSM.csproj -c Nomad
- Result: success
- Notes: Build complete: bin\\Nomad\\CSM.dll

- Command: Copy-Item -Force "bin/Release/CSM.dll" "builds/CSM-PCVR/CSM/CSM.dll"; Copy-Item -Force "bin/Nomad/CSM.dll" "builds/CSM-Nomad/CSM/CSM.dll"
- Result: success
- Notes: Updated build artifacts.
- Command: dotnet build CSM.csproj -c Release
- Result: success
- Notes: Build complete: bin\\Release\\CSM.dll

- Command: dotnet build CSM.csproj -c Nomad
- Result: success
- Notes: Build complete: bin\\Nomad\\CSM.dll

- Command: Copy-Item -Force "bin/Release/CSM.dll" "builds/CSM-PCVR/CSM/CSM.dll"; Copy-Item -Force "bin/Nomad/CSM.dll" "builds/CSM-Nomad/CSM/CSM.dll"
- Result: success
- Notes: Updated build artifacts.
- Command: dotnet build CSM.csproj -c Release
- Result: success
- Notes: Build complete: bin\\Release\\CSM.dll

- Command: dotnet build CSM.csproj -c Nomad
- Result: success
- Notes: Build complete: bin\\Nomad\\CSM.dll

- Command: Copy-Item -Force "bin/Release/CSM.dll" "builds/CSM-PCVR/CSM/CSM.dll"; Copy-Item -Force "bin/Nomad/CSM.dll" "builds/CSM-Nomad/CSM/CSM.dll"
- Result: success
- Notes: Updated build artifacts.
- Command: dotnet build CSM.csproj -c Release
- Result: success
- Notes: Build complete: bin\\Release\\CSM.dll

- Command: dotnet build CSM.csproj -c Nomad
- Result: success
- Notes: Build complete: bin\\Nomad\\CSM.dll

- Command: Copy-Item -Force "bin/Release/CSM.dll" "builds/CSM-PCVR/CSM/CSM.dll"; Copy-Item -Force "bin/Nomad/CSM.dll" "builds/CSM-Nomad/CSM/CSM.dll"
- Result: success
- Notes: Updated build artifacts.
