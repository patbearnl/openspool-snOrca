package com.openspool.tagwriter

import android.content.Context
import org.json.JSONArray
import org.json.JSONObject
import java.io.File

object SpoolLibrary {
    private const val FILE_NAME = "spool_library.json"

    fun load(context: Context): List<ImportedSpool> {
        return runCatching {
            val file = file(context)
            if (!file.exists()) return emptyList()
            val text = file.readText(Charsets.UTF_8).trim()
            if (text.isBlank()) return emptyList()

            val arr = JSONArray(text)
            buildList {
                for (i in 0 until arr.length()) {
                    val obj = arr.optJSONObject(i) ?: continue
                    val id = obj.optString("id", "").trim()
                    val displayName = obj.optString("display_name", "").trim()
                    val applyOverride = obj.optBoolean("apply_brand_override_on_write", true)
                    val tagObj = obj.optJSONObject("tag") ?: continue
                    val tag = SpoolTagData.fromJsonString(tagObj.toString()).getOrNull() ?: continue
                    if (id.isBlank() || displayName.isBlank()) continue
                    add(
                        ImportedSpool(
                            id = id,
                            displayName = displayName,
                            tagData = tag,
                            applyBrandOverrideOnWrite = applyOverride,
                        ),
                    )
                }
            }
        }.getOrElse { emptyList() }
    }

    fun save(context: Context, spools: List<ImportedSpool>) {
        runCatching {
            val arr = JSONArray()
            spools.forEach { spool ->
                val obj = JSONObject()
                obj.put("id", spool.id)
                obj.put("display_name", spool.displayName)
                obj.put("apply_brand_override_on_write", spool.applyBrandOverrideOnWrite)
                obj.put("tag", JSONObject(spool.tagData.toJsonString()))
                arr.put(obj)
            }
            file(context).writeText(arr.toString(2) + "\n", Charsets.UTF_8)
        }
    }

    fun clear(context: Context) {
        runCatching { file(context).delete() }
    }

    private fun file(context: Context): File = File(context.filesDir, FILE_NAME)
}
