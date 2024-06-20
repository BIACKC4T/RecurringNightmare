using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using UnityEngine;

namespace Unity.Muse.Animate
{
    /// <summary>
    /// Represents a key of multiple entities
    /// </summary>
    [Serializable]
    class KeyModel
    {
        [SerializeField]
        KeyData m_Data;

        public enum Property
        {
            Type,
            EntityList,
            EntityKey,
            Loop,
            Thumbnail
        }

        public ThumbnailModel Thumbnail => m_Data.Thumbnail;

        [NonSerialized]
        Dictionary<EntityKeyModel, EntityID> m_KeyToEntity = new();

        /// <summary>
        /// Checks if the key is in a valid state
        /// </summary>
        public bool IsValid
        {
            get
            {
                if (m_Data.Keys == null)
                    return false;

                foreach (var pair in m_Data.Keys)
                {
                    if (!pair.Key.IsValid || pair.Value == null || !pair.Value.IsValid)
                        return false;
                }

                return true;
            }
        }

        /// <summary>
        /// The type of this key
        /// </summary>
        public KeyData.KeyType Type
        {
            get => m_Data.Type;
            set
            {
                if (value == m_Data.Type)
                    return;

                m_Data.Type = value;
                OnChanged?.Invoke(this, Property.Type);
            }
        }

        /// <summary>
        /// The loop key model
        /// </summary>
        public LoopKeyModel Loop => m_Data.LoopKey;

        public delegate void Changed(KeyModel model, Property property);
        public event Changed OnChanged;

        /// <summary>
        /// Creates a new key
        /// </summary>
        public KeyModel()
        {
            m_Data.Type = KeyData.KeyType.FullPose;
            m_Data.Keys = new JsonDictionary<EntityID, EntityKeyModel>();
            m_Data.LoopKey = new LoopKeyModel();
            m_Data.Thumbnail = new ThumbnailModel();

            RegisterEvents();
        }

        [JsonConstructor]
        public KeyModel(KeyData m_Data)
        {
            this.m_Data = m_Data;
        }

        public void CopyTo(KeyModel other)
        {
            other.Type = Type;

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
                if (!TryGetKey(entityID, out var myKey))
                    continue;

                if (!other.HasEntity(entityID))
                    other.AddEntity(entityID, myKey.Posing, myKey.LocalPose, myKey.GlobalPose);

                if (!other.TryGetKey(entityID, out var otherKey))
                    continue;

                myKey.CopyTo(otherKey);
            }

            Loop.CopyTo(other.Loop);
            Thumbnail.CopyTo(other.Thumbnail);

            // Note: Thumbnails have no change event
            other.OnChanged?.Invoke(other, Property.EntityKey);
        }

        /// <summary>
        /// Create a duplicate of this key
        /// </summary>
        /// <returns>A new key instance that is a copy of this key</returns>
        public KeyModel Clone()
        {
            var newKey = new KeyModel();
            CopyTo(newKey);

            return newKey;
        }

        /// <summary>
        /// Adds an entity to the key.
        /// </summary>
        /// <param name="entityID">The ID of the entity to add</param>
        /// <param name="posing">The initial posing state of the key, which will be copied to the key.</param>
        /// <param name="numJoints">The number of joints of the entity.</param>
        public void AddEntity(EntityID entityID, PosingModel posing, int numJoints)
        {
            var keyModel = new EntityKeyModel(posing, numJoints);
            AddEntityInternal(entityID, keyModel);
            m_Data.LoopKey.AddEntity(entityID);
            OnChanged?.Invoke(this, Property.EntityList);
        }

        /// <summary>
        /// Adds an entity to the key.
        /// </summary>
        /// <param name="entityID">The ID of the entity to add</param>
        /// <param name="posing">The initial posing state of the key, which will be copied to the key.</param>
        /// <param name="localPose">The initial local pose state of the key, which will be copied to the key.</param>
        /// <param name="globalPose">The initial global pose state of the key, which will be copied to the key.</param>
        public EntityKeyModel AddEntity(EntityID entityID, PosingModel posing, ArmatureStaticPoseModel localPose, ArmatureStaticPoseModel globalPose)
        {
            var keyModel = new EntityKeyModel(posing, localPose, globalPose);
            AddEntityInternal(entityID, keyModel);
            m_Data.LoopKey.AddEntity(entityID);
            OnChanged?.Invoke(this, Property.EntityList);
            return keyModel;
        }

        /// <summary>
        /// Removes an entity from the key
        /// </summary>
        /// <param name="entityID">The ID of the entity to remove</param>
        public void RemoveEntity(EntityID entityID)
        {
            if (!m_Data.Keys.TryGetValue(entityID, out var keyModel))
                AssertUtils.Fail($"Entity was not registered: {entityID}");

            m_Data.Keys.Remove(entityID);
            UnregisterEntity(entityID, keyModel);
            m_Data.LoopKey.RemoveEntity(entityID);
            OnChanged?.Invoke(this, Property.EntityList);
        }

        /// <summary>
        /// Checks if an entity is registered in the key
        /// </summary>
        /// <param name="entityID">The ID of the entity</param>
        /// <returns>True if the entity was added to the key, False otherwise</returns>
        public bool HasEntity(EntityID entityID)
        {
            return m_Data.Keys.ContainsKey(entityID);
        }

        /// <summary>
        /// Checks if all given entities are registered in the key
        /// </summary>
        /// <param name="entityIds">The ID of the entities</param>
        /// <returns>True if all the entities were added to the key, False otherwise</returns>
        public bool HasEntities(IEnumerable<EntityID> entityIds)
        {
            foreach (var entityID in entityIds)
            {
                if (!HasEntity(entityID))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if both keys have the exact same list of entities
        /// </summary>
        /// <param name="other">The other key to check with</param>
        /// <returns>true if both keys have the same entities, false otherwise</returns>
        public bool HasSameEntitiesAs(KeyModel other)
        {
            {
                using var entityIds = TempHashSet<EntityID>.Allocate();
                GetAllEntities(entityIds.Set);

                if (!other.HasEntities(entityIds.Set))
                    return false;
            }

            {
                using var entityIds = TempHashSet<EntityID>.Allocate();
                other.GetAllEntities(entityIds.Set);

                if (!HasEntities(entityIds.Set))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Returns all the entity IDs that this key contains
        /// </summary>
        /// <param name="entitySet">A set where to store the result</param>
        public void GetAllEntities(HashSet<EntityID> entitySet)
        {
            foreach (var pair in m_Data.Keys)
            {
                entitySet.Add(pair.Key);
            }
        }

        /// <summary>
        /// Tries to retrieve an entity key
        /// </summary>
        /// <param name="entityID">The entity ID</param>
        /// <param name="keyModel">The retrieve entity key if it exists</param>
        /// <returns>True if the entity key exists and was retrieved, False otherwise</returns>
        public bool TryGetKey(EntityID entityID, out EntityKeyModel keyModel)
        {
            return m_Data.Keys.TryGetValue(entityID, out keyModel);
        }

        [OnDeserialized]
        public void OnAfterDeserialize(StreamingContext context)
        {
            RegisterEvents();
        }

        void AddEntityInternal(EntityID entityID, EntityKeyModel entityKeyModel)
        {
            if (m_Data.Keys.ContainsKey(entityID))
                AssertUtils.Fail($"Entity already registered: {entityID}");

            m_Data.Keys[entityID] = entityKeyModel;
            RegisterEntity(entityID, entityKeyModel);
        }

        void RegisterEvents()
        {
            m_Data.LoopKey.OnChanged += OnLoopKeyChanged;
            m_Data.Thumbnail.OnChanged += OnThumbnailChanged;
            
            RegisterAllEntities();
        }

        void OnThumbnailChanged()
        {
            OnChanged?.Invoke(this, Property.Thumbnail);
        }

        void UnregisterEvents()
        {
            m_Data.LoopKey.OnChanged -= OnLoopKeyChanged;
            m_Data.Thumbnail.OnChanged -= OnThumbnailChanged;

            UnregisterAllEntities();
        }

        void RegisterAllEntities()
        {
            foreach (var pair in m_Data.Keys)
                RegisterEntity(pair.Key, pair.Value);
        }

        void UnregisterAllEntities()
        {
            foreach (var pair in m_Data.Keys)
                UnregisterEntity(pair.Key, pair.Value);
        }
        
        void RemoveAllEntities()
        {
            var entitiesToRemove = new List<EntityID>();

            foreach (var pair in m_KeyToEntity)
            {
                entitiesToRemove.Add(pair.Value);
            }

            foreach (var entityID in entitiesToRemove)
            {
                RemoveEntity(entityID);
            }
        }

        void OnLoopKeyChanged(LoopKeyModel model, LoopKeyModel.Property property)
        {
            OnChanged?.Invoke(this, Property.Loop);
        }

        void RegisterEntity(EntityID entityID, EntityKeyModel entityKeyModel)
        {
            if (m_KeyToEntity.ContainsKey(entityKeyModel))
                AssertUtils.Fail($"EntityKeyModel already registered for entity: {entityID}");

            m_KeyToEntity[entityKeyModel] = entityID;
            entityKeyModel.OnChanged += OnEntityKeyModelChanged;
        }

        void UnregisterEntity(EntityID entityID, EntityKeyModel entityKeyModel)
        {
            if (!m_KeyToEntity.ContainsKey(entityKeyModel))
                AssertUtils.Fail($"EntityKeyModel was not registered for entity: {entityID}");

            m_KeyToEntity.Remove(entityKeyModel);
            entityKeyModel.OnChanged -= OnEntityKeyModelChanged;
        }

        void OnEntityKeyModelChanged(EntityKeyModel model, EntityKeyModel.Property property)
        {
            OnChanged?.Invoke(this, Property.EntityKey);
        }

        public void SetThumbnailCameraTransform(Vector3 position, Quaternion rotation)
        {
            m_Data.Thumbnail.Position = position;
            m_Data.Thumbnail.Rotation = rotation;
        }

        public Bounds GetWorldBounds()
        {
            var bounds = new Bounds();
            var first = true;
            foreach (var (_, model) in m_Data.Keys)
            {
                if (first)
                {
                    bounds = model.GetWorldBounds();
                    first = false;
                }
                else
                {
                    bounds.Encapsulate(model.GetWorldBounds());
                }
            }
            return bounds;
        }
    }
}
