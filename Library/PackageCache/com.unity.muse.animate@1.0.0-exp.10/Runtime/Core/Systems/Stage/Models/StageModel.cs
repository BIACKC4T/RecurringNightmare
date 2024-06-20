using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Muse.Animate
{
    /// <summary>
    /// Represents a stage.
    /// A stage contains all and only the data that should be saved with the session
    /// </summary>
    [Serializable]
    class StageModel
    {
        const int k_InitialActorCapacity = 8;
        const int k_InitialPropsCapacity = 8;

        static JsonSerializerSettings s_SerializationSettings = new()
        {
            MaxDepth = 50,
            ContractResolver = new UnityContractResolver()
        };

        [SerializeField]
        StageData m_Data;

        [NonSerialized]
        ActorRegistry m_ActorRegistry;

        [NonSerialized]
        PropRegistry m_PropRegistry;

        [NonSerialized]
        Dictionary<ActorID, ActorDefinitionComponent> m_InstantiatedActors = new();

        [NonSerialized]
        Dictionary<PropID, PropDefinitionComponent> m_InstantiatedProps = new();

        public PropRegistry PropRegistry => m_PropRegistry;
        public ActorRegistry ActorRegistry => m_ActorRegistry;

        public TimelineModel Timeline => m_Data.Timeline;
        public BakedTimelineModel BakedTimeline => m_Data.BakedTimeline;
        public BakedTimelineMappingModel BakedTimelineMapping => m_Data.BakedTimelineMapping;

        public string Name
        {
            get => m_Data.Name;
            set
            {
                Assert.IsFalse(string.IsNullOrEmpty(value), "Name cannot be empty or null");
                if (value == m_Data.Name)
                    return;

                m_Data.Name = value;
                OnNameChanged?.Invoke(this, m_Data.Name);
            }
        }

        public int NumActors => m_Data.Actors.Count;
        public int NumProps => m_Data.Props.Count;

        public int NumCameraViewpoints => m_Data.CameraViewpoints.Count;

        public bool IsValid
        {
            get
            {
                if (m_Data.Actors == null || m_Data.Timeline == null || m_Data.BakedTimeline == null)
                    return false;

                if (!m_Data.Timeline.IsValid || !m_Data.BakedTimeline.IsValid)
                    return false;

                foreach (var actor in m_Data.Actors)
                {
                    if (actor == null || !actor.IsValid)
                        return false;
                }

                return true;
            }
        }

        public TakesLibraryModel TakesLibrary => m_Data.TakesLibrary;

        public delegate void NameChanged(StageModel model, string newName);
        public event NameChanged OnNameChanged;

        public delegate void ActorAdded(StageModel model, ActorModel actorModel, ActorDefinitionComponent component);
        public event ActorAdded OnActorAdded;

        public delegate void ActorRemoved(StageModel model, ActorModel actorModel);
        public event ActorRemoved OnActorRemoved;

        public delegate void PropAdded(StageModel model, PropModel propModel, PropDefinitionComponent component);
        public event PropAdded OnPropAdded;

        public delegate void PropRemoved(StageModel model, PropModel propModel);
        public event PropRemoved OnPropRemoved;

        public StageModel(ActorRegistry actorRegistry, PropRegistry propRegistry, string name = "Unnamed")
        {
            Assert.IsNotNull(actorRegistry, "You must provide an actor registry");
            m_ActorRegistry = actorRegistry;

            Assert.IsNotNull(propRegistry, "You must provide a prop registry");
            m_PropRegistry = propRegistry;

            m_Data.Version = StageData.DataVersion;
            m_Data.Name = name;
            m_Data.Actors = new List<ActorModel>(k_InitialActorCapacity);
            m_Data.Props = new List<PropModel>(k_InitialPropsCapacity);
            m_Data.Timeline = new TimelineModel();
            m_Data.BakedTimeline = new BakedTimelineModel();
            m_Data.BakedTimelineMapping = new BakedTimelineMappingModel();
            m_Data.CameraViewpoints = new List<CameraCoordinatesModel>();
            m_Data.TakesLibrary = new TakesLibraryModel();

            RegisterEvents();
        }

        [JsonConstructor]
        public StageModel(StageData m_Data)
        {
            this.m_Data = m_Data;
        }

        public void RemoveEntity(EntityID entityID)
        {
            TryGetActorModel(entityID, out var actorModel);
            TryGetPropModel(entityID, out var propModel);

            if (actorModel == null && propModel == null)
                throw new ArgumentOutOfRangeException($"Cannot find entity: {entityID}");

            if (actorModel != null)
            {
                Assert.IsNull(propModel, "Entity cannot be both actor and prop");
                RemoveActor(actorModel.ID);
            }

            if (propModel != null)
            {
                Assert.IsNull(actorModel, "Entity cannot be both actor and prop");
                RemoveProp(propModel.ID);
            }
        }

        public ActorID CreateActor(string prefabID, Vector3 position, Quaternion rotation)
        {
            // Create actor model
            var actorID = ActorID.Generate();
            var actorModel = new ActorModel(actorID, prefabID, position, rotation);
            Assert.IsTrue(actorModel.IsValid, "Actor Model is invalid");

            // Register actor model
            Assert.IsFalse(m_Data.Actors.Contains(actorModel), "Actor Model was already added");
            m_Data.Actors.Add(actorModel);

            InstantiateActor(actorModel);

            return actorID;
        }

        public void RemoveActor(ActorID actorID)
        {
            var success = TryGetActorModel(actorID, out var actorModel);
            if (!success)
                AssertUtils.Fail($"Could not find actor with ID: {actorID.ToString()}");

            if (!m_InstantiatedActors.ContainsKey(actorID))
                AssertUtils.Fail($"Actor instance not found for: {actorID}");

            // Remove from timeline data
            m_Data.Timeline.RemoveEntity(actorID.EntityID);
            m_Data.BakedTimeline.RemoveEntity(actorID.EntityID);

            var actorComponent = m_InstantiatedActors[actorID];
            Assert.IsNotNull(actorComponent, "Got null actor instance");

            // Unregister actor model
            m_Data.Actors.Remove(actorModel);

            OnActorRemoved?.Invoke(this, actorModel);
            
            // Destroy instance
            var actorInstance = actorComponent.gameObject;
            GameObjectUtils.Destroy(actorInstance);
        }

        public void RemoveAllActors()
        {
            using var tmpList = TempList<ActorModel>.Allocate();

            foreach (var actorModel in m_Data.Actors)
            {
                tmpList.Add(actorModel);
            }

            foreach (var actorModel in tmpList.List)
            {
                RemoveActor(actorModel.ID);
            }
        }

        public ActorID GetActorID(int actorIndex)
        {
            var actorModel = GetActorModel(actorIndex);
            return actorModel.ID;
        }

        public ActorModel GetActorModel(int actorIndex)
        {
            if (actorIndex < 0 || actorIndex > NumActors)
                AssertUtils.Fail($"Invalid actor index: {actorIndex.ToString()}");
            var actorModel = m_Data.Actors[actorIndex];
            return actorModel;
        }

        public bool TryGetActorModel(EntityID entityID, out ActorModel model)
        {
            // TODO: fast lookup
            foreach (var actorModel in m_Data.Actors)
            {
                if (actorModel.EntityID == entityID)
                {
                    model = actorModel;
                    return true;
                }
            }

            model = null;
            return false;
        }

        public bool TryGetActorModel(ActorID actorID, out ActorModel model)
        {
            // TODO: fast lookup
            foreach (var actorModel in m_Data.Actors)
            {
                if (actorModel.ID == actorID)
                {
                    model = actorModel;
                    return true;
                }
            }

            model = null;
            return false;
        }

        public ActorDefinitionComponent GetActorInstance(ActorID actorID)
        {
            if (!m_InstantiatedActors.TryGetValue(actorID, out var actorDefinitionComponent))
                AssertUtils.Fail($"No instance found for actor: {actorID}");

            return actorDefinitionComponent;
        }

        public PropID CreateProp(string prefabID, Vector3 position, Quaternion rotation)
        {
            // Create model
            var propID = PropID.Generate();
            var propModel = new PropModel(propID, prefabID, position, rotation);
            Assert.IsTrue(propModel.IsValid, "Prop Model is invalid");

            // Register model
            Assert.IsFalse(m_Data.Props.Contains(propModel), "Prop Model was already added");
            m_Data.Props.Add(propModel);

            InstantiateProp(propModel);

            return propID;
        }

        public void RemoveProp(PropID propID)
        {
            var success = TryGetPropModel(propID, out var propModel);
            if (!success)
                AssertUtils.Fail($"Could not find prop with ID: {propID.ToString()}");

            if (!m_InstantiatedProps.ContainsKey(propID))
                AssertUtils.Fail($"Prop instance not found for: {propID}");

            // Remove from timeline data
            m_Data.Timeline.RemoveEntity(propID.EntityID);
            m_Data.BakedTimeline.RemoveEntity(propID.EntityID);

            var propComponent = m_InstantiatedProps[propID];
            Assert.IsNotNull(propComponent, "Got null prop instance");

            // Destroy instance
            var propInstance = propComponent.gameObject;
            GameObjectUtils.Destroy(propInstance);

            // Unregister actor model
            m_Data.Props.Remove(propModel);

            OnPropRemoved?.Invoke(this, propModel);
        }

        public void RemoveAllProps()
        {
            using var tmpList = TempList<PropModel>.Allocate();

            foreach (var propModel in m_Data.Props)
            {
                tmpList.Add(propModel);
            }

            foreach (var propModel in tmpList.List)
            {
                RemoveProp(propModel.ID);
            }
        }

        public PropID GetPropID(int propIndex)
        {
            var propModel = GetPropModel(propIndex);
            return propModel.ID;
        }

        public PropModel GetPropModel(int propIndex)
        {
            if (propIndex < 0 || propIndex > NumProps)
                AssertUtils.Fail($"Invalid prop index: {propIndex.ToString()}");
            var propModel = m_Data.Props[propIndex];
            return propModel;
        }

        public bool TryGetPropModel(PropID propID, out PropModel model)
        {
            // TODO: fast lookup
            foreach (var propModel in m_Data.Props)
            {
                if (propModel.ID == propID)
                {
                    model = propModel;
                    return true;
                }
            }

            model = null;
            return false;
        }

        public bool TryGetPropModel(EntityID entityID, out PropModel model)
        {
            // TODO: fast lookup
            foreach (var propModel in m_Data.Props)
            {
                if (propModel.EntityID == entityID)
                {
                    model = propModel;
                    return true;
                }
            }

            model = null;
            return false;
        }

        public PropDefinitionComponent GetPropInstance(PropID propID)
        {
            if (!m_InstantiatedProps.TryGetValue(propID, out var propDefinitionComponent))
                AssertUtils.Fail($"No instance found for prop: {propID}");

            return propDefinitionComponent;
        }

        public void Clear()
        {
            RemoveAllActors();
            RemoveAllProps();
            m_Data.Timeline.Clear();
            m_Data.BakedTimeline.Clear();
            m_Data.BakedTimelineMapping.Clear();
            m_Data.TakesLibrary.Clear();
        }

        public CameraCoordinatesModel GetCameraViewpoint(int idx)
        {
            if (idx < 0 || idx >= m_Data.CameraViewpoints.Count)
                AssertUtils.Fail($"Invalid camera viewpoint index: {idx}");

            return m_Data.CameraViewpoints[idx];
        }

        public CameraCoordinatesModel AddCameraViewpoint()
        {
            return AddCameraViewpoint(Vector3.zero, Quaternion.identity, 2f);
        }

        public CameraCoordinatesModel AddCameraViewpoint(Vector3 position, Quaternion rotation, float distanceToPivot)
        {
            var cameraCoordinates = new CameraCoordinatesModel(position, rotation, distanceToPivot);
            m_Data.CameraViewpoints.Add(cameraCoordinates);

            return cameraCoordinates;
        }

        public string Save()
        {
            var json = JsonConvert.SerializeObject(m_Data, s_SerializationSettings);
            return json;
        }

        public bool Load(string json)
        {
            StageData newData;
            try
            {
                newData = JsonConvert.DeserializeObject<StageData>(json, s_SerializationSettings);
            }
            catch (JsonSerializationException e)
            {
                Debug.LogError("Failed to deserialize stage data with error: " + e.Message);
                return false;
            }

            if (newData.Version != StageData.DataVersion)
            {
                Debug.LogError("Failed to deserialize stage data, data version was " +
                            newData.Version + " which is incompatible with current version " + StageData.DataVersion);
                return false;
            }

            Clear();

            m_Data = JsonConvert.DeserializeObject<StageData>(json, s_SerializationSettings);

            foreach (var actorModel in m_Data.Actors)
            {
                InstantiateActor(actorModel);
            }

            foreach (var propModel in m_Data.Props)
            {
                InstantiateProp(propModel);
            }

            RegisterEvents();
            return true;
        }

        void InstantiateActor(ActorModel actorModel)
        {
            Assert.IsTrue(actorModel.IsValid, "Actor Model is invalid");
            Assert.IsTrue(m_Data.Actors.Contains(actorModel), "Actor Model was not registered");

            var actorID = actorModel.ID;
            var prefabID = actorModel.PrefabID;

            if (!m_ActorRegistry.TryGetActorDefinition(prefabID, out var actorDefinition))
                AssertUtils.Fail($"Could not find prefab in registry: {prefabID}");

            // Instantiate actor instance
            var actorPrefab = actorDefinition.Prefab.gameObject;
            
            if (!Locator.TryGet<IRootObjectSpawner<GameObject>>(out var spawner))
                AssertUtils.Fail("Runtime object spawner not found");
            
            var actorInstance = spawner.Instantiate(actorPrefab, actorModel.SpawnPosition, actorModel.SpawnRotation);
            var actorComponent = actorInstance.GetComponent<ActorDefinitionComponent>();
            Assert.IsNotNull(actorComponent, "Actor must have an ActorDefinitionComponent");

            // Register actor instance
            m_InstantiatedActors[actorID] = actorComponent;

            OnActorAdded?.Invoke(this, actorModel, actorComponent);
        }

        void InstantiateProp(PropModel propModel)
        {
            Assert.IsTrue(propModel.IsValid, "Prop Model is invalid");
            Assert.IsTrue(m_Data.Props.Contains(propModel), "Prop Model was not registered");

            var propID = propModel.ID;
            var prefabID = propModel.PrefabID;

            if (!m_PropRegistry.TryGetPropInfo(prefabID, out var propDefinition))
                AssertUtils.Fail($"Could not find prefab in registry: {prefabID}");

            // Instantiate actor instance
            var propPrefab = propDefinition.Prefab.gameObject;
            
            if (!Locator.TryGet<IRootObjectSpawner<GameObject>>(out var spawner))
                AssertUtils.Fail("Runtime object spawner not found");
            
            var propInstance = spawner.Instantiate(propPrefab, propModel.SpawnPosition, propModel.SpawnRotation);
            
            // propInstance.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor;
            var propComponent = propInstance.GetComponent<PropDefinitionComponent>();
            Assert.IsNotNull(propComponent, "Actor must have a PropDefinitionComponent");

            // Register actor instance
            m_InstantiatedProps[propID] = propComponent;

            OnPropAdded?.Invoke(this, propModel, propComponent);
        }

        void RegisterEvents()
        {
            // TODO
        }
    }
}
