package com.openspool.tagwriter

import android.content.Context
import org.json.JSONArray

object PresetRepository {
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
                        ),
                    )
                }
            }.filter { it.brand.isNotBlank() && it.type.isNotBlank() }
        } catch (_: Exception) {
            emptyList()
        }
    }
}

