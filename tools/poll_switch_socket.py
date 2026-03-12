import socket
import time
import re
import pandas as pd

# Netgear M4250 details
HOST = "172.22.0.10"
PORT = 23
USER = "admin"
PASSWORD = "Taurus123!"

def telnet_cmd(host, port, user, password):
    try:
        s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        s.settimeout(10)
        s.connect((host, port))
        
        def read_until(pattern):
            buff = b""
            start_time = time.time()
            while time.time() - start_time < 5:
                try:
                    chunk = s.recv(1024)
                    buff += chunk
                    text = chunk.decode('ascii', errors='ignore')
                    if re.search(pattern, text) or re.search(pattern, buff.decode('ascii', errors='ignore')):
                        return True
                except socket.timeout:
                    break
            return False

        read_until("User:|Username:")
        s.sendall(user.encode('ascii') + b"\r\n")
        read_until("Password:")
        s.sendall(password.encode('ascii') + b"\r\n")
        time.sleep(1)
        
        s.sendall(b"enable\r\n")
        time.sleep(0.5)
        # Handle secondary password
        s.settimeout(1)
        try:
            res = s.recv(1024).decode('ascii', errors='ignore')
            if "Password" in res:
                s.sendall(password.encode('ascii') + b"\r\n")
                time.sleep(0.5)
        except:
            pass
        s.settimeout(10)

        s.sendall(b"terminal length 0\r\n")
        time.sleep(0.5)
        
        final_results = {}
        
        # Test commands specifically based on help output
        probe_cmds = [
            "show mac-address-table",
            "show mac-addr-table",
            "show mac addr-table",
            "show arp",
            "show ip arp"
        ]
        
        for cmd in probe_cmds:
            print(f"Executing: {cmd}")
            s.sendall(cmd.encode('ascii') + b"\r\n")
            time.sleep(2)
            res = ""
            start_time = time.time()
            while time.time() - start_time < 5:
                try:
                    chunk = s.recv(4096).decode('ascii', errors='ignore')
                    res += chunk
                    if re.search(r'[#>]', res.strip().splitlines()[-1] if res.strip() else ""):
                        break
                except socket.timeout:
                    break
            
            print(f"  Result size: {len(res)}")
            if "Invalid" not in res and "not found" not in res and len(res) > 200:
                if "mac" in cmd:
                    final_results['mac'] = res
                else:
                    final_results['arp'] = res
        
        s.close()
        return final_results
        
    except Exception as e:
        print(f"Socket Error: {e}")
        return None

results = telnet_cmd(HOST, PORT, USER, PASSWORD)

def parse_mac_table(output):
    # Try different regex patterns
    lines = output.splitlines()
    mac_data = []
    for line in lines:
        # Pattern 1: MAC and 0/X port
        match = re.search(r'([0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2}).*?(\d{1,2}/\d{1,2})', line)
        if match:
            mac_data.append({'mac': match.group(1).upper().replace('-', ':'), 'port': match.group(2)})
        else:
            # Pattern 2: MAC and single port number
            match = re.search(r'([0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2}).*?(\d+)', line)
            if match and len(match.group(2)) < 3: # Ignore VLAN/others
                mac_data.append({'mac': match.group(1).upper().replace('-', ':'), 'port': match.group(2)})
    return mac_data

def parse_arp_table(output):
    lines = output.splitlines()
    arp_data = []
    for line in lines:
        match = re.search(r'(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}).*?([0-9A-Fa-f]{4}\.[0-9A-Fa-f]{4}\.[0-9A-Fa-f]{4}|[0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2})', line)
        if match:
            ip = match.group(1)
            mac_raw = match.group(2)
            if '.' in mac_raw:
                parts = mac_raw.split('.')
                mac = "".join(parts)
                mac = ":".join([mac[i:i+2] for i in range(0, 12, 2)]).upper()
            else:
                mac = mac_raw.upper().replace('-', ':')
            arp_data.append({'ip': ip, 'mac': mac})
    return arp_data

if results:
    if 'mac' in results and 'arp' in results:
        mac_list = parse_mac_table(results['mac'])
        arp_list = parse_arp_table(results['arp'])
        
        mac_df = pd.DataFrame(mac_list)
        arp_df = pd.DataFrame(arp_list)
        
        if not mac_df.empty and not arp_df.empty:
            full_switch_df = pd.merge(mac_df, arp_df, on='mac', how='left')
            full_switch_df.to_csv('live_switch_data.csv', index=False)
            print(f"\nCaptured {len(full_switch_df)} live entries.")
            
            pdf_ports = pd.read_csv('extracted_ports_page24.csv')
            # Handle both '3' and '0/3' formats
            pdf_ports['Switch_Port_1'] = pdf_ports['Port'].astype(str)
            pdf_ports['Switch_Port_2'] = pdf_ports['Port'].apply(lambda x: f"0/{x}" if str(x).isdigit() else x)
            
            m1 = pd.merge(pdf_ports, full_switch_df, left_on='Switch_Port_1', right_on='port', how='left')
            m2 = pd.merge(pdf_ports, full_switch_df, left_on='Switch_Port_2', right_on='port', how='left')
            
            # Combine
            final_map = m1.copy()
            final_map.update(m2)
            
            final_map.to_csv('final_port_ip_map.csv', index=False)
            print("\n--- FINAL PORT-TO-DEVICE-IP MAP ---")
            print(final_map[['Port', 'Device_Raw', 'mac', 'ip']].to_string())
        else:
            print("Could not parse data.")
            print("MAC Preview:", results['mac'][:500])
            print("ARP Preview:", results['arp'][:500])
    else:
        print(f"Missing tables in results: {results.keys()}")
        if 'mac' in results: print("MAC Preview:", results['mac'][:500])
        else: print("MAC command failed entirely.")
        if 'arp' in results: print("ARP Preview:", results['arp'][:500])
        else: print("ARP command failed entirely.")
else:
    print("Failed to poll switch.")
