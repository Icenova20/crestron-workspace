import telnetlib
import pandas as pd
import time
import re

# Netgear M4250 details
HOST = "172.22.0.10"
USER = "admin"
PASSWORD = "Taurus123!"

def get_netgear_data():
    try:
        print(f"Connecting to {HOST} via Telnet...")
        tn = telnetlib.Telnet(HOST, 23, timeout=10)
        
        # Expect Username
        tn.read_until(b"User:", timeout=5)
        tn.write(USER.encode('ascii') + b"\n")
        
        # Expect Password
        tn.read_until(b"Password:", timeout=5)
        tn.write(PASSWORD.encode('ascii') + b"\n")
        
        time.sleep(1)
        
        # Enter enable mode if necessary (usually (Main Menu) or (Switch) prompt)
        # Netgear M4250 often has a menu-driven start or goes straight to CLI
        # If it's the standard CLI:
        tn.write(b"enable\n")
        time.sleep(0.5)
        
        # Disable paging for raw output
        tn.write(b"terminal length 0\n")
        time.sleep(0.5)
        
        print("Polling MAC table...")
        tn.write(b"show mac-address-table\n")
        time.sleep(2)
        mac_output = tn.read_very_eager().decode('ascii')
        
        print("Polling ARP table...")
        tn.write(b"show ip arp\n")
        time.sleep(2)
        arp_output = tn.read_very_eager().decode('ascii')
        
        tn.close()
        return mac_output, arp_output
        
    except Exception as e:
        print(f"Telnet Error: {e}")
        return None, None

def parse_mac_table(output):
    lines = output.splitlines()
    mac_data = []
    for line in lines:
        # Regex for MAC and Port (e.g. 0/5 or 1/12)
        match = re.search(r'([0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2}).*?(\d{1,2}/\d{1,2})', line)
        if match:
            mac_data.append({'mac': match.group(1).upper().replace('-', ':'), 'port': match.group(2)})
    return mac_data

def parse_arp_table(output):
    lines = output.splitlines()
    arp_data = []
    for line in lines:
        # Regex for IP and MAC (Standard dot or colon format)
        match = re.search(r'(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}).*?([0-9A-Fa-f]{4}\.[0-9A-Fa-f]{4}\.[0-9A-Fa-f]{4}|[0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2})', line)
        if match:
            ip = match.group(1)
            mac_raw = match.group(2)
            # Normalize MAC
            if '.' in mac_raw:
                parts = mac_raw.split('.')
                mac = "".join(parts)
                mac = ":".join([mac[i:i+2] for i in range(0, 12, 2)]).upper()
            else:
                mac = mac_raw.upper().replace('-', ':')
            arp_data.append({'ip': ip, 'mac': mac})
    return arp_data

# Main Execution
mac_raw, arp_raw = get_netgear_data()

if mac_raw and arp_raw:
    mac_list = parse_mac_table(mac_raw)
    arp_list = parse_arp_table(arp_raw)
    
    mac_df = pd.DataFrame(mac_list)
    arp_df = pd.DataFrame(arp_list)
    
    if not mac_df.empty and not arp_df.empty:
        full_switch_df = pd.merge(mac_df, arp_df, on='mac', how='left')
        full_switch_df.to_csv('live_switch_data.csv', index=False)
        print(f"\nCaptured {len(full_switch_df)} live entries.")
        print(full_switch_df.to_string())
        
        # Cross-reference with our Extracted PDF Ports
        try:
            pdf_ports = pd.read_csv('extracted_ports_page24.csv')
            # Normalize port formats if necessary (extracted uses '1', '2', switch uses '0/1', '0/2')
            pdf_ports['Switch_Port'] = pdf_ports['Port'].apply(lambda x: f"0/{x}" if str(x).isdigit() else x)
            
            final_map = pd.merge(pdf_ports, full_switch_df, left_on='Switch_Port', right_on='port', how='left')
            final_map.to_csv('final_port_ip_map.csv', index=False)
            print("\n--- FINAL PORT-TO-DEVICE-IP MAP ---")
            print(final_map[['Port', 'Device_Raw', 'mac', 'ip']].to_string())
        except Exception as e:
            print(f"Mapping error: {e}")
    else:
        print("Could not parse data. Raw samples:")
        print("MAC Sample:", mac_raw[:500])
        print("ARP Sample:", arp_raw[:500])
else:
    print("Failed to poll switch via Telnet.")
