using System;
using System.Collections.Generic;

namespace Unity.Muse.Animate
{
    /// <summary>
    /// Represents a scene serialized data
    /// </summary>
    [Serializable]
    struct StageData
    {
        /// <summary>
        /// Increase if data format changed in a way where old data can't be loaded anymore to prevent trying to use
        /// it.
        /// </summary>
        public const int DataVersion = 1;

        public int Version;
        public string Name;
        public List<ActorModel> Actors;
        public List<PropModel> Props;
        public TimelineModel Timeline;
        public BakedTimelineModel BakedTimeline;
        public BakedTimelineMappingModel BakedTimelineMapping;
        public List<CameraCoordinatesModel> CameraViewpoints;
        public TakesLibraryModel TakesLibrary;
    }
}
