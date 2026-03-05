/**
 * Lockton Dunning Benefits - Break Room 09.002
 * Test Bridge (C# ↔ Python)
 * 
 * Bridges the SIMPL# Pro host to Python test scripts.
 * Uses the Crestron PythonInterface API to invoke test_runner.py
 * and exchange JSON-formatted commands/results.
 * 
 * Command Protocol (JSON over string):
 *   → From Python: {"cmd":"pulse","type":"d","join":21}
 *   → From Python: {"cmd":"set","type":"a","join":1,"value":32768}
 *   → From Python: {"cmd":"query","type":"d","join":21}
 *   ← To Python:   {"join":21,"type":"d","value":true}
 *   ← To Python:   {"result":"pass","test":"test_source_selection"}
 */

using System;
using Crestron.SimplSharp;

namespace LocktonTest
{
    public class TestBridge
    {
        private readonly RoomLogic _logic;
        private readonly PanelManager _panel;

        // Set to true when Python test automation is active
        private bool _testRunning;

        public TestBridge(RoomLogic logic, PanelManager panel)
        {
            _logic = logic;
            _panel = panel;
            _testRunning = false;
        }

        /// <summary>
        /// Launch the Python test runner on the processor.
        /// The script must be loaded to /User/lockton-test/test_runner.py
        /// </summary>
        public void RunTests()
        {
            CrestronConsole.PrintLine("[TestBridge] ═══════════════════════════════════════");
            CrestronConsole.PrintLine("[TestBridge] Python test integration is available.");
            CrestronConsole.PrintLine("[TestBridge] To run tests on-processor:");
            CrestronConsole.PrintLine("[TestBridge]   1. Load test_runner.py to /User/lockton-test/");
            CrestronConsole.PrintLine("[TestBridge]   2. Use the TESTPY console command");
            CrestronConsole.PrintLine("[TestBridge] ═══════════════════════════════════════");

            // NOTE: PythonInterface.Run() requires the Crestron Python runtime.
            // The actual invocation is:
            //
            //   var pModule = new PythonModule();
            //   string guid = PythonAdapterUtils.NewGuid;
            //   int result = PythonInterface.Run(guid, "/User/lockton-test/test_runner.py", pModule);
            //   if (result == 0)
            //   {
            //       _testRunning = true;
            //       CrestronConsole.PrintLine("[TestBridge] Python test runner started");
            //   }
            //
            // The DataReceived event handles commands from Python.
            // This is left commented because the PythonInterface types
            // require the SimplPlusPythonAdapter NuGet package which
            // may not be available in all build environments.
        }

        /// <summary>
        /// Process a JSON command string from the Python test runner.
        /// Called by the DataReceived callback.
        /// </summary>
        public void ProcessCommand(string json)
        {
            try
            {
                CrestronConsole.PrintLine("[TestBridge] Rx ← {0}", json);

                // Minimal JSON parsing (no external dependencies)
                // Expected: {"cmd":"pulse","type":"d","join":21}
                string cmd = ExtractJsonString(json, "cmd");
                string type = ExtractJsonString(json, "type");
                uint join = (uint)ExtractJsonInt(json, "join");

                switch (cmd)
                {
                    case "pulse":
                        if (type == "d")
                        {
                            _logic.HandleDigitalPress(join);
                            SendResponse(join, type, _panel.GetDigitalFeedback(join) ? "true" : "false");
                        }
                        break;

                    case "set":
                        if (type == "a")
                        {
                            ushort value = (ushort)ExtractJsonInt(json, "value");
                            _logic.HandleAnalogChange(join, value);
                            SendResponse(join, type, _panel.GetAnalogFeedback(join).ToString());
                        }
                        break;

                    case "query":
                        if (type == "d")
                        {
                            bool val = _panel.GetDigitalFeedback(join);
                            SendResponse(join, type, val ? "true" : "false");
                        }
                        else if (type == "a")
                        {
                            ushort val = _panel.GetAnalogFeedback(join);
                            SendResponse(join, type, val.ToString());
                        }
                        break;

                    default:
                        CrestronConsole.PrintLine("[TestBridge] Unknown command: {0}", cmd);
                        break;
                }
            }
            catch (Exception ex)
            {
                ErrorLog.Error("[TestBridge] ProcessCommand error: {0}", ex.Message);
            }
        }

        private void SendResponse(uint join, string type, string value)
        {
            string response = string.Format("{{\"join\":{0},\"type\":\"{1}\",\"value\":{2}}}",
                join, type, value);
            CrestronConsole.PrintLine("[TestBridge] Tx → {0}", response);

            // In on-processor mode, this would be:
            // _pModule.SendData(response);
        }

        // ── Console-Based Test Runner (No Python Required) ──

        /// <summary>
        /// Run all tests directly from C# without Python.
        /// Uses the console for output. Ideal for quick validation.
        /// </summary>
        public void RunConsoleTests()
        {
            int passed = 0;
            int failed = 0;
            int total = 0;

            CrestronConsole.PrintLine("");
            CrestronConsole.PrintLine("╔══════════════════════════════════════════════════╗");
            CrestronConsole.PrintLine("║   LOCKTON DUNNING - PANEL JOIN TEST SUITE        ║");
            CrestronConsole.PrintLine("║   Break Room 09.002                              ║");
            CrestronConsole.PrintLine("╚══════════════════════════════════════════════════╝");
            CrestronConsole.PrintLine("");

            // — TEST: Source Selection Mutual Exclusivity —
            total++;
            _logic.HandleDigitalPress(JoinMap.AirMedia);
            bool t1 = _panel.GetDigitalFeedback(JoinMap.AirMedia) == true
                    && _panel.GetDigitalFeedback(JoinMap.MediaPlayer) == false;
            LogResult("Source: AirMedia selects exclusively", t1, ref passed, ref failed);

            total++;
            _logic.HandleDigitalPress(JoinMap.MediaPlayer);
            bool t2 = _panel.GetDigitalFeedback(JoinMap.MediaPlayer) == true
                    && _panel.GetDigitalFeedback(JoinMap.AirMedia) == false;
            LogResult("Source: MediaPlayer deselects AirMedia", t2, ref passed, ref failed);

            // — TEST: Power Off Clears Sources —
            total++;
            _logic.HandleDigitalPress(JoinMap.PowerOff);
            bool t3 = _panel.GetDigitalFeedback(JoinMap.AirMedia) == false
                    && _panel.GetDigitalFeedback(JoinMap.MediaPlayer) == false;
            LogResult("Power Off: Clears all source feedback", t3, ref passed, ref failed);

            // — TEST: Master Volume Echo —
            total++;
            _logic.HandleAnalogChange(JoinMap.MasterLevel, 32768);
            bool t4 = _panel.GetAnalogFeedback(JoinMap.MasterLevel) == 32768;
            LogResult("Volume: Master level echoes 32768", t4, ref passed, ref failed);

            // — TEST: Master Mute Toggle —
            total++;
            _logic.HandleDigitalPress(JoinMap.MasterMute);
            bool t5a = _panel.GetDigitalFeedback(JoinMap.MasterMute) == true;
            LogResult("Mute: Master mute toggles ON", t5a, ref passed, ref failed);

            total++;
            _logic.HandleDigitalPress(JoinMap.MasterMute);
            bool t5b = _panel.GetDigitalFeedback(JoinMap.MasterMute) == false;
            LogResult("Mute: Master mute toggles OFF", t5b, ref passed, ref failed);

            // — TEST: Restroom Volume & Mute —
            total++;
            _logic.HandleAnalogChange(JoinMap.RestroomLevel, 49152);
            bool t6 = _panel.GetAnalogFeedback(JoinMap.RestroomLevel) == 49152;
            LogResult("Volume: Restroom level echoes 49152", t6, ref passed, ref failed);

            total++;
            _logic.HandleDigitalPress(JoinMap.RestroomMute);
            bool t7 = _panel.GetDigitalFeedback(JoinMap.RestroomMute) == true;
            LogResult("Mute: Restroom mute toggles ON", t7, ref passed, ref failed);

            // — TEST: Mic Levels —
            total++;
            _logic.HandleAnalogChange(JoinMap.HandheldLevel, 16384);
            bool t8 = _panel.GetAnalogFeedback(JoinMap.HandheldLevel) == 16384;
            LogResult("Volume: Handheld mic echoes 16384", t8, ref passed, ref failed);

            total++;
            _logic.HandleAnalogChange(JoinMap.BodypackLevel, 8192);
            bool t9 = _panel.GetAnalogFeedback(JoinMap.BodypackLevel) == 8192;
            LogResult("Volume: Bodypack mic echoes 8192", t9, ref passed, ref failed);

            // — TEST: Mic Mute Toggles —
            total++;
            _logic.HandleDigitalPress(JoinMap.HandheldMute);
            bool t10 = _panel.GetDigitalFeedback(JoinMap.HandheldMute) == true;
            LogResult("Mute: Handheld mic toggles ON", t10, ref passed, ref failed);

            total++;
            _logic.HandleDigitalPress(JoinMap.BodypackMute);
            bool t11 = _panel.GetDigitalFeedback(JoinMap.BodypackMute) == true;
            LogResult("Mute: Bodypack mic toggles ON", t11, ref passed, ref failed);

            // — TEST: Schedule Auto-On Mutual Exclusivity —
            total++;
            _logic.HandleDigitalPress(JoinMap.AutoOn8am);
            bool t12 = _panel.GetDigitalFeedback(JoinMap.AutoOn8am) == true
                     && _panel.GetDigitalFeedback(JoinMap.AutoOn7am) == false
                     && _panel.GetDigitalFeedback(JoinMap.AutoOn9am) == false
                     && _panel.GetDigitalFeedback(JoinMap.AutoOnDisable) == false;
            LogResult("Schedule: Auto-On 8am exclusive", t12, ref passed, ref failed);

            // — TEST: Schedule Auto-Off Mutual Exclusivity —
            total++;
            _logic.HandleDigitalPress(JoinMap.AutoOff6pm);
            bool t13 = _panel.GetDigitalFeedback(JoinMap.AutoOff6pm) == true
                     && _panel.GetDigitalFeedback(JoinMap.AutoOff5pm) == false
                     && _panel.GetDigitalFeedback(JoinMap.AutoOff7pm) == false
                     && _panel.GetDigitalFeedback(JoinMap.AutoOffDisable) == false;
            LogResult("Schedule: Auto-Off 6pm exclusive", t13, ref passed, ref failed);

            // — TEST: Full Reset After Power Off —
            total++;
            _logic.HandleDigitalPress(JoinMap.AirMedia);      // Turn system on
            _logic.HandleAnalogChange(JoinMap.MasterLevel, 50000);
            _logic.HandleDigitalPress(JoinMap.MasterMute);     // Mute on
            _logic.HandleDigitalPress(JoinMap.PowerOff);       // Power off

            bool t14 = _panel.GetDigitalFeedback(JoinMap.AirMedia) == false
                     && _panel.GetAnalogFeedback(JoinMap.MasterLevel) == 0
                     && _panel.GetDigitalFeedback(JoinMap.MasterMute) == false;
            LogResult("Power Off: Full state reset verified", t14, ref passed, ref failed);

            // ── Summary ─────────────────────────────────
            CrestronConsole.PrintLine("");
            CrestronConsole.PrintLine("══════════════════════════════════════════════════");
            CrestronConsole.PrintLine("  RESULTS: {0}/{1} passed, {2} failed", passed, total, failed);

            if (failed == 0)
                CrestronConsole.PrintLine("  ✓ ALL TESTS PASSED");
            else
                CrestronConsole.PrintLine("  ✗ {0} TEST(S) FAILED", failed);

            CrestronConsole.PrintLine("══════════════════════════════════════════════════");
            CrestronConsole.PrintLine("");
        }

        private void LogResult(string name, bool pass, ref int passed, ref int failed)
        {
            if (pass)
            {
                passed++;
                CrestronConsole.PrintLine("  ✓ PASS: {0}", name);
            }
            else
            {
                failed++;
                CrestronConsole.PrintLine("  ✗ FAIL: {0}", name);
            }
        }

        // ── Minimal JSON Helpers (no dependencies) ──────

        private string ExtractJsonString(string json, string key)
        {
            string search = "\"" + key + "\":\"";
            int start = json.IndexOf(search);
            if (start < 0) return "";
            start += search.Length;
            int end = json.IndexOf("\"", start);
            if (end < 0) return "";
            return json.Substring(start, end - start);
        }

        private int ExtractJsonInt(string json, string key)
        {
            // Try {"key":123} format
            string search = "\"" + key + "\":";
            int start = json.IndexOf(search);
            if (start < 0) return 0;
            start += search.Length;

            string numStr = "";
            for (int i = start; i < json.Length; i++)
            {
                char c = json[i];
                if (char.IsDigit(c) || c == '-')
                    numStr += c;
                else if (numStr.Length > 0)
                    break;
            }

            int result;
            return int.TryParse(numStr, out result) ? result : 0;
        }
    }
}
