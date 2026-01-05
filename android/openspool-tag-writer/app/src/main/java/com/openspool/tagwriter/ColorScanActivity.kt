package com.openspool.tagwriter

import android.Manifest
import android.app.Activity
import android.content.Intent
import android.content.pm.PackageManager
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.result.contract.ActivityResultContracts
import androidx.camera.core.CameraSelector
import androidx.camera.core.ImageAnalysis
import androidx.camera.lifecycle.ProcessCameraProvider
import androidx.camera.view.PreviewView
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.material3.Button
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.viewinterop.AndroidView
import androidx.compose.ui.unit.dp
import androidx.core.content.ContextCompat
import java.util.concurrent.Executors

class ColorScanActivity : ComponentActivity() {
    companion object {
        const val EXTRA_COLOR_HEX = "color_hex"
    }

    private val analysisExecutor = Executors.newSingleThreadExecutor()

    private val requestPermission =
        registerForActivityResult(ActivityResultContracts.RequestPermission()) { granted ->
            if (granted) {
                setContent { OpenSpoolTagWriterTheme { ScanScreen(onDone = ::returnColor, onCancel = ::finish) } }
            } else {
                setContent { OpenSpoolTagWriterTheme { PermissionDenied(onCancel = ::finish) } }
            }
        }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        val granted = ContextCompat.checkSelfPermission(this, Manifest.permission.CAMERA) == PackageManager.PERMISSION_GRANTED
        if (granted) {
            setContent { OpenSpoolTagWriterTheme { ScanScreen(onDone = ::returnColor, onCancel = ::finish) } }
        } else {
            requestPermission.launch(Manifest.permission.CAMERA)
        }
    }

    override fun onDestroy() {
        super.onDestroy()
        analysisExecutor.shutdown()
    }

    private fun returnColor(hex: String) {
        setResult(Activity.RESULT_OK, Intent().putExtra(EXTRA_COLOR_HEX, hex))
        finish()
    }

    @Composable
    private fun ScanScreen(onDone: (String) -> Unit, onCancel: () -> Unit) {
        var hex by remember { mutableStateOf("#FFFFFF") }
        var previewColor by remember { mutableStateOf(Color.White) }

        Surface(modifier = Modifier.fillMaxSize(), color = MaterialTheme.colorScheme.background) {
            Column(modifier = Modifier.fillMaxSize()) {
                Box(modifier = Modifier.weight(1f)) {
                    AndroidView(
                        modifier = Modifier.fillMaxSize(),
                        factory = { ctx ->
                            val previewView = PreviewView(ctx)
                            startCamera(
                                previewView = previewView,
                                onColor = { r, g, b ->
                                    val h = ColorUtils.rgbToHex(r, g, b)
                                    hex = h
                                    previewColor = Color(r, g, b)
                                },
                            )
                            previewView
                        },
                    )

                    Box(
                        modifier = Modifier
                            .align(Alignment.Center)
                            .size(90.dp),
                    ) {
                        Box(
                            modifier = Modifier
                                .align(Alignment.Center)
                                .size(2.dp)
                                .background(Color.White),
                        )
                    }
                }

                Surface(color = MaterialTheme.colorScheme.surface) {
                    Row(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(16.dp),
                        horizontalArrangement = Arrangement.spacedBy(12.dp),
                        verticalAlignment = Alignment.CenterVertically,
                    ) {
                        Box(modifier = Modifier.size(32.dp).background(previewColor))
                        Text(text = hex, style = MaterialTheme.typography.titleMedium, modifier = Modifier.weight(1f))
                        Button(onClick = { onDone(hex) }) { Text("Use") }
                        Button(onClick = onCancel) { Text("Back") }
                    }
                }
            }
        }
    }

    @Composable
    private fun PermissionDenied(onCancel: () -> Unit) {
        Surface(modifier = Modifier.fillMaxSize(), color = MaterialTheme.colorScheme.background) {
            Column(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(16.dp),
                verticalArrangement = Arrangement.spacedBy(12.dp),
            ) {
                Text("Camera permission denied.", style = MaterialTheme.typography.titleMedium)
                Text("Enable the camera permission to use the color scanner.")
                Button(onClick = onCancel) { Text("Back") }
            }
        }
    }

    private fun startCamera(previewView: PreviewView, onColor: (r: Int, g: Int, b: Int) -> Unit) {
        val cameraProviderFuture = ProcessCameraProvider.getInstance(this)
        cameraProviderFuture.addListener(
            {
                val cameraProvider = cameraProviderFuture.get()

                val preview = androidx.camera.core.Preview.Builder().build().also {
                    it.setSurfaceProvider(previewView.surfaceProvider)
                }

                val analysis =
                    ImageAnalysis.Builder()
                        .setBackpressureStrategy(ImageAnalysis.STRATEGY_KEEP_ONLY_LATEST)
                        .build()

                analysis.setAnalyzer(analysisExecutor) { image ->
                    val rgb = YuvColorSampler.sampleCenterAverageRgb(image, radiusPx = 12)
                    if (rgb != null) onColor(rgb[0], rgb[1], rgb[2])
                    image.close()
                }

                cameraProvider.unbindAll()
                cameraProvider.bindToLifecycle(this, CameraSelector.DEFAULT_BACK_CAMERA, preview, analysis)
            },
            ContextCompat.getMainExecutor(this),
        )
    }
}

