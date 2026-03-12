import pandas as pd
import re

# Load the final port-to-mac map
df = pd.read_csv('final_port_ip_map.csv')

# Drop rows without MAC
df = df.dropna(subset=['mac']).copy()

# IP Assignment Counters
counters = {
    '10': 11, # Control
    '20': 11, # Encoders
    '30': 11, # Decoders
    '40': 11  # Audio
}

def clean_nickname(device_raw, port):
    if str(port) == "1":
        return "CP4N_Control"
    
    raw = str(device_raw).upper()
    
    # 1. Identify primary model to prioritize
    primary_model = ""
    models = ["NVX-E30", "NVX-D30", "NVX-E20-2G", "NVX-D20", "AM-3200", "TSW-1070", "TESIRA", "MXA920", "MXWAPX", "MXWNDX"]
    for m in models:
        if m in raw:
            primary_model = m
            break
            
    # Simplify common strings
    name = raw
    name = name.replace("HOSPITALITY", "HOSP")
    name = name.replace("SMALL TR", "SM_TR")
    name = name.replace("LARGE TR", "LG_TR")
    name = name.replace("ROOMKIT PRO", "RKP")
    name = name.replace("CONTROLLER", "CTRL")
    name = name.replace("WALL PLATE", "WP")
    
    # Remove PHX, RELAY, etc.
    cleaned = re.sub(r'PHX.*|RELAY.*|SD CARD.*|PWR.*', '', name).strip()
    
    # If we cleared everything but had something, keep a bit of the original
    if not cleaned and name:
        cleaned = name.split()[0]
    
    if not cleaned:
        cleaned = f"PORT_{port}"

    # If we have a primary model, ensure it's at the front
    if primary_model and not cleaned.startswith(primary_model):
         cleaned = f"{primary_model}_{cleaned}"

    # Final cleanup: keep alphanumeric and underscores, max 32 chars
    cleaned = re.sub(r'[^A-Z0-9_\-]', '_', cleaned)
    cleaned = re.sub(r'_+', '_', cleaned).strip('_')
    
    return cleaned[:31]

def assign_ip(device, port):
    device = str(device).upper()
    if str(port) == "1":
        return "172.22.0.1" # CP4N Control Port Uplink
    
    if 'NVX-E' in device or 'AM-3200' in device:
        subnet = '20'
    elif 'NVX-D' in device:
        subnet = '30'
    elif 'TESIRA' in device or 'FORTE' in device or 'SHURE' in device or 'MXW' in device or 'MXA' in device:
        subnet = '40'
    else:
        subnet = '10'
    
    ip = f"172.22.{subnet}.{counters[subnet]}"
    counters[subnet] += 1
    return ip

# Generate assignments
# -- MANUAL OVERRIDES --
# The automated extraction misaligned Port 24 due to PDF density.
# User specifies: Port 24 = NVX-E30 / LARGE TR ROOMKIT PRO / E.06 (VID)
df.loc[df['Port'] == '24', 'Device_Raw'] = 'NVX-E30 / LARGE TR ROOMKIT PRO / E.06 (VID) 24'

def clean_nickname(device_raw, port):
    if str(port) == "1":
        return "CP4N_Control"
    
    raw = str(device_raw).upper()
    
    # 1. Extract the ID (e.g. E.06, D.03, etc.)
    # We want to skip 'E30' or 'D30' in model names.
    # Look for [ED]\.[0-9]+ or [ED] [0-9]+ or lone [ED][0-9]+ at the end/middle
    id_match = re.search(r'(?<!NVX-)([ED])\.?\s?([0-9]{2})', raw)
    id_str = ""
    if id_match:
        id_str = f"{id_match.group(1)}{id_match.group(2)}"

    # 2. Identify primary model/what it is
    what = ""
    # Map common strings to pretty names
    location = ""
    if "HOSPITALITY" in raw or "HOSP" in raw: location = "Hospitality"
    elif "SMALL TR" in raw or "SM_TR" in raw: location = "SmallTR"
    elif "LARGE TR" in raw or "LG_TR" in raw: location = "LargeTR"
    
    if "ROOMKIT PRO" in raw:
        what = f"{location}Roomkit"
    elif "AM-3200" in raw:
        what = f"{location}AirMedia"
    elif "TSW-1070" in raw:
        what = f"{location}Touchpanel"
    elif "MXA920" in raw:
        what = f"{location}Mic"
    elif "MXW" in raw:
        what = f"{location}Wireless"
    elif "NVX-E" in raw:
        what = f"{location}Encoder"
    elif "NVX-D" in raw:
        what = f"{location}Decoder"
    elif "TESIRA" in raw or "FORTE" in raw:
        what = "DSP"
    else:
        # Fallback with safety
        parts = re.sub(r'PHX.*|RELAY.*|[ED][\.\s]?[0-9]+|PORT.*|NVX-.[0-9]+', '', raw).strip().split('_')[0].split()
        what = parts[0] if parts else "Device"

    # Combine: ID-What
    if id_str:
        nickname = f"{id_str}-{what}"
    else:
        nickname = f"{location}{what}" if location and location not in what else what

    # Final cleanup: allow alphanumeric, dashes, and underscores
    nickname = re.sub(r'[^a-zA-Z0-9_\-]', '', nickname)
    
    if not nickname or nickname == "_":
        nickname = f"PORT_{port}"
        
    return nickname[:31]

df['Assigned_IP'] = df.apply(lambda row: assign_ip(row['Device_Raw'], row['Port']), axis=1)
df['Nickname'] = df.apply(lambda row: clean_nickname(row['Device_Raw'], row['Port']), axis=1)

# Generate DHCP commands
dhcp_commands = ["# DHCP Reservations for Victory Commons (Refined)"]
for _, row in df.iterrows():
    mac = row['mac'].upper() # Ensure colons are present (mac is already C4:42...)
    cmd = f"reservedlease add {mac} {row['Assigned_IP']} {row['Nickname']}"
    dhcp_commands.append(cmd)

# Write to file
output_path = r"D:\Antigravity\Crestron-Workspace\projects\LocktonDunning_VictoryCommons_JT022715\docs\dhcp_reservations_v2.txt"
with open(output_path, 'w') as f:
    f.write("\n".join(dhcp_commands))

print(f"Regenerated {len(dhcp_commands)-1} reservations to {output_path}")
print("\n--- SAMPLE COMMANDS ---")
print("\n".join(dhcp_commands[1:10]))

# Prepare Markdown Table for NETWORK.md
markdown_rows = []
for _, row in df.sort_values('Port').iterrows():
    markdown_rows.append(f"| {row['Nickname']} | `{row['mac']}` | `{row['Assigned_IP']}` | Port {row['Port']} |")

print("\n--- MARKDOWN TABLE SNIPPET ---")
print("\n".join(markdown_rows[:15]))
