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

class MainActivity : ComponentActivity() {
    private val nfcAdapter: NfcAdapter? by lazy { NfcAdapter.getDefaultAdapter(this) }

    private var currentPayloadJson: String = SpoolTagData(colorHex = "#FF0000").toJsonString()
    private var onWriteResult: ((WriteResult) -> Unit)? = null
    private var onColorPicked: ((String) -> Unit)? = null

    private val scanColorLauncher =
        registerForActivityResult(ActivityResultContracts.StartActivityForResult()) { result ->
            if (result.resultCode != Activity.RESULT_OK) return@registerForActivityResult
            val hex = result.data?.getStringExtra(ColorScanActivity.EXTRA_COLOR_HEX) ?: return@registerForActivityResult
            onColorPicked?.invoke(hex)
        }

    private val readerCallback = NfcAdapter.ReaderCallback { tag: Tag ->
        val json = currentPayloadJson
        val result = NdefWriter.writeJson(tag, json)
        runOnUiThread { onWriteResult?.invoke(result) }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContent {
            MaterialTheme {
                App(
                    onUpdatePayloadJson = { currentPayloadJson = it },
                    onRegisterWriteResultListener = { onWriteResult = it },
                    onLaunchScan = { scanColorLauncher.launch(Intent(this, ColorScanActivity::class.java)) },
                    onRegisterColorPickedListener = { onColorPicked = it },
                    nfcAvailable = (nfcAdapter != null),
                )
            }
        }
    }

    override fun onResume() {
        super.onResume()
        val adapter = nfcAdapter ?: return
        adapter.enableReaderMode(
            this,
            readerCallback,
            NfcAdapter.FLAG_READER_NFC_A,
            null,
        )
    }

    override fun onPause() {
        super.onPause()
        nfcAdapter?.disableReaderMode(this)
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun App(
    onUpdatePayloadJson: (String) -> Unit,
    onRegisterWriteResultListener: ((WriteResult) -> Unit) -> Unit,
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

    fun rebuildPayload() {
        val minTemp = minTempText.toIntOrNull() ?: 0
        val maxTemp = maxTempText.toIntOrNull() ?: 0
        val bedMinTemp = bedMinTempText.toIntOrNull() ?: 0
        val bedMaxTemp = bedMaxTempText.toIntOrNull() ?: 0
        val normalizedHex = ColorUtils.normalizeHex(colorHex) ?: "#FFFFFF"

        val payload = SpoolTagData(
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

    onRegisterWriteResultListener { result ->
        lastWriteStatus = when (result) {
            is WriteResult.Success -> "Wrote ${result.bytesWritten} bytes to tag"
            is WriteResult.Failure -> "Write failed: ${result.message}"
        }
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

    Scaffold(
        topBar = { TopAppBar(title = { Text("OpenSpool Tag Writer") }) },
    ) { padding ->
        Column(
            modifier = Modifier
                .padding(padding)
                .padding(16.dp)
                .verticalScroll(rememberScrollState()),
            verticalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            Text(
                text = if (nfcAvailable) "Tap an NTAG215/216 to write the current JSON." else "NFC not available on this device.",
                style = MaterialTheme.typography.bodyMedium,
            )

            if (lastWriteStatus != null) {
                Text(text = lastWriteStatus!!, color = MaterialTheme.colorScheme.primary)
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
            Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(12.dp)) {
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
            Text("Tap tag any time; it writes the current values.", style = MaterialTheme.typography.bodySmall)
        }
    }
}
