#!/usr/bin/env python3
"""
Lockton Dunning Benefits - Break Room 09.002
Test Runner

Dual-mode test runner:
  1. --dry-run (default): Uses MockBridge for local development on WSL2
  2. On-processor: Called by TestBridge.cs via Crestron PythonInterface

Usage:
  python3 test_runner.py              # dry-run mode (default)
  python3 test_runner.py --dry-run    # explicit dry-run
  python3 test_runner.py --parity     # join parity check only
"""

import sys
import os

# Add tests directory to path for imports
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from mock_bridge import MockBridge
from test_cases import ALL_TESTS, verify_join_parity


def run_tests(bridge, verbose=True):
    """Execute all test cases against the given bridge."""
    passed = 0
    failed = 0
    failures = []

    if verbose:
        print("")
        print("╔══════════════════════════════════════════════════╗")
        print("║   LOCKTON DUNNING - PANEL JOIN TEST SUITE        ║")
        print("║   Break Room 09.002 (Python)                     ║")
        print("╚══════════════════════════════════════════════════╝")
        print("")

    for test_fn in ALL_TESTS:
        # Each test gets a fresh bridge to avoid state leakage
        fresh_bridge = MockBridge()

        try:
            success, name, detail = test_fn(fresh_bridge)
        except Exception as e:
            success = False
            name = test_fn.__name__
            detail = f"Exception: {e}"

        if success:
            passed += 1
            if verbose:
                print(f"  ✓ PASS: {name}")
        else:
            failed += 1
            failures.append((name, detail))
            if verbose:
                print(f"  ✗ FAIL: {name}")
                if detail:
                    print(f"          {detail}")

    total = passed + failed

    if verbose:
        print("")
        print("══════════════════════════════════════════════════")
        print(f"  RESULTS: {passed}/{total} passed, {failed} failed")
        if failed == 0:
            print("  ✓ ALL TESTS PASSED")
        else:
            print(f"  ✗ {failed} TEST(S) FAILED")
            print("")
            print("  Failed tests:")
            for name, detail in failures:
                print(f"    - {name}: {detail}")
        print("══════════════════════════════════════════════════")
        print("")

    return failed == 0


# ── Crestron On-Processor Entry Point ────────────────────

def crestron_main(data):
    """
    Entry point when called from SIMPL# Pro via PythonInterface.
    
    The 'data' parameter receives the JSON string from TestBridge.cs.
    Results are sent back via send_data().
    
    NOTE: This function signature matches the Crestron Python runtime
    expectations. The send_data function is injected by the runtime.
    """
    import json

    try:
        cmd = json.loads(data) if data else {}

        if cmd.get("cmd") == "run_all":
            # Run all tests using the bridge commands
            # In processor mode, each test would send individual
            # pulse/set/query commands back to C# via send_data
            result = {"status": "started", "total_tests": len(ALL_TESTS)}
            send_data(json.dumps(result))  # noqa: F821 - injected by Crestron runtime
        else:
            send_data(json.dumps({"error": "unknown command"}))  # noqa: F821
    except Exception as e:
        send_data(json.dumps({"error": str(e)}))  # noqa: F821


# ── CLI Entry Point ──────────────────────────────────────

def main():
    args = sys.argv[1:]

    if "--parity" in args:
        success = verify_join_parity()
        sys.exit(0 if success else 1)

    if "--dry-run" in args or not args:
        print("[Mode: Dry-Run (MockBridge)]")
        print("")

        # First run parity check
        print("── Join Parity Check ──")
        verify_join_parity()
        print("")

        # Then run all tests
        print("── Test Suite ──")
        success = run_tests(MockBridge(), verbose=True)
        sys.exit(0 if success else 1)

    print(f"Unknown argument: {args[0]}")
    print("Usage: python3 test_runner.py [--dry-run | --parity]")
    sys.exit(1)


if __name__ == "__main__":
    main()
