# OpenSpool Tag Writer (Android)

Small Android app to create an OpenSpool JSON payload and write it to an NTAG215/216 using NFC (NDEF `application/json`), with a basic color picker and a camera-based hex color scanner (gimmick-level).

## What it writes

The app writes one NDEF record:

- TNF: MIME media
- MIME type: `application/json`
- Payload: UTF-8 JSON like:

```json
{
  "protocol": "openspool",
  "version": "1.0",
  "brand": "Generic",
  "type": "PLA",
  "subtype": "Rapid",
  "color_hex": "#FF0000",
  "min_temp": 190,
  "max_temp": 220,
  "bed_min_temp": 50,
  "bed_max_temp": 60
}
```

Notes:

- `subtype` is optional; if left empty in the UI, it is omitted from the JSON.

## Build / run

This folder is a standalone Android Studio project.

1. Open `android/openspool-tag-writer` in Android Studio.
2. Let it download the Android SDK + Gradle deps.
3. Run on a phone with NFC (and camera for the scanner).

## Notes

- UID of the tag cannot be changed (factory programmed).
- If the tag is read-only / locked, writing will fail.
- Camera scanning is not color-calibrated; it samples the center region and averages RGB.
