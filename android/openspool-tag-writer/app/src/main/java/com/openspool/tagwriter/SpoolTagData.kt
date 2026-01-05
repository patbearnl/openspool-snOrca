package com.openspool.tagwriter

import org.json.JSONObject

data class SpoolTagData(
    val protocol: String = "openspool",
    val version: String = "1.0",
    val brand: String = "Generic",
    val type: String = "PLA",
    val subtype: String = "",
    val colorHex: String = "#FFFFFF",
    val minTemp: Int = 190,
    val maxTemp: Int = 220,
    val bedMinTemp: Int = 50,
    val bedMaxTemp: Int = 60,
) {
    companion object {
        fun fromJsonString(json: String): Result<SpoolTagData> {
            return runCatching {
                val obj = JSONObject(json)

                val protocol = obj.optString("protocol", "openspool")
                val version = obj.optString("version", "1.0")
                val brand = obj.optString("brand", "Generic")
                val type = obj.optString("type", "PLA")
                val subtype = obj.optString("subtype", "")
                val colorHexRaw = obj.optString("color_hex", "#FFFFFF")
                val colorHex = ColorUtils.normalizeHex(colorHexRaw) ?: "#FFFFFF"

                val minTemp = obj.optInt("min_temp", 190)
                val maxTemp = obj.optInt("max_temp", 220)
                val bedMinTemp = obj.optInt("bed_min_temp", 50)
                val bedMaxTemp = obj.optInt("bed_max_temp", 60)

                SpoolTagData(
                    protocol = protocol,
                    version = version,
                    brand = brand,
                    type = type,
                    subtype = subtype,
                    colorHex = colorHex,
                    minTemp = minTemp,
                    maxTemp = maxTemp,
                    bedMinTemp = bedMinTemp,
                    bedMaxTemp = bedMaxTemp,
                )
            }
        }
    }

    fun toJsonString(): String {
        val obj = JSONObject()
        obj.put("protocol", protocol)
        obj.put("version", version)
        obj.put("brand", brand)
        obj.put("type", type)
        if (subtype.isNotBlank()) obj.put("subtype", subtype)
        obj.put("color_hex", colorHex)
        obj.put("min_temp", minTemp)
        obj.put("max_temp", maxTemp)
        obj.put("bed_min_temp", bedMinTemp)
        obj.put("bed_max_temp", bedMaxTemp)
        return obj.toString()
    }
}
