using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using UnityEngine;

namespace Unity.Muse.Animate
{
    /// <summary>
    /// Represents a loop key, ie a key that points to a past frame, with a transform
    /// </summary>
    [Serializable]
    class LoopKeyModel : ICopyable<LoopKeyModel>
    {
        const int k_DefaultNumBakingLoopbacks = 1;

        public enum Property
        {
            StartFrame,
            Transform,
            NumBakingLoopbacks,
            EntityList
        }

        [SerializeField]
        LoopKeyData m_Data;

        [NonSerialized]
        Dictionary<RigidTransformModel, EntityID> m_TransformToEntity = new();

        /// <summary>
        /// Checks if the key is in a valid state
        /// </summary>
        public bool IsValid
        {
            get
            {
                if (m_Data.StartFrame < 0)
                    return false;

                if (m_Data.Transforms == null)
                    return false;

                foreach (var pair in m_Data.Transforms)
                {
                    if (!pair.Key.IsValid || pair.Value == null || !pair.Value.IsValid)
                        return false;
                }

                return true;
            }
        }

        /// <summary>
        /// This is the number of times that a loop is played back and baked again to ensure better continuity of the loop
        /// 0 means the loop is only baked forward (ie using past prediction only)
        /// Any other value means that the loop is rewinded to its start and baked again using its own future prediction
        /// </summary>
        public int NumBakingLoopbacks
        {
            get => m_Data.NumBakingLoopbacks;

            set
            {
                var correctedValue = value;
                if (correctedValue < 0)
                    correctedValue = k_DefaultNumBakingLoopbacks;

                if (correctedValue == m_Data.NumBakingLoopbacks)
                    return;

                m_Data.NumBakingLoopbacks = correctedValue;
                OnChanged?.Invoke(this, Property.NumBakingLoopbacks);
            }
        }

        /// <summary>
        /// The index of the first frame of the loop
        /// </summary>
        public int StartFrame
        {
            get => m_Data.StartFrame;
            set
            {
                if (value == m_Data.StartFrame)
                    return;

                m_Data.StartFrame = value;
                OnChanged?.Invoke(this, Property.StartFrame);
            }
        }

        public delegate void Changed(LoopKeyModel model, Property property);
        public event Changed OnChanged;

        /// <summary>
        /// Creates a new loop key
        /// </summary>
        /// <param name="translation">The position offset to be applied to the source frame root joint.</param>
        /// <param name="rotation">The rotation offset to be applied to the source frame root joint.</param>
        /// <param name="startFrame">The index of the first frame of the loop.</param>
        public LoopKeyModel(Vector3 translation, Quaternion rotation, int startFrame = 0)
        {
            m_Data.Transforms = new JsonDictionary<EntityID, RigidTransformModel>();
            m_Data.StartFrame = startFrame;
            m_Data.NumBakingLoopbacks = k_DefaultNumBakingLoopbacks;

            RegisterEvents();
        }

        /// <summary>
        /// Creates a new loop key
        /// </summary>
        /// <param name="translation">The position offset to be applied to the source frame root joint.</param>
        /// <param name="startFrame">The index of the first frame of the loop.</param>
        public LoopKeyModel(Vector3 translation, int startFrame = 0) : this(translation, Quaternion.identity, startFrame) { }

        /// <summary>
        /// Creates a new loop key
        /// </summary>
        /// <param name="rotation">The rotation offset to be applied to the source frame root joint.</param>
        /// <param name="startFrame">The index of the first frame of the loop.</param>
        public LoopKeyModel(Quaternion rotation, int startFrame = 0) : this(Vector3.zero, rotation, startFrame) { }

        /// <summary>
        /// Creates a new loop key
        /// </summary>
        /// <param name="startFrame">The index of the first frame of the loop.</param>
        public LoopKeyModel(int startFrame = 0) : this(Vector3.zero, Quaternion.identity, startFrame) { }

        [JsonConstructor]
        public LoopKeyModel(LoopKeyData m_Data)
        {
            this.m_Data = m_Data;
        }

        public void CopyTo(LoopKeyModel other)
        {
            other.StartFrame = StartFrame;
            other.NumBakingLoopbacks = NumBakingLoopbacks;

            // List all entities of this key
            using var myEntityIDs = TempHashSet<EntityID>.Allocate();
            GetAllEntities(myEntityIDs.Set);

            // List all entities of other key
            using var otherEntityIDs = TempHashSet<EntityID>.Allocate();
            other.GetAllEntities(otherEntityIDs.Set);

            // Remove entities that we don't have
            foreach (var entityID in otherEntityIDs)
            {
                if (!HasEntity(entityID))
                    other.RemoveEntity(entityID);
            }

            // Copy transforms, and add entities if required
            foreach (var entityID in myEntityIDs)
            {
                if (!other.HasEntity(entityID))
                    other.AddEntity(entityID);

                if (!other.TryGetOffset(entityID, out var otherOffset))
                    continue;

                if (!TryGetOffset(entityID, out var myOffset))
                    continue;

                myOffset.CopyTo(otherOffset);
            }
        }

        public LoopKeyModel Clone()
        {
            var clone = new LoopKeyModel();
            CopyTo(clone);
            return clone;
        }

        /// <summary>
        /// Adds an entity to the key.
        /// </summary>
        /// <param name="entityID">The ID of the entity to add</param>
        public RigidTransformModel AddEntity(EntityID entityID)
        {
            var keyModel = new RigidTransformModel();
            AddEntityInternal(entityID, keyModel);
            OnChanged?.Invoke(this, Property.EntityList);
            return keyModel;
        }

        /// <summary>
        /// Adds an entity to the key.
        /// </summary>
        /// <param name="entityID">The ID of the entity to add</param>
        /// <param name="transformData">The transform data of the entity</param>
        public RigidTransformModel AddEntity(EntityID entityID, RigidTransformData transformData)
        {
            var keyModel = new RigidTransformModel();
            keyModel.Transform = transformData;
            AddEntityInternal(entityID, keyModel);
            OnChanged?.Invoke(this, Property.EntityList);
            return keyModel;
        }

        /// <summary>
        /// Removes an entity from the key
        /// </summary>
        /// <param name="entityID">The ID of the entity to remove</param>
        public void RemoveEntity(EntityID entityID)
        {
            RemoveEntityInternal(entityID);
            OnChanged?.Invoke(this, Property.EntityList);
        }

        /// <summary>
        /// Removes all entities from the key
        /// </summary>
        public void RemoveAllEntities()
        {
            using var tmpList = TempList<EntityID>.Allocate();
            foreach (var pair in m_Data.Transforms)
            {
                tmpList.Add(pair.Key);
            }

            foreach (var entityID in tmpList.List)
            {
                RemoveEntityInternal(entityID);
            }

            OnChanged?.Invoke(this, Property.EntityList);
        }

        /// <summary>
        /// Checks if an entity is registered in the key
        /// </summary>
        /// <param name="entityID">The ID of the entity</param>
        /// <returns>True if the entity was added to the key, False otherwise</returns>
        public bool HasEntity(EntityID entityID)
        {
            return m_Data.Transforms.ContainsKey(entityID);
        }

        /// <summary>
        /// Returns all the entity IDs that this key contains
        /// </summary>
        /// <param name="entitySet">A set where to store the result</param>
        public void GetAllEntities(HashSet<EntityID> entitySet)
        {
            foreach (var pair in m_Data.Transforms)
            {
                entitySet.Add(pair.Key);
            }
        }

        /// <summary>
        /// Try to get the loop transform offset of a specific entity
        /// </summary>
        /// <param name="entityID">The ID of the entity</param>
        /// <param name="offset">The loop offset</param>
        /// <returns>true if the entity was registered, false otherwise</returns>
        public bool TryGetOffset(EntityID entityID, out RigidTransformModel offset)
        {
            return m_Data.Transforms.TryGetValue(entityID, out offset);
        }

        [OnDeserialized]
        public void OnAfterDeserialize(StreamingContext context)
        {
            RegisterEvents();
        }

        void AddEntityInternal(EntityID entityID, RigidTransformModel transformModel)
        {
            if (m_Data.Transforms.ContainsKey(entityID))
                AssertUtils.Fail($"Entity already registered: {entityID}");

            m_Data.Transforms[entityID] = transformModel;
            RegisterEntity(entityID, transformModel);
        }

        void RemoveEntityInternal(EntityID entityID)
        {
            if (!m_Data.Transforms.TryGetValue(entityID, out var keyModel))
                AssertUtils.Fail($"Entity was not registered: {entityID}");

            m_Data.Transforms.Remove(entityID);
            UnregisterEntity(entityID, keyModel);
        }

        void RegisterEvents()
        {
            foreach (var pair in m_Data.Transforms)
            {
                RegisterEntity(pair.Key, pair.Value);
            }
        }

        void RegisterEntity(EntityID entityID, RigidTransformModel transformModel)
        {
            if (m_TransformToEntity.ContainsKey(transformModel))
                AssertUtils.Fail($"RigidTransformModel already registered for entity: {entityID}");

            m_TransformToEntity[transformModel] = entityID;
            transformModel.OnChanged += OnEntityTransformModelChanged;
        }

        void UnregisterEntity(EntityID entityID, RigidTransformModel transformModel)
        {
            if (!m_TransformToEntity.ContainsKey(transformModel))
                AssertUtils.Fail($"RigidTransformModel was not registered for entity: {entityID}");

            m_TransformToEntity.Remove(transformModel);
            transformModel.OnChanged -= OnEntityTransformModelChanged;
        }

        void OnEntityTransformModelChanged(RigidTransformModel model)
        {
            OnChanged?.Invoke(this, Property.Transform);
        }
    }
}
