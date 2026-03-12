# Lockton Dunning SIMPL# Pro Logic Core

This project contains the pure C# implementation of the Break Room 09.002 control logic. It is designed to run as the primary program on a Crestron 4-Series processor.

## Architecture
- **ControlSystem.cs**: Entry point and device registration (TSW-1070 @ 0x07, XPanel @ 0x04).
- **RoomLogic.cs**: State machine for sources, volume, and scheduling.
- **PanelManager.cs**: Logic-to-UI binding.
- **JoinMap.cs**: Centralized join definitions matching the CH5 project.

## Prerequisites
- Visual Studio 2019/2022
- Crestron SIMPL# Pro SDK
- .NET Framework 4.7.2

## Build & Deploy
1. Open the `.csproj` in Visual Studio.
2. Build as **Release**.
3. Load the resulting `.cpz` to a program slot on the processor via Crestron Toolbox or SFTP.

## Diagnostics
Use the following console commands on the processor:
- `STATUS`: Print the current state of all audio zones and source selections.
- `TESTALL`: Run the automated join verification suite.
