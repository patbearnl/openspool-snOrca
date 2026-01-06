package com.openspool.tagwriter

import androidx.compose.ui.graphics.Color
import kotlin.math.roundToInt

object ColorUtils {
    private val hexRegex = Regex("^#?[0-9a-fA-F]{6}$")
    private val hexFindRegex = Regex("#?[0-9a-fA-F]{6}")

    fun normalizeHex(input: String): String? {
        val trimmed = input.trim()
        if (!hexRegex.matches(trimmed)) return null
        val raw = trimmed.removePrefix("#").uppercase()
        return "#$raw"
    }

    fun hexToColor(hex: String): Color? {
        val normalized = normalizeHex(hex) ?: return null
        val rgb = normalized.removePrefix("#").toInt(16)
        val r = (rgb shr 16) and 0xFF
        val g = (rgb shr 8) and 0xFF
        val b = rgb and 0xFF
        return Color(r, g, b)
    }

    fun rgbToHex(r: Int, g: Int, b: Int): String {
        return "#%02X%02X%02X".format(r.coerceIn(0, 255), g.coerceIn(0, 255), b.coerceIn(0, 255))
    }

    fun colorToHex(color: Color): String {
        val r = (color.red * 255f).roundToInt()
        val g = (color.green * 255f).roundToInt()
        val b = (color.blue * 255f).roundToInt()
        return rgbToHex(r, g, b)
    }

    fun extractHexColors(input: String): List<String> {
        return hexFindRegex.findAll(input).mapNotNull { normalizeHex(it.value) }.toList()
    }
}
