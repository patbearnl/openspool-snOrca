# snOrca OpenSpool filament profile converter (Windows)

Small WinForms tool to convert a `3dfilamentprofiles.com` **My Spools** export (`my-spools.csv` or `my-spools.json`) into SnOrca filament preset JSON files.

Also supports **Spoolman** JSON exports (array of spool objects).

**Output folder (default):**

`%APPDATA%\\Snapmaker_Orca\\user\\default\\filament\\base\\`

## What it generates

Two modes:

- **Material presets (recommended):** creates one preset per `brand + type + subtype` (works best for machine spool matching).
- **Per-spool presets:** creates one preset per spool (includes color name + id suffix in the preset name).

Each generated `*.json` filament preset includes:

- `version` (required by SnOrca to load user presets)
- `from: "user"`, `instantiation: "true"` (or `false` if you enable “Hide material presets unless a matching spool is present”)
- `filament_vendor`: vendor/brand
- `filament_type`: normalized Orca type (`PLA`, `PETG`, `ABS`, ...)
- `filament_sub_type`: derived from `material_type` (treats `"Basic"` as “no subtype”)
- `default_filament_colour`: first hex color found in `rgb`
- nozzle/bed temps copied from the installed SnOrca fdm templates (when available)

## Build

From the repo root:

`cd tools\\snorca-spool-converter\\SnOrcaSpoolConverter`

`dotnet build -c Release`
