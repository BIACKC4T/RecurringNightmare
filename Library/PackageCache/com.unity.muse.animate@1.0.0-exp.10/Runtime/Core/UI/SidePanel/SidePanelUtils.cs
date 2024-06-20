using System;
using System.Collections.Generic;

namespace Unity.Muse.Animate
{
    static class SidePanelUtils
    {
        public enum PageType
        {
            TakesLibrary,
            TimelinesLibrary,
            PosesLibrary,
            ActorsLibrary,
            PropsLibrary,
            StagesLibrary
        }

        static readonly Dictionary<PageType, (string title, string icon, string tooltip)> k_PageInfos = new()
        {
            { PageType.TakesLibrary, ("Generation", "caret-double-right", "Open the Takes panel") },
            { PageType.TimelinesLibrary, ("Timelines", "deep-pose-timelines-library", "Open the Timelines panel") },
            { PageType.PosesLibrary, ("Poses", "deep-pose-poses-library", "Open the Poses Library panel") },
            { PageType.ActorsLibrary, ("Actors", "deep-pose-actors-library", "Open the Actors Library panel") },
            { PageType.PropsLibrary, ("Props", "deep-pose-props-library", "Open the Props Library panel") },
            { PageType.StagesLibrary, ("Stages", "deep-pose-stages-library", "Open the Stages Library panel") }
        };

        public static string GetTitle(this PageType type) => k_PageInfos[type].title;
        public static string GetIcon(this PageType type) => k_PageInfos[type].icon;
        public static string GetTooltip(this PageType type) => k_PageInfos[type].tooltip;
    }
}
