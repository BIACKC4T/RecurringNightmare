using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;

namespace Unity.Muse.Common
{
    [Serializable]
    internal struct ModeStruct
    {
        public string type;
        public string label;
        /// <summary>
        /// Title for this mode, which will be used for the window and asset naming for instance.
        /// </summary>
        public string title;
        public string version;
        public bool enabled;
        public OperatorData[] operators;
    }
    [Serializable]
    internal struct Modes
    {
        public List<ModeStruct> modes;
    }

    internal static class ModesFactory
    {
        static readonly Dictionary<string, ModeStruct> k_Modes = new Dictionary<string, ModeStruct>();

        [Preserve]
        static ModesFactory()
        {
            LoadMuseModes();
        }

        static Dictionary<string, ModeStruct> modes
        {
            get
            {
                if (k_Modes.Count == 0)
                    LoadMuseModes();
                return k_Modes;
            }
        }

        internal static void LoadMuseModes()
        {
            k_Modes.Clear();
            var modeAssets = ResourceManager.LoadAll<MuseModeAsset>("");
            foreach (var modeAsset in modeAssets)
            {
                var modes = modeAsset.modes.modes.Where(m => m.enabled);
                foreach (var mode in modes)
                {
                    k_Modes[mode.type] = mode;
                }
            }
        }

        /// <summary>
        /// Gets the mode data for the given mode.
        /// </summary>
        /// <param name="mode">Mode to get data from</param>
        /// <returns>The mode data.</returns>
        public static ModeStruct? GetModeData(string mode)
        {
            modes.TryGetValue(mode ?? "", out var result);
            return result;
        }

        public static List<string> GetModes()
        {
            var result = new List<string>();
            foreach (var val in modes.Values)
            {
                result.Add(val.label);
            }

            return result;
        }

        public static int GetModeIndexFromKey(string key)
        {
            var index = 0;
            foreach (var val in modes.Keys)
            {
                if (val == key)
                    return index;
                index++;
            }

            return -1;
        }

        public static string GetModeKeyFromIndex(int index)
        {
            var keys = modes.Keys;
            if (index < 0 && index >= keys.Count)
            {
                Debug.LogError("Index for Mode: "+ index + " can't be found");
                return string.Empty;
            }

            foreach (var val in keys)
            {
                if (index == 0)
                    return val;
                index--;
            }

            return string.Empty;
        }

        public static IEnumerable<IOperator> GetMode(int index)
        {
            var modeKey = GetModeKeyFromIndex(index);

            if (string.IsNullOrEmpty(modeKey))
                return Enumerable.Empty<IOperator>();

            var modeStruct = modes[modeKey];
            return GetOperators(modeStruct.operators);
        }

        public static IEnumerable<IOperator> GetMode(string mode)
        {

            if (!modes.TryGetValue(mode, out var modeStruct))
            {
                Debug.LogError("Mode: "+ mode + " can't be found");
                return Enumerable.Empty<IOperator>();
            }

            return GetOperators(modeStruct.operators);
        }

        public static IEnumerable<IOperator> GetOperators(OperatorData[] operators)
        {
            var result = new List<IOperator>();

            foreach (var op in operators)
            {
                var operatorInstance = OperatorsFactory.GetOperatorInstance(op.type);
                if (operatorInstance == null)
                {
                    Debug.LogError("Can't create: Unity.Muse.Common."+ op.type + " can't be found");
                    continue;
                }
                operatorInstance.SetOperatorData(op);

                result.Add(operatorInstance);
            }

            return result;
        }
    }
}