using System;
using System.Collections.Generic;
using System.Reflection;
using Crestron.SimplSharpPro;

namespace LocktonTest
{
    public static class SignalResolver
    {
        private static readonly Dictionary<uint, string> DigitalNames = new Dictionary<uint, string>();
        private static readonly Dictionary<uint, string> AnalogNames = new Dictionary<uint, string>();

        static SignalResolver()
        {
            try
            {
                PopulateMaps();
            }
            catch (Exception ex)
            {
                Crestron.SimplSharp.CrestronConsole.PrintLine("[Resolver] Init Error: " + ex.Message);
            }
        }

        private static void PopulateMaps()
        {
            Type type = typeof(JoinMap);
            // Get all public constants
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            foreach (FieldInfo field in fields)
            {
                if (field.FieldType != typeof(uint)) continue;

                uint value = (uint)field.GetValue(null);
                string name = field.Name;

                // Simple categorization based on name and JoinMap usage
                // Digital overlaps: Mutes, Power, Selects, Transport, Schedule
                // Analog: Levels
                if (name.ToLower().EndsWith("level"))
                {
                    if (!AnalogNames.ContainsKey(value)) AnalogNames.Add(value, name);
                }
                else
                {
                    // Most joins in this project are digital
                    if (!DigitalNames.ContainsKey(value)) DigitalNames.Add(value, name);
                }
            }
        }

        public static string GetName(eSigType type, uint number)
        {
            if (type == eSigType.Bool)
            {
                return DigitalNames.ContainsKey(number) ? DigitalNames[number] : "Unknown";
            }
            if (type == eSigType.UShort)
            {
                return AnalogNames.ContainsKey(number) ? AnalogNames[number] : "Unknown";
            }
            return "Unknown";
        }
    }
}
