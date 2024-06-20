using System;
using System.Collections.Generic;
using System.IO;
using Unity.Muse.AppUI.UI;
using Unity.Muse.Common.Account;
using UnityEngine;

namespace Unity.Muse.Common
{
    /// <summary>
    /// Standardized preferences accessor for Editor and Runtime.
    /// </summary>
    static class GlobalPreferences
    {
        const string k_AssetsRoot = "Assets";
        internal static event Action preferencesChanged;
        
        static IMusePreferences s_Preferences;
        
        static readonly Dictionary<string, Func<string>> k_Callbacks = new Dictionary<string, Func<string>>();

#if !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod]
        public static void Init() => Init(new RuntimePreferences());
#endif

        public static void Init(IMusePreferences preferences)
        {
            s_Preferences = preferences;
            s_Preferences.changed += OnPreferencesChanged;
        }
        
        static void OnPreferencesChanged() => preferencesChanged?.Invoke();

        public static void Delete<T>(string preferenceName, PreferenceScope scope = PreferenceScope.Project)
        {
            s_Preferences.Delete<T>(preferenceName, scope);
        }

        /// <summary>
        /// Last-fetched list of organizations
        /// </summary>
        public static List<OrganizationInfo> organizations
        {
            get => s_Preferences.Get<List<OrganizationInfo>>(nameof(organizations), PreferenceScope.Project,
                defaultValue: new());
            set => s_Preferences.Set(nameof(organizations), value, PreferenceScope.Project);
        }

        /// <summary>
        /// Last-fetched legal consent
        /// </summary>
        public static LegalConsentInfo legalConsent
        {
            get => s_Preferences.Get<LegalConsentInfo>(nameof(legalConsent), PreferenceScope.User, new());
            set => s_Preferences.Set(nameof(legalConsent), value, PreferenceScope.User);
        }

        /// <summary>
        /// Last selected organization, null if none.
        /// </summary>
        public static OrganizationInfo organization
        {
            get => s_Preferences.Get<OrganizationInfo>(nameof(organization), PreferenceScope.Project);
            set => s_Preferences.Set(nameof(organization), value, PreferenceScope.Project);
        }

        /// <summary>
        /// Current usage information for the current user
        /// </summary>
        public static UsageInfo usage
        {
            get => s_Preferences.Get(nameof(usage), PreferenceScope.Project, defaultValue: new UsageInfo());
            set => s_Preferences.Set(nameof(usage), value, PreferenceScope.Project);
        }

        /// <summary>
        /// Has the trial dialog been shown at least once to this user?
        /// </summary>
        public static bool trialDialogShown
        {
            get => s_Preferences.Get<bool>(nameof(trialDialogShown), PreferenceScope.Project, defaultValue: new());
            set => s_Preferences.Set(nameof(trialDialogShown), value, PreferenceScope.Project);
        }

        /// <summary>
        /// If the subscriptStart message has been displayed or not (should be displayed only once per user lifetime)
        /// </summary>
        public static bool subscriptionStartDisplayed
        {
            get => s_Preferences.Get<bool>(nameof(subscriptionStartDisplayed), PreferenceScope.User);
            set => s_Preferences.Set(nameof(subscriptionStartDisplayed), value, PreferenceScope.User);
        }
        
        public static bool deleteWithoutWarning
        {
            get => s_Preferences.Get<bool>(nameof(deleteWithoutWarning), PreferenceScope.Project);
            set => s_Preferences.Set(nameof(deleteWithoutWarning), value, PreferenceScope.Project);
        }
        
        public static CanvasControlScheme canvasControlScheme
        {
            get => s_Preferences.Get<CanvasControlScheme>(nameof(canvasControlScheme), PreferenceScope.Project, CanvasControlScheme.Editor);
            set => s_Preferences.Set(nameof(canvasControlScheme), value, PreferenceScope.Project);
        }
        
        public static string GetMuseAssetGeneratedFolderPathFromMode(string currentMode)
        {
            var directory = k_AssetsRoot;

            if (k_Callbacks.TryGetValue(currentMode, out var callback))
            {
                var ret = callback?.Invoke();
                if (IsValidMuseGeneratedPath(ret, false))
                    directory = ret;
            }

            return directory;
        }

        internal static void RegisterAssetGeneratedPath(string mode, Func<string> callback)
        {
            k_Callbacks[mode] = callback;
        }

        internal static bool IsValidMuseGeneratedPath(string museAssetPath, bool checkIfExists = true)
        {
            return !string.IsNullOrWhiteSpace(museAssetPath) && museAssetPath.StartsWith(k_AssetsRoot) &&
                   (!checkIfExists || Directory.Exists(museAssetPath));
        }
        
        internal static string SanitizeMuseGeneratedPath(string path)
        {
            return IsValidMuseGeneratedPath(path) ? path : k_AssetsRoot;
        }
    }
}
