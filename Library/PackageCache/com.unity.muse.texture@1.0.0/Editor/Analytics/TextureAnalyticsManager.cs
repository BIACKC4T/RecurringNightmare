using Unity.Muse.Texture.Analytics;
using UnityEditor;

namespace Unity.Muse.Texture.Editor.Analytics
{
    static class TextureAnalyticsManager
    {
        const int maxEventsPerHour = 500;
        static string vendorKey = "unity.muse";

        [InitializeOnLoadMethod]
        static void Init()
        {
            EditorAnalytics.RegisterEventWithLimit(SaveTextureData.eventName, maxEventsPerHour, 6, vendorKey);
            EditorAnalytics.RegisterEventWithLimit(GenerateAnalyticsData.eventName, maxEventsPerHour, 6, vendorKey);
        }
    }
}
