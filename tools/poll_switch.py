import pandas as pd
import subprocess
import json
import re

# Load the port-to-device mapping we extracted
port_map_df = pd.read_csv('extracted_ports_page24.csv')

def get_switch_data():
    ps_command = """
    Import-Module Posh-SSH;
    $secPass = ConvertTo-SecureString 'Taurus123!' -AsPlainText -Force;
    $cred = New-Object System.Management.Automation.PSCredential ('admin', $secPass);
    $session = New-SSHSession -ComputerName '172.22.0.10' -Credential $cred -AcceptKey -ErrorAction Stop;
    
    $macTable = Invoke-SSHCommand -SessionId $session.SessionId -Command 'show mac-address-table' | Select-Object -ExpandProperty Output;
    $arpTable = Invoke-SSHCommand -SessionId $session.SessionId -Command 'show ip arp' | Select-Object -ExpandProperty Output;
    
    Remove-SSHSession -SessionId $session.SessionId;
    
    @{
        mac = $macTable;
        arp = $arpTable;
    } | ConvertTo-Json
    """
    
    process = subprocess.Popen(["pwsh", "-Command", ps_command], stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
    stdout, stderr = process.communicate()
    
    if stderr:
        print(f"Error: {stderr}")
        return None
    
    return json.loads(stdout)

def parse_mac_table(output):
    # Example table format (Netgear M4250 usually looks like this):
    # Vlan     Mac Address           Type    Ports
    # ----     -----------           ----    -----
    # 1        28:94:01:7F:D6:37     Self    0/1
    # 1        00:10:7F:00:00:00     Dynamic 0/2
    lines = output.splitlines()
    mac_data = []
    for line in lines:
        # Regex to find MAC and Port (e.g. 0/5 or 1/12)
        match = re.search(r'([0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2}).*?(\d{1,2}/\d{1,2})', line)
        if match:
            mac_data.append({'mac': match.group(1).upper().replace('-', ':'), 'port': match.group(2)})
    return mac_data

def parse_arp_table(output):
    # Example ARP format:
    # Protocol  Address          Age (min)  Hardware Addr   Type   Interface
    # Internet  172.22.0.1             -   0010.7f00.0000  ARPA   Vlan 1
    lines = output.splitlines()
    arp_data = []
    for line in lines:
        # Regex for IP and MAC (Standard dot or colon format)
        match = re.search(r'(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}).*?([0-9A-Fa-f]{4}\.[0-9A-Fa-f]{4}\.[0-9A-Fa-f]{4}|[0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2})', line)
        if match:
            ip = match.group(1)
            mac_raw = match.group(2)
            # Normalize MAC to AA:BB:CC...
            if '.' in mac_raw: # Cisco/Netgear 0000.0000.0000 format
                parts = mac_raw.split('.')
                mac = "".join(parts)
                mac = ":".join([mac[i:i+2] for i in range(0, 12, 2)]).upper()
            else:
                mac = mac_raw.upper().replace('-', ':')
            arp_data.append({'ip': ip, 'mac': mac})
    return arp_data

# Main
print("Polling switch...")
data = get_switch_data()
if data:
    macs = parse_mac_table(data['mac'])
    arps = parse_arp_table(data['arp'])
    
    mac_df = pd.DataFrame(macs)
    arp_df = pd.DataFrame(arps)
    
    # Merge MAC and ARP to get Port -> IP
    full_switch_df = pd.merge(mac_df, arp_df, on='mac', how='left')
    
    # Save the live data
    full_switch_df.to_csv('live_switch_data.csv', index=False)
    print(f"Captured {len(full_switch_df)} live entries.")
    print(full_switch_df.to_string())
else:
    print("Failed to get switch data.")
