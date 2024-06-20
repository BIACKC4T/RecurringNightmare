using System;
using System.Collections;
using Unity.AppUI.Core;
using Unity.Muse.AppUI.UI;
using Unity.DeepPose.Components;
using Unity.EditorCoroutines.Editor;
using Unity.Muse.Common;
using Unity.Muse.Common.Account;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine.UIElements;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using AnimationMode = Unity.Muse.AppUI.UI.AnimationMode;

namespace Unity.Muse.Animate.Editor
{
    class MuseAnimateEditorWindow : EditorWindow, ICoroutineRunner
    {
        [SerializeField]
        Application m_ApplicationPrefab;

        [SerializeField]
        string m_SessionFolder = "Assets/MuseAnimateSessions";

        [SerializeField]
        VisualTreeAsset m_ApplicationUxml;

        /// <summary>
        /// Remembers the current session so we can restore it if there's a domain reload.
        /// </summary>
        /// <remarks>
        /// Why are we saving the path instead of the object reference? Because the object reference doesn't
        /// seem to contain valid data when we restart Unity (maybe a Unity bug?).
        /// </remarks>
        [SerializeField, HideInInspector]
        string m_CurrentSessionPath;

        MuseAnimateSession CurrentSession
        {
            get => m_CurrentSession;
            set
            {
                m_CurrentSession = value;
                m_CurrentSessionPath = AssetDatabase.GetAssetPath(value);
            }
        }

        [SerializeField]
        ThemeStyleSheet m_ApplicationTheme;

        [SerializeField]
        UITemplatesRegistry m_UITemplatesRegistry;

        const string k_WindowName = "Muse Animate Tool";

        Application m_Application;
        Scene m_Scene;
        PhysicsScene m_PhysicsScene;
        bool m_OwnsApplication;
        VisualElement m_RootVisualElement;
        
        VisualElement m_Placeholder;
        
        MuseAnimateSession m_CurrentSession;

        bool m_IsUsable;

        static MuseAnimateEditorWindow Window { get; set; }

        [UnityEditor.MenuItem("Muse/New Animate Generator", false, 100)]
        public static void ShowWindow()
        {
            if (!CheckUnityVersion()) return;
            
            if (Window)
            {
                Window.RestartApplication();
            }
            else
            {
                Window = GetWindow<MuseAnimateEditorWindow>(k_WindowName);
            }
            
            // Create new generator asset
            Window.CreateNewSessionAsset();
        }
        
        #if UNITY_MUSE_DEV
        [UnityEditor.MenuItem("Muse/Internals/Deprecate Muse Animate")]
        public static void DeprecateMuseAnimate()
        {
            ClientStatus.Instance.Status = new ClientStatusResponse
            {
                obsolete_date = DateTime.Today.Subtract(TimeSpan.FromDays(1)).ToString("O"),
                status = "Deprecated"
            };
        }
        
        [UnityEditor.MenuItem("Muse/Internals/Undeprecate Muse Animate")]
        public static void UndeprecateMuseAnimate()
        {
            ClientStatus.Instance.Status = new ClientStatusResponse
            {
                obsolete_date = DateTime.Today.AddDays(1).ToString("O"),
                status = ""
            };
        }
        #endif

        void OnClientStatusChanged(ClientStatusResponse status)
        {
            if (!ClientStatus.Instance.IsClientUsable)
            {
                ShowUpdatePackageUI();
            }
            else if (m_IsUsable)
            {
                // TODO: Rework this crappy logic
                HidePlaceHolder();
            }
        }
        
        static bool CheckUnityVersion()
        {
            var versionString =  UnityEngine.Application.unityVersion;
            var components = versionString.Split('.');
            var version = new Version(components[0] + "." + components[1]);

            if (version > ApplicationConstants.MaxUnityVersion)
            {
                var message = $"Muse Animate does not yet support {UnityEngine.Application.unityVersion}. Please use Unity {ApplicationConstants.MaxUnityVersion} or earlier.";
                EditorUtility.DisplayDialog("Unsupported Unity Version",
                    message,
                    "Close");
                return false;
            }
            
            return true;
        }
        
        void OnGUI() 
        {
            Event e = Event.current;
            
            if (e.isKey) 
            {
                if (e.keyCode != KeyCode.None)
                {
                    if (e.type == EventType.KeyDown)
                    {
                        InputUtils.KeyDown(e.keyCode);
                    }
                    else if (e.type == EventType.KeyUp)
                    {
                        InputUtils.KeyUp(e.keyCode);
                    }
                }
            }
        }
        
        void CreateGUI()
        {
            // Temporarily disable the app if the Unity version is not supported
            if (!CheckUnityVersion())
            {
                Window.Close();
                return;
            }

            ClientStatus.Instance.OnClientStatusChanged -= OnClientStatusChanged;
            ClientStatus.Instance.OnClientStatusChanged += OnClientStatusChanged;
            ClientStatus.Instance.UpdateStatus();
            
            // Inject the template registry for Edit Mode (this needs to be done before
            // we can create the components in the visual tree.)
            if (m_UITemplatesRegistry == null)
            {
                DevLogger.LogError("UITemplatesRegistry asset not found");
                return;
            }
            Locator.Provide(m_UITemplatesRegistry);
            Locator.Provide<ICoroutineRunner>(this);

            if (m_Application != null)
            {
                m_Application.Shutdown();
            }
            
            m_Application = LoadApplication();

            ClearRootVisualElement(m_RootVisualElement);
            m_RootVisualElement = CreateRootVisualElement();

            InitializeApplication();
            
            // If we are coming from a domain reload, we need to load the current session again.
            if (!string.IsNullOrEmpty(m_CurrentSessionPath))
            {
                var session = AssetDatabase.LoadAssetAtPath<MuseAnimateSession>(m_CurrentSessionPath);
                if (session != null)
                {
                    LoadSession(session);
                }
            }
            
            AccountController.Register(this);
        }
        
        VisualElement CreateRootVisualElement()
        {
            m_ApplicationUxml.CloneTree(rootVisualElement);
            rootVisualElement.styleSheets.Add(m_ApplicationTheme);
            return rootVisualElement;
        }

        static void ClearRootVisualElement(VisualElement rootVisualElement)
        {
            if (rootVisualElement == null) return;
            
            foreach (var element in rootVisualElement.Children())
            {
                element.RemoveFromHierarchy();
            }
        }

        void ShowPlaceholder()
        {
            if (m_Placeholder == null)
            {
                m_Placeholder = new VisualElement
                {
                    style =
                    {
                        position = Position.Absolute,
                        left = 0,
                        right = 0,
                        top = 0,
                        bottom = 0,
                        backgroundColor = new Color(0, 0, 0, 1.0f),
                        display = DisplayStyle.Flex
                    }
                };

                var preloader = new Preloader
                {
                    style =
                    {
                        width = new Length(100, LengthUnit.Percent),
                        height = new Length(100, LengthUnit.Percent)
                    }
                };
                
                m_Placeholder.Add(preloader);
                rootVisualElement.Add(m_Placeholder);
            }
            
            m_Placeholder.style.display = DisplayStyle.Flex;
            m_Placeholder.BringToFront();
        }

        void HidePlaceHolder()
        {
            if (m_Placeholder == null) return;
            
            m_Placeholder.style.display = DisplayStyle.None;
            m_Placeholder.SendToBack();
        }

        void ShowUpdatePackageUI()
        {
            if (m_Placeholder != null)
            {
                m_Placeholder.style.display = DisplayStyle.Flex;
                m_Placeholder.BringToFront();

                var preloader = m_Placeholder.Q<Preloader>();
                preloader.style.display = DisplayStyle.None;

                var textGroup = new VisualElement
                {
                    name = "muse-node-disable-message-group",
                    style =
                    {
                        width = new Length(100, LengthUnit.Percent),
                        height = new Length(100, LengthUnit.Percent)
                    }
                };
                textGroup.Add(new Text
                {
                    text = TextContent.clientStatusUpdateMessage, enableRichText = true,
                    style =
                    {
                        width = new Length(100, LengthUnit.Percent),
                        height = new Length(100, LengthUnit.Percent),
                        unityTextAlign = TextAnchor.MiddleCenter
                    }
                });
                textGroup.AddToClassList("muse-node-message-link");
                textGroup.RegisterCallback<PointerUpEvent>(_ => UpdateAnimatePackage());
                m_Placeholder.Add(textGroup);
            }
        }

        static void UpdateAnimatePackage()
        {
#if UNITY_EDITOR
            UnityEditor.PackageManager.UI.Window.Open("com.unity.muse.animate");
#endif
        }
        
        void OnAuthoringStarted(AuthoringStartedMessage message)
        {
            m_IsUsable = true;
         
            // TODO: Rework this crappy logic
            if (ClientStatus.Instance.IsClientUsable)
                HidePlaceHolder();
        }

        void SimulatePhysics(float deltaTime)
        {
            if (!m_PhysicsScene.IsValid())
            {
                return;
            }

            m_PhysicsScene.Simulate(deltaTime);
        }

        Application LoadApplication()
        {
#if UNITY_2023_1_OR_NEWER
            var application = FindFirstObjectByType<Application>();
#else
            var application = FindObjectOfType<Application>();
#endif
            if ((m_Application = application) == null)
            {
                var appPrefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(m_ApplicationPrefab);
                PrefabUtility.LoadPrefabContentsIntoPreviewScene(appPrefabPath, m_Scene);

                // The application is the first root object in the scene
                m_Application = m_Scene.GetRootGameObjects()[0].GetComponent<Application>();

                Locator.Provide<IRootObjectSpawner<GameObject>>(new RuntimeRootObjectSpawner(m_Application.transform));

                m_Application.SubscribeToMessage<SaveSessionMessage>(SaveCurrentSession);
                m_Application.SubscribeToMessage<ExportAnimationMessage>(ExportAnimation);
                m_Application.SubscribeToMessage<VersionErrorMessage>(OnVersionError);
                m_Application.SubscribeToMessage<AuthoringStartedMessage>(OnAuthoringStarted);
                m_OwnsApplication = true;
            }

            return m_Application;
        }

        void OnVersionError(VersionErrorMessage message)
        {
            const string toastMessage = "Version incompatibility detected. Please update to the latest version of Muse Animate.";
            m_RootVisualElement.Q<Panel>().OpenToast(toastMessage,
                NotificationStyle.Negative,
                NotificationDuration.Indefinite,
                AnimationMode.Slide);
        }

        /// <summary>
        /// Perform any cleanup necessary to stop the application, cancelling any pending requests, as well as
        /// triggering a SaveSessionMessage.
        /// </summary>
        void StopApplication()
        {
            if (m_Application)
            {
                m_Application.Shutdown();
            }
        }

        void InitializeApplication()
        {
            ShowPlaceholder();

            m_IsUsable = false;
            
            if (m_Application == null)
            {
                return;
            }

            m_Application.Initialize(m_RootVisualElement);
            m_Application.ViewCamera.scene = m_Scene;
            m_Application.ThumbnailCamera.scene = m_Scene;

            // Do not render the view camera automatically. We will call Render() manually.
            m_Application.ViewCamera.enabled = false;
        }

        void RestartApplication()
        {
            StopApplication();
            InitializeApplication();
        }
        
        void OnEnable()
        {
            m_Scene = EditorSceneManager.NewPreviewScene();
            m_PhysicsScene = m_Scene.GetPhysicsScene();

            // TODO: Change this to use a Locator after we reorganize the package so we don't
            // have this coupling.
            PhysicsSolver.OnSimulate += SimulatePhysics;
            
            // When the editor is closing, OnDisable/OnDestroy is not called. Since the Application isn't being
            // shut down normally, we need to catch this event to save the session. (Note: this may introduce a 
            // slight delay when trying to close the Editor.)
            EditorApplication.quitting += SaveCurrentSession;
        }

        // TODO: Why does OnDisable get called twice on Unity startup?
        void OnDisable()
        {
            StopApplication();
            
            if (m_OwnsApplication)
            {
                // Is this necessary?
                DestroyImmediate(m_Application.gameObject);
            }

            m_Application = null;
            m_OwnsApplication = false;
            
            Window = null;
            
            PhysicsSolver.OnSimulate -= SimulatePhysics;
            EditorSceneManager.ClosePreviewScene(m_Scene);
            ClientStatus.Instance.OnClientStatusChanged -= OnClientStatusChanged;
            EditorApplication.quitting -= SaveCurrentSession;
        }

        void OnFocus()
        {
            if (m_Application != null)
            {
                m_Application.Resume();
            }
        }
        
        void OnLostFocus()
        {
            if (m_Application != null)
            {
                m_Application.Pause();
            }
        }

        /// <summary>
        /// Create a new session asset and set it as the current session.
        /// </summary>
        void CreateNewSessionAsset()
        {
            // Create a new session asset
            if (!AssetDatabase.IsValidFolder(m_SessionFolder))
            {
                var parentFolder = m_SessionFolder[..m_SessionFolder.LastIndexOf('/')];
                var folderName = m_SessionFolder[(m_SessionFolder.LastIndexOf('/') + 1)..];
                AssetDatabase.CreateFolder(parentFolder, folderName);
            }

            var path = AssetDatabase.GenerateUniqueAssetPath($"{m_SessionFolder}/Session.asset");
            var asset = CreateInstance<MuseAnimateSession>();

            DevLogger.LogInfo($"Creating Muse Animate Generator asset: {path}");
            AssetDatabase.CreateAsset(asset, path);

            CurrentSession = asset;
        }

        void SaveCurrentSession()
        {
            if (CurrentSession == null)
            {
                return;
            }

            DevLogger.LogInfo($"Saving Muse Animate Generator: {CurrentSession.name}");
            
            CurrentSession.SetData(m_Application.ApplicationContext.Stage);

            // Save the asset to the project
            EditorUtility.SetDirty(CurrentSession);
            AssetDatabase.SaveAssetIfDirty(CurrentSession);
        }

        void SaveCurrentSession(SaveSessionMessage message) => SaveCurrentSession();

        static void ExportAnimation(ExportAnimationMessage message)
        {
            if (message.ExportType is Application.ApplicationHsm.Author.ExportType.HumanoidAnimation)
            {
                var clip = HumanoidAnimationExport.Export(message.ExportData);
                if (clip != null)
                {
                    var path = EditorUtility.SaveFilePanelInProject("Save Animation", "Animation", "anim", "Save animation");
                    if (!string.IsNullOrEmpty(path))
                    {
                        AssetDatabase.CreateAsset(clip, path);
                        AssetDatabase.SaveAssetIfDirty(clip);
                        AssetDatabase.Refresh();
                    }
                }
            }
        }

        void LoadSession(MuseAnimateSession session)
        {
            // If we are creating a new Application instance, so we need to wait a frame before loading the session
            // because the Application is not fully initialized until the next Update
            EditorApplication.delayCall += () => m_Application.PublishMessage(new LoadSessionMessage(session.JsonData));
            CurrentSession = session;
        }

        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            if (EditorUtility.InstanceIDToObject(instanceID) is MuseAnimateSession session)
            {
                if (!CheckUnityVersion()) return false;
                
                if (UnityEngine.Application.isPlaying)
                {
#if UNITY_2023_1_OR_NEWER
                    var application = FindFirstObjectByType<Application>();
#else
                    var application = FindObjectOfType<Application>();
#endif
                    if (application != null)
                    {
                        application.PublishMessage(new LoadSessionMessage(session.JsonData));
                    }
                }
                else
                {
                    if (Window != null)
                    {
                        // If the window is already open, load the selected session asset without closing the window.
                        
                        // Ensure that the application logic stops cleanly before we load the new session.
                        Window.RestartApplication();
                        Window.LoadSession(session);
                    }
                    else
                    {
                        // The window is not open, so we need to open it and then load the session asset.
                        
                        // The act of opening the window will initialize the application
                        Window = GetWindow<MuseAnimateEditorWindow>(k_WindowName);
                        Window.LoadSession(session);
                    }
                }

                return true;
            }

            return false;
        }

        public void StartCoroutine(IEnumerator routine)
        {
            EditorCoroutineUtility.StartCoroutine(routine, this);
        }
    }
}
