using System;
using System.Collections.Generic;

namespace Unity.Muse.Animate
{
    /// <summary>
    /// This represents a fully baked timeline, ie that has each individual frame entirely baked as final data
    /// </summary>
    [Serializable]
    struct BakedTimelineData
    {
        public List<BakedFrameModel> Frames;
    }
}
