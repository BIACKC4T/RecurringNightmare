using Unity.DeepPose.Components;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Muse.AppUI.UI;

namespace Unity.Muse.Animate
{
    class ApplicationContext
    {
        const string k_MenuName = "application-menu";

        public CameraModel Camera => m_CameraContext.Camera;
        public CameraMovementModel CameraMovement => m_CameraContext.CameraMovement;
        public CameraMovementViewModel CameraMovementViewModel { get; }
        public Panel RootVisualElement { get; }
        public ApplicationMenuUIModel ApplicationMenuUIModel { get; }
        public StageModel Stage { get; }
        public TutorialLogic Tutorial { get; }
        public ThumbnailsService ThumbnailsService { get; }
        
        readonly ClipboardService m_Clipboard;
        readonly PoseLibraryService m_PoseLibrary;
        readonly CameraMovementView m_CameraMovementView;
        readonly CameraContext m_CameraContext;
        readonly PhysicsSolverComponent m_PosingPhysicsSolver;
        readonly PhysicsSolverComponent m_BakingPhysicsSolver;
        readonly TextToMotionService m_TextToMotionService;

        public ApplicationContext(VisualElement rootVisualElement, ActorRegistry actorRegistry, PropRegistry propRegistry, Camera camera, Camera thumbnailCamera,
            PhysicsSolverComponent posingPhysicsSolver, PhysicsSolverComponent bakingPhysicsSolver)
        {
            // Solvers
            m_PosingPhysicsSolver = posingPhysicsSolver;
            m_BakingPhysicsSolver = bakingPhysicsSolver;

            if (!UnityEngine.Application.isPlaying)
            {
                m_PosingPhysicsSolver.Initialize();
                m_BakingPhysicsSolver.Initialize();
            }

            // UI
            RootVisualElement = rootVisualElement.Q<Panel>();

            // Scene Camera
            m_CameraContext = new CameraContext(camera);

            // Scene Camera View Model
            CameraMovementViewModel = new CameraMovementViewModel(m_CameraContext.CameraMovement);
            var cameraGameObject = m_CameraContext.Camera.Target.gameObject;

            // Scene Camera View (Handles the PointerEvents on the scene)
            m_CameraMovementView = cameraGameObject.GetComponent<CameraMovementView>();
            if (m_CameraMovementView == null)
                m_CameraMovementView = cameraGameObject.AddComponent<CameraMovementView>();
            m_CameraMovementView.SetModel(CameraMovementViewModel);

            // Models
            Stage = new StageModel(actorRegistry, propRegistry, "Unnamed");

            // Services
            ThumbnailsService = new ThumbnailsService(thumbnailCamera);
            m_Clipboard = new ClipboardService();
            m_PoseLibrary = new PoseLibraryService();
            m_TextToMotionService = new TextToMotionService();

            // Logic Modules
            Tutorial = new TutorialLogic(RootVisualElement);
            // Menu
            ApplicationMenuUIModel = new ApplicationMenuUIModel();
            var applicationMenuView = RootVisualElement.Q<ApplicationMenuUI>(k_MenuName);
            applicationMenuView?.SetModel(ApplicationMenuUIModel);
        }

        public AuthorContext CreateAuthoringContext()
        {
            return new AuthorContext(
                Stage,
                RootVisualElement,
                m_PosingPhysicsSolver,
                m_BakingPhysicsSolver,
                m_CameraContext,
                m_CameraMovementView,
                ThumbnailsService,
                m_Clipboard,
                m_PoseLibrary,
                ApplicationMenuUIModel,
                m_TextToMotionService
            );
        }
    }
}
