using System;
using Unity.DeepPose.Components;
using UnityEngine.UIElements;

namespace Unity.Muse.Animate
{
    class AuthorMotionToTimelineContext
    {
        const string k_UIName = "motion-to-timeline-ui";

        /// <summary>Model of the TextToMotion workflow.</summary>
        public MotionToTimelineAuthoringModel Model { get; }

        /// <summary>Baking Logic used to bake timeline data (keys) to baked timeline data.</summary>
        public BakingLogic OutputTimelineBaking { get; }
        
        /// <summary>Model of the broader Authoring workflow inside which this context exists.</summary>
        public AuthoringModel AuthoringModel { get; }

        /// <summary>A local Playback Logic for this tool, used to preview the dense motion output of the Text to Motion model </summary>
        public PlaybackModel Playback { get; }
        
        /// <summary>A repository of all the takes.</summary>
        public TakesLibraryModel TakesLibraryModel { get; }
        
        /// <summary>UI of the Motion to Timeline workflow.</summary>
        public MotionToTimelineUIModel UI { get; }
        
        /// <summary>UI used to display controls for the TextToMotion playback.</summary>
        public BakedTimelinePlaybackUIModel InputPlaybackUI => UI?.PlaybackUIModel;

        /// <summary>Visual representation of the baked animation of the final timeline.
        /// The final timeline is the result of the TextToMotion and MotionToKeys decimation.
        /// It is the final output of the whole TextToMotion workflow.</summary>
        public BakedTimelineViewLogic OutputBakedTimelineViewLogic { get; }
        
        /// <summary>Visual representation of the baked generated animation from the TextToMotion model.</summary>
        public BakedTimelineViewLogic InputBakedTimelineViewLogic { get; }

        /// <summary>The ba.</summary>
        public BakedTimelineModel InputBakedTimeline { get; }
        
        /// <summary>The baked result of the output timeline.</summary>
        public BakedTimelineModel OutputBakedTimeline { get; }
        
        /// <summary>The model of the stage (scene).</summary>
        public StageModel Stage { get; }

        /// <summary>Output timeline from the Motion To Keys decimation.</summary>
        public TimelineModel OutputTimeline { get; }

        /// <summary>Used by the MotionToKeys decimation process.</summary>
        public PoseAuthoringLogic PoseAuthoringLogic { get; }
        
        /// <summary>The MotionToKeys logic.</summary>
        public MotionToKeysSamplingLogic MotionToKeysSampling { get; }
        public ThumbnailsService ThumbnailsService { get; }
        public CameraModel Camera => CameraContext.Camera;
        public CameraMovementModel CameraMovement => CameraContext.CameraMovement;
        public CameraContext CameraContext { get; }
        public SelectionModel<EntityID> EntitySelectionModel { get; }
        public SelectionModel<LibraryItemModel> TakeSelection { get; }
        public VisualElement RootUI { get; }
        public SelectionModel<TimelineModel.SequenceKey> KeySelectionModel { get; }
        public SelectionModel<TimelineModel.SequenceTransition> TransitionSelectionModel { get; }
        
        /// <summary>The Baking Task Status UI Model (Spinning Circle)</summary>
        public BakingTaskStatusViewModel BakingTaskStatusUI { get; }
        
        /// <summary>The Baking Notice UI Model (Message Overlay)</summary>
        public BakingNoticeViewModel BakingNoticeUI { get; }

        // Motion To Timeline
        readonly BakedTimelineMappingModel OutputBakedTimelineMapping;
        
        public AuthorMotionToTimelineContext(VisualElement root,
            AuthoringModel authoringModel,
            StageModel stageModel,
            PoseAuthoringLogic poseAuthoring,
            PhysicsSolverComponent physicsSolver,
            SelectionModel<LibraryItemModel> takeSelection,
            SelectionModel<EntityID> entitySelectionModel,
            CameraContext cameraContext,
            TakesLibraryModel takesLibraryModel,
            BakingTaskStatusViewModel bakingTaskStatusUI,
            BakingNoticeViewModel bakingNoticeUI,
            ThumbnailsService thumbnailsService,
            ClipboardService clipboardService)
        {
            RootUI = root;
            CameraContext = cameraContext;
            AuthoringModel = authoringModel;
            Stage = stageModel;
            TakeSelection = takeSelection;
            TakesLibraryModel = takesLibraryModel;
            ThumbnailsService = thumbnailsService;
            Model = new MotionToTimelineAuthoringModel();
            Playback = new PlaybackModel(0f, ApplicationConstants.FramesPerSecond);
            EntitySelectionModel = entitySelectionModel;
            KeySelectionModel = new SelectionModel<TimelineModel.SequenceKey>();
            TransitionSelectionModel = new SelectionModel<TimelineModel.SequenceTransition>();
            InputBakedTimeline = new BakedTimelineModel();
            OutputTimeline = new TimelineModel();
            OutputBakedTimeline = new BakedTimelineModel();
            PoseAuthoringLogic = poseAuthoring;
            MotionToKeysSampling = new MotionToKeysSamplingLogic(Stage, PoseAuthoringLogic, InputBakedTimeline, OutputTimeline);
            InputBakedTimelineViewLogic = new BakedTimelineViewLogic("M2T Input", InputBakedTimeline, EntitySelectionModel);

            TimelineBakerBase mttBaker = ApplicationConstants.UseMotionCloudInference &&
                ApplicationConstants.MotionSynthesisEnabled
                    ? new TimelineBakerCloud(physicsSolver)
                    : new TimelineBakerAutoRegressive(physicsSolver);
            
            OutputBakedTimelineMapping = new BakedTimelineMappingModel();
            OutputTimelineBaking = new BakingLogic(OutputTimeline, OutputBakedTimeline, OutputBakedTimelineMapping, mttBaker);
            OutputBakedTimelineViewLogic = new BakedTimelineViewLogic("M2T Output", OutputBakedTimeline, EntitySelectionModel);
            
            // Motion to Timeline UI
            var motionToTimelineTimelineUIModel = new TimelineViewModel(authoringModel, OutputTimeline, Playback, OutputTimelineBaking, OutputBakedTimelineMapping, KeySelectionModel, TransitionSelectionModel, clipboardService, null);
            motionToTimelineTimelineUIModel.IsReadOnly = true;
            UI = new MotionToTimelineUIModel(Model, motionToTimelineTimelineUIModel);
            
            var ui = root.Q<MotionToTimelineUI>(k_UIName);
            ui.SetModel(UI);
            
            // Playback & UI
            InputPlaybackUI.SetPlaybackModel(Playback);

            // Baking Task Status
            BakingTaskStatusUI = bakingTaskStatusUI;
            
            // Baking Notice
            BakingNoticeUI = bakingNoticeUI;
        }
    }
}
