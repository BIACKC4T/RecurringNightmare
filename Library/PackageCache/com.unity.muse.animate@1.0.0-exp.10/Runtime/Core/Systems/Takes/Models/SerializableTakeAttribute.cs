using System;

namespace Unity.Muse.Animate
{
    /// <summary>
    /// Specifies the type of the take (the enum value) written to the JSON file.
    /// </summary>
    class SerializableTakeAttribute : Attribute
    {
        public TakeModel.TakeType Type { get; }
        public SerializableTakeAttribute(TakeModel.TakeType type)
        {
            Type = type;
        }
    }
}
