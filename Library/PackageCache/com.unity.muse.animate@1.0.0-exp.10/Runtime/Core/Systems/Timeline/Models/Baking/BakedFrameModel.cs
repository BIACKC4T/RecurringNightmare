using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using UnityEngine;

namespace Unity.Muse.Animate
{
    [Serializable]
    class BakedFrameModel : ICopyable<BakedFrameModel>
    {
        [SerializeField]
        BakedFrameData m_Data;

        [NonSerialized]
        Dictionary<BakedArmaturePoseModel, EntityID> m_ArmatureToEntity = new();

        public bool IsValid
        {
            get
            {
                if (m_Data.EntityPoses == null)
                    return false;

                foreach (var pair in m_Data.EntityPoses)
                {
                    if (!pair.Key.IsValid || pair.Value == null || !pair.Value.IsValid)
                        return false;
                }

                return true;
            }
        }

        public delegate void EntityAdded(BakedFrameModel model, EntityID entityID);
        public event EntityAdded OnEntityAdded;

        public delegate void EntityRemoved(BakedFrameModel model, EntityID entityID);
        public event EntityRemoved OnEntityRemoved;

        public delegate void EntityPoseChanged(BakedFrameModel model, EntityID entityID);
        public event EntityPoseChanged OnEntityPoseChanged;

        public BakedFrameModel()
        {
            m_Data.EntityPoses = new JsonDictionary<EntityID, BakedArmaturePoseModel>();
            RegisterEvents();
        }

        [JsonConstructor]
        public BakedFrameModel(BakedFrameData m_Data)
        {
            this.m_Data = m_Data;
        }

        public BakedFrameModel(BakedFrameModel other)
        {
            other.CopyTo(this);
        }

        public void AddEntity(EntityID entityID, int numJoints, int numBodies)
        {
            if (m_Data.EntityPoses.ContainsKey(entityID))
                AssertUtils.Fail($"Entity is already registered: {entityID}");

            var poseModel = new BakedArmaturePoseModel(numJoints, numBodies);
            m_Data.EntityPoses[entityID] = poseModel;
            RegisterEntity(entityID, poseModel);

            OnEntityAdded?.Invoke(this, entityID);
        }

        public void RemoveEntity(EntityID entityID)
        {
            if (!m_Data.EntityPoses.TryGetValue(entityID, out var poseModel))
                AssertUtils.Fail($"Entity was not registered: {entityID}");

            m_Data.EntityPoses.Remove(entityID);
            UnregisterEntity(entityID, poseModel);

            OnEntityRemoved?.Invoke(this, entityID);
        }

        public bool HasEntity(EntityID entityID)
        {
            return m_Data.EntityPoses.ContainsKey(entityID);
        }

        public void GetAllEntities(HashSet<EntityID> set)
        {
            foreach (var pair in m_Data.EntityPoses)
            {
                set.Add(pair.Key);
            }
        }

        public bool TryGetPose(EntityID entityID, out BakedArmaturePoseModel bakedArmaturePoseModel)
        {
            return m_Data.EntityPoses.TryGetValue(entityID, out bakedArmaturePoseModel);
        }

        [OnDeserialized]
        public void OnAfterDeserialize(StreamingContext context)
        {
            RegisterEvents();
        }

        void RegisterEvents()
        {
            foreach (var pair in m_Data.EntityPoses)
            {
                RegisterEntity(pair.Key, pair.Value);
            }
        }

        void RegisterEntity(EntityID entityID, BakedArmaturePoseModel poseModel)
        {
            if (m_ArmatureToEntity.ContainsKey(poseModel))
                AssertUtils.Fail($"Armature was already registered for entity: {entityID}");

            m_ArmatureToEntity[poseModel] = entityID;
            poseModel.OnChanged += OnArmaturePoseModelChanged;
        }

        void UnregisterEntity(EntityID entityID, BakedArmaturePoseModel poseModel)
        {
            if (!m_ArmatureToEntity.ContainsKey(poseModel))
                AssertUtils.Fail($"BakedArmaturePoseModel was not registered for entity: {entityID}");

            m_ArmatureToEntity.Remove(poseModel);
            poseModel.OnChanged -= OnArmaturePoseModelChanged;
        }

        void OnArmaturePoseModelChanged(BakedArmaturePoseModel model, BakedArmaturePoseModel.Property property)
        {
            var entityID = m_ArmatureToEntity[model];
            OnEntityPoseChanged?.Invoke(this, entityID);
        }

        public void CopyTo(BakedFrameModel other)
        {
            other.m_Data.EntityPoses = new JsonDictionary<EntityID, BakedArmaturePoseModel>();
            foreach (var (key, value) in m_Data.EntityPoses)
            {
                var actorPose = new BakedArmaturePoseModel(value);
                other.m_Data.EntityPoses[key] = actorPose;
            }
            other.RegisterEvents();
        }

        public BakedFrameModel Clone()
        {
            return new BakedFrameModel(this);
        }

        public Bounds GetWorldBounds()
        {
            var bounds = new Bounds();
            var first = true;

            foreach (var (_, pose) in m_Data.EntityPoses)
            {
                if (first)
                {
                    bounds = pose.GetWorldBounds();
                    first = false;
                }
                else
                {
                    bounds.Encapsulate(pose.GetWorldBounds());
                }
            }
            
            return bounds;
        }
    }
}
