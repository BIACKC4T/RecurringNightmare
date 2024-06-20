using System;
using UnityEditor;
using UnityEngine.Serialization;

namespace Unity.Muse.Common.Account
{
    /// <summary>
    /// Keeps properties that only need to be checked once per editor session.
    /// </summary>
    [Serializable]
    class AccountStatus : ScriptableSingleton<AccountStatus>
    {
        public bool statusChecked;
        public bool legalConsentChecked;
        public bool entitlementsChecked;        // Used to avoid checking entitlements on every domain reload.
        public ClientStatusResponse status = new();
    }
}
