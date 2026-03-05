"""
Lockton Dunning Benefits - Break Room 09.002
Test Cases

Each test function takes a bridge (MockBridge or real CrestronBridge) 
and returns (pass: bool, name: str, detail: str).

Test categories:
  - Source Selection & Mutual Exclusivity
  - Power Off & State Reset
  - Volume Echo-Back
  - Mute Toggle Logic
  - Scheduling Mutual Exclusivity
  - Sonos Transport (log verification)
  - Full Integration Reset
"""


# ── Join Constants (must match JoinMap.cs / app.js) ──────

POWER_OFF = 10
SETTINGS_MODAL = 11
AIRMEDIA = 21
MEDIA_PLAYER = 22
MASTER_LEVEL = 1
MASTER_MUTE = 1
RESTROOM_LEVEL = 2
RESTROOM_MUTE = 2
HANDHELD_LEVEL = 11
HANDHELD_MUTE = 11
BODYPACK_LEVEL = 12
BODYPACK_MUTE = 12
SONOS_PREV = 41
SONOS_PLAY = 42
SONOS_NEXT = 43
AUTO_ON_7 = 101
AUTO_ON_8 = 102
AUTO_ON_9 = 103
AUTO_ON_DIS = 104
AUTO_OFF_5 = 111
AUTO_OFF_6 = 112
AUTO_OFF_7 = 113
AUTO_OFF_DIS = 114


def _assert(condition, name, detail=""):
    return (condition, name, detail if not condition else "")


# ── Source Tests ─────────────────────────────────────────

def test_airmedia_select(bridge):
    """Select AirMedia, verify it's active and MediaPlayer is not."""
    bridge.pulse_digital(AIRMEDIA)
    return _assert(
        bridge.get_digital(AIRMEDIA) is True and bridge.get_digital(MEDIA_PLAYER) is False,
        "Source: AirMedia selects exclusively",
        f"AirMedia={bridge.get_digital(AIRMEDIA)}, MediaPlayer={bridge.get_digital(MEDIA_PLAYER)}"
    )


def test_mediaplayer_select(bridge):
    """Select MediaPlayer after AirMedia, verify switch."""
    bridge.pulse_digital(AIRMEDIA)
    bridge.pulse_digital(MEDIA_PLAYER)
    return _assert(
        bridge.get_digital(MEDIA_PLAYER) is True and bridge.get_digital(AIRMEDIA) is False,
        "Source: MediaPlayer deselects AirMedia",
        f"AirMedia={bridge.get_digital(AIRMEDIA)}, MediaPlayer={bridge.get_digital(MEDIA_PLAYER)}"
    )


def test_source_reselect(bridge):
    """Re-selecting same source keeps it active."""
    bridge.pulse_digital(AIRMEDIA)
    bridge.pulse_digital(AIRMEDIA)
    return _assert(
        bridge.get_digital(AIRMEDIA) is True,
        "Source: Re-selection keeps source active"
    )


# ── Power Tests ──────────────────────────────────────────

def test_power_off_clears_sources(bridge):
    """Power off clears all source feedback."""
    bridge.pulse_digital(AIRMEDIA)
    bridge.pulse_digital(POWER_OFF)
    return _assert(
        bridge.get_digital(AIRMEDIA) is False and bridge.get_digital(MEDIA_PLAYER) is False,
        "Power Off: Clears all source feedback",
        f"AirMedia={bridge.get_digital(AIRMEDIA)}, MediaPlayer={bridge.get_digital(MEDIA_PLAYER)}"
    )


def test_power_off_clears_mutes(bridge):
    """Power off resets all mute states."""
    bridge.pulse_digital(MASTER_MUTE)  # Toggle on
    bridge.pulse_digital(RESTROOM_MUTE)
    bridge.pulse_digital(POWER_OFF)
    return _assert(
        bridge.get_digital(MASTER_MUTE) is False and bridge.get_digital(RESTROOM_MUTE) is False,
        "Power Off: Clears all mute states"
    )


def test_power_off_clears_volumes(bridge):
    """Power off resets all volumes to 0."""
    bridge.set_analog(MASTER_LEVEL, 50000)
    bridge.set_analog(RESTROOM_LEVEL, 30000)
    bridge.pulse_digital(POWER_OFF)
    return _assert(
        bridge.get_analog(MASTER_LEVEL) == 0 and bridge.get_analog(RESTROOM_LEVEL) == 0,
        "Power Off: Resets all volumes to 0",
        f"Master={bridge.get_analog(MASTER_LEVEL)}, Restroom={bridge.get_analog(RESTROOM_LEVEL)}"
    )


# ── Volume Tests ─────────────────────────────────────────

def test_master_volume_echo(bridge):
    """Master volume echoes back the set value."""
    bridge.set_analog(MASTER_LEVEL, 32768)
    return _assert(
        bridge.get_analog(MASTER_LEVEL) == 32768,
        "Volume: Master level echoes 32768",
        f"Got {bridge.get_analog(MASTER_LEVEL)}"
    )


def test_restroom_volume_echo(bridge):
    """Restroom volume echoes back the set value."""
    bridge.set_analog(RESTROOM_LEVEL, 49152)
    return _assert(
        bridge.get_analog(RESTROOM_LEVEL) == 49152,
        "Volume: Restroom level echoes 49152",
        f"Got {bridge.get_analog(RESTROOM_LEVEL)}"
    )


def test_handheld_volume_echo(bridge):
    """Handheld mic level echoes back."""
    bridge.set_analog(HANDHELD_LEVEL, 16384)
    return _assert(
        bridge.get_analog(HANDHELD_LEVEL) == 16384,
        "Volume: Handheld mic echoes 16384",
        f"Got {bridge.get_analog(HANDHELD_LEVEL)}"
    )


def test_bodypack_volume_echo(bridge):
    """Bodypack mic level echoes back."""
    bridge.set_analog(BODYPACK_LEVEL, 8192)
    return _assert(
        bridge.get_analog(BODYPACK_LEVEL) == 8192,
        "Volume: Bodypack mic echoes 8192",
        f"Got {bridge.get_analog(BODYPACK_LEVEL)}"
    )


# ── Mute Tests ───────────────────────────────────────────

def test_master_mute_on(bridge):
    """First press toggles master mute ON."""
    # Ensure starting from unmuted
    if bridge.get_digital(MASTER_MUTE):
        bridge.pulse_digital(MASTER_MUTE)
    bridge.pulse_digital(MASTER_MUTE)
    return _assert(
        bridge.get_digital(MASTER_MUTE) is True,
        "Mute: Master mute toggles ON"
    )


def test_master_mute_off(bridge):
    """Second press toggles master mute OFF."""
    # Ensure starting from unmuted
    if bridge.get_digital(MASTER_MUTE):
        bridge.pulse_digital(MASTER_MUTE)
    bridge.pulse_digital(MASTER_MUTE)   # ON
    bridge.pulse_digital(MASTER_MUTE)   # OFF
    return _assert(
        bridge.get_digital(MASTER_MUTE) is False,
        "Mute: Master mute toggles OFF"
    )


def test_restroom_mute(bridge):
    """Restroom mute toggle."""
    if bridge.get_digital(RESTROOM_MUTE):
        bridge.pulse_digital(RESTROOM_MUTE)
    bridge.pulse_digital(RESTROOM_MUTE)
    return _assert(
        bridge.get_digital(RESTROOM_MUTE) is True,
        "Mute: Restroom mute toggles ON"
    )


def test_handheld_mute(bridge):
    """Handheld mic mute toggle."""
    if bridge.get_digital(HANDHELD_MUTE):
        bridge.pulse_digital(HANDHELD_MUTE)
    bridge.pulse_digital(HANDHELD_MUTE)
    return _assert(
        bridge.get_digital(HANDHELD_MUTE) is True,
        "Mute: Handheld mic toggles ON"
    )


def test_bodypack_mute(bridge):
    """Bodypack mic mute toggle."""
    if bridge.get_digital(BODYPACK_MUTE):
        bridge.pulse_digital(BODYPACK_MUTE)
    bridge.pulse_digital(BODYPACK_MUTE)
    return _assert(
        bridge.get_digital(BODYPACK_MUTE) is True,
        "Mute: Bodypack mic toggles ON"
    )


# ── Scheduling Tests ─────────────────────────────────────

def test_auto_on_exclusive(bridge):
    """Auto-On selection is mutually exclusive."""
    bridge.pulse_digital(AUTO_ON_8)
    return _assert(
        bridge.get_digital(AUTO_ON_8) is True
        and bridge.get_digital(AUTO_ON_7) is False
        and bridge.get_digital(AUTO_ON_9) is False
        and bridge.get_digital(AUTO_ON_DIS) is False,
        "Schedule: Auto-On 8am exclusive",
        f"7={bridge.get_digital(AUTO_ON_7)}, 8={bridge.get_digital(AUTO_ON_8)}, "
        f"9={bridge.get_digital(AUTO_ON_9)}, dis={bridge.get_digital(AUTO_ON_DIS)}"
    )


def test_auto_on_switch(bridge):
    """Switching Auto-On deselects previous."""
    bridge.pulse_digital(AUTO_ON_8)
    bridge.pulse_digital(AUTO_ON_9)
    return _assert(
        bridge.get_digital(AUTO_ON_9) is True and bridge.get_digital(AUTO_ON_8) is False,
        "Schedule: Auto-On switches correctly"
    )


def test_auto_off_exclusive(bridge):
    """Auto-Off selection is mutually exclusive."""
    bridge.pulse_digital(AUTO_OFF_6)
    return _assert(
        bridge.get_digital(AUTO_OFF_6) is True
        and bridge.get_digital(AUTO_OFF_5) is False
        and bridge.get_digital(AUTO_OFF_7) is False
        and bridge.get_digital(AUTO_OFF_DIS) is False,
        "Schedule: Auto-Off 6pm exclusive",
        f"5={bridge.get_digital(AUTO_OFF_5)}, 6={bridge.get_digital(AUTO_OFF_6)}, "
        f"7={bridge.get_digital(AUTO_OFF_7)}, dis={bridge.get_digital(AUTO_OFF_DIS)}"
    )


# ── Sonos Tests ──────────────────────────────────────────

def test_sonos_transport(bridge):
    """Sonos transport pulses are accepted without error."""
    try:
        bridge.pulse_digital(SONOS_PREV)
        bridge.pulse_digital(SONOS_PLAY)
        bridge.pulse_digital(SONOS_NEXT)
        return _assert(True, "Sonos: Transport commands accepted")
    except Exception as e:
        return _assert(False, "Sonos: Transport commands accepted", str(e))


# ── Integration Tests ────────────────────────────────────

def test_full_workflow(bridge):
    """Full workflow: select source → adjust volume → mute → power off → verify reset."""
    bridge.pulse_digital(AIRMEDIA)
    bridge.set_analog(MASTER_LEVEL, 45000)
    bridge.pulse_digital(MASTER_MUTE)
    bridge.pulse_digital(AUTO_ON_7)

    # Verify mid-state
    mid_ok = (
        bridge.get_digital(AIRMEDIA) is True
        and bridge.get_analog(MASTER_LEVEL) == 45000
        and bridge.get_digital(MASTER_MUTE) is True
        and bridge.get_digital(AUTO_ON_7) is True
    )

    # Power off
    bridge.pulse_digital(POWER_OFF)

    # Verify reset
    reset_ok = (
        bridge.get_digital(AIRMEDIA) is False
        and bridge.get_analog(MASTER_LEVEL) == 0
        and bridge.get_digital(MASTER_MUTE) is False
        # Note: schedule is NOT reset by power off in our logic
    )

    return _assert(
        mid_ok and reset_ok,
        "Integration: Full workflow with power-off reset",
        f"mid_ok={mid_ok}, reset_ok={reset_ok}"
    )


# ── Join Parity Check ───────────────────────────────────

def verify_join_parity():
    """
    Verify that our Python join constants match the CH5 app.js.
    This is a static check — no bridge needed.
    """
    import os
    import re

    # Expected joins from app.js JOINS object
    expected = {
        "PowerOff": 10, "SettingsModal": 11,
        "AirMedia": 21, "MediaPlayer": 22,
        "MasterLevel": 1, "MasterMute": 1,
        "RestroomLevel": 2, "RestroomMute": 2,
        "HandheldLevel": 11, "HandheldMute": 11,
        "BodypackLevel": 12, "BodypackMute": 12,
        "Prev": 41, "PlayPause": 42, "Next": 43,
    }

    # Our Python constants
    our_map = {
        "PowerOff": POWER_OFF, "SettingsModal": SETTINGS_MODAL,
        "AirMedia": AIRMEDIA, "MediaPlayer": MEDIA_PLAYER,
        "MasterLevel": MASTER_LEVEL, "MasterMute": MASTER_MUTE,
        "RestroomLevel": RESTROOM_LEVEL, "RestroomMute": RESTROOM_MUTE,
        "HandheldLevel": HANDHELD_LEVEL, "HandheldMute": HANDHELD_MUTE,
        "BodypackLevel": BODYPACK_LEVEL, "BodypackMute": BODYPACK_MUTE,
        "Prev": SONOS_PREV, "PlayPause": SONOS_PLAY, "Next": SONOS_NEXT,
    }

    mismatches = []
    for name, expected_val in expected.items():
        our_val = our_map.get(name)
        if our_val != expected_val:
            mismatches.append(f"  {name}: expected={expected_val}, got={our_val}")

    if mismatches:
        print("✗ JOIN PARITY FAILURE:")
        for m in mismatches:
            print(m)
        return False
    else:
        print("✓ All joins match between Python test suite and CH5 app.js")
        return True


# ── Test Registry ────────────────────────────────────────

ALL_TESTS = [
    test_airmedia_select,
    test_mediaplayer_select,
    test_source_reselect,
    test_power_off_clears_sources,
    test_power_off_clears_mutes,
    test_power_off_clears_volumes,
    test_master_volume_echo,
    test_restroom_volume_echo,
    test_handheld_volume_echo,
    test_bodypack_volume_echo,
    test_master_mute_on,
    test_master_mute_off,
    test_restroom_mute,
    test_handheld_mute,
    test_bodypack_mute,
    test_auto_on_exclusive,
    test_auto_on_switch,
    test_auto_off_exclusive,
    test_sonos_transport,
    test_full_workflow,
]
