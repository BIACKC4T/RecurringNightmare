using System;

namespace Unity.Muse.Animate
{
    /// <summary>
    /// Interface for an entry in a library.
    /// </summary>
    interface ILibraryItem
    {
        public enum Property
        {
            Title,
            Thumbnail
        }
        
        public delegate void ItemChanged(Property property);
        
        /// <summary>
        /// Title label of the item, shown inside the Library UI.
        /// </summary>
        public string Title { get; }
        
        /// <summary>
        /// Preview of the item, shown inside the Library UI.
        /// </summary>
        public ThumbnailModel Thumbnail { get; }

        /// <summary>
        /// Event invoked when the library item data changes.
        /// </summary>
        public event ItemChanged OnItemChanged;
    }
}
