package com.openspool.tagwriter

import android.content.Context
import org.json.JSONArray
import org.json.JSONObject

object PresetRepository {
    private fun JSONObject.optIntOrNull(key: String): Int? {
        if (!has(key) || isNull(key)) return null
        return when (val v = opt(key)) {
            is Number -> v.toInt()
            is String -> v.trim().toIntOrNull()
            else -> null
        }
    }

    fun loadPresets(context: Context): List<Preset> {
        return try {
            val json = context.assets.open("presets.json").bufferedReader(Charsets.UTF_8).use { it.readText() }
            val arr = JSONArray(json)
            buildList {
                for (i in 0 until arr.length()) {
                    val obj = arr.getJSONObject(i)
                    add(
                        Preset(
                            brand = obj.optString("brand", "").trim(),
                            type = obj.optString("type", "").trim(),
                            subtype = obj.optString("subtype", "").trim(),
                            displayName = obj.optString("display_name", "").trim(),
                            minTemp = obj.optIntOrNull("min_temp"),
                            maxTemp = obj.optIntOrNull("max_temp"),
                            bedMinTemp = obj.optIntOrNull("bed_min_temp"),
                            bedMaxTemp = obj.optIntOrNull("bed_max_temp"),
                        ),
                    )
                }
            }.filter { it.brand.isNotBlank() && it.type.isNotBlank() }
        } catch (_: Exception) {
            emptyList()
        }
    }
}
