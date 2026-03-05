# Crestron SIMPL+ Module Development

This project is a dedicated environment for developing, documenting, and standardizing Crestron SIMPL+ modules. It emphasizes visual clarity in SIMPL Windows, robust protocol handling, and automated boilerplate generation.

## Project Overview

*   **Primary Language:** SIMPL+ (C-like language for Crestron control systems).
*   **Key Feature:** The `SamsungDisplayControl` module demonstrates advanced patterns like dual-protocol support (MDC & EX-Link) and automatic protocol detection.
*   **Tooling:** Python scripts are used to scaffold new modules with correct naming conventions and symbol layouts.

## Key Files & Directories

*   **`SIMPL+Context.md`**: The bible for this project. It defines the strict coding standards, naming conventions (Hungarian notation), and visual alignment rules (using `_SKIP_`). **Always consult this before writing code.**
*   **`new_module.py`**: A CLI wizard that generates new `.usp` module files. It handles the tedious setup of inputs, outputs, parameters, and symbol alignment automatically.
*   **`SamsungDisplayControl/`**: Contains the reference implementation for a Samsung Display module.
    *   `SamsungDisplayControl.usp`: The source code. Note the use of `_SKIP_` for pin alignment and the dual-protocol logic.
    *   `SamsungDisplayControl_Help.html`: The source for the help documentation.
    *   `SamsungDisplayControl_Help.pdf`: The compiled documentation (generated via `wkhtmltopdf`).

## Development Workflow

### 1. Create a New Module
Use the Python script to scaffold a new module. This ensures you start with the correct headers and I/O definitions.
```bash
python3 new_module.py
```
Follow the prompts to define your Digital, Analog, and Serial inputs/outputs.

### 2. Implementation Rules & Template

#### Visual Alignment (`_SKIP_`)
To ensure the module symbol looks professional in SIMPL Windows, align Inputs and Outputs using the `_SKIP_` keyword.
*   **Signal Alignment:** If an Input has no corresponding Output at the same index, use `_SKIP_` on the Output side to maintain horizontal alignment for subsequent signals.
*   **Parameter Placement:** To push Parameters to the **bottom** of the symbol, insert a `_SKIP_` for every row used by signal pins (Inputs/Outputs).
    *   *Rule:* Number of Skips = Total Number of Input/Output Rows used.

#### Naming Conventions (Hungarian Notation)
*   `_b`: Digital (Boolean) e.g., `Power_On_b`
*   `_n`: Analog (Number) e.g., `Volume_Level_n`
*   `_s`: Serial (String) e.g., `Command_s`
*   `_fb`: Feedback (Output) e.g., `Power_Is_On_fb`
*   `p_`: Parameters (e.g., `p_nDeviceID`)
*   `g_`: Global Variables (e.g., `g_nInitialized`)

#### Full Module Template
```c
/*******************************************************************************************
  AUTHOR: James Ashcraft
  VERSION: 1.0.0
*******************************************************************************************/

#SYMBOL_NAME      "My Module"
#DEFAULT_VOLATILE
#ENABLE_STACK_CHECKING
#ENABLE_TRACE

// ------ INPUTS --------
DIGITAL_INPUT   Initialize_b;    // Row 1
ANALOG_INPUT    Set_Value_n;     // Row 2

// ------ OUTPUTS --------
DIGITAL_OUTPUT  Is_Initialized_fb; // Row 1
ANALOG_OUTPUT   Value_fb;          // Row 2

// ------ PARAMETERS --------
// Push to bottom (Rows 1-2 used, so 2 skips)
INTEGER_PARAMETER _SKIP_,_SKIP_, p_nDeviceID;

// ------ GLOBALS --------
INTEGER g_nInitialized;

// ------ EVENTS --------
PUSH Initialize_b
{
    g_nInitialized = 1;
    Is_Initialized_fb = 1;
}

Function Main()
{
    g_nInitialized = 0;
}
```

### 3. Documentation & PDF Generation
Documentation is treated as a first-class citizen.
1.  Edit the `_Help.html` file.
2.  Generate the PDF using `wkhtmltopdf`:
    ```bash
    wkhtmltopdf MyModule_Help.html MyModule_Help.pdf
    ```
3.  Ensure the `.usp` file points to this PDF using the `#HELP_PDF_FILE` directive.

## SIMPL+ Best Practices (from Context)

*   **Protocol Parity:** If a module supports multiple protocols (e.g., RS232 vs IP, or different vendor protocols), abstract the commands so one internal function (e.g., `SendCommand`) handles the translation.
*   **Clean Symbols:** A module that looks messy in SIMPL Windows is considered broken. Prioritize the integrator's experience by aligning pins.
