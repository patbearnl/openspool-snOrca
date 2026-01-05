package com.openspool.tagwriter

import android.nfc.FormatException
import android.nfc.NdefMessage
import android.nfc.NdefRecord
import android.nfc.Tag
import android.nfc.tech.Ndef
import android.nfc.tech.NdefFormatable
import java.io.IOException

sealed class WriteResult {
    data class Success(val bytesWritten: Int) : WriteResult()
    data class Failure(val message: String) : WriteResult()
}

object NdefWriter {
    fun writeJson(tag: Tag, json: String): WriteResult {
        val payload = json.toByteArray(Charsets.UTF_8)
        val message = NdefMessage(arrayOf(NdefRecord.createMime("application/json", payload)))
        val messageBytes = message.toByteArray()

        val ndef = Ndef.get(tag)
        if (ndef != null) {
            try {
                ndef.connect()
                if (!ndef.isWritable) return WriteResult.Failure("Tag is read-only")
                if (messageBytes.size > ndef.maxSize) return WriteResult.Failure("Too large for tag (${messageBytes.size} > ${ndef.maxSize})")
                ndef.writeNdefMessage(message)
                return WriteResult.Success(messageBytes.size)
            } catch (e: IOException) {
                return WriteResult.Failure("I/O error: ${e.message ?: e.javaClass.simpleName}")
            } catch (e: FormatException) {
                return WriteResult.Failure("Format error: ${e.message ?: e.javaClass.simpleName}")
            } finally {
                try {
                    ndef.close()
                } catch (_: Exception) {
                }
            }
        }

        val formatable = NdefFormatable.get(tag) ?: return WriteResult.Failure("Tag is not NDEF compatible")
        try {
            formatable.connect()
            formatable.format(message)
            return WriteResult.Success(messageBytes.size)
        } catch (e: IOException) {
            return WriteResult.Failure("I/O error: ${e.message ?: e.javaClass.simpleName}")
        } catch (e: FormatException) {
            return WriteResult.Failure("Format error: ${e.message ?: e.javaClass.simpleName}")
        } finally {
            try {
                formatable.close()
            } catch (_: Exception) {
            }
        }
    }
}

