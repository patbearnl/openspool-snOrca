package com.openspool.tagwriter

import org.json.JSONArray
import org.json.JSONObject

data class ImportedSpool(
    val id: String,
    val displayName: String,
    val tagData: SpoolTagData,
    val applyBrandOverrideOnWrite: Boolean = true,
)

object SpoolImport {
    /**
     * Supports:
     * - OpenSpool single JSON object
     * - 3dfilamentprofiles.com "my-spools.json" export (array of objects)
     * - Spoolman export (array of objects)
     */
    fun importFromJson(
        json: String,
        defaultTempsProvider: (type: String) -> Temps?,
    ): ImportResult {
        val trimmed = json.trim()
        if (trimmed.isEmpty()) return ImportResult.Error("Empty file")

        // Try OpenSpool object first.
        SpoolTagData.fromJsonString(trimmed).getOrNull()?.let { tag ->
            if (tag.protocol.equals("openspool", ignoreCase = true)) {
                val defaultTemps = defaultTempsProvider(tag.type)
                val patched =
                    tag.copy(
                        minTemp = defaultTemps?.minTemp ?: tag.minTemp,
                        maxTemp = defaultTemps?.maxTemp ?: tag.maxTemp,
                        bedMinTemp = defaultTemps?.bedMinTemp ?: tag.bedMinTemp,
                        bedMaxTemp = defaultTemps?.bedMaxTemp ?: tag.bedMaxTemp,
                    )
                return ImportResult.Single(patched)
            }
        }

        // Try 3DFP array export.
        return runCatching {
            val arr = JSONArray(trimmed)
            val spools =
                buildList {
                    for (i in 0 until arr.length()) {
                        val obj = arr.optJSONObject(i) ?: continue
                        val spool =
                            from3dfpObject(obj, defaultTempsProvider)
                                ?: fromSpoolmanObject(obj, defaultTempsProvider)
                        if (spool != null) add(spool)
                    }
                }
            if (spools.isEmpty()) ImportResult.Error("No usable spools found in JSON")
            else ImportResult.Multiple(spools)
        }.getOrElse {
            ImportResult.Error("Unsupported JSON format")
        }
    }

    /**
     * Supports 3dfilamentprofiles.com "my-spools.csv" export.
     */
    fun importFromCsv(
        csv: String,
        defaultTempsProvider: (type: String) -> Temps?,
    ): ImportResult {
        val rows = CsvUtils.parseCsv(csv)
        if (rows.isEmpty()) return ImportResult.Error("No rows found in CSV")

        val spools =
            rows.mapNotNull { row ->
                val obj = JSONObject(row)
                from3dfpObject(obj, defaultTempsProvider)
            }

        return if (spools.isEmpty()) ImportResult.Error("No usable spools found in CSV") else ImportResult.Multiple(spools)
    }

    private fun from3dfpObject(
        obj: JSONObject,
        defaultTempsProvider: (type: String) -> Temps?,
    ): ImportedSpool? {
        val id = obj.optString("id", "").trim()
        val brand = obj.optString("brand", "").trim()
        val material = obj.optString("material", "").trim()
        val materialType = obj.optString("material_type", "").trim()
        val colorName = obj.optString("color", "").trim()
        val rgbRaw = obj.optString("rgb", "").trim()

        if (brand.isBlank() || material.isBlank()) return null

        val type = OpenSpoolTypeMapper.normalize(material) ?: material
        val colors = ColorUtils.extractHexColors(rgbRaw)
        val colorHex = colors.firstOrNull() ?: "#FFFFFF"
        val multiColorHexes = if (colors.size > 1) colors.drop(1) else emptyList()

        val subtype =
            when {
                materialType.equals("basic", ignoreCase = true) -> ""
                materialType.isBlank() -> material.takeIf { !it.equals(type, ignoreCase = true) }.orEmpty()
                material.equals(type, ignoreCase = true) -> materialType
                else -> "$material $materialType"
            }

        val temps = defaultTempsProvider(type)
        val tag =
            SpoolTagData(
                brand = brand,
                type = type,
                subtype = subtype,
                colorHex = colorHex,
                multiColorHexes = multiColorHexes,
                minTemp = temps?.minTemp ?: 190,
                maxTemp = temps?.maxTemp ?: 220,
                bedMinTemp = temps?.bedMinTemp ?: 50,
                bedMaxTemp = temps?.bedMaxTemp ?: 60,
            )

        val idSuffix = id.takeLast(6).ifBlank { "------" }
        val nameParts =
            listOf(
                brand,
                type,
                subtype.takeIf { it.isNotBlank() },
                colorName.takeIf { it.isNotBlank() },
                "#$idSuffix",
            ).filterNotNull()

        return ImportedSpool(
            id = id.ifBlank { idSuffix },
            displayName = nameParts.joinToString(" "),
            tagData = tag,
        )
    }

    /**
     * Supports Spoolman JSON exports like:
     * {
     *   "id": "...",
     *   "manufacturer": "3D-Fuel",
     *   "name": "Autumn Orange",
     *   "material": "PLA+",
     *   "color_hex": "F76647",
     *   "color_hexes": ["#AABBCC", "#DDEEFF"],
     *   "extruder_temp": 220,
     *   "extruder_temp_range": [210, 230],
     *   "bed_temp": 60,
     *   "bed_temp_range": [50, 70],
     *   "finish": "Matte"
     * }
     */
    private fun fromSpoolmanObject(
        obj: JSONObject,
        defaultTempsProvider: (type: String) -> Temps?,
    ): ImportedSpool? {
        val id = obj.optString("id", "").trim()
        val manufacturer = obj.optString("manufacturer", "").trim()
        val material = obj.optString("material", "").trim()
        val name = obj.optString("name", "").trim()
        val finish = obj.optString("finish", "").trim()

        if (manufacturer.isBlank() || material.isBlank()) return null

        val type = OpenSpoolTypeMapper.normalize(material) ?: material

        val (colorHex, multiColorHexes) =
            run {
                val hexes = obj.optJSONArray("color_hexes")
                val parsedHexes =
                    if (hexes != null && hexes.length() > 0) {
                        buildList {
                            for (i in 0 until hexes.length()) {
                                val hex = ColorUtils.normalizeHex(hexes.optString(i, "")) ?: continue
                                add(hex)
                            }
                        }
                    } else {
                        emptyList()
                    }

                val primary =
                    parsedHexes.firstOrNull()
                        ?: ColorUtils.normalizeHex(obj.optString("color_hex", ""))
                        ?: "#FFFFFF"
                val extras = if (parsedHexes.size > 1) parsedHexes.drop(1) else emptyList()
                primary to extras
            }

        val subtype =
            when {
                finish.isNotBlank() -> finish
                else -> ""
            }

        fun rangeToPair(arr: JSONArray?): Pair<Int?, Int?> {
            if (arr == null || arr.length() < 2) return null to null
            val lo = arr.optInt(0, Int.MIN_VALUE).takeIf { it != Int.MIN_VALUE }
            val hi = arr.optInt(1, Int.MIN_VALUE).takeIf { it != Int.MIN_VALUE }
            return lo to hi
        }

        val (nozzleLo, nozzleHi) = rangeToPair(obj.optJSONArray("extruder_temp_range"))
        val (bedLo, bedHi) = rangeToPair(obj.optJSONArray("bed_temp_range"))

        val defaults = defaultTempsProvider(type)

        val minTemp = nozzleLo ?: obj.optInt("extruder_temp", defaults?.minTemp ?: 190)
        val maxTemp = nozzleHi ?: obj.optInt("extruder_temp", defaults?.maxTemp ?: 220)
        val bedMinTemp = bedLo ?: obj.optInt("bed_temp", defaults?.bedMinTemp ?: 50)
        val bedMaxTemp = bedHi ?: obj.optInt("bed_temp", defaults?.bedMaxTemp ?: 60)

        val tag =
            SpoolTagData(
                brand = manufacturer,
                type = type,
                subtype = subtype,
                colorHex = colorHex,
                multiColorHexes = multiColorHexes,
                minTemp = minTemp,
                maxTemp = maxTemp,
                bedMinTemp = bedMinTemp,
                bedMaxTemp = bedMaxTemp,
            )

        val idSuffix = id.takeLast(6).ifBlank { "------" }
        val nameParts =
            listOf(
                manufacturer,
                type,
                subtype.takeIf { it.isNotBlank() },
                name.takeIf { it.isNotBlank() },
                "#$idSuffix",
            ).filterNotNull()

        return ImportedSpool(
            id = id.ifBlank { idSuffix },
            displayName = nameParts.joinToString(" "),
            tagData = tag,
        )
    }
}

sealed class ImportResult {
    data class Single(val tagData: SpoolTagData) : ImportResult()

    data class Multiple(val spools: List<ImportedSpool>) : ImportResult()

    data class Error(val message: String) : ImportResult()
}

data class Temps(
    val minTemp: Int,
    val maxTemp: Int,
    val bedMinTemp: Int,
    val bedMaxTemp: Int,
)

object OpenSpoolTypeMapper {
    private val supportedTypes =
        setOf(
            "PLA",
            "ABS",
            "ASA",
            "ASA-CF",
            "PETG",
            "PCTG",
            "TPU",
            "TPU-AMS",
            "PC",
            "PA",
            "PA-CF",
            "PA-GF",
            "PA6-CF",
            "PLA-CF",
            "PET-CF",
            "PETG-CF",
            "PVA",
            "HIPS",
            "PLA-AERO",
            "PPS",
            "PPS-CF",
            "PPA-CF",
            "PPA-GF",
            "ABS-GF",
            "ASA-AERO",
            "PE",
            "PP",
            "EVA",
            "PHA",
            "BVOH",
            "PE-CF",
            "PP-CF",
            "PP-GF",
        )

    fun normalize(material: String): String? {
        val m = material.trim()
        if (m.isBlank()) return null

        val u = m.uppercase()

        // Exact known OpenSpool types (including CF/GF/AERO variants).
        val compact = u.replace("_", "-").replace(" ", "")
        if (supportedTypes.contains(compact)) return compact

        // Handle common vendor naming.
        if (compact.startsWith("PLA+")) return "PLA"
        if (compact == "PLA+/PRO" || compact == "PLA/PRO") return "PLA"
        if (compact.startsWith("PLA")) return "PLA"

        if (compact.startsWith("PETG") || compact.startsWith("PET")) return "PETG"
        if (compact.startsWith("PCTG")) return "PCTG"
        if (compact.startsWith("ABS")) return "ABS"
        if (compact.startsWith("ASA")) return "ASA"
        if (compact.startsWith("TPU")) return "TPU"
        if (compact.startsWith("PC")) return "PC"
        if (compact.startsWith("PVA")) return "PVA"
        if (compact.startsWith("BVOH")) return "BVOH"
        if (compact.startsWith("HIPS")) return "HIPS"
        if (compact.startsWith("PA6")) return "PA"
        if (compact.startsWith("PA")) return "PA"
        if (compact.startsWith("PPS")) return "PPS"
        if (compact.startsWith("PPA")) return "PPA-CF"

        return null
    }
}

object CsvUtils {
    fun parseCsv(csv: String): List<Map<String, String>> {
        val lines =
            csv.lineSequence()
                .map { it.trimEnd() }
                .filter { it.isNotBlank() }
                .toList()
        if (lines.isEmpty()) return emptyList()

        val header = parseLine(lines.first()).map { it.trim() }
        if (header.isEmpty()) return emptyList()

        return buildList {
            for (line in lines.drop(1)) {
                val values = parseLine(line)
                if (values.isEmpty()) continue
                val row =
                    buildMap {
                        for (i in header.indices) {
                            put(header[i], values.getOrNull(i).orEmpty())
                        }
                    }
                add(row)
            }
        }
    }

    private fun parseLine(line: String): List<String> {
        val out = ArrayList<String>()
        val sb = StringBuilder()
        var inQuotes = false
        var i = 0
        while (i < line.length) {
            val ch = line[i]
            when (ch) {
                '"' -> {
                    val next = line.getOrNull(i + 1)
                    if (inQuotes && next == '"') {
                        sb.append('"')
                        i++
                    } else {
                        inQuotes = !inQuotes
                    }
                }

                ',' -> {
                    if (inQuotes) sb.append(ch)
                    else {
                        out.add(sb.toString())
                        sb.setLength(0)
                    }
                }

                else -> sb.append(ch)
            }
            i++
        }
        out.add(sb.toString())
        return out
    }
}
