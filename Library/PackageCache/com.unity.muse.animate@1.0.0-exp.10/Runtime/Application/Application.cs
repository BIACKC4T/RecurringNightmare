using System;
using System.Collections;
using System.Collections.Generic;
using Hsm;

using Unity.DeepPose.Components;
using Unity.Muse.Common;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.Muse.Animate
{
    /// <summary>
    /// Represents the Application instance, responsible for main logic of the app
    /// </summary>
    partial class Application : MonoBehaviour, ICoroutineRunner
    {
        [SerializeField]
        public ApplicationConfiguration ApplicationConfiguration;
        
        [SerializeField]
        public PhysicsSolverComponent PosingPhysicsSolver;

        [SerializeField]
        public PhysicsSolverComponent MotionPhysicsSolver;

        public Camera ViewCamera { get; private set; }

        public Camera ThumbnailCamera { get; private set; }
        
        public UIDocument UIDocument { get; private set; }
        
        public Transform Environment { get; private set; }

        [NonSerialized]
        public ApplicationContext ApplicationContext;
        
        readonly MessageBus m_MessageBus = new ();

        public async void GetAuthHeaders(Action<Dictionary<string, string>> onSuccess)
        {
            var headers = new Dictionary<string, string>();
#if UNITY_EDITOR
            var token = await UnityConnectUtils.GetUserAccessTokenAsync();
            headers.Add(ApplicationConstants.AuthorizationHeaderName, $"Bearer {token}");
#endif
            onSuccess?.Invoke(headers);
        }

        partial class ApplicationHsm { }

        StateMachine m_ApplicationFlow = null;
        StateMachine m_CameraFlow = new();
        bool m_Paused;

        float DeltaTime => m_Paused ? 0 : Time.deltaTime;

        static Application s_Instance;

        public static Application Instance
        {
            get
            {
                // UI Toolkit uses Instance before the Awake call is made from Application,
                // in that case we recover it in the scene:
                if (s_Instance == null)
                {
#if UNITY_2023_1_OR_NEWER
                    s_Instance = FindFirstObjectByType<Application>();
#else
                    s_Instance = FindObjectOfType<Application>();
#endif
                }

                Assert.IsNotNull(s_Instance, "Application was not found in the scene");
                return s_Instance;
            }
        }
        
        public StateMachine ApplicationFlow => m_ApplicationFlow;

        void OnEnable()
        {
            if (UnityEngine.Application.isPlaying)
            {
                Locator.Provide(ApplicationConfiguration.UITemplatesRegistry);
                Locator.Provide<IRootObjectSpawner<GameObject>>(new RuntimeRootObjectSpawner(transform));
                Locator.Provide<ICoroutineRunner>(this);
            }
        }

        void Start()
        {
            Assert.IsNotNull(ApplicationConfiguration, "No ApplicationConfiguration is defined in Application Prefab.");
            UIDocument = Instantiate(ApplicationConfiguration.UIDocumentPrefab, transform);
            Initialize(UIDocument.rootVisualElement);
        }

        void OnDisable()
        {
            Shutdown();
        }

        public void Initialize(VisualElement rootVisualElement)
        {            
            DevLogger.LogInfo("Application -> Initialize()");
            Assert.IsNotNull(ApplicationConfiguration, "No ApplicationConfiguration is defined in Application Prefab.");
            Assert.IsTrue(s_Instance == null || s_Instance == this, "There must be only one instance of Application in the scene");
            s_Instance = this;
            
            ApplicationConstants.LoopGhostMaterial = ApplicationConfiguration.LoopGhostMaterial;

            InitializeCameras();
            InitializeEnvironment();

            ApplicationLayers.AssignLayers(transform);
            PhysicsUtils.SetLayerCollisionMatrix();

            ApplicationContext = new ApplicationContext(rootVisualElement, ApplicationConfiguration.ActorRegistry, ApplicationConfiguration.PropRegistry, ViewCamera, ThumbnailCamera,
                PosingPhysicsSolver, MotionPhysicsSolver);

            m_ApplicationFlow = new StateMachine();
            m_ApplicationFlow.Init<ApplicationHsm.Root>(this);
#if !DEEPPOSE_HDRP && !DEEPPOSE_URP
            ApplicationContext.Camera.RenderScaling = Vector2.one * 2f;
            #endif
#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
            {
                EditorApplication.update -= EditorUpdate;
                EditorApplication.update += EditorUpdate;
            }
#endif
        }

        void InitializeCameras()
        {
#if DEEPPOSE_HDRP
            Camera cameraPrefab = ApplicationConfiguration.CameraPrefabHDRP;
            Shader.EnableKeyword("RENDER_PIPELINE_HDRP");
#elif DEEPPOSE_URP
            Camera cameraPrefab = RenderPipelineUtils.IsUsingUrp() ? ApplicationConfiguration.CameraPrefabURP : ApplicationConfiguration.CameraPrefab;
            Shader.DisableKeyword("RENDER_PIPELINE_HDRP");
#else
            Camera cameraPrefab = ApplicationConfiguration.CameraPrefab;
            Shader.DisableKeyword("RENDER_PIPELINE_HDRP");
#endif
           
            ViewCamera = Instantiate(cameraPrefab, transform);
            ViewCamera.cullingMask = ApplicationLayers.LayerMaskAll;
            ViewCamera.name = "ViewCamera";
            
            ThumbnailCamera = Instantiate(cameraPrefab, transform);
            ThumbnailCamera.cullingMask = ApplicationLayers.LayerMaskThumbnail | ApplicationLayers.LayerMaskHandles | ApplicationLayers.LayerMaskPosing;
            ThumbnailCamera.gameObject.SetActive(false);
            ThumbnailCamera.name = "ThumbnailCamera";
        }

        void ClearCameras()
        {
            if (ViewCamera)
            {
                GameObjectUtils.Destroy(ViewCamera.gameObject);
            }
            
            if (ThumbnailCamera)
            {
                GameObjectUtils.Destroy(ThumbnailCamera.gameObject);
            }
        }
        
        void InitializeEnvironment()
        {
#if DEEPPOSE_HDRP
            Environment = Instantiate(ApplicationConfiguration.EnvironmentPrefabHDRP, transform);
#elif DEEPPOSE_URP
            if (RenderPipelineUtils.IsUsingUrp())
            {
                Instantiate(ApplicationConfiguration.EnvironmentPrefabURP, transform);
            }
            else
            {
                Instantiate(ApplicationConfiguration.EnvironmentPrefab, transform);
            }
            
            Shader.DisableKeyword("RENDER_PIPELINE_HDRP");
#else
            Environment = Instantiate(ApplicationConfiguration.EnvironmentPrefab, transform);
#endif
        }
        
        void ClearEnvironment()
        {
            if (Environment != null)
            {
                GameObjectUtils.Destroy(Environment.gameObject);
            }
        }
        
        public void Shutdown()
        {
            DevLogger.LogInfo("Application -> Shutdown()");
#if UNITY_EDITOR
            EditorApplication.update -= EditorUpdate;
#endif
            m_ApplicationFlow?.Shutdown();
            
            // Request a save before clearing the stage
            PublishMessage<SaveSessionMessage>();
            
            m_ApplicationFlow = null;
            ApplicationContext?.Stage.RemoveAllActors();
            ApplicationContext?.Stage.RemoveAllProps();
            ClearCameras();
            ClearEnvironment();
            s_Instance = null;
        }

        public void Update()
        {
            if (m_ApplicationFlow == null)
                return;
            
            m_ApplicationFlow.Update(DeltaTime);
        }
        
        public void LateUpdate()
        {
            if (m_ApplicationFlow == null)
                return;
            
            m_ApplicationFlow.LateUpdate(DeltaTime);
        }
        
        void EditorUpdate()
        {
            m_ApplicationFlow?.Update(DeltaTime);
            m_ApplicationFlow?.LateUpdate(DeltaTime);
        }

        public void PublishMessage<T>(T message)
        {
            m_MessageBus.Publish(message);
        }
        
        public void PublishMessage<T>() where T : new()
        {
            m_MessageBus.Publish(new T());
        }
        
        public void SubscribeToMessage<T>(Action<T> handler)
        {
            m_MessageBus.Subscribe(handler);
        }
        
        public void UnsubscribeFromMessage<T>(Action<T> handler)
        {
            m_MessageBus.Unsubscribe(handler);
        }

        public new void StartCoroutine(IEnumerator routine)
        {
            base.StartCoroutine(routine);
        }

        public void Pause()
        {
            DevLogger.LogInfo("Application -> Pause()");
            m_Paused = true;
        }

        public void Resume()
        {
            DevLogger.LogInfo("Application -> Resume()");
            m_Paused = false;
        }
    }
}
