using System;
using UnityEngine;

namespace Unity.Muse.Animate
{
    /// <summary>
    /// This represents a fully baked frame for a single actor
    /// </summary>
    [Serializable]
    struct BakedFrameData
    {
        public JsonDictionary<EntityID, BakedArmaturePoseModel> EntityPoses;
    }
}
