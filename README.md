# snOrca OpenSpool

Community fork of **Snapmaker Orca** that adds **OpenSpool** tag support for Snapmaker U1 running custom [**paxx12 firmware**](https://github.com/paxx12/SnapmakerU1-Extended-Firmware) (and other setups that report OpenSpool-compatible spool metadata).

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
   - If multiple `Vendor + Type <something>` presets exist, it prefers `Basic` (then the shortest/most general name).
3. If the vendor has no presets: fallback to `Generic`
   - `Generic + Type + Subtype` (if it exists)
   - `Generic + Type`

If your tags follow the common naming scheme `<brand> <type> <subtype>` (subtype optional), you should always get a Machine Filament entry.

Note: `subtype` matching is case-insensitive. The included Android app and converter treat `Basic` as “no subtype”.

## Custom filament libraries (optional)

You can add your own vendor library (e.g. "PatLabs") by placing filament presets in:

- `%APPDATA%\\Snapmaker_Orca\\user\\default\\filament\\base\\` (root presets for Machine Filament matching)

Note: this fork intentionally keeps the existing app key (`Snapmaker_Orca`) so your normal config/profile storage continues to work.

## Downloads

Releases (Windows installer / portable builds) are published here:

- `https://github.com/patbearnl/openspool-snOrca/releases`


## Support

If this project helps, you can support development:

[![Buy me a coffee](https://img.buymeacoffee.com/button-api/?text=Buy%20me%20a%20coffee&emoji=&slug=patjewaalv&button_colour=5F7FFF&font_colour=ffffff&font_family=Bree&outline_colour=000000&coffee_colour=FFDD00)](https://buymeacoffee.com/patjewaalv)

## Android tag writer

Android tag writer lives in a separate repository:

- `https://github.com/patbearnl/Android-Openspool-Ntag-Writer-with-JSon-cvs-import-support-for-3DFP-and-Spoolman-import-supoort`

It reads/writes OpenSpool JSON onto NFC tags.

- Download from Releases: look for `snOrca-openspool-ntag.apk`
- Modes: Read (load + edit), Write (tap tag to save)
- Presets: searchable list based on OrcaFilamentLibrary
- Import: 3dfilamentprofiles.com `my-spools.csv` / `my-spools.json` exports
- Brand override (optional): keeps imported brands as-is; override is applied only when writing to a tag (toggle is saved per spool)
- Dark mode follows system theme

OpenSpool JSON fields:
- Required: `protocol`, `version`, `brand`, `type`, `color_hex`
- Optional: `subtype`, `min_temp`, `max_temp`, `bed_min_temp`, `bed_max_temp`

## Windows install notes

If the Windows app won't start, install these prerequisites:

- Microsoft WebView2 Runtime: `https://aka.ms/webview2`
- MSVC x64 Redistributable: `https://aka.ms/vs/17/release/vc_redist.x64.exe`

Upgrades should not touch your `%APPDATA%\\Snapmaker_Orca\\user\\` folder.

## Tools

The Windows profile/spool converter lives in a separate repository:

- `https://github.com/patbearnl/3DFP-Openspool-Spoolman-2-orca-Profiles-Converter`

## Building (Windows)

Tools:

- Visual Studio 2022/2026 (C++ Desktop workload, MSVC v143 toolset)
- CMake, Git (+ Git LFS), Strawberry Perl

Common build commands:

- `build_release_vs2026.bat deps`
- `build_release_vs2026.bat slicer`

After building, run `cmake --build build-vs2026 --config Release --target install` to install into the local output folder (see `HANDOFF.md` for current paths).

### Building (macOS Tahoe)

Tools:

- XCode
- CMake (version 3.31.x is mandatory), Git, gettext, libtool, automake, autoconf, texinfo

Please follow the [Orca Slicer Build Guide](https://www.orcaslicer.com/wiki/developer-reference/How-to-build), but before running the `build_release_macos.sh` script, add 2 CMake directives to `build_release_macos.sh`, as suggested in [Steve Scargall's blog post](https://stevescargall.com/blog/2025/10/how-to-build-orcaslicer-from-source-on-macos-15-sequoia-a-step-by-step-guide/):
```
-DCMAKE_CXX_COMPILER=/usr/bin/g++ \
-DCMAKE_C_COMPILER=/usr/bin/gcc
```

Now run the build script to download dependencies, but don't expect the script to succeed. When the script fails, apply the following changes to two dependencies:

1. In file `.\deps\wxWidgets\patch_cotire_test_cmake_minimum_required.cmake`, wrap the Windows-specific part in an `if (WIN32) ... endif()` block (AFAIK, this part is just to ensure windows is able to expand an archive, which works on a mac out of the box). The Windows-specific part starts with a comment "# Ensure WebView2 SDK is available without relying on CMake's libarchive extraction.", add `if (WIN32)` above that comment. At the end of the file, add `endif()`. Below is an excerpt from the modified part of the file:
```
# ... beginning of the file, don't modify anything before

if (WIN32)   # Added to make the file runnable on macos
  # Ensure WebView2 SDK is available without relying on CMake's libarchive extraction.
  # wxWidgets' MSW webview build expects `3rdparty/webview2/build/native/include/WebView2.h`.
  set(_webview2_header "3rdparty/webview2/build/native/include/WebView2.h")
  set(_webview2_version "1.0.1418.22")
  set(_webview2_url "https://www.nuget.org/api/v2/package/Microsoft.Web.WebView2/${_webview2_version}")
  set(_webview2_sha256 "51d2ef56196e2a9d768a6843385bcb9c6baf9ed34b2603ddb074fb4995543a99")

  # ... rest of file ...

  endif()
endif()      # Added to close the if block
```

2. We also need to patch one of the OpenVDB files, which can be done with the following shell command, ran from the root of the project:
```
sed -i '' 's/OpT::template eval/OpT::eval/g' "deps/build/arm64/dep_OpenVDB-prefix/src/dep_OpenVDB/openvdb/openvdb/tree/NodeManager.h"
```

Now run the `.\build_release_macos.sh` script again and it should succeed.

If you want to create a dmg, install `create-dmg` with `brew install create-dmg` and then run
```
create-dmg --volname "Snapmaker Orca Installer" --volicon "build/arm64/Snapmaker_Orca/Snapmaker Orca.app/Contents/Resources/Icon.icns" --window-pos 200 120 --window-size 800 400 --icon-size 100 --icon "Snapmaker Orca.app" 200 190 --hide-extension "Snapmaker Orca.app" --app-drop-link 600 185 "Snapmaker_Orca-OpenSpool.dmg" "build/arm64/Snapmaker_Orca/Snapmaker Orca.app"
```

## License

This project is licensed under the **GNU Affero General Public License v3.0**.

See `LICENSE.txt`.

## Credits

- Snapmaker Orca (upstream)
- OrcaSlicer / Bambu Studio / PrusaSlicer / Slic3r
- OpenSpool protocol community
- [paxx12](https://github.com/paxx12/SnapmakerU1-Extended-Firmware) for being the first with Snapmaker U1 custom firmware


