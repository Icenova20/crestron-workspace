# Lockton Dunning Test Program - Project Memory

## Project Overview
**Test System for Break Room 09.002 AV Control**
A SIMPL# Pro (C#) program with Python test automation for validating the Lockton Dunning CH5 touch panel join contract on a Crestron 4-Series processor.

## Architecture
- **C# Host:** `src/ControlSystem.cs` — Entry point, registers TSW-1070 (IPID 0x03) and XPanel (0x04)
- **Panel Manager:** `src/PanelManager.cs` — SigChange wiring, feedback getters/setters
- **Room Logic:** `src/RoomLogic.cs` — State machine (sources, volume, mute, scheduling, power)
- **Join Map:** `src/JoinMap.cs` — Static constants matching CH5 app.js
- **Test Bridge:** `src/TestBridge.cs` — Console test runner + Python bridge
- **Python Tests:** `tests/test_runner.py` — 20 test cases with dry-run support

## Join Map Reference
See `src/JoinMap.cs` (source of truth) or the [CH5 project GEMINI.md](file:///d:/Antigravity/ch5-workspace/html/Lockton%20Dunning%20Benefits%20Series%20-%20Granite%20Park%20AV%20RFP%20(JT022716)/GEMINI.md) for the complete table.

## Console Commands (On-Processor)
| Command | Description |
|:--------|:------------|
| `TESTALL` | Run full join test suite (15 C# tests) |
| `STATUS` | Print current room state |
| `TESTPY` | Launch Python test runner |

## Development Commands (Windows)
```powershell
# Run full test suite with mock bridge
python tests/test_runner.py --dry-run

# Join parity check only
python tests/test_runner.py --parity
```

## Compilation
Requires Visual Studio + Crestron SIMPL# Pro SDK (Windows).
1. Open `src/` as SIMPL# Pro project
2. Target .NET Framework 4.7
3. Add NuGet: Crestron SimplSharpPro (v2.17+)
4. Build → outputs `.cpz`

## Deployment
1. Load `.cpz` to program slot on CP4/RMC4
2. Load `lockton_dunning.ch5z` to TSW-1070 or use WebXPanel
3. SSH to processor, run `TESTALL`
