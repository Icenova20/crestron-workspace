import socket
import time
import re

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
        # Handle secondary password if prompted
        s.settimeout(1)
        try:
            res = s.recv(1024).decode('ascii', errors='ignore')
            if "Password" in res:
                s.sendall(password.encode('ascii') + b"\r\n")
                time.sleep(0.5)
        except:
            pass
        s.settimeout(10)

        # List commands
        print("Listing commands...")
        s.sendall(b"show ?\r\n")
        time.sleep(3)
        
        res = ""
        try:
            while True:
                chunk = s.recv(4096).decode('ascii', errors='ignore')
                res += chunk
                # If we see "More" or similar, keep going (but terminal length 0 should prevent it)
                if "--More--" in res:
                    s.sendall(b" ")
                if re.search(r'[#>]', res.strip().splitlines()[-1] if res.strip() else ""):
                    break
        except:
            pass
        
        s.close()
        return res
        
    except Exception as e:
        print(f"Socket Error: {e}")
        return None

help_output = telnet_cmd(HOST, PORT, USER, PASSWORD)
if help_output:
    print("\n--- HELP OUTPUT ---")
    print(help_output)
    # Search for bridge or mac
    print("\n--- SEARCH RESULTS ---")
    for line in help_output.splitlines():
        if re.search(r'mac|bridge|arp|address', line, re.I):
            print(line)
else:
    print("Failed to get help output.")
