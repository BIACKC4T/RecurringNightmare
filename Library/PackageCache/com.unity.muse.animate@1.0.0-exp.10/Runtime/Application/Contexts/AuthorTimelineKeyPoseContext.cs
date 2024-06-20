using UnityEngine.UIElements;

namespace Unity.Muse.Animate
{
    class AuthorTimelineKeyPoseContext
    {
        public StageModel Stage { get; }
        public AuthoringModel AuthoringModel { get; }
        public PoseAuthoringLogic PosingLogic { get; }
        public SelectionModel<EntityID> EntitySelection { get; }
        public SelectionModel<EntityID> EntityEffectorSelection => PosingLogic.EntityManipulatorSelection;
        public PosingToolbarView PosingToolbar { get; }
        public UndoRedoToolbarView UndoRedoToolbar { get; }
        public SelectedEffectorsToolbarViewModel SelectedEffectorsToolbar { get; }
        public TutorialToolbarViewModel TutorialToolbar { get; }

        public TakesUIModel TakesUI { get; }
        public CameraModel Camera => m_CameraContext.Camera;
        public CameraMovementModel CameraMovement => m_CameraContext.CameraMovement;

        readonly CameraContext m_CameraContext;

        public AuthorTimelineKeyPoseContext(VisualElement uiRoot, StageModel stageModel, AuthoringModel authoringModel, TakesUIModel takesUIModel, PoseAuthoringLogic posingLogic, SelectionModel<EntityID> entitySelection,
            CameraContext cameraContext)
        {
            Stage = stageModel;
            AuthoringModel = authoringModel;
            m_CameraContext = cameraContext;
            PosingLogic = posingLogic;
            EntitySelection = entitySelection;
            TakesUI = takesUIModel;
            
            // Toolbars
            PosingToolbar = uiRoot.Q<PosingToolbarView>(PosingToolbarView.defaultName);
            UndoRedoToolbar = uiRoot.Q<UndoRedoToolbarView>(UndoRedoToolbarView.defaultName);

            // Selected Effectors Toolbar
            SelectedEffectorsToolbar = new SelectedEffectorsToolbarViewModel(AuthoringModel);
            var selectedEffectorsToolbarView = uiRoot.Q<SelectedEffectorsToolbarView>(SelectedEffectorsToolbarView.defaultName);
            selectedEffectorsToolbarView.SetModel(SelectedEffectorsToolbar);

            // Toolbar
            TutorialToolbar = new TutorialToolbarViewModel();
            TutorialToolbar.OnRequestedToggleInfo += TutorialTracks.StartTutorial;

            var toolbarView = uiRoot.Q<TutorialToolbarView>(TutorialToolbarView.viewName);
            toolbarView.SetModel(TutorialToolbar);
        }
    }
}
