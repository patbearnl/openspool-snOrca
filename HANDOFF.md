# Handoff — OpenSpool SnOrca (Snapmaker Orca) fork

This document captures the current state so a new agent can continue without re-discovering context.

## Repo / location

- Local source: `E:\Documents\snapmaker\OrcaSlicer`
- GitHub fork: `https://github.com/patbearnl/openspool-snOrca`
- Working branch: `openspool-v2.2.1`
- Remote tracking: `openspool/main` is aligned with `HEAD`.

## User goal

- Fork Snapmaker Orca to support **OpenSpool** tags and show them as **Machine Filament** entries with color block + tool number.
- Keep aligned with Snapmaker Orca **2.2.1** behavior, but point in-app updater to the fork (GitHub Releases).

## What works

- Windows build + install works with VS2026 toolchain.
- App launches from the installed output folder.
- WCP connects (device online), and logs show spool info arriving from the printer.

## Where the app binary ends up

After `install`, the runnable app is:

- `E:\Documents\snapmaker\OrcaSlicer\Snapmaker_Orca\snapmaker-orca.exe`

## Evidence (logs)

Log directory:

- `%APPDATA%\Snapmaker_Orca\log\`

Example showing spools received from printer:

- `%APPDATA%\Snapmaker_Orca\log\debug_Mon_Jan_05_15_21_07_12048.log.0`
  - Contains `_deviceFilamentInfoMap` with arrays like `filament_vendor`, `filament_type`, `filament_sub_type`, `filament_color_rgba`, etc.

## Root cause for “no Machine Filament”

- Printer reports `Generic PLA Basic` (vendor/type/subtype).
- Snapmaker’s built-in preset is commonly named `Generic PLA` (without “Basic”).
- `PresetComboBoxes.cpp` only adds Machine Filament entries if the computed `filament_name` matches an existing filament preset (`f.name == filament_name` or `filament_name + " @U1"`).
- When the name doesn’t match, the entry gets removed from `machine_filaments`, so nothing appears in the UI.

## Fix applied (important)

### Commit: `2a42334395` — “Fix machine filament name resolution”

- File: `src/slic3r/GUI/SSWCP.cpp`
- Change: Resolve the machine-reported `(vendor, type, subtype)` into an **existing** filament preset name before inserting into `preset_bundle->machine_filaments`.
- Special-case:
  - If `vendor == "Generic"` and `subtype == "Basic"`, treat subtype as empty so it maps to `Generic PLA` etc.
- Candidate + fallback logic:
  - tries `vendor type subtype`, then `vendor type`, and also tries `candidate + " @U1"` if present.

Expected outcome: “Machine Filament” should now appear for tags like `Generic PLA Basic`.

## Build/toolchain fixes already in repo

### Commit: `5fe9a3894a` — “Allow CMake 4.x on Windows”

- File: `CMakeLists.txt`
- Upstream blocked CMake >= 4 on Windows; VS2026 ships `cmake 4.1.1-msvc1`.
- Changed fatal error to warning so configure can proceed.

### Commit: `75ba4741cd` — “Ignore installed Snapmaker_Orca output”

- File: `.gitignore`
- Added `Snapmaker_Orca/` so install output doesn’t show as untracked.

## Updater changes

- File: `src/libslic3r/AppConfig.cpp`
- `VERSION_CHECK_URL_STABLE`:
  - `https://api.github.com/repos/patbearnl/openspool-snOrca/releases/latest`
- `VERSION_CHECK_URL`:
  - `https://api.github.com/repos/patbearnl/openspool-snOrca/releases`

Note: Releases/tags still need to be created on GitHub for the in-app updater to find anything.

## How to build (PowerShell; use VS CMake)

Use the Visual Studio bundled CMake:

```powershell
cd E:\Documents\snapmaker\OrcaSlicer

$cmake = "E:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe"

& $cmake --build build-vs2026 --config Release --target ALL_BUILD --parallel 8
& $cmake --build build-vs2026 --config Release --target install --parallel 8
```

## Next steps for the next agent

1) Ask the user to run the newly installed `Snapmaker_Orca\snapmaker-orca.exe` after commit `2a42334395`.
2) Verify “Machine Filament” now appears when a tag reports `Generic PLA Basic`.
3) If it still doesn’t:
   - Add temporary logging in `SSWCP.cpp` to print chosen preset candidate.
   - Check if `PresetComboBoxes.cpp` still erases entries (means preset name mismatch persists).
4) Plan GitHub Releases/tagging scheme (semver comparison in updater) once machine-filament is confirmed.

