#!/usr/bin/env python3
import os
import datetime

def get_input(prompt, required=True):
    while True:
        value = input(prompt).strip()
        if value or not required:
            return value
        print("This field is required.")

def get_variable_type_suffix(var_type):
    if var_type == 'd': return '_b'
    if var_type == 'a': return '_n'
    if var_type == 's': return '_s'
    return ''

def get_full_variable_name(name, var_type, direction=None):
    # var_type: 'd' (digital), 'a' (analog), 's' (string), 'p' (parameter)
    # direction: 'in', 'out', 'param'
    
    # Auto-append suffix if missing
    suffix = ""
    if var_type == 'd' and not name.endswith('_b'): suffix = '_b'
    elif var_type == 'a' and not name.endswith('_n'): suffix = '_n'
    elif var_type == 's' and not name.endswith('_s') and not name.endswith(']'): suffix = '_s' # arrays might end in ]
    
    # Check prefixes based on conventions
    # This is a basic helper, user can override by typing the full name
    
    return f"{name}{suffix}"

def collect_ios(section_name, type_map):
    items = []
    print(f"\n--- {section_name} ---")
    print("Types: (d)igital, (a)nalog, (s)tring, (x) done")
    while True:
        v_type = input(f"Add {section_name} Type [d/a/s/x]: ").lower().strip()
        if v_type == 'x':
            break
        if v_type not in ['d', 'a', 's']:
            print("Invalid type.")
            continue
        
        name = input("  Name (e.g., StartSystem): ").strip()
        if not name: continue

        # Handle string arrays
        array_spec = ""
        if v_type == 's':
            size = input("  String Size [255]: ").strip()
            if not size: size = "255"
            array_spec = f"[{size}]"
        
        # Apply Naming Convention Logic automatically
        # Prefix logic could be complex, so we'll stick to suffixes for now 
        # and let the user type the semantic name.
        
        suffix = get_variable_type_suffix(v_type)
        if v_type == 's':
            # For strings, suffix goes before array brackets
             final_name = f"{name}{suffix}{array_spec}"
        else:
            final_name = f"{name}{suffix}"

        decl_type = type_map[v_type]
        items.append(f"{decl_type} {final_name};")
    return items

def main():
    print("=== Crestron SIMPL+ Module Generator ===")
    
    # 1. Metadata
    module_name = get_input("Module Filename (no ext): ")
    if not module_name.lower().endswith('.usp'):
        filename = module_name + ".usp"
    else:
        filename = module_name
        module_name = module_name[:-4]

    symbol_name = get_input(f"Symbol Name [{module_name}]: ", required=False) or module_name
    author = get_input("Author: ")
    description = get_input("Description: ")
    category = get_input("Category [User Modules]: ", required=False) or "User Modules"
    
    # 2. Collect I/O
    inputs = collect_ios("INPUTS", {'d': 'DIGITAL_INPUT', 'a': 'ANALOG_INPUT', 's': 'STRING_INPUT'})
    outputs = collect_ios("OUTPUTS", {'d': 'DIGITAL_OUTPUT', 'a': 'ANALOG_OUTPUT', 's': 'STRING_OUTPUT'})
    
    # 3. Parameters
    params = []
    print("\n--- PARAMETERS ---")
    print("Types: (i)nteger, (s)tring, (x) done")
    while True:
        p_type = input("Add Parameter Type [i/s/x]: ").lower().strip()
        if p_type == 'x': break
        
        name = input("  Name (e.g., DeviceID): ").strip()
        if not name: continue
        
        if not name.startswith('p_'):
            name = 'p_' + name
            
        if p_type == 'i':
            params.append(f"INTEGER_PARAMETER {name};")
        elif p_type == 's':
            size = input("  String Size [50]: ").strip() or "50"
            if not name.endswith('_s'): name = name + "_s"
            params.append(f"STRING_PARAMETER {name}[{size}];")

    # 4. Generate Content
    date_str = datetime.date.today().strftime("%Y-%m-%d")
    
    # Build I/O blocks
    input_block = "\n".join(inputs)
    output_block = "\n".join(outputs)
    param_block = "\n".join(params)

    template = f"""/*******************************************************************************************\n  MODULE DESCRIPTION:\n  {description}\n\n  AUTHOR: {author}\n  DATE: {date_str}\n  VERSION: 1.0.0\n*******************************************************************************************/\n\n//==================\n// COMPILER DIRECTIVES\n//==================\n#SYMBOL_NAME      \"{symbol_name}\"\n#CATEGORY         \"{category}\"\n#DEFAULT_VOLATILE\n#ENABLE_STACK_CHECKING\n#ENABLE_TRACE\n\n//==================\n// I/O DEFINITIONS\n//==================\n\n// ------ INPUTS --------\n{input_block}\n\n// ------ OUTPUTS --------\n{output_block}\n\n// ------ PARAMETERS --------\n{param_block}\n\n//==================\n// GLOBAL VARIABLES\n//==================\nINTEGER g_nInitialized;\n\n\n//==================\n// EVENT HANDLERS\n//==================\n\n/*\nPUSH Initialize_b\n{{\n    If (g_nInitialized = 0)\n    {{\n        g_nInitialized = 1;\n        // Initialization code here\n    }}\n}}\n*/\n\n//==================\n// MAIN FUNCTION\n//==================\n\nFunction Main()\n{{\n    g_nInitialized = 0;\n    WaitForInitialization();\n}}\n"""

    # 5. Write File
    full_path = os.path.join(os.getcwd(), 'simplplus', filename)
    
    # Ensure directory exists (it should, but good practice)
    os.makedirs(os.path.dirname(full_path), exist_ok=True)
    
    with open(full_path, 'w') as f:
        f.write(template)
        
    print(f"\n[SUCCESS] Module generated at: {full_path}")

if __name__ == "__main__":
    main()
