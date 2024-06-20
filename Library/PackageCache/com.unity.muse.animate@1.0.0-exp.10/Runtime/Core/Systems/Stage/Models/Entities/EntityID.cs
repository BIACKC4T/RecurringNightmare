using System;
using System.Runtime.Serialization;
using UnityEngine;

namespace Unity.Muse.Animate
{
    [Serializable]
    struct EntityID : IEquatable<EntityID>
    {
        public bool IsValid => m_Id != Guid.Empty;

        [NonSerialized]
        Guid m_Id;

        [SerializeField]
        string m_SerializedId;

        public static EntityID Generate()
        {
            var id = new EntityID();
            id.m_Id = Guid.NewGuid();
            return id;
        }

        public EntityID(EntityID other)
        {
            m_Id = other.m_Id;
            m_SerializedId = null;
        }

        public bool Equals(EntityID other)
        {
            return m_Id == other.m_Id;
        }

        public override bool Equals(object obj)
        {
            return obj is EntityID other && Equals(other);
        }

        public override int GetHashCode()
        {
            return m_Id.GetHashCode();
        }

        public static bool operator ==(EntityID left, EntityID right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EntityID left, EntityID right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return m_Id.ToString();
        }

        [OnSerializing]
        public void OnBeforeSerialize(StreamingContext context)
        {
            m_SerializedId = m_Id.ToString();
        }

        [OnDeserialized]
        public void OnAfterDeserialize(StreamingContext context)
        {
            if (!Guid.TryParse(m_SerializedId, out m_Id))
                throw new Exception($"Could not parse GUID: {m_SerializedId}");
        }
    }
}
