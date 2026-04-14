# Perform Agent Probe Results
**Date:** 2026-04-08  
**Device:** HP OmniBook 5 Laptop 16-af1xxx  
**App Version:** HP AI Companion v2.6.1003.0  

> **Environment note:** This app is not intended to run on this machine. Device
> checks are bypassed via patched DLLs, and the RPC client DLL is patched to
> return empty results or success codes to pass internal checks. As a result,
> only volume and brightness controls work reliably. All other intent results
> are inconclusive and should not be treated as product bugs.

## What Works Reliably

### Volume Control
| Command | Intent | Value | Notes |
|---|---|---|---|
| "set volume to 50%" | `set_audio_volume_speaker` | `50` | Actually changes system volume |
| "set volume to 100%" | `set_audio_volume_speaker` | `100` | |
| "set volume to 0%" | `set_audio_volume_speaker` | `0` | |
| "increase the volume" | `increase_audio_volume_speaker` | `default` | |
| "decrease the volume" | `decrease_audio_volume_speaker` | `default` | |
| "what is my current volume?" | `get_audio_volume_speaker` | `default` | |

### Brightness Control
| Command | Intent | Value | Notes |
|---|---|---|---|
| "set brightness to 50%" | `set_display_brightness` | `50` | Actually changes system brightness |
| "set brightness to 100%" | `set_display_brightness` | `100` | |
| "increase the brightness" | `increase_display_brightness` | `default` | |
| "decrease the brightness" | `decrease_display_brightness` | `default` | |
| "what is my current brightness?" | `get_display_brightness` | `default` | |

### Agent Validation (Working Correctly)
The agent validates input ranges and rejects values outside 0-100:
| Command | Intent | Value | Result |
|---|---|---|---|
| "set volume to 500%" | `set_audio_volume_speaker` | - | Agent declines the request |
| "set brightness to -50%" | `set_display_brightness` | - | Agent declines the request |
| "set brightness to 500%" | `set_display_brightness` | - | Agent declines the request |

---

## Confirmed Bugs

### BUG 1: Non-numeric volume values default to 50%
- **Severity:** Medium
- **Repro:** "set volume to A", "set volume to hello", "set volume to emoji"
- **Expected:** Agent should reject the input or ask for a valid number
- **Actual:** NLP resolves `set_audio_volume_speaker` with empty Value, app defaults to 50%
- **Test:** `PerformAgent.feature` — `Non-numeric volume value defaults to 50 instead of being rejected`

---

## Inconclusive (Patched Environment)

The following intents were probed but results are unreliable due to patched DLLs
and the RPC client returning empty/success stubs. These need re-testing on
supported hardware.

### Audio — Mute/Unmute
| Command | Intent | Notes |
|---|---|---|
| "mute my speakers" | `off_audio_speaker` | Inconclusive — patched RPC |
| "unmute my speakers" | `on_audio_speaker` | Inconclusive |
| "mute my microphone" | `off_audio_mic` | Inconclusive |
| "unmute my microphone" | `on_audio_mic` | Inconclusive |

### Audio — Microphone Volume
| Command | Intent | Notes |
|---|---|---|
| "set microphone volume to 80%" | `set_audio_volume_mic` | Inconclusive |

### Audio — Noise Cancellation
| Command | Intent | Notes |
|---|---|---|
| "turn on noise cancellation" | `on_audio_ns_speaker` | Inconclusive — Realtek CIM namespace missing |
| "turn off noise cancellation" | `off_audio_ns_speaker` | Inconclusive |

### Display — HDR
| Command | Intent | Notes |
|---|---|---|
| "enable HDR" | `on_display_hdr` | Inconclusive |
| "disable HDR" | `off_display_hdr` | Inconclusive |

### Camera
| Command | Intent | Notes |
|---|---|---|
| "turn on my camera" | `on_camera` | Inconclusive |
| "turn off my camera" | `off_camera` | Inconclusive |
| "turn on camera blur" | `on_camera_blur` | Inconclusive |
| "enable auto frame" | `on_camera_autoframe` | Inconclusive |

### Mouse
| Command | Intent | Notes |
|---|---|---|
| "set mouse speed to 10" | `set_mouse_speed` | Inconclusive |
| "increase mouse speed" | `increase_mouse_speed` | Inconclusive |
| "swap mouse buttons" | `swap_button_mouse` | Inconclusive |

### Power / BIOS
| Command | Intent | Notes |
|---|---|---|
| "what is my current power mode?" | `get_power` | Inconclusive |
| "set power mode to high performance" | `set_power_highperformance` | Inconclusive |
| "enable fast charge" | `set_user_confirm_yesno_yes` | Inconclusive |
| "enable fast boot" | `on_system_fastboot` | Inconclusive |

### System Settings
| Command | Intent | Notes |
|---|---|---|
| "enable dynamic lock" | `on_privacy_dynamiclock` | Inconclusive |
| "disable toast notifications" | `off_notifications` | Inconclusive |

### Experience Modes
| Command | Intent | Notes |
|---|---|---|
| "turn on gaming mode" | `default_fallback` | No dedicated intent — falls to cloud AI |
| "turn on movie mode" | `default_fallback` | Same |
| "turn on conference mode" | `default_fallback` | Same |
| "turn on music mode" | `default_fallback` | Same |

### Audio Presets
| Command | Intent | Notes |
|---|---|---|
| "set audio preset to music" | `default_fallback` | No dedicated intent |
| "set audio preset to movie" | `default_fallback` | Same |
