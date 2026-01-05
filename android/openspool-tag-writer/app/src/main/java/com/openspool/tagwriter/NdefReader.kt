package com.openspool.tagwriter

import android.nfc.Tag
import android.nfc.tech.Ndef
import java.io.IOException

sealed class ReadResult {
    data class Success(val json: String, val bytesRead: Int) : ReadResult()
    data class Failure(val message: String) : ReadResult()
}

object NdefReader {
    fun readJson(tag: Tag): ReadResult {
        val ndef = Ndef.get(tag) ?: return ReadResult.Failure("Tag is not NDEF compatible")
        try {
            ndef.connect()
            val message = ndef.ndefMessage ?: return ReadResult.Failure("No NDEF message found")
            val record =
                message.records.firstOrNull { it.tnf == android.nfc.NdefRecord.TNF_MIME_MEDIA } ?: return ReadResult.Failure(
                    "No MIME record found",
                )

            val mimeType = try {
                String(record.type, Charsets.US_ASCII)
            } catch (_: Exception) {
                ""
            }

            if (mimeType.lowercase() != "application/json") {
                return ReadResult.Failure("Unsupported MIME type: $mimeType")
            }

            val payload = record.payload ?: return ReadResult.Failure("Empty payload")
            val json = try {
                String(payload, Charsets.UTF_8)
            } catch (e: Exception) {
                return ReadResult.Failure("Invalid UTF-8 payload: ${e.message ?: e.javaClass.simpleName}")
            }

            return ReadResult.Success(json = json, bytesRead = payload.size)
        } catch (e: IOException) {
            return ReadResult.Failure("I/O error: ${e.message ?: e.javaClass.simpleName}")
        } finally {
            try {
                ndef.close()
            } catch (_: Exception) {
            }
        }
    }
}

