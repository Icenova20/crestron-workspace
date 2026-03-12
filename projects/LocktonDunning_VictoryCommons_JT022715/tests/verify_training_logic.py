#!/usr/bin/env python3
"""
Lockton Dunning Benefits - Training Room 09.091
Headless Logic Verification Script

This script simulates the behavior of 'Lockton_Training_Logic.usp'
by mocking the inputs and asserting the expected pulse/serial outputs.
"""

class MockTrainingLogic:
    def __init__(self, left_ip="10.1.1.21", right_ip="10.1.1.22"):
        # Parameters
        self.p_nvx_left_ip = left_ip
        self.p_nvx_right_ip = right_ip
        
        # Inputs
        self.cisco_mic_muted_fb = 0
        self.cisco_active_source_fb = ""
        self.cisco_standby_state_fb = ""
        self.shure_mxw_button_press = [0, 0]
        
        # Outputs (Tracking pulses and values)
        self.pulses = []
        self.nvx_route_name = ""
        self.nvx_decoder_ip = ""
        
    def reset_output_tracking(self):
        self.pulses = []
        
    def pulse(self, name):
        self.pulses.append(name)

    # --- Simulated Module Logic (Matching .usp exactly) ---
    
    def change_active_source(self, val):
        self.cisco_active_source_fb = val
        if val == "Wallplate":
            self.route_nvx("Wallplate")
        elif val == "AirMedia":
            self.route_nvx("AirMedia")
            
    def route_nvx(self, name):
        # First Screen
        self.nvx_route_name = name
        self.nvx_decoder_ip = self.p_nvx_left_ip
        self.pulse("NVX_Do_Route")
        
        # Second Screen (simulating delay)
        self.nvx_route_name = name
        self.nvx_decoder_ip = self.p_nvx_right_ip
        self.pulse("NVX_Do_Route")
        
    def change_standby_state(self, val):
        self.cisco_standby_state_fb = val
        if val in ["Off", "Halfwake"]:
            self.pulse("Planar_Power_On")
        elif val == "Standby":
            self.pulse("Planar_Power_Off")
            
    def change_mic_mute_fb(self, val):
        self.cisco_mic_muted_fb = val
        if val == 1:
            self.pulse("Shure_Mxw_Mute_On_All")
            self.pulse("Shure_Mxa920_Mute_On")
        else:
            self.pulse("Shure_Mxw_Mute_Off_All")
            self.pulse("Shure_Mxa920_Mute_Off")
            
    def push_mxw_button(self, index):
        self.pulse("Cisco_Toggle_Mic_Mute")

# --- Test Cases ---

def test_source_routing():
    logic = MockTrainingLogic()
    print("Testing NVX Routing...")
    
    logic.change_active_source("Wallplate")
    assert "NVX_Do_Route" in logic.pulses
    assert logic.pulses.count("NVX_Do_Route") == 2
    assert logic.nvx_route_name == "Wallplate"
    assert logic.nvx_decoder_ip == logic.p_nvx_right_ip
    print("  ✓ Wallplate routing correct")
    
    logic.reset_output_tracking()
    logic.change_active_source("AirMedia")
    assert logic.nvx_route_name == "AirMedia"
    print("  ✓ AirMedia routing correct")

def test_power_logic():
    logic = MockTrainingLogic()
    print("Testing Power Logic...")
    
    logic.change_standby_state("Halfwake")
    assert "Planar_Power_On" in logic.pulses
    print("  ✓ Power On (Halfwake) correct")
    
    logic.reset_output_tracking()
    logic.change_standby_state("Standby")
    assert "Planar_Power_Off" in logic.pulses
    print("  ✓ Power Off (Standby) correct")

def test_mute_sync():
    logic = MockTrainingLogic()
    print("Testing Mute Sync...")
    
    logic.change_mic_mute_fb(1)
    assert "Shure_Mxw_Mute_On_All" in logic.pulses
    assert "Shure_Mxa920_Mute_On" in logic.pulses
    print("  ✓ Mute On sync correct")
    
    logic.reset_output_tracking()
    logic.change_mic_mute_fb(0)
    assert "Shure_Mxw_Mute_Off_All" in logic.pulses
    assert "Shure_Mxa920_Mute_Off" in logic.pulses
    print("  ✓ Mute Off sync correct")

def test_button_pass_through():
    logic = MockTrainingLogic()
    print("Testing Button Passthrough...")
    
    logic.push_mxw_button(0)
    assert "Cisco_Toggle_Mic_Mute" in logic.pulses
    print("  ✓ MXW Button -> Cisco Mute Toggle correct")

if __name__ == "__main__":
    print("╔══════════════════════════════════════════════════╗")
    print("║   LOCKTON DUNNING - TRAINING ROOM LOGIC TEST     ║")
    print("║   Headless 09.091 (Python Mock)                  ║")
    print("╚══════════════════════════════════════════════════╝")
    print("")
    
    try:
        test_source_routing()
        test_power_logic()
        test_mute_sync()
        test_button_pass_through()
        print("\n  RESULTS: ALL LOGIC TESTS PASSED")
    except AssertionError as e:
        print(f"\n  ✗ TEST FAILED")
        exit(1)
