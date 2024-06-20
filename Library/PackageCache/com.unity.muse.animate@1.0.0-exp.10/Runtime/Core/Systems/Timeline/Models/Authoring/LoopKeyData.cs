using System;
using UnityEngine;

namespace Unity.Muse.Animate
{
    [Serializable]
    struct LoopKeyData
    {
        public int StartFrame;
        public int NumBakingLoopbacks;
        public JsonDictionary<EntityID, RigidTransformModel> Transforms;
    }
}
