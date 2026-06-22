# GingerPaw for Windows — Port Plan

## Context

GingerPaw (macOS codename FlowOSS) is an on-device push-to-talk dictation app: hold a hotkey, speak, release, get the transcript pasted into the focused app, with an optional local-LLM cleanup pass. This is a Windows port with full feature parity, built on a deliberately-chosen native stack (.NET 8 + WPF + Whisper.net + LLamaSharp), using the **same model families** the Mac app uses (Whisper for STT, Qwen2.5-0.5B-Instruct for cleanup) so behavior and quality stay comparable across platforms.

No training is involved anywhere in this project — both the Mac app and the Windows port only ever run **pretrained, quantized, downloaded** open-weight models. The only new "ML work" here is *loading and prompting* existing weights, not producing new ones.

This is its own repo, separate from the macOS `gingerpaw` repo (sibling directory) — the two codebases share no code or build tooling (Swift/Xcode vs .NET/Visual Studio), so keeping them separate avoids polluting either repo's tooling/config with an unrelated platform. License: MIT, matching the Mac app.

## Architecture mapping (Mac → Windows)

The Mac app's 9 FlowKit modules map 1:1 to 9 new .NET projects under `src/`, each wrapping one OS-specific primitive behind the same protocol/interface seam the Mac app uses for testability:

| FlowKit module | Windows project | Key OS primitive |
|---|---|---|
| `Settings` | `GingerPaw.Settings` | JSON file at `%AppData%\GingerPaw\settings.json` (vs `UserDefaults`) |
| `Permissions` | `GingerPaw.Permissions` | Mic privacy only — no Input Monitoring/Accessibility equivalent exists on Windows |
| `Audio` | `GingerPaw.Audio` | NAudio/WASAPI capture, 16kHz mono PCM WAV (vs `AVAudioRecorder`) |
| `Hotkeys` | `GingerPaw.Hotkeys` | `WH_KEYBOARD_LL` via `SetWindowsHookEx` (vs `CGEventTap`) — default key **Right Ctrl**, not Fn (Fn is usually swallowed by keyboard firmware before reaching user-mode hooks on Windows) |
| `Transcription` | `GingerPaw.Transcription` | `Whisper.net` (whisper.cpp binding) over a GGUF Whisper model (vs WhisperKit/CoreML) |
| `TextProcessing` | `GingerPaw.TextProcessing` | `LLamaSharp` (llama.cpp binding) over a GGUF Qwen2.5-0.5B-Instruct model (vs MLX) |
| `TextInsertion` | `GingerPaw.TextInsertion` | `SendInput` (Ctrl+V) + `Clipboard` (vs `CGEvent` + ⌘V) |
| `Dictation` | `GingerPaw.Dictation` | Ported near-verbatim — same state machine, same method names |
| `Overlay` | `GingerPaw.Overlay` | Borderless `Topmost` WPF window (vs the Mac pill panel) |
| `AppCore` | `GingerPaw.App` | Composition root + tray icon (`H.NotifyIcon.Wpf`) + settings window (vs menu-bar UI) |

Tests live in `tests/`, one xUnit project per testable module, using hand-written stub classes for `IAudioRecording`/`ISpeechTranscriber`/`ITextInserter`/`ITextProcessor` — the same substitution pattern `DictationCoordinatorTests.swift` uses on the Mac side.

## One-time developer setup

1. **.NET 8 SDK** (8.0.4xx band) — `winget install Microsoft.DotNet.SDK.8`; pin via `global.json` at repo root.
2. **Visual Studio 2022** (17.10+), ".NET desktop development" workload — gives the WPF designer and a debugger that can step through P/Invoke calls (needed for the keyboard hook / `SendInput` work). VS Code + C# Dev Kit is fine for the non-UI library projects.
3. No extra Windows SDK/header install needed — this is pure P/Invoke against `user32.dll` exports (`SetWindowsHookEx`, `SendInput`), not native C++ compilation; VS's bundled Windows SDK component covers it.
4. **NuGet packages**, added per project:
   - `GingerPaw.Audio` → `NAudio`
   - `GingerPaw.Hotkeys` → none (raw P/Invoke)
   - `GingerPaw.Transcription` → `Whisper.net` + `Whisper.net.Runtime` (CPU; `Whisper.net.Runtime.Cuda12` optional for NVIDIA boxes)
   - `GingerPaw.TextProcessing` → `LLamaSharp` + `LLamaSharp.Backend.Cpu` (CUDA/Vulkan backend optional)
   - `GingerPaw.TextInsertion` → none beyond WPF's built-in `Clipboard` + P/Invoke
   - `GingerPaw.App` → `H.NotifyIcon.Wpf` (tray icon)
   - test projects → `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`
5. `models/` is already gitignored at repo root — model files must never get committed.
6. **Order a code-signing certificate now, not at packaging time** (OV minimum, EV preferred) — lead time is 1-2 weeks and it's load-bearing, not optional (see Risks).

## Do we need to download models locally? Yes — two, both pretrained, no training

| Model | Source | Default variant | Size | Used by |
|---|---|---|---|---|
| Whisper STT | Hugging Face `ggerganov/whisper.cpp`, fetched via `WhisperGgmlDownloader.Default.GetGgmlModelAsync(GgmlType.Base)` | `ggml-base.bin` | ~142 MB | `GingerPaw.Transcription` — parity with the Mac default `openai_whisper-base` |
| Qwen2.5-0.5B-Instruct cleanup LLM | Hugging Face `bartowski/Qwen2.5-0.5B-Instruct-GGUF` | Q4_K_M quant | ~0.40 GB | `GingerPaw.TextProcessing` (v2 only) — same model family as the Mac app's MLX processor |

Dev-time: drop both under `models/<whisper|qwen>/` (gitignored). Shipped build: mirror the Mac app's "bundled if present, else download" pattern — bundle Whisper in the installer (always needed), but defer the Qwen download to the first time the user enables "AI formatting" (it defaults off on Mac too, so most users never need it). Before bundling Qwen weights, confirm the exact Qwen2.5 license terms on the HF model card and add a `THIRD_PARTY_NOTICES` file crediting both models, alongside the app's own MIT license.

## Phased build order

**Phase A — De-risk the hard OS integration first, no UI polish.** Scaffold the solution; build `GingerPaw.Hotkeys` (low-level keyboard hook, default Right Ctrl), `GingerPaw.Audio` (NAudio→16kHz mono WAV), `GingerPaw.Transcription` (Whisper.net), `GingerPaw.TextInsertion` (Clipboard + 4-event `SendInput` Ctrl+V sequence — down/down/up/up, since `SendInput` has no combined modifier-flag concept like `CGEvent.flags`). Wire them directly in a throwaway `Program.cs`. **Milestone:** hold Right Ctrl in Notepad, speak, release, see pasted text — verified against at least 3 real foreground apps (Win32, Chromium-based, WPF/UWP) since this retires the riskiest unknowns before anything else is built on top.

**Phase B — Port `DictationCoordinator`, add tray UI, settings, permissions.** Port the state machine into `GingerPaw.Dictation` keeping method names (`StartRecording`/`StopRecordingAndProcess`/`CancelRecording`) and the exact state graph, using a `record`-hierarchy for the discriminated `DictationState` (closest C# analog to Swift's associated-value enum). Build `GingerPaw.Settings` (JSON store, injectable path for tests), `GingerPaw.Permissions` (mic check only — UI should say plainly that Input Monitoring/Accessibility have no Windows equivalent rather than show fake toggles), `AppComposition`/`AppRuntime` mirroring the Mac composition root, and a tray icon via `H.NotifyIcon.Wpf`. Set `ShutdownMode="OnExplicitShutdown"` in `App.xaml` — WPF's default shuts the whole app down when any transient window (e.g. Settings) closes, which would look like a crash. **Milestone:** app runs with zero visible windows, tray icon present, full loop working through the ported coordinator, settings persist across restarts.

**Phase C — Floating pill overlay.** Borderless/`Topmost`/`ShowInTaskbar=false` WPF window mirroring the Mac pill's recording/processing/inserting/copied/failed visual states (simplified glyph is fine — skip pixel-perfect paw art for v1). Smoke-test on a multi-monitor, mixed-DPI rig and opt into Per-Monitor-V2 DPI awareness in the app manifest — easy to get positioning wrong otherwise.

**Phase D — LLM cleanup pass (v2, deferred until A–C are solid).** `LLamaSharpTextProcessor` using `LLamaWeights.LoadFromFile` + a `StatelessExecutor` (better fit than the Mac's `ChatSession` since each call is independent), porting the exact few-shot prompt and output-cleaning logic from `MLXTextProcessor.swift` verbatim. Gate behind `settings.FormatEnabled`, fall back silently to the raw transcript on any processor failure — same as today.

**Phase E — Packaging, signing, autostart.** Package via MSIX (verify empirically it doesn't restrict `SetWindowsHookEx`/`SendInput` before committing; fall back to a plain self-contained publish + Inno Setup/WiX installer if it does). Sign with the cert ordered in setup. Add a `LaunchAtStartup` setting wired to the Startup folder or `HKCU\...\Run`.

## Testing strategy

Same principle as the Mac app: anything behind a protocol/interface seam gets a stub-based xUnit test; raw OS-boundary code (the hook, WASAPI, `SendInput`, tray) is manual-only by nature, exactly as `RightOptionHotkeyMonitor`/`ClipboardTextInserter`'s real OS calls have no unit coverage today either.

- **Testable now:** port all 4 scenarios from `DictationCoordinatorTests.swift` verbatim against the C# coordinator with stub `IAudioRecording`/`ISpeechTranscriber`/`ITextInserter`; `SettingsStore` JSON round-trip; overlay positioning math as a pure function; `LLamaSharpTextProcessor`'s prompt-building and output-cleaning string logic (no model load needed).
- **Manual-only:** hotkey reliability across keyboards, WASAPI against real mic hardware, `SendInput` landing correctly in real apps (including elevated ones — confirm the copy-only fallback engages rather than silently failing), tray/menu interactions, install/uninstall/SmartScreen behavior, autostart across reboot.

## Risks, ranked

1. **AV/SmartScreen false-positives.** A `WH_KEYBOARD_LL` hook + `SendInput` + a window-less background process hits three classic "looks like malware/keylogger" heuristics at once. Mitigation: code-sign (ordered during setup, not at Phase E), keep the hook callback fast and always call `CallNextHookEx`.
2. **`SendInput` silently no-ops against elevated/protected foreground windows**, with no clean way to predict this in advance. Treat copy-only as the default safety net, not an edge case — detect reactively rather than trying to query relative process elevation up front.
3. **Qwen2.5 license terms for bundling weights** — verify the exact license on the HF model card before shipping it inside an installer; add `THIRD_PARTY_NOTICES`.
4. **WPF tray-app lifecycle/DPI footguns** — wrong `ShutdownMode` kills the app on first Settings-window close; missing Per-Monitor-V2 DPI awareness mispositions the overlay on mixed-DPI setups. Both are cheap to get right early and annoying to debug late.

## Critical files to keep open while porting (source of truth, in the sibling `..\gingerpaw` repo)

- `Packages/FlowKit/Sources/Dictation/DictationCoordinator.swift` + `DictationModels.swift`
- `Packages/FlowKit/Sources/Hotkeys/RightOptionHotkeyMonitor.swift`
- `Packages/FlowKit/Sources/TextInsertion/ClipboardTextInserter.swift`
- `Packages/FlowKit/Sources/TextProcessing/MLXTextProcessor.swift`
- `Packages/FlowKit/Sources/AppCore/AppComposition.swift` + `AppRuntime.swift`
- `Packages/FlowKit/Sources/Settings/FlowSettings.swift`
- `Packages/FlowKit/Sources/Overlay/DictationOverlayController.swift`
- `Packages/FlowKit/Tests/DictationTests/DictationCoordinatorTests.swift`

## Verification

- After Phase A: manual end-to-end test (hold hotkey → speak → release → paste) against Notepad, a Chromium-based app, and a WPF/UWP app.
- After Phase B: `dotnet test` runs the ported coordinator tests green; restart the app and confirm settings persisted.
- After Phase C: visually compare overlay states against the Mac pill; verify on a second, differently-scaled monitor.
- After Phase D: enable "AI formatting", dictate a rambly multi-item sentence, confirm it restructures into a list without changing meaning; disable the toggle and confirm raw transcript passthrough.
- After Phase E: install the signed package on a clean VM, confirm no unresolved SmartScreen block, confirm autostart survives a reboot.
