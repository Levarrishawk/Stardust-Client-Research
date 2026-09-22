# Block 5: Instant-Message Send Result

Authority: `C:\Stardust-DEV\Stardust.exe` (SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`)

`ChatOnSendInstantMessage` / `0x88DBB381` is confirmed at listener branch `0x009005F8`, parser `0x00AF2290`, and acknowledgement handling region `0x00900628..0x00900896`.

```text
LE32 result
LE32 sequence
```

The field order is result first, sequence second. The client looks up retained outbound-tell context by sequence, selects localized feedback for the result, and resolves/removes the pending operation. Stardust actively emits 0 (success), 4 (offline/no avatar), 9 (ignored), and 16 (too long). The executable also retains legacy result branches; unknown values must remain numeric and diagnostic.

This is an acknowledgement only and does not echo recipient or message text. A replacement therefore needs a bounded pending-send table keyed by fixed-width U32 sequence. Confidence: schema/correlation/result handling **Confirmed**.

