# Antigravity Crestron Workspace Memory

This workspace is a standardized environment for Crestron SIMPL+ and SIMPL Window development, emphasizing modularity, visual professionality, and automated build processes.

## Environment & Tools
- **OS**: Windows 11 with WSL installed (Ubuntu/Debian).
- **Primary IDE**: VS Code with Antigravity.
- **Build Tool**: `tools/build-project.ps1`.
  - **Function**: Reconciles `projects/PROJECT_NAME/dependencies.json`.
  - **Logic**: Copies modular assets (.usp, .ush, .umc, .clz) from `modules/simpl` to the project's logic folder and then compiles local project logic.
  - **Critical Rule**: Dependencies are **copy-only**. We do not auto-recompile modular drivers during a project build to prevent `.ush` header corruption.

## Workspace Architecture

### 1. `modules/simpl` (The Modular Library)
Global, reusable drivers for hardware.
- **Cisco_ExternalSource_v2.0**: Parameter-driven SSH driver for Webex Codecs.
  - *Status*: Stabilized. Legacy `v1.0` decommissioned.
- **NvxRouteManager**: Centralized NVX directory.
- **PlanarDisplay / ShureMxw / ShureMxa920 / SonosControl**: Verified hardware drivers.

### 2. `projects/` (Project Instances)
Project-specific logic and configuration.
- **LocktonDunning_GranitePark(JT022716)**:
  - **Three-Tier C# Architecture**:
    - **`LogicCore/` (Shared Brain)**: Centralized logic for Break Room, Training Room, and Restrooms. Common core for both SIMPL and Standalone.
    - **`Standalone/` (Pure Pro)**: A 100% C# system that registers hardware directly (NVX, Shure, Planar, Tesira, Cisco). No SIMPL Windows required.
    - **`SimplWrapper/` (Original Path)**: Refactored legacy logic that provides S+ modules with access to the shared `LogicCore`.
  - **C# Advanced Features**:
    - **NVX Engine**: Auto-configures decoder IPs and triggers routes.
    - **Scheduling Heartbeat**: 1-minute logic loop for automated room power.
    - **Sonos Transport**: Pulse-based transport feedback for S+ parity.

## C# Standalone Development (.NET 8.0 & Debugging)

### 1. NuGet Packages
Crestron provides three core NuGet packages depending on the deployment target:
- `Crestron.SimplSharp.SDK.Program`: Use for standalone C# programs. Automates `.cpz` generation upon build.
- `Crestron.SimplSharp.SDK.Library`: Use for creating libraries intended to wrap into traditional SIMPL+ modules (creates `.clz`).
- `Crestron.SimplSharp.SDK.ProgramLibrary`: For creating libraries that will only be consumed by other Crestron C# programs.
*Note: Target `.NET 8.0` natively instead of `.NET 4.7` for massive performance gains, memory efficiency, and access to modern C# features. Use `dotnet build` to compile.*

### 2. Remote Debugging on 4-Series (CP4N)
You can attach a live C# debugger (like JetBrains Rider's "Mono Remote" or Visual Studio's "VSMonoDebugger") directly to the running processor via SSH:
1. Upload the `.cpz` to the processor.
2. `PROGLOAD -P:[Slot] -D` to load it.
3. Start the debug mode:
   - `DEBUGPROGRAM -P:[Slot] -Port:50000 -IP:0.0.0.0 -S` (Halts the program at the constructor until your debugger connects).
   - `DEBUGPROGRAM -P:[Slot] -Port:50000 -IP:0.0.0.0` (Starts normally, allows attaching later).
4. `PROGRESET -P:[Slot]` to restart the program into debug mode.

**Safely Detaching the Debugger:**
Always clean up to prevent locking the program slot:
1. `STOPPROGRAM -P:[Slot]`
2. `DEBUGPROGRAM -P:[Slot] -C` (Clears the debug flag).
3. *Disconnect IDE debugger safely.*
4. `PROGRESET -P:[Slot]` (Restarts normally).

### 3. Debugging on VC-4 (Virtual Control)
- Edit `/opt/crestron/virtualcontrol/conf/debug.conf` on the server.
- Add line: `[ROOMID],[PORT]` (e.g., `MYROOM,50000`).
- Restart the room via the VC-4 UI and attach your IDE using Mono Remote.
- When finished, remove the line from `debug.conf` and restart the room.

## Coding Standards (The Bible: `SIMPL+Context.md`)
- **Hungarian Notation**:
  - `_b`: Digital (Boolean)
  - `_n`: Analog (Number/Integer)
  - `_s`: Serial (String)
  - `_fb`: Feedback
  - `p_`: Parameter
- **Visual Alignment**:
  - Use `_SKIP_` on I/O pins to maintain horizontal parity in SIMPL Windows symbols.
  - Use `_SKIP_` in Parameter blocks to push hardware attributes to the bottom of the symbol.

## Critical Lessons Learned (The 9999 Rule)
> [!IMPORTANT]
> **Signal Handle Collisions**
> SIMPL Windows `(144) Invalid Data [9999]` errors happen when multiple `.umc` modules use the same `H=9999` placeholder for unused/skip signals.
> **Fix**: Every module MUST have unique internal handle ranges for unused signals:
> - Cisco v1: 7901+
> - NVX: 8001+
> - Planar: 8101+
> - Shure MXA: 8201+
> - Shure MXW: 8301+
> - Sonos: 8401+

### 4. SIMPL+ Syntax Quirks
- **Duplicate Skip Pins**: Never use comma-separated `_SKIP_` on a single line (e.g., `DIGITAL_INPUT _SKIP_, _SKIP_;`). The compiler treats them as duplicate variable names. Use unique numbered skips (`_SKIP1_`, `_SKIP2_`) or separate lines.
- **Variable vs Array Declaration Order**: SIMPL+ requires all non-array variables (e.g., `DIGITAL_INPUT Pin1;`) to be declared *before* any array declarations of the same type (e.g., `DIGITAL_INPUT Pins[10];`) within the same I/O block.
    - *Workaround*: If high-count join mapping is needed, use individual numbered skip signals (e.g., `_SI4, _SI5...`) instead of `_SKIP_[n]` if they must be interspersed with regular pins.
- **Time Functions**: Use `GetHourNum()` for integer hour comparisons (0-23) to avoid undefined variable or type-mismatch errors during Boolean logic.

### 5. Professional Symbol Standards
- **Standardized Grouping**: For complex modules, use exactly one `_SKIP_` between functional groups (e.g., Audio vs. Sources) for visual distinction.
- **Semantic UMC Alignment**:
    - **Rule**: Pair related Input/Output pins (e.g., `Mute_Tgl` and `Mute_fb`) on the same row, even if indices don't match chronologically.
    - **Implementation**: Automated via `tools/generate-umc-wrappers.ps1`.
- **Parameter Bottom-Alignment**: Pushes parameters below the center-block dividers, mirroring professional manufacturer modules.

## Network Documentation Standard

To maintain a clean global context and prevent token bloat, do **not** store project-specific IP tables here.
- **Global Rule**: Each project folder MUST contain a `NETWORK.md` file.
- **CP4N Baseline**: 
  - `172.22.0.1`: Control System
  - `172.22.0.2`: Internal Router Gateway
- **Format**: Use the format in `projects/<PROJECT_NAME>/NETWORK.md`.

## Hardware Context
- **Cisco**: SSH/API control. Tracks Standby ("Off", "Halfwake", "Standby") and Active Source strings.
- **NVX**: Multicast routing via specialized RouteManager.
- **Planar**: Display control via serial/CEC.
- **Shure**: MXA920 (Ceiling) and MXW (Wireless) mic mute synchronization.
- **Sonos**: Web-based transport and metadata feedback.

## Active Work & Next Steps
- **Status**: **Three-Tier C# Expansion Complete**.
  - Logic Core unified (Multi-Room).
  - Standalone "Pure Pro" path enabled for all hardware.
  - UMC Semantics stabilized.
- **Next**: Hardware integration testing for Shure and Planar standalone drivers.
