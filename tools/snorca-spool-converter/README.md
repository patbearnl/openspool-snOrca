# snOrca spool → filament profile converter (Windows)

Small WinForms tool to convert a `3dfilamentprofiles.com` **My Spools** export (`my-spools.csv` or `my-spools.json`) into SnOrca filament preset JSON files.

**Output folder (default):**

`%APPDATA%\\Snapmaker_Orca\\user\\default\\filament\\`

## What it generates

For each spool, it writes one `*.json` filament profile with:

- `from: "user"`, `instantiation: "true"`
- `filament_vendor`: vendor/brand
- `filament_type`: normalized Orca type (`PLA`, `PETG`, `ABS`, ...)
- `filament_sub_type`: derived from `material_type` (treats `"Basic"` as empty)
- `default_filament_colour`: first hex color found in `rgb`

## Build

From the repo root:

`cd tools\\snorca-spool-converter\\SnOrcaSpoolConverter`

`dotnet build -c Release`

