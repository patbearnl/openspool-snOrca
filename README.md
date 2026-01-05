# snOrca OpenSpool

Community fork of **Snapmaker Orca** that adds **OpenSpool** tag support for Snapmaker U1 (and other setups that report OpenSpool-compatible spool metadata).

Based on Snapmaker Orca `2.2.1`.

This fork focuses on one thing: turning the spool info coming from the printer into **"Machine Filament"** entries (tool number + color block) by matching it against the existing filament preset library.

## What it does

- Reads spool metadata reported by the printer (vendor/brand, type, subtype, color, temps).
- Resolves that metadata to an existing filament preset at startup / connect time.
- Shows it under **Machine Filament** in the filament dropdown, with the correct tool number + color.

## Matching rules (important)

OpenSpool tags typically carry fields like `brand`, `type`, optional `subtype`, and a color.

The slicer tries to match presets in this order:

1. `Vendor + Type + Subtype` (if subtype is present and a preset exists)
2. `Vendor + Type`
3. If the vendor has no presets: fallback to `Generic`
   - `Generic + Type + Subtype` (if it exists)
   - `Generic + Type`

If your tags follow the common naming scheme `<brand> <type> <subtype>` (subtype optional), you should always get a Machine Filament entry.

## Custom filament libraries (optional)

You can add your own vendor library (e.g. "PatLabs") by placing filament presets in:

- `%APPDATA%\\Snapmaker_Orca\\user\\default\\filament\\`

Note: this fork intentionally keeps the existing app key (`Snapmaker_Orca`) so your normal config/profile storage continues to work.

## Downloads

Releases (Windows installer / portable builds) are published here:

- `https://github.com/patbearnl/openspool-snOrca/releases`

## Windows install notes

If the app won't start, install these prerequisites:

- Microsoft WebView2 Runtime: `https://aka.ms/webview2`
- MSVC x64 Redistributable: `https://aka.ms/vs/17/release/vc_redist.x64.exe`

Upgrades should not touch your `%APPDATA%\\Snapmaker_Orca\\user\\` folder.

## Building (Windows)

Tools:

- Visual Studio 2022/2026 (C++ Desktop workload, MSVC v143 toolset)
- CMake, Git (+ Git LFS), Strawberry Perl

Common build commands:

- `build_release_vs2026.bat deps`
- `build_release_vs2026.bat slicer`

After building, run `cmake --build build-vs2026 --config Release --target install` to install into the local output folder (see `HANDOFF.md` for current paths).

## License

This project is licensed under the **GNU Affero General Public License v3.0**.

See `LICENSE.txt`.

## Credits

- Snapmaker Orca (upstream)
- OrcaSlicer / Bambu Studio / PrusaSlicer / Slic3r
- OpenSpool protocol community
- paxx12 for being the first with Snapmaker U1 custom firmware
