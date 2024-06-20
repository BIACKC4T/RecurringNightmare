using System;

namespace Unity.Muse.Animate
{
    /// <summary>
    /// This represents a user-authored key
    /// </summary>
    [Serializable]
    struct KeyData
    {
        /// <summary>
        /// There are multiple type of keys
        /// </summary>
        public enum KeyType
        {
            /// <summary>
            /// A key that contains no specific information, just a point in time
            /// </summary>
            Empty = 0,
            /// <summary>
            /// A key that contains a fully specified pose
            /// </summary>
            FullPose = 1,
            /// <summary>
            /// A key that loops a previous key, with a translation and rotation offsets
            /// </summary>
            Loop = 2
        }

        public static KeyType[] AllKeyTypes = (KeyType[])Enum.GetValues(typeof(KeyType));
        public KeyType Type;

        public JsonDictionary<EntityID, EntityKeyModel> Keys;
        public LoopKeyModel LoopKey;
        public ThumbnailModel Thumbnail;
    }
}
