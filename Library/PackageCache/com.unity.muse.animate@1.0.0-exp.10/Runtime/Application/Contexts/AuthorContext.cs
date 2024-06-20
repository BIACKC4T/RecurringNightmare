using System;
using Unity.DeepPose.Components;
using UnityEngine;
using Unity.Muse.AppUI.UI;
using Unity.DeepPose.Core;
using UnityEngine.UIElements;

namespace Unity.Muse.Animate
{
    class AuthorContext
    {
        const string k_TaskStatusViewName = "task-status-view";
        const string k_NoticeViewName = "notice-view";
        const string k_SceneViewPlayAreaElementName = "scene-view-play-area";
        const string k_SceneViewTitleLabelElementName = "scene-view-title-label";
        const string k_TakeLibraryUIName = "take-library-ui";

        public CameraMovementView CameraMovementView { get; }
        public StageModel Stage { get; }
        public AuthoringModel Authoring { get; }
        public TakesLibraryModel TakesLibrary { get; }
        public TakesLibraryUIModel TakesLibraryUI => TakesUIModel.LibraryUI;
        public TakesUIModel TakesUIModel { get; }
        public TakesUI TakesUI { get; }
        public SelectionModel<LibraryItemModel> TakeSelection { get; }
        public ThumbnailsService ThumbnailsService { get; }
        public ClipboardService Clipboard { get; }
        public PoseLibraryService PoseLibrary { get; }
        public CameraModel Camera => CameraContext.Camera;
        public CameraContext CameraContext { get; }
        public ApplicationMenuUIModel ApplicationMenuUIModel { get; }
        public VisualElement RootUI { get; }
        public SceneViewPlayArea ScenePlayArea { get; }
        public Text SceneViewTitle { get; }
        public AuthorTextToMotionTakeContext TextToMotionTakeContext { get; }
        public AuthorMotionToTimelineContext MotionToTimelineContext { get; }
        public AuthorTimelineContext TimelineContext { get; }
        public TextToMotionService TextToMotionService { get; }
        public PoseAuthoringLogic PoseAuthoringLogic { get; }
        public SidePanelUIModel SidePanel { get; }
        
        // Shared selections
        // Logic models

        // UI View Models
        SelectionType m_LastSelectionType;

        // Text To Motion
        readonly BakedTimelineViewLogic m_TextToMotionOutputViewLogic;
        readonly BakedTimelineModel m_TextToMotionOutput;
        readonly MotionToTimelineAuthoringModel m_MotionToTimelineAuthoringModel;
        readonly TimelineAuthoringModel m_TimelineAuthoringModel;

        public AuthorContext(StageModel stageModel, VisualElement rootVisualElement, 
            PhysicsSolverComponent posingPhysicsSolver, PhysicsSolverComponent bakingPhysicsSolver, CameraContext cameraContext,
            CameraMovementView cameraMovementView, ThumbnailsService thumbnailsService, ClipboardService clipboardService,
            PoseLibraryService poseLibraryService, ApplicationMenuUIModel applicationMenuUIModel, TextToMotionService textToMotionService)
        {
            // Root VisualElement
            RootUI = rootVisualElement;

            // Libraries
            TakesLibrary = stageModel.TakesLibrary;

            // Selection Models
            TakeSelection = new SelectionModel<LibraryItemModel>();
            var entitySelectionModel = new SelectionModel<EntityID>();

            // Services
            ThumbnailsService = thumbnailsService;
            Clipboard = clipboardService;
            PoseLibrary = poseLibraryService;
            TextToMotionService = textToMotionService;

            // Core Models
            Stage = stageModel;
            Authoring = new AuthoringModel();

            // Pose Authoring
            // Note: PoseAuthoringLogic is in the global Authoring context because
            // MotionToKeysSampling uses PoseAuthoringLogic for pose reconstruction.
            PoseAuthoringLogic = new PoseAuthoringLogic(Authoring, posingPhysicsSolver, entitySelectionModel, cameraContext.Camera, rootVisualElement);

            // Timeline Baking (Motion Completion & Physics)
            // Note: This is in AuthorContext because the bakingLogics can only take 1 timeline, bakedTimelineMapping, bakedTimeline
            TimelineBakerBase motionCompletionBaker = ApplicationConstants.UseMotionCloudInference &&
                ApplicationConstants.MotionSynthesisEnabled
                    ? new TimelineBakerCloud(bakingPhysicsSolver)
                    : new TimelineBakerAutoRegressive(bakingPhysicsSolver);

            var timelineBakingLogic = new BakingLogic(Stage.Timeline, Stage.BakedTimeline, Stage.BakedTimelineMapping, motionCompletionBaker);

            // Camera View Interactions
            CameraMovementView = cameraMovementView;

            //----------------------------
            // UI Models

            ApplicationMenuUIModel = applicationMenuUIModel;
            var inspectorsPanelViewModel = new InspectorsPanelViewModel();
            SidePanel = new SidePanelUIModel();
            TakesUIModel = new TakesUIModel(Authoring, TakesLibrary, TakeSelection, Clipboard);
            TakesUIModel.IsVisible = true;

            var bakingTaskStatusViewModel = new BakingTaskStatusViewModel();
            var bakingNoticeViewModel = new BakingNoticeViewModel();
            
            //----------------------------
            // Child Contexts
            CameraContext = cameraContext;

            TimelineContext = new AuthorTimelineContext(
                Stage,
                Authoring,
                rootVisualElement,
                entitySelectionModel,
                bakingTaskStatusViewModel,
                bakingNoticeViewModel,
                TakesUIModel,
                PoseAuthoringLogic,
                timelineBakingLogic,
                CameraContext,
                ThumbnailsService,
                Clipboard,
                PoseLibrary,
                inspectorsPanelViewModel
            );

            TextToMotionTakeContext = new AuthorTextToMotionTakeContext(
                rootVisualElement,
                Authoring,
                TakeSelection,
                entitySelectionModel,
                CameraContext,
                TakesLibrary,
                TextToMotionService,
                TakesUIModel,
                bakingTaskStatusViewModel,
                bakingNoticeViewModel
            );

            MotionToTimelineContext = new AuthorMotionToTimelineContext(
                rootVisualElement,
                Authoring,
                Stage,
                PoseAuthoringLogic,
                bakingPhysicsSolver,
                TakeSelection,
                entitySelectionModel,
                CameraContext,
                TakesLibrary,
                bakingTaskStatusViewModel,
                bakingNoticeViewModel,
                ThumbnailsService,
                Clipboard);

            //----------------------------
            // UI Components

            // Side Panel UI
            var sidePanelView = RootUI.Q<SidePanelUI>();
            sidePanelView.SetModel(SidePanel);

            // Takes Side Panel UI
            TakesUI = new TakesUI();
            TakesUI.name = k_TakeLibraryUIName;
            TakesUI.SetModel(TakesUIModel);

            // Inspectors UI
            var inspectorsPanelView = RootUI.Q<InspectorsPanelView>();
            inspectorsPanelView.SetModel(inspectorsPanelViewModel);

            // Task Status UI
            var taskStatusView = RootUI.Q<BakingTaskStatusView>(k_TaskStatusViewName);
            taskStatusView.SetModel(bakingTaskStatusViewModel);

            // Notice UI
            var noticeView = RootUI.Q<BakingNoticeView>(k_NoticeViewName);
            noticeView.SetModel(bakingNoticeViewModel);

            // Application Menu UI
            ApplicationMenuUIModel = applicationMenuUIModel;

            // Misc UI Elements
            ScenePlayArea = RootUI.Q<SceneViewPlayArea>(k_SceneViewPlayAreaElementName);
            ScenePlayArea.SetContext(this);

            SceneViewTitle = RootUI.Q<Text>(k_SceneViewTitleLabelElementName);
            SceneViewTitle.style.display = DisplayStyle.Flex;
        }

        public bool TryGetPoseCopyHumanoidAnimator(out Animator animator)
        {
            animator = null;
            if (TimelineContext.EntitySelection.Count != 1)
                return false;

            var entityID = TimelineContext.EntitySelection.GetSelection(0);

            var armature = GetCurrentViewArmature(entityID);
            if (armature == null)
                return false;

            return armature.gameObject.TryGetHumanoidAnimator(out animator);
        }

        public ArmatureMappingComponent GetCurrentViewArmature(EntityID entityID)
        {
            switch (Authoring.Mode)
            {
                case AuthoringModel.AuthoringMode.Unknown:
                    return null;

                case AuthoringModel.AuthoringMode.Timeline:
                    switch (Authoring.Timeline.Mode)
                    {
                        case TimelineAuthoringModel.AuthoringMode.Unknown:
                            return null;

                        case TimelineAuthoringModel.AuthoringMode.Preview:
                            return TimelineContext.BakedTimelineViewLogic.GetPreviewArmature(entityID);

                        case TimelineAuthoringModel.AuthoringMode.EditKey:
                            if (!TimelineContext.KeySelection.HasSelection)
                                return null;

                            var selectedKey = TimelineContext.KeySelection.GetSelection(0);

                            return selectedKey.Key.Type switch
                            {
                                KeyData.KeyType.Empty => null,
                                KeyData.KeyType.FullPose => PoseAuthoringLogic.GetViewArmature(entityID),
                                KeyData.KeyType.Loop => null,
                                _ => throw new ArgumentOutOfRangeException()
                            };

                        case TimelineAuthoringModel.AuthoringMode.EditTransition:
                            return TimelineContext.TransitionContext.BakedTimelineViewLogic.GetPreviewArmature(entityID);

                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                case AuthoringModel.AuthoringMode.TextToMotionTake:
                    return TextToMotionTakeContext.OutputBakedTimelineViewLogic.GetPreviewArmature(entityID);

                case AuthoringModel.AuthoringMode.ConvertMotionToTimeline:

                    switch (MotionToTimelineContext.Model.Step)
                    {
                        case MotionToTimelineAuthoringModel.AuthoringStep.None:
                            return null;

                        case MotionToTimelineAuthoringModel.AuthoringStep.NoPreview:
                            return MotionToTimelineContext.InputBakedTimelineViewLogic.GetPreviewArmature(entityID);

                        case MotionToTimelineAuthoringModel.AuthoringStep.PreviewIsAvailable:
                            return MotionToTimelineContext.OutputBakedTimelineViewLogic.GetPreviewArmature(entityID);

                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void RemoveTakesUI()
        {
            SidePanel.RemovePanel(SidePanelUtils.PageType.TakesLibrary, TakesUI);
        }

        /// <summary>
        /// FIXME: We need to do this cleanup because this context "owns" some UI elements. Not an ideal situation.
        /// </summary>
        public void Clear()
        {
            RemoveTakesUI();
            ScenePlayArea.Clear();
        }
    }
}
