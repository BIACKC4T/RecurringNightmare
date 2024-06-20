using System;
using Unity.Muse.AppUI.UI;

namespace Unity.AppUI.Core
{
    public record TooltipPlacementContext(PopoverPlacement placement) : IContext
    {
        public PopoverPlacement placement { get; } = placement;
    }
}
