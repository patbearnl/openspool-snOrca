#!/usr/bin/env python3
from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path
from typing import Optional


@dataclass(frozen=True)
class Temps:
    nozzle_low: int
    nozzle_high: int
    bed_min: int
    bed_max: int


def _read_json(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def _first_int(obj: dict, key: str) -> Optional[int]:
    value = obj.get(key)
    if not isinstance(value, list) or not value:
        return None
    try:
        return int(str(value[0]).strip())
    except Exception:
        return None


def load_base_temp_map(base_dir: Path) -> dict[str, Temps]:
    mapping: dict[str, Temps] = {}
    for path in sorted(base_dir.glob("fdm_filament_*.json")):
        if path.name in {"fdm_filament_common.json"}:
            continue

        obj = _read_json(path)
        nozzle_low = _first_int(obj, "nozzle_temperature_range_low")
        nozzle_high = _first_int(obj, "nozzle_temperature_range_high")
        bed = _first_int(obj, "hot_plate_temp")
        bed_first = _first_int(obj, "hot_plate_temp_initial_layer")

        if nozzle_low is None or nozzle_high is None or bed is None or bed_first is None:
            continue

        bed_min = min(bed, bed_first)
        bed_max = max(bed, bed_first)
        temps = Temps(nozzle_low=nozzle_low, nozzle_high=nozzle_high, bed_min=bed_min, bed_max=bed_max)

        filament_types = obj.get("filament_type")
        if isinstance(filament_types, list) and filament_types:
            for t in filament_types:
                t_str = str(t).strip()
                if t_str:
                    mapping[t_str.upper()] = temps
        else:
            suffix = path.stem.removeprefix("fdm_filament_").upper()
            mapping[suffix] = temps
    return mapping


def _normalize_type_for_base(type_value: str, base_map: dict[str, Temps]) -> Optional[str]:
    t = type_value.strip().upper()
    if not t:
        return None

    if t in base_map:
        return t

    # Common OpenSpool + Orca naming quirks.
    if t.startswith("PET"):
        if "PETG" in base_map:
            return "PETG"

    if t.startswith("PA"):
        if "PA" in base_map:
            return "PA"

    if t.startswith("PPA"):
        if "PPA-CF" in base_map:
            return "PPA-CF"

    if "-" in t:
        head = t.split("-", 1)[0]
        if head == "PET" and "PETG" in base_map:
            return "PETG"
        if head.startswith("PA") and "PA" in base_map:
            return "PA"
        if head in base_map:
            return head

    # Fuzzy fallback for branded names like "ePLA-LW".
    contains = t
    if "PLA" in contains and "PLA" in base_map:
        return "PLA"
    if ("PETG" in contains or contains.startswith("PET")) and "PETG" in base_map:
        return "PETG"
    if "PCTG" in contains and "PCTG" in base_map:
        return "PCTG"
    if contains.startswith("PPS") and "PPS" in base_map:
        return "PPS"
    if contains.startswith("PC") and "PC" in base_map:
        return "PC"
    if "ABS" in contains and "ABS" in base_map:
        return "ABS"
    if "ASA" in contains and "ASA" in base_map:
        return "ASA"
    if "TPU" in contains and "TPU" in base_map:
        return "TPU"
    if "BVOH" in contains and "BVOH" in base_map:
        return "BVOH"
    if "PVA" in contains and "PVA" in base_map:
        return "PVA"

    return None


def iter_system_profiles(filament_dir: Path) -> list[tuple[str, str]]:
    profiles: list[tuple[str, str]] = []
    for path in sorted(filament_dir.rglob("* @System.json")):
        if "base" in path.parts:
            continue
        brand = path.parent.name if path.parent != filament_dir else ""
        profiles.append((brand, path.name))
    return profiles


def main() -> int:
    repo_root = Path(__file__).resolve().parents[3]
    filament_dir = repo_root / "resources" / "profiles" / "OrcaFilamentLibrary" / "filament"
    base_dir = filament_dir / "base"
    out_path = repo_root / "android" / "openspool-tag-writer" / "app" / "src" / "main" / "assets" / "presets.json"

    if not filament_dir.is_dir():
        raise SystemExit(f"Missing filament library directory: {filament_dir}")

    base_map = load_base_temp_map(base_dir)

    presets = []
    for brand_dir, filename in iter_system_profiles(filament_dir):
        stem = filename.removesuffix(".json").removesuffix(" @System")
        parts = stem.split(" ")

        if brand_dir:
            brand = brand_dir
            if parts and parts[0].lower() == brand_dir.lower():
                parts = parts[1:]
        else:
            if len(parts) < 2:
                continue
            brand = parts[0]
            parts = parts[1:]

        if not parts:
            continue

        type_value = parts[0]
        subtype = " ".join(parts[1:]).strip()
        display_name = f"{brand} {type_value}" + (f" {subtype}" if subtype else "")

        temps_key = _normalize_type_for_base(type_value, base_map)
        temps = base_map.get(temps_key) if temps_key else None

        entry: dict[str, object] = {
            "brand": brand,
            "type": type_value,
            "subtype": subtype,
            "display_name": display_name,
        }
        if temps:
            entry.update(
                {
                    "min_temp": temps.nozzle_low,
                    "max_temp": temps.nozzle_high,
                    "bed_min_temp": temps.bed_min,
                    "bed_max_temp": temps.bed_max,
                }
            )
        presets.append(entry)

    presets.sort(key=lambda p: str(p.get("display_name", "")).lower())
    out_path.write_text(json.dumps(presets, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"Wrote {len(presets)} presets to {out_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
