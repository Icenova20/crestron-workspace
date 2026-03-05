# Lockton Dunning Touch Panel Test Program — Walkthrough

## What Was Built

A **two-tier test program** for the Lockton Dunning Break Room 09.002 CH5 touch panel:

| Layer | Files | Purpose |
|:------|:------|:--------|
| **C# SIMPL# Pro** | 5 files in `src/` | Runs on 4-Series processor, manages TSW-1070 panel, handles all room logic |
| **Python Test Suite** | 3 files in `tests/` | 20 automated test cases with dry-run support for local development |

### Project Structure

```
lockton-test/
├── src/
│   ├── ControlSystem.cs    — Entry point (TSW-1070 + XPanel registration, console commands)
│   ├── JoinMap.cs          — Static join constants matching CH5 app.js
│   ├── PanelManager.cs     — SigChange event wiring + feedback setters/getters
│   ├── RoomLogic.cs        — State machine (sources, volume, mute, scheduling, power)
│   └── TestBridge.cs       — Console test runner (15 C# tests) + Python bridge
├── tests/
│   ├── test_runner.py      — Dual-mode runner (dry-run / on-processor)
│   ├── test_cases.py       — 20 test functions + join parity checker
│   └── mock_bridge.py      — Pure-Python MockBridge for offline testing
└── GEMINI.md
```

---

## Validation Results

### Python Dry-Run (20/20 ✓)

```
✓ All joins match between Python test suite and CH5 app.js

  ✓ PASS: Source: AirMedia selects exclusively
  ✓ PASS: Source: MediaPlayer deselects AirMedia
  ✓ PASS: Source: Re-selection keeps source active
  ✓ PASS: Power Off: Clears all source feedback
  ✓ PASS: Power Off: Clears all mute states
  ✓ PASS: Power Off: Resets all volumes to 0
  ✓ PASS: Volume: Master level echoes 32768
  ✓ PASS: Volume: Restroom level echoes 49152
  ✓ PASS: Volume: Handheld mic echoes 16384
  ✓ PASS: Volume: Bodypack mic echoes 8192
  ✓ PASS: Mute: Master mute toggles ON
  ✓ PASS: Mute: Master mute toggles OFF
  ✓ PASS: Mute: Restroom mute toggles ON
  ✓ PASS: Mute: Handheld mic toggles ON
  ✓ PASS: Mute: Bodypack mic toggles ON
  ✓ PASS: Schedule: Auto-On 8am exclusive
  ✓ PASS: Schedule: Auto-On switches correctly
  ✓ PASS: Schedule: Auto-Off 6pm exclusive
  ✓ PASS: Sonos: Transport commands accepted
  ✓ PASS: Integration: Full workflow with power-off reset

  RESULTS: 20/20 passed, 0 failed
  ✓ ALL TESTS PASSED
```

### Bug Found & Fixed During Testing

**Join 11 Collision:** Digital join 11 is shared between `SettingsModal` (informational pulse) and `HandheldMute` (stateful toggle). The MockBridge initially short-circuited on SettingsModal, preventing the mute toggle from executing. Fixed by removing the early return — SettingsModal has no state side-effects.

---

## Next Steps (Processor Deployment)

1. **Compile:** Open `src/` as SIMPL# Pro project in Visual Studio → Build `.cpz`
2. **Deploy:** Load `.cpz` to program slot + load `lockton_dunning.ch5z` to panel
3. **Test:** SSH into processor → run `TESTALL` console command
4. **Manual:** Open CH5 UI on panel/WebXPanel, verify interactive behavior
