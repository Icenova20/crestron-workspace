"""
Lockton Dunning Benefits - Break Room 09.002
Mock Bridge for Local Dry-Run Testing

Simulates the C# RoomLogic + PanelManager behavior in pure Python.
Allows running the full test suite on WSL2 without a Crestron processor.

This mirrors the behavior of:
  - RoomLogic.cs  (state machine)
  - PanelManager.cs (feedback state tracking)
"""


class MockBridge:
    """In-memory simulation of the Crestron processor join state."""

    # Join map (must match JoinMap.cs exactly)
    SOURCES = [21, 22]
    AUTO_ON = [101, 102, 103, 104]
    AUTO_OFF = [111, 112, 113, 114]
    MUTE_JOINS = [1, 2, 11, 12]
    VOLUME_JOINS = [1, 2, 11, 12]
    SONOS = [41, 42, 43]

    POWER_OFF = 10
    SETTINGS_MODAL = 11

    def __init__(self):
        self.digital_state = {}  # join -> bool
        self.analog_state = {}   # join -> int (0-65535)
        self.event_log = []      # list of (action, join, value) tuples

        # Initialize known joins
        for j in self.MUTE_JOINS:
            self.digital_state[j] = False
        for j in self.SOURCES:
            self.digital_state[j] = False
        for j in self.AUTO_ON:
            self.digital_state[j] = (j == 104)  # Disable is default
        for j in self.AUTO_OFF:
            self.digital_state[j] = (j == 114)  # Disable is default
        for j in self.VOLUME_JOINS:
            self.analog_state[j] = 0

    def pulse_digital(self, join):
        """Simulate a digital press (rising edge) from the panel."""
        self.event_log.append(("pulse", join, True))

        # Power Off
        if join == self.POWER_OFF:
            self._power_off()
            return

        # Note: SettingsModal (d11) shares join number with Handheld Mute.
        # It is informational only (no state change), so we do NOT
        # short-circuit here — the mute toggle below handles join 11.

        # Source Selection (mutually exclusive)
        if join in self.SOURCES:
            for s in self.SOURCES:
                self.digital_state[s] = (s == join)
            return

        # Mute Toggle
        if join in self.MUTE_JOINS:
            self.digital_state[join] = not self.digital_state.get(join, False)
            return

        # Sonos (log only)
        if join in self.SONOS:
            return

        # Auto-On (mutually exclusive)
        if join in self.AUTO_ON:
            for j in self.AUTO_ON:
                self.digital_state[j] = (j == join)
            return

        # Auto-Off (mutually exclusive)
        if join in self.AUTO_OFF:
            for j in self.AUTO_OFF:
                self.digital_state[j] = (j == join)
            return

    def set_analog(self, join, value):
        """Simulate an analog value change from the panel."""
        self.event_log.append(("analog", join, value))
        if join in self.VOLUME_JOINS:
            self.analog_state[join] = value

    def get_digital(self, join):
        """Query current digital feedback state."""
        return self.digital_state.get(join, False)

    def get_analog(self, join):
        """Query current analog feedback value."""
        return self.analog_state.get(join, 0)

    def _power_off(self):
        """Reset all state (mirrors RoomLogic.PowerOff)."""
        for s in self.SOURCES:
            self.digital_state[s] = False
        for j in self.MUTE_JOINS:
            self.digital_state[j] = False
        for j in self.VOLUME_JOINS:
            self.analog_state[j] = 0
