using UnityEditor;

namespace Unity.Muse.Common.Editor.Analytics
{
    static class AnalyticsManager
    {
        [InitializeOnLoadMethod]
        static void Init()
        {
            Model.OnAnalytics += OnAnalytics;
        }

        static void OnAnalytics(string eventName, object parameters, int version)
        {
            var result = EditorAnalytics.SendEventWithLimit(eventName, parameters, version);
        }
    }
}
