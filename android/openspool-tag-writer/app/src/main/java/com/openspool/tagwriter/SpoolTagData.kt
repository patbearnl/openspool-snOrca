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
