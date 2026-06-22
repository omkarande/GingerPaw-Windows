# GingerPaw for Windows

GingerPaw is an on-device push-to-talk dictation app: hold a hotkey, speak, release it, and
your words get pasted into whatever app you're focused on. Transcription and optional cleanup
both run locally — no audio or text ever leaves your machine.

This is the Windows port of GingerPaw for macOS (codename FlowOSS) — a separate, from-scratch
.NET implementation of the same app, sharing no code with the Mac version.

## How it works

1. Hold **Right Ctrl** and speak.
2. Release it — your speech is transcribed locally with [Whisper](https://github.com/openai/whisper).
3. Optionally, a small local AI model ([Qwen2.5-0.5B-Instruct](https://huggingface.co/Qwen/Qwen2.5-0.5B-Instruct))
   cleans up the transcript into tidy prose or a list, off by default.
4. The result is pasted into whatever app you're currently focused on.

No audio or text leaves your machine — transcription and formatting both run entirely on-device.

## Install

Download the latest installer from the [Releases page](https://github.com/omkarande/GingerPaw-Windows/releases) —
look for `GingerPaw-Setup.exe` under the most recent release, run it, and follow the wizard.

The installer isn't code-signed yet, so Windows SmartScreen will show an "unrecognized
publisher" warning the first time you run it — click "More info" → "Run anyway" to continue.

GingerPaw runs in the background as a tray icon after install (look for it under the `^`
overflow arrow near the clock if it's not immediately visible). Right-click it for Settings.

## Building from source

Requires the .NET 8 SDK.

```
dotnet build GingerPaw.sln
dotnet run --project src/GingerPaw.App/GingerPaw.App.csproj
```

See `CLAUDE.md` for the full architecture mapping against the macOS app and detailed build/
phase notes, and `plan.md` for the original phased build plan.

## License

MIT — see `LICENSE`. Bundled/downloaded model weights (Whisper, Qwen2.5-0.5B-Instruct) carry
their own licenses — see `THIRD_PARTY_NOTICES.md`.
