using System;

namespace Unity.Muse.AppUI.UI
{
    /// <summary>
    /// Interface for pressable elements.
    /// </summary>
    public interface IPressable
    {
        /// <summary>
        /// The Pressable element.
        /// </summary>
        Pressable clickable { get; }
    }
}
