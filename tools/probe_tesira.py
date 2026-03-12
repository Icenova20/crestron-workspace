import socket
import time
import re

# Tesira details
HOST = "172.22.40.14"
PORT = 22 # Testing SSH port first
USER = "default"
PASS = ""

def probe_tesira_ssh():
    print(f"Probing Tesira SSH at {HOST}:{PORT}...")
    try:
        s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        s.settimeout(5)
        s.connect((HOST, PORT))
        banner = s.recv(1024).decode('ascii', errors='ignore')
        print(f"SSH Banner: {banner.strip()}")
        s.close()
        return True
    except Exception as e:
        print(f"SSH Connection Failed: {e}")
        return False

def probe_tesira_telnet():
    # Biamp often uses 23 for TTP/Telnet if SSH is acting up
    print(f"Probing Tesira Telnet at {HOST}:23...")
    try:
        s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        s.settimeout(5)
        s.connect((HOST, 23))
        s.sendall(b"\r\n")
        time.sleep(1)
        res = s.recv(1024).decode('ascii', errors='ignore')
        print(f"Telnet Response: {res.strip()}")
        s.close()
        return True
    except Exception as e:
        print(f"Telnet Connection Failed: {e}")
        return False

if __name__ == "__main__":
    ssh_ok = probe_tesira_ssh()
    telnet_ok = probe_tesira_telnet()
    
    if not ssh_ok and not telnet_ok:
        print("\n[CRITICAL] Both SSH and Telnet failed. Check routing/firewalls.")
    elif ssh_ok:
        print("\n[SUCCESS] SSH Service is listening. Issue may be credentials or driver handshake.")
    elif telnet_ok:
        print("\n[INFO] Telnet is open but SSH is not. The driver might need to switch to Telnet mode.")
