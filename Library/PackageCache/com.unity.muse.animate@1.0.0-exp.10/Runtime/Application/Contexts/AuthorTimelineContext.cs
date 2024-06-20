using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.Animate
{
    class AuthorTimelineContext
    {
        const string k_TimelineViewName = "timeline-view";
        const string k_TimelineOverlayUIName = "timeline-authoring-overlay";

        public StageModel Stage { get; }
        public PlaybackModel Playback { get; }
        public AuthoringModel AuthoringModel { get; }
        public SelectionModel<EntityID> EntitySelection { get; }
        public SelectionModel<TimelineModel.SequenceKey> KeySelection { get; }
        public SelectionModel<TimelineModel.SequenceTransition> TransitionSelection { get; }
        public SelectionModel<LibraryItemModel> TakeSelection { get; }
        public PoseAuthoringLogic PoseAuthoringLogic { get; }
        public BakingLogic TimelineBakingLogic { get; }
        public BakedTimelineViewLogic BakedTimelineViewLogic { get; }
        public LoopAuthoringLogic LoopAuthoringLogic { get; }
        public ThumbnailsService ThumbnailsService { get; }
        public ClipboardService Clipboard { get; }
        public PoseLibraryService PoseLibrary { get; }
        public AuthorTimelinePreviewContext PreviewContext { get; }
        public AuthorTimelineKeyPoseContext KeyPoseContext { get; }
        public AuthorTimelineKeyLoopContext KeyLoopContext { get; }
        public AuthorTimelineTransitionContext TransitionContext { get; }
        public TimelineViewModel TimelineUI { get; }
        public TimelineAuthoringOverlayUIModel Overlay { get; }
        public SelectedEntitiesToolbarViewModel SelectedEntitiesToolbar { get; }
        public AddEntitiesToolbarViewModel AddEntitiesToolbarViewModel { get; }
        public InspectorsPanelViewModel InspectorsPanelViewModel { get; }
        public VisualElement RootUI { get; }
        public BakingNoticeViewModel BakingNoticeUI { get; }
        public BakingTaskStatusViewModel BakingTaskStatusUI { get; }
        public UndoRedoToolbarView UndoRedoToolbar { get; }
        public TimelineModel Timeline => Stage.Timeline;
        public BakedTimelineModel BakedTimeline => Stage.BakedTimeline;
        public BakedTimelineMappingModel BakedTimelineMapping => Stage.BakedTimelineMapping;
        public CameraModel Camera => m_CameraContext.Camera;
        public CameraMovementModel CameraMovement => m_CameraContext.CameraMovement;
        
        CameraContext m_CameraContext;

        public AuthorTimelineContext(
            StageModel stageModel,
            AuthoringModel authoringModel,
            VisualElement rootVisualElement,
            SelectionModel<EntityID> selectionModel,
            BakingTaskStatusViewModel bakingTaskStatusUI,
            BakingNoticeViewModel bakingNoticeUI,
            TakesUIModel takesUIModel,
            PoseAuthoringLogic poseAuthoring,
            BakingLogic timelineBaking,
            CameraContext cameraContext,
            ThumbnailsService thumbnailsService,
            ClipboardService clipboardService,
            PoseLibraryService poseLibraryService,
            InspectorsPanelViewModel inspectorsPanel)
        {
            // Root VisualElement
            RootUI = rootVisualElement;

            // Libraries

            // Selection Models
            EntitySelection = selectionModel;
            KeySelection = new SelectionModel<TimelineModel.SequenceKey>();
            TransitionSelection = new SelectionModel<TimelineModel.SequenceTransition>();
            TakeSelection = new SelectionModel<LibraryItemModel>();

            // Services
            ThumbnailsService = thumbnailsService;
            Clipboard = clipboardService;
            PoseLibrary = poseLibraryService;

            // Core Models
            Stage = stageModel;
            AuthoringModel = authoringModel;
            Playback = new PlaybackModel(0f, ApplicationConstants.FramesPerSecond)
            {
                MaxFrame = Stage.BakedTimeline.FramesCount - 1
            };

            // UI Models used by other UI Models
            InspectorsPanelViewModel = inspectorsPanel;
            
            // Toolbars
            UndoRedoToolbar = RootUI.Q<UndoRedoToolbarView>(UndoRedoToolbarView.defaultName);
            
            // Pose Authoring
            PoseAuthoringLogic = poseAuthoring;

            // Timeline Baking
            TimelineBakingLogic = timelineBaking;

            //----------------------------
            // UI Models

            TimelineUI = new TimelineViewModel(
                AuthoringModel, 
                Stage.Timeline, 
                Playback, 
                TimelineBakingLogic, 
                Stage.BakedTimelineMapping, 
                KeySelection, 
                TransitionSelection, 
                Clipboard, 
                InspectorsPanelViewModel);

            Overlay = new TimelineAuthoringOverlayUIModel();
            
            BakingTaskStatusUI = bakingTaskStatusUI;
            BakingNoticeUI = bakingNoticeUI;
            BakingTaskStatusUI.TrackBakingLogics(TimelineBakingLogic);
            
            BakedTimelineViewLogic = new BakedTimelineViewLogic("Timeline - Baked Timeline View", Stage.BakedTimeline, EntitySelection);
            SelectedEntitiesToolbar = new SelectedEntitiesToolbarViewModel(AuthoringModel);
            AddEntitiesToolbarViewModel = new AddEntitiesToolbarViewModel(Stage);

            //----------------------------
            // UI Components
            
            // Timeline UI
            var timelineView = RootUI.Q<TimelineView>(k_TimelineViewName);
            timelineView.SetModel(TimelineUI);
            
            var timelineOverlayUI = RootUI.Q<TimelineAuthoringOverlayUI>(k_TimelineOverlayUIName);
            timelineOverlayUI.SetModel(Overlay);

            // Selected Entities Toolbar UI
            var selectedEntitiesToolbarView = RootUI.Q<SelectedEntitiesToolbarView>(SelectedEntitiesToolbarView.defaultName);
            selectedEntitiesToolbarView.SetModel(SelectedEntitiesToolbar);

            // Add Entities Toolbar UI
            var addEntitiesToolbarView = RootUI.Q<AddEntitiesToolbarView>(AddEntitiesToolbarView.defaultName);
            addEntitiesToolbarView.SetModel(AddEntitiesToolbarViewModel);
            
            //----------------------------
            // Logic Models
            LoopAuthoringLogic = new LoopAuthoringLogic(EntitySelection, Stage.BakedTimeline, KeySelection);

            // Child Contexts
            m_CameraContext = cameraContext;

            TransitionContext = new AuthorTimelineTransitionContext(AuthoringModel, EntitySelection, Playback, BakedTimelineViewLogic, m_CameraContext);
            KeyPoseContext = new AuthorTimelineKeyPoseContext(rootVisualElement, Stage, AuthoringModel, takesUIModel, PoseAuthoringLogic, EntitySelection, m_CameraContext);
            KeyLoopContext = new AuthorTimelineKeyLoopContext(rootVisualElement, LoopAuthoringLogic, AuthoringModel, Stage.Timeline, KeySelection, EntitySelection, m_CameraContext);
            PreviewContext = new AuthorTimelinePreviewContext(
                AuthoringModel, 
                Stage, 
                Stage.Timeline, 
                TimelineBakingLogic, 
                Stage.BakedTimeline, 
                Stage.BakedTimelineMapping, 
                EntitySelection, 
                Playback, 
                BakedTimelineViewLogic, 
                BakingNoticeUI,
                m_CameraContext);
        }
    }
}
