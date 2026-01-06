package com.openspool.tagwriter

import android.content.Context

object ImportSettings {
    private const val PREFS = "openspool_tag_writer_prefs"
    private const val KEY_BRAND_OVERRIDE = "import_brand_override"

    fun getBrandOverride(context: Context): String {
        return context.getSharedPreferences(PREFS, Context.MODE_PRIVATE).getString(KEY_BRAND_OVERRIDE, "").orEmpty()
    }

    fun setBrandOverride(context: Context, value: String) {
        context.getSharedPreferences(PREFS, Context.MODE_PRIVATE).edit().putString(KEY_BRAND_OVERRIDE, value).apply()
    }
}

