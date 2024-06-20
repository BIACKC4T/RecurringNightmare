using System;
using UnityEngine;

namespace Unity.Muse.Common
{
    [Serializable]
    internal struct OperatorData
    {
        public string type;
        public string version;
        public string [] settings;
        public bool enabled;
        public string assembly;

        public OperatorData(string type, string version, string [] settings, bool enabled)
        {
            this.type = type;
            this.version = version;
            this.settings = settings;
            this.enabled = enabled;
            this.assembly = String.Empty;
        }

        public OperatorData(string type, string assembly, string version, string [] settings, bool enabled)
        {
            this.type = type;
            this.version = version;
            this.settings = settings;
            this.enabled = enabled;
            this.assembly = assembly;
        }

        public void FromJson(string json)
        {
            this = JsonUtility.FromJson<OperatorData>(json);
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }
    }
}
