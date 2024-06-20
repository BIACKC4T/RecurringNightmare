using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Muse.Animate
{
    [Serializable]
    class ActorModel
    {
        [SerializeField]
        ActorData m_Data;

        public ActorID ID => m_Data.ID;
        public EntityID EntityID => m_Data.ID.EntityID;
        public string PrefabID => m_Data.PrefabID;

        public bool IsValid => m_Data.ID.IsValid;

        public Vector3 SpawnPosition => m_Data.SpawnPosition;
        public Quaternion SpawnRotation => m_Data.SpawnRotation;

        public ActorModel(ActorID id, string prefabId, Vector3 position, Quaternion rotation)
        {
            m_Data.ID = id;
            m_Data.PrefabID = prefabId;
            m_Data.SpawnPosition = position;
            m_Data.SpawnRotation = rotation;
        }

        [JsonConstructor]
        public ActorModel(ActorData m_Data)
        {
            this.m_Data = m_Data;
        }

        [OnDeserialized]
        public void OnAfterDeserialize(StreamingContext context)
        {
            Assert.IsTrue(IsValid, "Invalid data");
        }
    }
}
