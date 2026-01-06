package com.openspool.tagwriter

data class Preset(
    val brand: String,
    val type: String,
    val subtype: String,
    val displayName: String,
    val minTemp: Int? = null,
    val maxTemp: Int? = null,
    val bedMinTemp: Int? = null,
    val bedMaxTemp: Int? = null,
)
