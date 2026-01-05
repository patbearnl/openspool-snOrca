package com.openspool.tagwriter

import android.app.Activity
import android.content.Intent
import android.nfc.NfcAdapter
import android.nfc.Tag
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Slider
import androidx.compose.material3.Surface
import androidx.compose.material3.Tab
import androidx.compose.material3.TabRow
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp

private enum class NfcMode {
    WRITE,
    READ,
}

class MainActivity : ComponentActivity() {
    private val nfcAdapter: NfcAdapter? by lazy { NfcAdapter.getDefaultAdapter(this) }

    private var currentMode: NfcMode = NfcMode.WRITE
    private var nfcArmed: Boolean = false

    private var currentPayloadJson: String = SpoolTagData(colorHex = "#FF0000").toJsonString()
    private var onWriteResult: ((WriteResult) -> Unit)? = null
    private var onReadResult: ((ReadResult) -> Unit)? = null
    private var onTagDataLoaded: ((SpoolTagData) -> Unit)? = null
    private var onColorPicked: ((String) -> Unit)? = null

    private val scanColorLauncher =
        registerForActivityResult(ActivityResultContracts.StartActivityForResult()) { result ->
            if (result.resultCode != Activity.RESULT_OK) return@registerForActivityResult
            val hex = result.data?.getStringExtra(ColorScanActivity.EXTRA_COLOR_HEX) ?: return@registerForActivityResult
            onColorPicked?.invoke(hex)
        }

    private val readerCallback = NfcAdapter.ReaderCallback { tag: Tag ->
        when (currentMode) {
            NfcMode.WRITE -> {
                val json = currentPayloadJson
                val result = NdefWriter.writeJson(tag, json)
                runOnUiThread { onWriteResult?.invoke(result) }
            }

            NfcMode.READ -> {
                val result = NdefReader.readJson(tag)
                runOnUiThread { onReadResult?.invoke(result) }

                if (result is ReadResult.Success) {
                    val parsed = SpoolTagData.fromJsonString(result.json).getOrNull() ?: return@ReaderCallback
                    runOnUiThread { onTagDataLoaded?.invoke(parsed) }
                }
            }
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContent {
            OpenSpoolTagWriterTheme {
                App(
                    onUpdatePayloadJson = { currentPayloadJson = it },
                    onModeChanged = { currentMode = it },
                    onArmChanged = {
                        nfcArmed = it
                        updateReaderMode()
                    },
                    onRegisterWriteResultListener = { onWriteResult = it },
                    onRegisterReadResultListener = { onReadResult = it },
                    onRegisterTagDataLoadedListener = { onTagDataLoaded = it },
                    onLaunchScan = { scanColorLauncher.launch(Intent(this, ColorScanActivity::class.java)) },
                    onRegisterColorPickedListener = { onColorPicked = it },
                    nfcAvailable = (nfcAdapter != null),
                )
            }
        }
    }

    override fun onResume() {
        super.onResume()
        updateReaderMode()
    }

    override fun onPause() {
        super.onPause()
        nfcAdapter?.disableReaderMode(this)
    }

    private fun updateReaderMode() {
        val adapter = nfcAdapter ?: return
        if (nfcArmed) {
            adapter.enableReaderMode(
                this,
                readerCallback,
                NfcAdapter.FLAG_READER_NFC_A,
                null,
            )
        } else {
            adapter.disableReaderMode(this)
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun App(
    onUpdatePayloadJson: (String) -> Unit,
    onModeChanged: (NfcMode) -> Unit,
    onArmChanged: (Boolean) -> Unit,
    onRegisterWriteResultListener: ((WriteResult) -> Unit) -> Unit,
    onRegisterReadResultListener: ((ReadResult) -> Unit) -> Unit,
    onRegisterTagDataLoadedListener: ((SpoolTagData) -> Unit) -> Unit,
    onLaunchScan: () -> Unit,
    onRegisterColorPickedListener: ((String) -> Unit) -> Unit,
    nfcAvailable: Boolean,
) {
    var brand by remember { mutableStateOf("Generic") }
    var type by remember { mutableStateOf("PLA") }
    var subtype by remember { mutableStateOf("") }

    var minTempText by remember { mutableStateOf("190") }
    var maxTempText by remember { mutableStateOf("220") }
    var bedMinTempText by remember { mutableStateOf("50") }
    var bedMaxTempText by remember { mutableStateOf("60") }

    var red by remember { mutableIntStateOf(255) }
    var green by remember { mutableIntStateOf(0) }
    var blue by remember { mutableIntStateOf(0) }

    var colorHex by remember { mutableStateOf("#FF0000") }
    var lastWriteStatus by remember { mutableStateOf<String?>(null) }
    var lastReadStatus by remember { mutableStateOf<String?>(null) }

    var nfcMode by remember { mutableStateOf(NfcMode.WRITE) }
    var isArmed by remember { mutableStateOf(false) }

    fun rebuildPayload() {
        val minTemp = minTempText.toIntOrNull() ?: 0
        val maxTemp = maxTempText.toIntOrNull() ?: 0
        val bedMinTemp = bedMinTempText.toIntOrNull() ?: 0
        val bedMaxTemp = bedMaxTempText.toIntOrNull() ?: 0
        val normalizedHex = ColorUtils.normalizeHex(colorHex) ?: "#FFFFFF"

        val payload =
            SpoolTagData(
                brand = brand.trim().ifEmpty { "Generic" },
                type = type.trim().ifEmpty { "PLA" },
                subtype = subtype.trim(),
                colorHex = normalizedHex,
                minTemp = minTemp,
                maxTemp = maxTemp,
                bedMinTemp = bedMinTemp,
                bedMaxTemp = bedMaxTemp,
            )
        onUpdatePayloadJson(payload.toJsonString())
    }

    fun applyTagData(tag: SpoolTagData) {
        brand = tag.brand
        type = tag.type
        subtype = tag.subtype

        minTempText = tag.minTemp.toString()
        maxTempText = tag.maxTemp.toString()
        bedMinTempText = tag.bedMinTemp.toString()
        bedMaxTempText = tag.bedMaxTemp.toString()

        colorHex = tag.colorHex
        ColorUtils.hexToColor(tag.colorHex)?.let { c ->
            red = (c.red * 255f).toInt().coerceIn(0, 255)
            green = (c.green * 255f).toInt().coerceIn(0, 255)
            blue = (c.blue * 255f).toInt().coerceIn(0, 255)
        }

        rebuildPayload()
    }

    onRegisterWriteResultListener { result ->
        lastWriteStatus =
            when (result) {
                is WriteResult.Success -> "Wrote ${result.bytesWritten} bytes ✅"
                is WriteResult.Failure -> "Write failed: ${result.message}"
            }
    }

    onRegisterReadResultListener { result ->
        lastReadStatus =
            when (result) {
                is ReadResult.Success -> "Read ${result.bytesRead} bytes ✅"
                is ReadResult.Failure -> "Read failed: ${result.message}"
            }
    }

    onRegisterTagDataLoadedListener { tag ->
        applyTagData(tag)
    }

    onRegisterColorPickedListener { picked ->
        val normalized = ColorUtils.normalizeHex(picked) ?: return@onRegisterColorPickedListener
        colorHex = normalized
        ColorUtils.hexToColor(normalized)?.let { c ->
            red = (c.red * 255f).toInt()
            green = (c.green * 255f).toInt()
            blue = (c.blue * 255f).toInt()
        }
        rebuildPayload()
    }

    val previewColor = Color(red, green, blue)
    rebuildPayload()

    Scaffold(topBar = { TopAppBar(title = { Text("snOrca OpenSpool Ntag Writer") }) }) { padding ->
        Surface(
            modifier = Modifier.fillMaxSize(),
            color = MaterialTheme.colorScheme.background,
        ) {
            Column(modifier = Modifier.padding(padding)) {
                TabRow(selectedTabIndex = if (nfcMode == NfcMode.WRITE) 0 else 1) {
                    Tab(
                        selected = nfcMode == NfcMode.WRITE,
                        onClick = { nfcMode = NfcMode.WRITE; onModeChanged(NfcMode.WRITE) },
                        text = { Text("Write") },
                    )
                    Tab(
                        selected = nfcMode == NfcMode.READ,
                        onClick = { nfcMode = NfcMode.READ; onModeChanged(NfcMode.READ) },
                        text = { Text("Read") },
                    )
                }

                Column(
                    modifier = Modifier
                        .padding(16.dp)
                        .verticalScroll(rememberScrollState()),
                    verticalArrangement = Arrangement.spacedBy(12.dp),
                ) {
                    Text(
                        text =
                            if (!nfcAvailable) {
                                "NFC not available on this device."
                            } else {
                                if (isArmed) {
                                    if (nfcMode == NfcMode.WRITE) {
                                        "NFC armed (WRITE). Tap a tag to write the current values."
                                    } else {
                                        "NFC armed (READ). Tap a tag to load values into the form."
                                    }
                                } else {
                                    "NFC is off. Press Start to arm NFC."
                                }
                            },
                        style = MaterialTheme.typography.bodyMedium,
                    )

                    Row(horizontalArrangement = Arrangement.spacedBy(12.dp), modifier = Modifier.fillMaxWidth()) {
                        Button(
                            onClick = {
                                isArmed = !isArmed
                                onArmChanged(isArmed)
                            },
                            enabled = nfcAvailable,
                            modifier = Modifier.weight(1f),
                        ) {
                            Text(if (isArmed) "Stop NFC" else "Start NFC")
                        }
                    }

                    if (lastWriteStatus != null) {
                        Text(text = lastWriteStatus!!, color = MaterialTheme.colorScheme.primary)
                    }
                    if (lastReadStatus != null) {
                        Text(text = lastReadStatus!!, color = MaterialTheme.colorScheme.primary)
                    }

                    OutlinedTextField(
                        value = brand,
                        onValueChange = { brand = it; rebuildPayload() },
                        label = { Text("Brand") },
                        modifier = Modifier.fillMaxWidth(),
                    )
                    OutlinedTextField(
                        value = type,
                        onValueChange = { type = it; rebuildPayload() },
                        label = { Text("Type") },
                        modifier = Modifier.fillMaxWidth(),
                    )
                    OutlinedTextField(
                        value = subtype,
                        onValueChange = { subtype = it; rebuildPayload() },
                        label = { Text("Subtype (optional)") },
                        modifier = Modifier.fillMaxWidth(),
                    )

                    Row(horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                        OutlinedTextField(
                            value = minTempText,
                            onValueChange = { minTempText = it; rebuildPayload() },
                            label = { Text("Min nozzle °C") },
                            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                            modifier = Modifier.weight(1f),
                        )
                        OutlinedTextField(
                            value = maxTempText,
                            onValueChange = { maxTempText = it; rebuildPayload() },
                            label = { Text("Max nozzle °C") },
                            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                            modifier = Modifier.weight(1f),
                        )
                    }
                    Row(horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                        OutlinedTextField(
                            value = bedMinTempText,
                            onValueChange = { bedMinTempText = it; rebuildPayload() },
                            label = { Text("Min bed °C") },
                            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                            modifier = Modifier.weight(1f),
                        )
                        OutlinedTextField(
                            value = bedMaxTempText,
                            onValueChange = { bedMaxTempText = it; rebuildPayload() },
                            label = { Text("Max bed °C") },
                            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                            modifier = Modifier.weight(1f),
                        )
                    }

                    Text("Color", style = MaterialTheme.typography.titleMedium)
                    Row(
                        verticalAlignment = Alignment.CenterVertically,
                        horizontalArrangement = Arrangement.spacedBy(12.dp),
                    ) {
                        Box(
                            modifier = Modifier
                                .size(42.dp)
                                .background(previewColor),
                        )
                        OutlinedTextField(
                            value = colorHex,
                            onValueChange = {
                                colorHex = it
                                val normalized = ColorUtils.normalizeHex(it)
                                if (normalized != null) {
                                    ColorUtils.hexToColor(normalized)?.let { c ->
                                        red = (c.red * 255f).toInt()
                                        green = (c.green * 255f).toInt()
                                        blue = (c.blue * 255f).toInt()
                                    }
                                    rebuildPayload()
                                }
                            },
                            label = { Text("Hex (#RRGGBB)") },
                            modifier = Modifier.weight(1f),
                        )
                    }

                    Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                        Text("R: $red")
                        Slider(
                            value = red.toFloat(),
                            onValueChange = {
                                red = it.toInt()
                                colorHex = ColorUtils.rgbToHex(red, green, blue)
                                rebuildPayload()
                            },
                            valueRange = 0f..255f,
                        )
                        Text("G: $green")
                        Slider(
                            value = green.toFloat(),
                            onValueChange = {
                                green = it.toInt()
                                colorHex = ColorUtils.rgbToHex(red, green, blue)
                                rebuildPayload()
                            },
                            valueRange = 0f..255f,
                        )
                        Text("B: $blue")
                        Slider(
                            value = blue.toFloat(),
                            onValueChange = {
                                blue = it.toInt()
                                colorHex = ColorUtils.rgbToHex(red, green, blue)
                                rebuildPayload()
                            },
                            valueRange = 0f..255f,
                        )
                    }

                    Row(horizontalArrangement = Arrangement.spacedBy(12.dp), modifier = Modifier.fillMaxWidth()) {
                        Button(onClick = onLaunchScan, modifier = Modifier.weight(1f)) {
                            Text("Scan color (camera)")
                        }
                        Button(
                            onClick = {
                                brand = "Generic"
                                type = "PLA"
                                subtype = ""
                                minTempText = "190"
                                maxTempText = "220"
                                bedMinTempText = "50"
                                bedMaxTempText = "60"
                                red = 255; green = 0; blue = 0
                                colorHex = "#FF0000"
                                rebuildPayload()
                            },
                            modifier = Modifier.weight(1f),
                        ) {
                            Text("Reset")
                        }
                    }

                    Spacer(modifier = Modifier.height(8.dp))
                    Text(
                        text =
                            if (nfcMode == NfcMode.WRITE) {
                                "Tip: Switch to READ to load an existing tag, edit values, then WRITE to save."
                            } else {
                                "Tip: Tap a tag to read it, edit values, then switch to WRITE to save."
                            },
                        style = MaterialTheme.typography.bodySmall,
                    )
                }
            }
        }
    }
}

