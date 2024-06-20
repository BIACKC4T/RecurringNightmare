using System;
using Unity.Muse.Common.Analytics;

namespace Unity.Muse.Texture.Editor.Analytics
{
    [Serializable]
    class SaveTextureData : IAnalyticsData
    {
        public const string eventName = "muse_textureTool_save";
        public string EventName => eventName;
        public int Version => 1;

        public bool is_pbr_material;
        public string material_hash;
    }
}
