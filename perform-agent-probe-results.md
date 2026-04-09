# Perform Agent Probe Results
**Date:** 2026-04-08  
**Device:** HP OmniBook 5 Laptop 16-af1xxx  
**App Version:** HP AI Companion v2.6.1003.0  

## Complete Intent Map (37 unique intents discovered)

All intents are logged as `Final intent:<name> Value:<value>` in the HelicarrierLog.

### Audio - Volume
| Command | Intent | Value | Works? | Notes |
|---|---|---|---|---|
| "set volume to 50%" | `set_audio_volume_speaker` | `50` | YES | Actually changes system volume |
| "set volume to 100%" | `set_audio_volume_speaker` | `100` | YES | |
| "set volume to 0%" | `set_audio_volume_speaker` | `0` | YES | |
| "set volume to 500%" | `set_audio_volume_speaker` | `500` | YES | No validation - accepts out-of-range |
| "set volume to A" | `set_audio_volume_speaker` | `(empty)` | **BUG** | Defaults to 50% instead of rejecting |
| "increase the volume" | `increase_audio_volume_speaker` | `default` | YES | |
| "decrease the volume" | `decrease_audio_volume_speaker` | `default` | YES | |
| "what is my current volume?" | `get_audio_volume_speaker` | `default` | YES | |

### Audio - Mute/Unmute
| Command | Intent | Value | Works? | Notes |
|---|---|---|---|---|
| "mute my speakers" | `off_audio_speaker` | `default` | **FLAKY** | Sometimes no intent fires; PowerShell errors |
| "unmute my speakers" | `on_audio_speaker` | `default` | YES | |
| "mute my microphone" | `off_audio_mic` | `default` | YES | |
| "unmute my microphone" | `on_audio_mic` | `default` | **FLAKY** | Agent response not always detected |

### Audio - Microphone Volume
| Command | Intent | Value | Works? | Notes |
|---|---|---|---|---|
| "set microphone volume to 80%" | `set_audio_volume_mic` | `80` | **FLAKY** | Intent fires but agent response detection times out |

### Audio - Noise Cancellation
| Command | Intent | Value | Works? | Notes |
|---|---|---|---|---|
| "turn on noise cancellation" | `on_audio_ns_speaker` | `(empty)` | YES | Uses PowerShell `Invoke-CimMethod -Namespace "Root\RealtekAudio"` |
| "turn off noise cancellation" | `off_audio_ns_speaker` | `(empty)` | YES | Same PowerShell mechanism |

### Audio - Presets
| Command | Intent | Value | Works? | Notes |
|---|---|---|---|---|
| "set audio preset to music" | `default_fallback` | - | NO | No dedicated intent; triggers PowerShell errors |
| "set audio preset to movie" | `default_fallback` | - | NO | Same - falls back to cloud AI |

### Display - Brightness
| Command | Intent | Value | Works? | Notes |
|---|---|---|---|---|
| "set brightness to 50%" | `set_display_brightness` | `50` | YES | Actually changes system brightness |
| "set brightness to 100%" | `set_display_brightness` | `100` | YES | |
| "set brightness to -50%" | `set_display_brightness` | `-50` | YES | No validation - accepts negative values |
| "set brightness to 500%" | `set_display_brightness` | `500` | YES | No validation - accepts out-of-range |
| "increase the brightness" | `increase_display_brightness` | `default` | YES | |
| "decrease the brightness" | `decrease_display_brightness` | `default` | YES | |
| "what is my current brightness?" | `get_display_brightness` | `default` | YES | |

### Display - HDR
| Command | Intent | Value | Works? | Notes |
|---|---|---|---|---|
| "enable HDR" | `on_display_hdr` | `(empty)` | YES | Intent fires; actual HDR toggle depends on hardware |
| "disable HDR" | `off_display_hdr` | `(empty)` | YES | |

### Camera
| Command | Intent | Value | Works? | Notes |
|---|---|---|---|---|
| "turn on my camera" | `on_camera` | `(empty)` | YES | |
| "turn off my camera" | `off_camera` | `(empty)` | **FLAKY** | Sometimes triggers PowerShell errors |
| "turn on camera blur" | `on_camera_blur` | `(empty)` | YES | |
| "enable auto frame" | `on_camera_autoframe` | `(empty)` | YES | |

### Mouse
| Command | Intent | Value | Works? | Notes |
|---|---|---|---|---|
| "set mouse speed to 10" | `set_mouse_speed` | `10` | YES | |
| "increase mouse speed" | `increase_mouse_speed` | `default` | YES | |
| "swap mouse buttons" | `swap_button_mouse` | `(empty)` | **FLAKY** | Intent fires but agent response detection issues |

### Power / BIOS
| Command | Intent | Value | Works? | Notes |
|---|---|---|---|---|
| "what is my current power mode?" | `get_power` | `(empty)` | **FAIL** | Agent doesn't respond or times out |
| "set power mode to high performance" | `set_power_highperformance` | `power_highperformance` | YES | |
| "enable fast charge" | `set_user_confirm_yesno_yes` | `(empty)` | YES | Interesting - triggers a confirmation intent |
| "enable fast boot" | `on_system_fastboot` | `(empty)` | YES | |

### System Settings
| Command | Intent | Value | Works? | Notes |
|---|---|---|---|---|
| "enable dynamic lock" | `on_privacy_dynamiclock` | `(empty)` | YES | Triggers TypeLoadException in log |
| "disable toast notifications" | `off_notifications` | `(empty)` | YES | Triggers TypeLoadException in log |

### Experience Modes
| Command | Intent | Value | Works? | Notes |
|---|---|---|---|---|
| "turn on gaming mode" | `default_fallback` | - | NO | No dedicated intent; falls to cloud AI |
| "turn on movie mode" | `default_fallback` | - | NO | Same |
| "turn on conference mode" | `default_fallback` | - | NO | Same |
| "turn on music mode" | `default_fallback` | - | NO | Same |

---

## Bugs Found

### BUG 1: Non-numeric volume values default to 50% (confirmed)
- **Severity:** Medium
- **Repro:** "set volume to A", "set volume to hello", "set volume to emoji"
- **Expected:** Agent should reject the input or ask for a valid number
- **Actual:** NLP resolves `set_audio_volume_speaker` with empty Value, app defaults to 50%
- **Test:** `PerformAgent.feature` - `Non-numeric volume value defaults to 50 instead of being rejected`

### BUG 2: No input validation on out-of-range values
- **Severity:** Low
- **Repro:** "set volume to 500%", "set brightness to 1000%", "set brightness to -50%"
- **Expected:** Agent should clamp to valid range or reject
- **Actual:** Intent fires with the exact value (500, 1000, -1). System may or may not clamp internally.

### BUG 3: TypeLoadException on system setting commands
- **Severity:** Medium
- **Repro:** Commands for dynamic lock, toast notifications, fast boot
- **Expected:** Clean execution or graceful error message
- **Actual:** `SendChatMessageAsync: "Capturing the property value threw an exception: TypeLoadException"` in logs. The agent responds to the user as if the action succeeded, but the error suggests the property access failed.

### BUG 4: PowerShell `Invoke-CimMethod : Invalid namespace` errors
- **Severity:** Medium (hardware-specific)
- **Repro:** Noise cancellation, experience modes, audio presets, some camera commands
- **Expected:** Graceful error handling when the Realtek CIM namespace doesn't exist
- **Actual:** Raw PowerShell error logged: `Invoke-CimMethod : Invalid namespace`. This is expected on non-Realtek hardware, but the user sees no error — the agent either responds generically or silently fails.
- **Affected intents:** Noise cancellation, experience modes (gaming/movie/conference/music), audio presets

### BUG 5: PortraitControl initialization failure
- **Severity:** Low
- **Repro:** Triggered by mute speakers, conference mode, experience modes
- **Actual:** `IsPortraitSupported: {0}` followed by `Error in Portrait Initialization`. Suggests the portrait/camera control subsystem crashes on init when the hardware doesn't support it.

### BUG 6: Experience modes have no dedicated intents
- **Severity:** Medium (functionality gap)
- **Repro:** "turn on gaming mode", "turn on movie mode", etc.
- **Expected:** Dedicated intents that trigger multi-command macros
- **Actual:** All fall to `default_fallback` — the NLP model doesn't recognize these commands. The cloud AI responds with generic advice instead of executing the mode switch.

---

## What Actually Works on This System

### Fully functional (intent fires + system state changes):
- Volume: get, set (0-100), increase, decrease
- Brightness: get, set (0-100), increase, decrease
- Mouse: set speed, increase speed
- Camera: on, blur, auto-frame
- HDR: on/off
- Power: set high performance
- Fast boot: on

### Intent fires but execution uncertain:
- Microphone: mute, set volume
- Noise cancellation: on/off (Realtek-dependent)
- Dynamic lock, toast notifications (TypeLoadException)
- Fast charge (confirmation flow)
- Swap mouse buttons

### Not functional (falls to cloud AI):
- Experience modes (gaming, movie, conference, music)
- Audio presets (music, movie)
- Power mode query ("what is my power mode?")
