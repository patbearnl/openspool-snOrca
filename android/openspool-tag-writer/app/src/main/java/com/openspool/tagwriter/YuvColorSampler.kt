package com.openspool.tagwriter

import androidx.camera.core.ImageProxy
import kotlin.math.max
import kotlin.math.min

object YuvColorSampler {
    /**
     * Samples a (2*radius+1)^2 square around the center pixel, converts YUV->RGB, and returns average [r,g,b].
     * This is not color calibrated; it's meant as a gimmick.
     */
    fun sampleCenterAverageRgb(image: ImageProxy, radiusPx: Int): IntArray? {
        val yPlane = image.planes.getOrNull(0) ?: return null
        val uPlane = image.planes.getOrNull(1) ?: return null
        val vPlane = image.planes.getOrNull(2) ?: return null

        val width = image.width
        val height = image.height
        if (width <= 0 || height <= 0) return null

        val cx = width / 2
        val cy = height / 2
        val x0 = max(0, cx - radiusPx)
        val x1 = min(width - 1, cx + radiusPx)
        val y0 = max(0, cy - radiusPx)
        val y1 = min(height - 1, cy + radiusPx)

        val yBuf = yPlane.buffer
        val uBuf = uPlane.buffer
        val vBuf = vPlane.buffer

        val yRowStride = yPlane.rowStride
        val yPixelStride = yPlane.pixelStride
        val uRowStride = uPlane.rowStride
        val uPixelStride = uPlane.pixelStride
        val vRowStride = vPlane.rowStride
        val vPixelStride = vPlane.pixelStride

        var rSum = 0L
        var gSum = 0L
        var bSum = 0L
        var count = 0L

        for (y in y0..y1) {
            for (x in x0..x1) {
                val yIndex = y * yRowStride + x * yPixelStride
                val uvX = x / 2
                val uvY = y / 2
                val uIndex = uvY * uRowStride + uvX * uPixelStride
                val vIndex = uvY * vRowStride + uvX * vPixelStride

                val yValue = (yBuf.get(yIndex).toInt() and 0xFF)
                val uValue = (uBuf.get(uIndex).toInt() and 0xFF)
                val vValue = (vBuf.get(vIndex).toInt() and 0xFF)

                val rgb = yuvToRgb(yValue, uValue, vValue)
                rSum += rgb[0]
                gSum += rgb[1]
                bSum += rgb[2]
                count++
            }
        }

        if (count == 0L) return null
        return intArrayOf(
            (rSum / count).toInt(),
            (gSum / count).toInt(),
            (bSum / count).toInt(),
        )
    }

    // Basic BT.601 conversion, full range-ish; good enough for a gimmick picker.
    private fun yuvToRgb(y: Int, u: Int, v: Int): IntArray {
        val yF = y.toFloat()
        val uF = (u - 128).toFloat()
        val vF = (v - 128).toFloat()

        var r = yF + 1.402f * vF
        var g = yF - 0.344136f * uF - 0.714136f * vF
        var b = yF + 1.772f * uF

        r = r.coerceIn(0f, 255f)
        g = g.coerceIn(0f, 255f)
        b = b.coerceIn(0f, 255f)

        return intArrayOf(r.toInt(), g.toInt(), b.toInt())
    }
}

