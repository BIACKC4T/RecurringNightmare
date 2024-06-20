using System;
using System.Collections;
using System.IO;
using Hsm;
using Unity.AppUI.Core;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using Unity.Muse.Animate.Usd;
using UnityEngine.UIElements;
using Unity.Muse.AppUI.UI;
using Unity.Muse.Animate.Fbx;

namespace Unity.Muse.Animate
{
    partial class Application
    {
        internal partial class ApplicationHsm
        {
            public class Author : ApplicationState<AuthorContext>, IPointerClickHandler, IKeyDownHandler
            {
                // TODO: This is temporary. The platform will pass an argument eventually that indicates the export type
                public enum ExportType
                {
                    None,
                    FBX,
                    USD,
                    HumanoidAnimation,
                }

                ExportType m_LastExportType = ExportType.None;
                Transition m_NextTransition = Transition.None();

                MenuItemModel[] m_AppMenuItems;

                bool IsEditingKey => Context.Authoring.Mode is AuthoringModel.AuthoringMode.Timeline &&
                    Context.Authoring.Timeline.Mode is TimelineAuthoringModel.AuthoringMode.EditKey;

                public bool IsBusy
                {
                    get
                    {
                        if (Context.TimelineContext.TimelineBakingLogic.IsRunning)
                            return true;

                        if (Context.TextToMotionService.Baking.IsRunning)
                            return true;

                        if (Context.MotionToTimelineContext.OutputTimelineBaking.IsRunning)
                            return true;

                        if (Context.MotionToTimelineContext.MotionToKeysSampling.IsRunning)
                            return true;

                        return false;
                    }
                }

                public override Transition GetTransition()
                {
                    return m_NextTransition;
                }

                public override void OnEnter(object[] args)
                {
                    base.OnEnter(args);

                    // Open / Show the Authoring UI Elements
                    UpdateSceneViewCamera();
                    UpdateTitle();

                    // Add the Takes UI to the Side Panel
                    Context.SidePanel.AddPanel(SidePanelUtils.PageType.TakesLibrary, Context.TakesUI);

                    // Register to models
                    RegisterEvents();

                    // Add the entities instances
                    AddEntitiesInstances();
                    
                    // Hide the posing controls by default
                    Context.PoseAuthoringLogic.IsVisible = false;
                    // Force an update since the value might not have changed
                    Context.PoseAuthoringLogic.ForceUpdateAllViews();
                    
                    Context.TimelineContext.BakedTimelineViewLogic.IsVisible = false;

                    // Reinitialize the undo stack
                    UndoRedoLogic.Instance.Reset();
                    UndoRedoLogic.Instance.SetInitialCheckpoint();

                    SetupAppMenu();
                    
                    Instance.PublishMessage(new AuthoringStartedMessage());
                }

                public override void OnExit()
                {
                    base.OnExit();

                    UnregisterEvents();
                    RemoveEntitiesInstances();

                    // Remove the Takes UI to the Side Panel
                    Context.SidePanel.RemovePanel(SidePanelUtils.PageType.TakesLibrary, Context.TakesUI);

                    Context.TextToMotionService.Stop();
                    Context.ApplicationMenuUIModel.RemoveItems(m_AppMenuItems);
                    Context.Clear();
                    Instance.PublishMessage(new AuthoringEndedMessage());
                }

                public override void Update(float aDeltaTime)
                {
                    base.Update(aDeltaTime);
                    Context.ScenePlayArea.Update();
                    UpdateStateTransition();
                }
                
                public override void LateUpdate(float aDeltaTime)
                {
                    base.LateUpdate(aDeltaTime);
                    Context.ScenePlayArea.LateUpdate();
                }

                /// <summary>
                /// Update the various baking logics of this state.
                /// </summary>
                /// <param name="delta"></param>
                /// <returns>Returns true if is interrupting further bakes.</returns>
                public override bool UpdateBaking(float delta)
                {
                    Context.TextToMotionService.Update(delta, true);

                    // Do not bake timeline if the posing solver is still solving a pose
                    // Note: This is necessary because the posing is done over multiple updates/frames.
                    if (Context.PoseAuthoringLogic.IsSolving)
                        return true;

                    // Do not bake when holding / dragging mouse (For a smoother experience)
                    if (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2))
                        return true;

                    // Note: I still do baking of the timeline at this level because
                    // I think it is used outside of the timeline editing context
                    if (Context.TimelineContext.TimelineBakingLogic.NeedToUpdate)
                    {
                        Context.TimelineContext.TimelineBakingLogic.Update(delta, false);
                    }

                    if (Context.ThumbnailsService.HasRequests)
                    {
                        Context.ThumbnailsService.Update();
                    }

                    return false;
                }

                void SetupAppMenu()
                {
                    m_AppMenuItems = new MenuItemModel[]
                    {
                        new("Pose",
                            action: null,
                            isEnabledCallback: () => IsEditingKey,
                            sectionName: "--Keyframe Authoring", 
                            index: 0),
                        new("Pose/Copy",
                            action: DoCopyPose,
                            shortcutText: PlatformUtils.GetCommandLabel("C"),
                            isEnabledCallback: CanCopyPose),
                        new("Pose/Paste",
                            action: DoPastePose,
                            shortcutText: PlatformUtils.GetCommandLabel("V"),
                            isEnabledCallback: CanPastePose),
                        new("Pose/Reset",
                            action: DoResetPose,
                            shortcutText: PlatformUtils.GetCommandLabel("R"),
                            isEnabledCallback: CanCopyPose),
                        new("Export", 
                            null, 
                            isEnabledCallback: () => ApplicationConstants.IsUSDExportEnabled || ApplicationConstants.IsFBXExportEnabled),
                        new(
                            "Export/Export to USD",
                            () =>
                            {
                                StartExport(ExportType.USD);
                            },
                            isEnabledCallback: () => ApplicationConstants.IsUSDExportEnabled),
                        new("Export/Export to FBX",
                            () =>
                            {
                                StartExport(ExportType.FBX);
                            },
                            isEnabledCallback: () => ApplicationConstants.IsFBXExportEnabled),
                        new("Key",
                            action: null,
                            isEnabledCallback: () => IsEditingKey,
                            sectionName: "--Keyframe Authoring", 
                            index: 1),
                        new("Key/Move Left",
                            action: () => DoMoveKey(true),
                            isEnabledCallback: () => CanMoveKey(true),
                            sectionName: "--Move"),
                        new("Key/Move Right",
                            action: () => DoMoveKey(),
                            isEnabledCallback: () => CanMoveKey(),
                            sectionName: "--Move"),
                        new("Key/Copy",
                            action: DoCopyKey,
                            shortcutText: PlatformUtils.GetCommandLabel("C"),
                            sectionName: "--CopyPaste"),
                        new("Key/Paste (Replace)",
                            action: DoPasteReplaceKey,
                            shortcutText: PlatformUtils.GetCommandLabel("V"),
                            isEnabledCallback: CanPasteKey,
                            sectionName: "--CopyPaste"),
                        new("Key/Paste Left",
                            action: () => DoPasteKey(true),
                            isEnabledCallback: CanPasteKey,
                            sectionName: "--CopyPaste"),
                        new("Key/Paste Right",
                            action: () => DoPasteKey(),
                            isEnabledCallback: CanPasteKey,
                            sectionName: "--CopyPaste"),
                        new("Key/Duplicate Left",
                            action: () => DoDuplicateKey(true),
                            sectionName: "--Duplicate"),
                        new("Key/Duplicate Right",
                            action: () => DoDuplicateKey(),
                            shortcutText: PlatformUtils.GetCommandLabel("D"),
                            sectionName: "--Duplicate"),
                        new("Key/Delete",
                            action: DoDeleteKey,
                            shortcutText: "Delete",
                            isEnabledCallback: CanDeleteKey,
                            sectionName: "--Delete")
                    };

                    Context.ApplicationMenuUIModel.AddItems(m_AppMenuItems);
                }

                void OnSceneViewChanged(GeometryChangedEvent evt)
                {
                    UpdateSceneViewCamera();
                }

                void UpdateSceneViewCamera()
                {
                    // Not sure why the following code is disabled.
#if false
                    var rootBounds = Context.ScenePlayArea.panel.visualTree.worldBound;
                    var panelBounds = Context.ScenePlayArea.worldBound;

                    var diffX = (rootBounds.width - panelBounds.width) / rootBounds.width;

                    var centerY = panelBounds.yMin + panelBounds.height / 2f;
                    var diffY = (rootBounds.height / 2f - centerY) / rootBounds.height;

                    Context.CameraContext.SetViewportOffset(new Vector2(diffX, diffY));
#endif
                }

                void RegisterEvents()
                {
                    // Items Libraries Requests
                    Context.Authoring.OnRequestedDeleteSelectedLibraryItems += OnRequestedDeleteSelectedLibraryItems;
                    Context.Authoring.OnRequestedSelectLibraryItem += OnRequestedSelectLibraryItem;
                    Context.Authoring.OnRequestedScrollToLibraryItem += OnRequestedScrollToLibraryItem;
                    Context.Authoring.OnRequestedEditLibraryItem += OnRequestedEditLibraryItem;
                    Context.Authoring.OnRequestedExportLibraryItem += OnRequestedExportLibraryItem;
                    Context.Authoring.OnRequestedDeleteLibraryItem += OnRequestedDeleteLibraryItem;
                    Context.Authoring.OnRequestedDuplicateLibraryItem += OnRequestedDuplicateLibraryItem;

                    // Thumbnail Requests
                    Context.Authoring.OnRequestedGenerateKeyThumbnail += RequestKeyThumbnail;
                    Context.Authoring.OnRequestedGenerateFrameThumbnail += RequestFrameThumbnail;

                    // Text to Motion Requests
                    Context.Authoring.OnRequestedTextToMotionSolve += OnRequestedTextToMotionSolve;
                    Context.Authoring.OnRequestedTextToMotionGenerate += OnRequestedTextToMotionGenerate;
                    Context.TextToMotionTakeContext.BakingTaskStatusUI.TrackBakingLogics(Context.TextToMotionService.Baking);

                    // Motion to Timeline Requests
                    Context.Authoring.OnRequestedConvertMotionToKeys += OnRequestedConvertMotionToKeys;
                    Context.Authoring.OnRequestedMotionToTimelineSolve += OnRequestedMotionToTimelineSolve;

                    // Takes UI Requests
                    Context.TakesUIModel.OnRequestedGenerate += OnTakesRequestedGenerate;
                    Context.TakesUIModel.LibraryUI.OnRequestedDeleteItem += OnTakesLibraryRequestedDeleteItem;
                    Context.TakesUIModel.LibraryUI.OnRequestedEditItem += OnTakesLibraryRequestedEditItem;
                    Context.TakesUIModel.LibraryUI.OnRequestedDuplicateItem += OnTakesLibraryRequestedDuplicateItem;
                    Context.TakesUIModel.LibraryUI.OnRequestedSelectItem += OnTakesLibraryRequestedSelectItem;
                    Context.TakesUIModel.LibraryUI.OnRequestedExportItem += OnTakesLibraryRequestedExportItem;
                    Context.TakesUIModel.LibraryUI.OnRequestedDeleteSelectedItems += OnTakesLibraryRequestedDeleteSelectedItems;
                    Context.TakesUIModel.LibraryUI.OnRequestedSetPrompt += OnRequestedSetPrompt;

                    // UI Events
                    Context.ScenePlayArea.RegisterCallback<GeometryChangedEvent>(OnSceneViewChanged);

                    // Authoring Events
                    Context.Authoring.OnModeChanged += OnAuthoringModeChanged;
                    Context.Authoring.OnTitleChanged += OnAuthoringTitleChanged;
                    Context.Authoring.Timeline.OnRequestSaveTimeline += OnRequestSaveTimeline;

                    // Take Library Events
                    Context.TakeSelection.OnSelectionChanged += OnTakeSelectionChanged;
                    Context.TakesLibrary.OnTakeChanged += OnTakeChanged;

                    // Stage Entities Events
                    Context.Stage.OnActorAdded += OnStageActorAdded;
                    Context.Stage.OnActorRemoved += OnStageActorRemoved;
                    Context.Stage.OnPropAdded += OnStagePropAdded;
                    Context.Stage.OnPropRemoved += OnStagePropRemoved;
                    
                    // Service events
                    Context.TextToMotionService.OnStateChanged += OnTextToMotionServiceStateChanged;
                    Context.TextToMotionService.OnRequestFailed += OnTextToMotionServiceRequestFailed;
                }

                void UnregisterEvents()
                {
                    // Item Libraries Requests
                    Context.Authoring.OnRequestedDeleteSelectedLibraryItems -= OnRequestedDeleteSelectedLibraryItems;
                    Context.Authoring.OnRequestedSelectLibraryItem -= OnRequestedSelectLibraryItem;
                    Context.Authoring.OnRequestedScrollToLibraryItem -= OnRequestedScrollToLibraryItem;
                    Context.Authoring.OnRequestedEditLibraryItem -= OnRequestedEditLibraryItem;
                    Context.Authoring.OnRequestedExportLibraryItem -= OnRequestedExportLibraryItem;
                    Context.Authoring.OnRequestedDeleteLibraryItem -= OnRequestedDeleteLibraryItem;
                    Context.Authoring.OnRequestedDuplicateLibraryItem -= OnRequestedDuplicateLibraryItem;

                    // Thumbnail Requests
                    Context.Authoring.OnRequestedGenerateKeyThumbnail -= RequestKeyThumbnail;
                    Context.Authoring.OnRequestedGenerateFrameThumbnail -= RequestFrameThumbnail;

                    // Text to Motion Requests
                    Context.Authoring.OnRequestedTextToMotionSolve -= OnRequestedTextToMotionSolve;
                    Context.Authoring.OnRequestedTextToMotionGenerate -= OnRequestedTextToMotionGenerate;
                    Context.TextToMotionTakeContext.BakingTaskStatusUI.UntrackBakingLogics(Context.TextToMotionService.Baking);

                    // Motion to Timeline Requests
                    Context.Authoring.OnRequestedMotionToTimelineSolve -= OnRequestedMotionToTimelineSolve;

                    // Takes UI Requests
                    Context.TakesUIModel.OnRequestedGenerate -= OnTakesRequestedGenerate;
                    Context.TakesUIModel.LibraryUI.OnRequestedDeleteItem -= OnTakesLibraryRequestedDeleteItem;
                    Context.TakesUIModel.LibraryUI.OnRequestedEditItem -= OnTakesLibraryRequestedEditItem;
                    Context.TakesUIModel.LibraryUI.OnRequestedDuplicateItem -= OnTakesLibraryRequestedDuplicateItem;
                    Context.TakesUIModel.LibraryUI.OnRequestedSelectItem -= OnTakesLibraryRequestedSelectItem;
                    Context.TakesUIModel.LibraryUI.OnRequestedExportItem -= OnTakesLibraryRequestedExportItem;
                    Context.TakesUIModel.LibraryUI.OnRequestedDeleteSelectedItems -= OnTakesLibraryRequestedDeleteSelectedItems;

                    // UI Events
                    Context.ScenePlayArea.UnregisterCallback<GeometryChangedEvent>(OnSceneViewChanged);

                    // Authoring Events
                    Context.Authoring.OnModeChanged -= OnAuthoringModeChanged;
                    Context.Authoring.OnTitleChanged -= OnAuthoringTitleChanged;
                    Context.Authoring.Timeline.OnRequestSaveTimeline -= OnRequestSaveTimeline;

                    // Stage Entities Events
                    Context.Stage.OnActorAdded -= OnStageActorAdded;
                    Context.Stage.OnActorRemoved -= OnStageActorRemoved;
                    Context.Stage.OnPropAdded -= OnStagePropAdded;
                    Context.Stage.OnPropRemoved -= OnStagePropRemoved;
                }

                void AddEntitiesInstances()
                {
                    for (var i = 0; i < Context.Stage.NumActors; i++)
                    {
                        var actorModel = Context.Stage.GetActorModel(i);
                        var actorComponent = Context.Stage.GetActorInstance(actorModel.ID);

                        AddEntity(actorModel.EntityID,
                            actorComponent.ReferencePosingArmature,
                            actorComponent.ReferenceViewArmature,
                            actorComponent.ReferencePhysicsArmature,
                            actorComponent.ReferenceMotionArmature,
                            actorComponent.ReferenceTextToMotionArmature,
                            actorComponent.PosingToCharacterArmatureMapping,
                            PhysicsEntityType.Active,
                            actorComponent.EvaluationJointMask);
                    }

                    for (var i = 0; i < Context.Stage.NumProps; i++)
                    {
                        var propModel = Context.Stage.GetPropModel(i);
                        var propComponent = Context.Stage.GetPropInstance(propModel.ID);

                        AddEntity(propModel.EntityID,
                            propComponent.ReferencePosingArmature,
                            propComponent.ReferenceViewArmature,
                            propComponent.ReferencePhysicsArmature,
                            null,
                            null,
                            null,
                            PhysicsEntityType.Kinematic);
                    }
                }

                void RemoveEntitiesInstances()
                {
                    using var toRemoveList = TempList<EntityID>.Allocate();
                    for (var i = 0; i < Context.Stage.NumActors; i++)
                    {
                        var actorModel = Context.Stage.GetActorModel(i);
                        toRemoveList.Add(actorModel.EntityID);
                    }

                    for (var i = 0; i < Context.Stage.NumProps; i++)
                    {
                        var propModel = Context.Stage.GetPropModel(i);
                        toRemoveList.Add(propModel.EntityID);
                    }

                    foreach (var entityID in toRemoveList)
                    {
                        RemoveEntity(entityID);
                    }
                }

                void StartConvertMotionToKeys(TextToMotionTake source)
                {
                    source.BakedTimelineModel.CopyTo(Context.MotionToTimelineContext.InputBakedTimeline);
                    Context.Authoring.Mode = AuthoringModel.AuthoringMode.ConvertMotionToTimeline;
                    Context.MotionToTimelineContext.Model.Step = MotionToTimelineAuthoringModel.AuthoringStep.NoPreview;
                }

                void EnableUndoRedoForKey(KeyModel keyModel)
                {
                    using var myEntityIDs = TempHashSet<EntityID>.Allocate();
                    keyModel.GetAllEntities(myEntityIDs.Set);
                    foreach (var entityID in myEntityIDs)
                    {
                        if (!keyModel.TryGetKey(entityID, out var keyframeModel))
                        {
                            // This should never return false
                            AssertUtils.Fail($"Can't retrieve KeyframeModel for entity {entityID}");
                        }

                        var entityIdCopy = entityID;
                        UndoRedoLogic.Instance.TrackModel(keyframeModel, entityKeyModel =>
                        {
                            Context.PoseAuthoringLogic.RestoreKey(entityIdCopy, entityKeyModel);
                        });
                    }
                }

                void StartEditing(LibraryItemModel item)
                {
                    if (item is TakeModel takeModel)
                    {
                        StartEditing(takeModel);
                    }
                }

                void StartEditing(TakeModel take)
                {
                    // Select the take
                    Context.Authoring.RequestSelectLibraryItem(Context.TakesLibrary, Context.TakeSelection, take);

                    // Start editing the take
                    if (take is KeySequenceTake keyedTake)
                    {
                        StartEditing(keyedTake);
                    }
                    else if (take is TextToMotionTake motionTake)
                    {
                        StartEditing(motionTake);
                    }
                }

                void StartEditing(KeySequenceTake keyedTake)
                {
                    // Copy the data from the KeySequenceTake to the main authoring/editing timeline
                    keyedTake.TimelineModel.CopyTo(Context.TimelineContext.Timeline);

                    // Stop the playback
                    Context.TimelineContext.Playback.CurrentFrame = 0;
                    Context.TimelineContext.Playback.Stop();

                    // Start editing it
                    if (Context.Authoring.Mode == AuthoringModel.AuthoringMode.Timeline)
                    {
                        if (Context.TimelineContext.Timeline.KeyCount > 0)
                            Context.Authoring.Timeline.RequestEditKeyIndex(0);
                    }
                    else
                    {
                        Context.Authoring.Mode = AuthoringModel.AuthoringMode.Timeline;
                    }
                }

                void StartEditing(TextToMotionTake motionTake)
                {
                    if (motionTake == null)
                    {
                        Context.TextToMotionTakeContext.OutputBakedTimelineViewLogic.IsVisible = false;
                        Context.TextToMotionTakeContext.Playback.MaxFrame = 0;
                        
                        return;
                    }

                    // Select the take
                    Context.Authoring.RequestSelectLibraryItem(Context.TakesLibrary, Context.TakeSelection, motionTake);
                    
                    Context.TextToMotionTakeContext.OutputBakedTimelineViewLogic.IsVisible = true;
                    Context.TextToMotionTakeContext.Model.Target = motionTake;

                    motionTake.BakedTimelineModel.CopyTo(Context.TextToMotionTakeContext.OutputBakedTimeline);
                    Context.TextToMotionTakeContext.Playback.MaxFrame = Context.TextToMotionTakeContext.OutputBakedTimeline.FramesCount - 1;
                    Context.TextToMotionTakeContext.Playback.Play(true);

                    Context.Authoring.Mode = AuthoringModel.AuthoringMode.TextToMotionTake;
                }

                void StopEditingTextToMotionTake()
                {
                    if (Context.Authoring.Mode != AuthoringModel.AuthoringMode.TextToMotionTake)
                        return;

                    Context.TextToMotionTakeContext.OutputBakedTimelineViewLogic.IsVisible = false;
                    Context.TextToMotionTakeContext.Playback.MaxFrame = 0;
                    Context.TextToMotionTakeContext.Model.Target = null;
                }

                void UpdateTitle()
                {
                    Context.SceneViewTitle.text = Context.Authoring.Title;
                }

                void AddEntity(EntityID entityID,
                    ArmatureMappingComponent referencePosingArmature,
                    ArmatureMappingComponent referenceViewArmature,
                    ArmatureMappingComponent referencePhysicsArmature,
                    ArmatureMappingComponent referenceMotionArmature,
                    ArmatureMappingComponent referenceTextToMotionArmature,
                    ArmatureToArmatureMapping posingToEntityArmatureMapping,
                    PhysicsEntityType physicsEntityType,
                    JointMask jointMask = null)
                {
                    Context.TimelineContext.PoseAuthoringLogic.AddEntity(entityID,
                        referencePosingArmature,
                        referenceViewArmature,
                        referencePhysicsArmature,
                        posingToEntityArmatureMapping,
                        DoResetPose,
                        DoCopyPose,
                        DoPastePose,
                        CanPastePose,
                        jointMask);

                    Context.TimelineContext.TimelineBakingLogic.AddEntity(entityID, referencePhysicsArmature, referenceMotionArmature, physicsEntityType);
                    Context.TimelineContext.BakedTimelineViewLogic.AddEntity(entityID, referenceViewArmature, ApplicationLayers.LayerBaking);
                    Context.TimelineContext.LoopAuthoringLogic.AddEntity(entityID, referenceViewArmature);
                    Context.ThumbnailsService.AddEntity(entityID, referenceViewArmature);

                    Context.TimelineContext.PoseAuthoringLogic.SnapPhysicsToPosing(entityID);

                    if (Context.PoseAuthoringLogic.TryGetPosingLogic(entityID, out var entityPosingLogic) && entityPosingLogic.PosingModel != null)
                        Context.TimelineContext.Timeline.AddEntity(entityID, entityPosingLogic.PosingModel, referencePhysicsArmature.NumJoints);

                    // Text to Motion
                    Context.TextToMotionService.AddEntity(entityID,
                        null,
                        referenceTextToMotionArmature,
                        PhysicsEntityType.Active);

                    Context.TextToMotionTakeContext.OutputBakedTimelineViewLogic.AddEntity(entityID, referenceViewArmature, ApplicationLayers.LayerBaking);

                    // Motion to Timeline
                    Context.MotionToTimelineContext.OutputTimelineBaking.AddEntity(entityID, referencePhysicsArmature, referenceMotionArmature, physicsEntityType);
                    Context.MotionToTimelineContext.InputBakedTimelineViewLogic.AddEntity(entityID, referenceViewArmature, ApplicationLayers.LayerBaking);
                    Context.MotionToTimelineContext.OutputBakedTimelineViewLogic.AddEntity(entityID, referenceViewArmature, ApplicationLayers.LayerBaking);
                    Context.MotionToTimelineContext.MotionToKeysSampling.AddEntity(entityID, jointMask, referenceMotionArmature);

                    // Select newly added actor, alone
                    Context.TimelineContext.EntitySelection.Clear();
                    Context.TimelineContext.EntitySelection.Select(entityID);

                    // Capture default pose for Reset Pose feature
                    // This is essentially a hack for 26th June Release
                    // TODO: serialize default poses to assets instead
                    if (Context.Stage.TryGetActorModel(entityID, out var actorModel)
                        && !Context.PoseLibrary.HasDefaultPose(actorModel)
                        && entityPosingLogic != null
                        && Context.PoseAuthoringLogic.TryGetSolvedPoses(entityID, out var localPose, out var globalPose))
                    {
                        var entityKeyModel = new EntityKeyModel(entityPosingLogic.PosingModel, localPose, globalPose);
                        Context.PoseLibrary.RegisterDefaultPose(actorModel, entityKeyModel);
                    }
                }

                void RemoveEntity(EntityID entityID)
                {
                    Context.TimelineContext.EntitySelection.Unselect(entityID);

                    Context.TimelineContext.LoopAuthoringLogic.RemoveEntity(entityID);
                    Context.TimelineContext.BakedTimelineViewLogic.RemoveEntity(entityID);

                    Context.TimelineContext.TimelineBakingLogic.RemoveEntity(entityID);
                    Context.TimelineContext.PoseAuthoringLogic.RemoveEntity(entityID);
                    Context.ThumbnailsService.RemoveEntity(entityID);

                    Context.TextToMotionService.RemoveEntity(entityID);
                    Context.TextToMotionTakeContext.OutputBakedTimelineViewLogic.RemoveEntity(entityID);

                    Context.MotionToTimelineContext.OutputTimelineBaking.RemoveEntity(entityID);
                    Context.MotionToTimelineContext.InputBakedTimelineViewLogic.RemoveEntity(entityID);
                    Context.MotionToTimelineContext.OutputBakedTimelineViewLogic.RemoveEntity(entityID);
                    Context.MotionToTimelineContext.MotionToKeysSampling.RemoveEntity(entityID);
                }

                void RequestKeyThumbnail(ThumbnailModel target, KeyModel key)
                {
                    Context.ThumbnailsService.RequestThumbnail(target, key, Context.Camera.Position, Context.Camera.Rotation);
                }

                void RequestFrameThumbnail(ThumbnailModel target, BakedTimelineModel timeline, int frame)
                {
                    Context.ThumbnailsService.RequestThumbnail(target, timeline, frame, Context.Camera.Position, Context.Camera.Rotation, 3, 3, 0, 0);
                }
                /*
                                void RecoverEffectorsFromBakedTimeline(int bakedFrameIndex)
                                {
                                    var from = Context.BakedTimeline.GetFrame(bakedFrameIndex);
                
                                    for (var i = 0; i < Context.Stage.NumActors; i++)
                                    {
                                        var actorModel = Context.Stage.GetActorModel(i);
                
                                        if (from.TryGetPose(actorModel.EntityID, out var bakedPose))
                                        {
                                            // Apply the pose from the baked frame on the posing armature
                                            var posingArmature = Context.PoseAuthoringLogic.GetPosingArmature(actorModel.EntityID);
                                            bakedPose.ApplyTo(posingArmature.ArmatureMappingData);
                
                                            // We snap physics BEFORE effector recovery as we know the baked frame should have a physically-accurate pose,
                                            // EXCEPT for the first keyframe. See note below.
                                            Context.PoseAuthoringLogic.SnapPhysicsToPosing(actorModel.EntityID);
                                            Context.PoseAuthoringLogic.DoEffectorRecovery(actorModel.EntityID);
                
                                            // For the first baked frame, we need to make sure there is no ground penetration. This can
                                            // happen if we are extracting keys from a baked timeline does not obey physics (e.g.
                                            // text-to-motion).
                                            if (bakedFrameIndex == 0)
                                                Context.PoseAuthoringLogic.ResolveGroundPenetration(actorModel.EntityID);
                
                                            // We do not snap physics AFTER effector recovery as this could lead to penetration when effector recovery is imperfect.
                                            // Instead we let the physics solve try to match the recovered pose
                                        }
                                        else
                                        {
                                            Debug.Log($"Failed to locate pose in frame for entityID: {actorModel.EntityID}");
                                        }
                                    }
                                }
                                */

                bool CanCopyPose()
                {
                    return Context.TryGetPoseCopyHumanoidAnimator(out var _);
                }

                void DoDuplicateKey(bool left = false)
                {
                    if (Context.TimelineContext.KeySelection.Count != 1)
                        return;

                    var selectedKey = Context.TimelineContext.KeySelection.GetSelection(0);
                    var oldIndex = Context.TimelineContext.Timeline.IndexOf(selectedKey);

                    var toIndex = left ? oldIndex : oldIndex + 1;
                    var newKey = Context.TimelineContext.Timeline.DuplicateKey(oldIndex, toIndex);

                    Context.TimelineContext.KeySelection.Clear();
                    Context.TimelineContext.KeySelection.Select(newKey);
                }

                void DoMoveKey(bool left = false)
                {
                    if (Context.TimelineContext.KeySelection.Count != 1)
                        return;

                    var keyModel = Context.TimelineContext.KeySelection.GetSelection(0);
                    var keyIndex = Context.TimelineContext.Timeline.IndexOf(keyModel);
                    var toIndex = left ? keyIndex - 1 : keyIndex + 1;
                    Context.TimelineContext.Timeline.MoveKey(keyIndex, toIndex);
                    Context.TimelineContext.KeySelection.Clear();
                    Context.TimelineContext.KeySelection.Select(Context.TimelineContext.Timeline.GetKey(toIndex));
                }

                void DoCopyKey()
                {
                    if (Context.TimelineContext.KeySelection.Count != 1)
                        return;

                    var selectedKey = Context.TimelineContext.KeySelection.GetSelection(0);
                    Context.Clipboard.Copy(selectedKey.Key);

                    if (selectedKey.OutTransition != null)
                        Context.Clipboard.Copy(selectedKey.OutTransition.Transition);
                }

                void DoDeleteKey()
                {
                    Context.Authoring.Timeline.RequestDeleteSelectedKeys();
                }

                void DoCopyPose()
                {
                    if (Context.TimelineContext.KeySelection.Count != 1 || Context.TimelineContext.EntitySelection.Count != 1)
                        return;

                    var selectedKey = Context.TimelineContext.KeySelection.GetSelection(0);
                    var selectedEntityID = Context.TimelineContext.EntitySelection.GetSelection(0);
                    if (!selectedKey.Key.TryGetKey(selectedEntityID, out var entityKeyModel))
                        return;

                    Context.Clipboard.Copy(entityKeyModel);
                    DeepPoseAnalytics.SendActionOfInterestEvent(DeepPoseAnalytics.ActionOfInterest.CopyPose);
                }

                bool CanPastePose()
                {
                    if (Context.TimelineContext.EntitySelection.Count != 1 || Context.TimelineContext.KeySelection.Count != 1)
                        return false;

                    var selectedEntityID = Context.TimelineContext.EntitySelection.GetSelection(0);
                    var selectedKey = Context.TimelineContext.KeySelection.GetSelection(0);
                    if (!selectedKey.Key.TryGetKey(selectedEntityID, out var entityKeyModel))
                        return false;

                    var canPasteKey = Context.Clipboard.CanPaste(entityKeyModel);
                    return canPasteKey;
                }

                bool CanPasteKey()
                {
                    if (Context.TimelineContext.KeySelection.Count != 1)
                        return false;

                    var keyModel = Context.TimelineContext.KeySelection.GetSelection(0);
                    return Context.Clipboard.CanPaste(keyModel.Key);
                }

                bool CanMoveKey(bool left = false)
                {
                    if (Context.TimelineContext.KeySelection.Count != 1)
                        return false;

                    var keyModel = Context.TimelineContext.KeySelection.GetSelection(0);
                    var keyIndex = Context.TimelineContext.Timeline.IndexOf(keyModel);
                    return left ? keyIndex > 0 : keyIndex < Context.TimelineContext.Timeline.KeyCount - 1;
                }

                bool CanDeleteKey() => Context.TimelineContext.KeySelection.Count == 1 && Context.TimelineContext.Timeline.KeyCount > 1;

                void DoPasteAny()
                {
                    DoPasteReplaceKey();
                    DoPastePose();
                }

                void DoPasteReplaceKey()
                {
                    if (Context.TimelineContext.KeySelection.Count != 1)
                        return;

                    var selectedKey = Context.TimelineContext.KeySelection.GetSelection(0);
                    Context.Clipboard.Paste(selectedKey.Key);

                    if (selectedKey.OutTransition != null)
                        Context.Clipboard.Paste(selectedKey.OutTransition.Transition);

                    Context.TimelineContext.PoseAuthoringLogic.RestorePosingStateFromKey(selectedKey.Key);
                }

                void DoPasteKey(bool left = false)
                {
                    var selectedKey = Context.TimelineContext.KeySelection.GetSelection(0);
                    var keyIndex = Context.TimelineContext.Timeline.IndexOf(selectedKey);
                    var toIndex = left ? keyIndex : keyIndex + 1;

                    // Create a key to paste into (doesn't matter what it contains)
                    var newKey = Context.TimelineContext.Timeline.DuplicateKey(keyIndex, toIndex);
                    Context.Clipboard.Paste(newKey.Key);

                    Context.TimelineContext.KeySelection.Clear();
                    Context.TimelineContext.KeySelection.Select(newKey);
                }

                void DoPastePose()
                {
                    if (Context.TimelineContext.KeySelection.Count != 1 || Context.TimelineContext.EntitySelection.Count != 1)
                        return;

                    var selectedKey = Context.TimelineContext.KeySelection.GetSelection(0);
                    var selectedEntityID = Context.TimelineContext.EntitySelection.GetSelection(0);
                    if (!selectedKey.Key.TryGetKey(selectedEntityID, out var entityKeyModel))
                        return;

                    Context.Clipboard.Paste(entityKeyModel);
                    Context.TimelineContext.PoseAuthoringLogic.RestorePosingStateFromKey(selectedKey.Key);
                    UndoRedoLogic.Instance.Prime();
                    UndoRedoLogic.Instance.Push();

                    DeepPoseAnalytics.SendActionOfInterestEvent(DeepPoseAnalytics.ActionOfInterest.PastePose);
                }

                void DoResetPose()
                {
                    if (Context.TimelineContext.KeySelection.Count != 1)
                        return;

                    var selectedKey = Context.TimelineContext.KeySelection.GetSelection(0);

                    for (var i = 0; i < Context.TimelineContext.EntitySelection.Count; i++)
                    {
                        var selectedEntity = Context.TimelineContext.EntitySelection.GetSelection(i);

                        if (!Context.Stage.TryGetActorModel(selectedEntity, out var actorModel))
                            continue;

                        if (!Context.PoseLibrary.TryGetDefaultPose(actorModel, out var sourceEntityKey))
                            continue;

                        if (!selectedKey.Key.TryGetKey(selectedEntity, out var destinationEntityKey))
                            continue;

                        sourceEntityKey.CopyTo(destinationEntityKey);
                    }

                    Context.TimelineContext.PoseAuthoringLogic.RestorePosingStateFromKey(selectedKey.Key);
                    UndoRedoLogic.Instance.Prime();
                    UndoRedoLogic.Instance.Push();

                    DeepPoseAnalytics.SendActionOfInterestEvent(DeepPoseAnalytics.ActionOfInterest.ResetPose);
                }

                // Takes UI Events Handlers

                void OnTakesRequestedGenerate()
                {
                    Context.Authoring.RequestTextToMotionGenerate(Context.TakesUIModel.Prompt, Context.TakesUIModel.Seed, Context.TakesUIModel.TakesAmount, Context.TakesUIModel.Duration, Context.TakesUIModel.InferenceModel);
                }

                void OnTakesLibraryRequestedDeleteSelectedItems(LibraryModel library, SelectionModel<LibraryItemModel> selectionModel)
                {
                    Context.Authoring.RequestDeleteSelectedLibraryItems(library, selectionModel);
                }

                void OnTakesLibraryRequestedDeleteItem(LibraryModel library, LibraryItemModel item)
                {
                    Context.Authoring.RequestDeleteLibraryItem(library, item);
                }

                void OnTakesLibraryRequestedSelectItem(LibraryModel library, SelectionModel<LibraryItemModel> selectionModel, LibraryItemModel item)
                {
                    Context.Authoring.RequestSelectLibraryItem(library, selectionModel, item);
                }

                void OnTakesLibraryRequestedEditItem(LibraryModel library, LibraryItemModel item)
                {
                    Context.Authoring.RequestEditLibraryItem(library, item);
                }

                void OnTakesLibraryRequestedExportItem(LibraryModel library, LibraryItemModel item)
                {
                    Context.Authoring.RequestExportLibraryItem(library, item);
                }

                void OnTakesLibraryRequestedDuplicateItem(LibraryModel library, LibraryItemModel item)
                {
                    Context.Authoring.RequestDuplicateLibraryItem(library, item);
                }
                
                void OnRequestedTextToMotionSolve(string prompt, int? seed, float? temperature, int length, ITimelineBakerTextToMotion.Model model)
                {
                    var take = new TextToMotionTake(
                        prompt,
                        seed,
                        "T2M Take",
                        temperature,
                        length,
                        model,
                        new ThumbnailModel());

                    Context.TakesLibrary.Add(take);
                    Context.TextToMotionService.Request(take);
                }

                void OnRequestedTextToMotionGenerate(string prompt, int? seed, int takesAmount, float duration, ITimelineBakerTextToMotion.Model model)
                {
                    Context.TextToMotionTakeContext.Model.RequestPrompt = prompt;
                    Context.TextToMotionTakeContext.Model.RequestTakesCounter = 0;
                    Context.TextToMotionTakeContext.Model.RequestTakesAmount = takesAmount;
                    Context.TextToMotionTakeContext.Model.RequestDuration = duration;
                    Context.TextToMotionTakeContext.Model.RequestModel = model;

                    while (Context.TextToMotionTakeContext.Model.RequestTakesCounter <
                           Context.TextToMotionTakeContext.Model.RequestTakesAmount)
                    {
                        var numFrames = (int)(Context.TextToMotionTakeContext.Model.RequestDuration * ApplicationConstants.FramesPerSecond);
                        numFrames = Mathf.Clamp(numFrames - numFrames % 8, 16, 300);

                        // Queue the Text to Motion solve
                        Context.Authoring.RequestTextToMotionSolve(Context.TextToMotionTakeContext.Model.RequestPrompt,
                            seed + Context.TextToMotionTakeContext.Model.RequestTakesCounter,
                            null,
                            numFrames,
                            Context.TextToMotionTakeContext.Model.RequestModel
                            );
                        Context.TextToMotionTakeContext.Model.RequestTakesCounter++;
                    }
                }

                /// <summary>
                /// Perform TextToMotion generation on next frame.
                /// </summary>
                void ScheduleInitialTextToMotionGenerate(string prompt, int takesAmount, float duration, ITimelineBakerTextToMotion.Model model)
                {
                    IEnumerator GenerateAtNextFrame(string prompt, int takes)
                    {
                        yield return null;
                        OnRequestedTextToMotionGenerate(prompt, 0, takes, duration, model);
                    }

                    Instance.StartCoroutine(GenerateAtNextFrame(prompt, takesAmount));
                }

                void OnRequestedMotionToTimelineSolve(float sensitivity, bool useMotionCompletion)
                {
                    Context.TimelineContext.TimelineBakingLogic.Cancel();
                    Context.MotionToTimelineContext.OutputTimelineBaking.Cancel();
                    Context.MotionToTimelineContext.MotionToKeysSampling.Cancel();
                    Context.MotionToTimelineContext.MotionToKeysSampling.QueueBaking(sensitivity, useMotionCompletion);
                }

                void OnRequestedConvertMotionToKeys(TextToMotionTake take)
                {
                    StartConvertMotionToKeys(take);
                }
                
                void OnRequestedSetPrompt(string prompt)
                {
                    Context.TakesUIModel.Prompt = prompt;
                }

                public void OnPointerClick(PointerEventData eventData)
                {
                    if (ApplicationConstants.DebugStatesInputEvents) Log($"OnPointerClick({eventData.button})");
                }

                public void OnKeyDown(KeyPressEvent eventData)
                {
                    if (ApplicationConstants.DebugStatesInputEvents) Log($"OnKeyDown(KeyCode: {eventData.KeyCode})");
                    
                    if (Context.TakesUIModel.IsWriting)
                    {
                        eventData.Use();
                        return;
                    }

                    switch (eventData.KeyCode)
                    {
                        case KeyCode.D:
                            if (eventData.IsControlOrCommand)
                            {
                                DoDuplicateAny();
                                eventData.Use();
                            }

                            break;

                        case KeyCode.C:
                            if (eventData.IsControlOrCommand)
                            {
                                DoCopyAny();
                                eventData.Use();
                            }

                            break;

                        case KeyCode.V:
                            if (eventData.IsControlOrCommand)
                            {
                                DoPasteAny();
                                eventData.Use();
                            }

                            break;

                        case KeyCode.R:
                            if (eventData.IsControlOrCommand)
                            {
                                DoResetPose();
                                eventData.Use();
                            }

                            break;
                        case KeyCode.Y when eventData.IsControlOrCommand:
                            UndoRedoLogic.Instance.Redo();
                            eventData.Use();
                            break;
                        case KeyCode.Z when eventData.IsControlOrCommand && eventData.IsShift:
                            UndoRedoLogic.Instance.Redo();
                            eventData.Use();
                            break;
                        case KeyCode.Z when eventData.IsControlOrCommand:
                            UndoRedoLogic.Instance.Undo();
                            eventData.Use();
                            break;
                    }
                }

                void DoCopyAny()
                {
                    DoCopyKey();
                    DoCopyPose();
                }

                void DoDuplicateAny()
                {
                    DoDuplicateKey();
                }

                void OnStageActorAdded(StageModel stageModel, ActorModel actorModel, ActorDefinitionComponent actorComponent)
                {
                    AddEntity(actorModel.EntityID,
                        actorComponent.ReferencePosingArmature,
                        actorComponent.ReferenceViewArmature,
                        actorComponent.ReferencePhysicsArmature,
                        actorComponent.ReferenceMotionArmature,
                        actorComponent.ReferenceTextToMotionArmature,
                        actorComponent.PosingToCharacterArmatureMapping,
                        PhysicsEntityType.Active,
                        actorComponent.EvaluationJointMask);

                    InitializeKeyPoses(actorModel);
                }

                void InitializeKeyPoses(ActorModel actorModel)
                {
                    for (var i = 0; i < Context.TimelineContext.Timeline.KeyCount; i++)
                    {
                        var key = Context.TimelineContext.Timeline.Keys[i];
                        Context.PoseAuthoringLogic.ApplyPosingStateToKey(actorModel.EntityID, key.Key);
                    }
                }

                void OnStageActorRemoved(StageModel stageModel, ActorModel actorModel)
                {
                    RemoveEntity(actorModel.EntityID);
                }

                void OnStagePropAdded(StageModel stageModel, PropModel propModel, PropDefinitionComponent propComponent)
                {
                    AddEntity(propModel.EntityID,
                        propComponent.ReferencePosingArmature,
                        propComponent.ReferenceViewArmature,
                        propComponent.ReferencePhysicsArmature,
                        null,
                        null,
                        null,
                        PhysicsEntityType.Kinematic);
                }

                void OnStagePropRemoved(StageModel stageModel, PropModel propModel)
                {
                    RemoveEntity(propModel.EntityID);
                }

                void OnAuthoringModeChanged()
                {
                    UpdateStateTransition();
                }

                void OnAuthoringTitleChanged()
                {
                    UpdateTitle();
                }

                void OnTakeSelectionChanged(SelectionModel<LibraryItemModel> model)
                {
                    OnRequestSaveTimeline();
                    if (model.HasSelection && model.GetSelection(0) is TakeModel take)
                    {
                        Context.Authoring.ActiveTake = take;
                    }
                }

                void OnTakeChanged(TakeModel take, TakeModel.TakeProperty property)
                {
                    if (property is TakeModel.TakeProperty.Progress && take.Progress >= 1f)
                    {
                        // If we just finished solving/baking, we need a new thumbnail.
                        // We do the request in the Author state because it has access to the
                        // ThumbnailService as well as the CameraModel.
                        take.RequestThumbnailUpdate(Context.ThumbnailsService, Context.Camera);
                    }
                }

                void OnRequestedSelectLibraryItem(LibraryModel library, SelectionModel<LibraryItemModel> selectionModel, LibraryItemModel item)
                {
                    /*
                    if (IsBusy)
                        return;
                    */
                    selectionModel.Clear();
                    selectionModel.Select(item);
                }

                void OnRequestedScrollToLibraryItem(LibraryModel library, LibraryItemModel item)
                {
                    if (library == Context.TakesLibrary)
                    {
                        Context.TakesLibraryUI.RequestScrollToItem(item);
                    }
                }

                void OnRequestedEditLibraryItem(LibraryModel library, LibraryItemModel item)
                {
                    StartEditing(item);
                }

                void OnRequestedExportLibraryItem(LibraryModel library, LibraryItemModel item)
                {
                    // TODO: The default export type is FBX. How do we make this configurable?
                    // StartExport(ExportType.FBX, item);
                    StartExport(ExportType.HumanoidAnimation, item);
                }
                
                void OnRequestedDeleteLibraryItem(LibraryModel library, LibraryItemModel item)
                {
                    // TODO: rethink why we need LibraryModel, since it is not possible to
                    // actually modify LibraryModel.
                    switch (library)
                    {
                        case TakesLibraryModel takesLibrary when item is TakeModel take:
                            DeleteTakeFromLibrary(takesLibrary, take);
                            break;
                    }
                }

                void DeleteTakeFromLibrary(TakesLibraryModel takesLibrary, TakeModel take)
                {
                    // There should be only one TakeLibraryModel in the application.
                    Assert.IsTrue(takesLibrary == Context.TakesLibrary);

                    // We should select another take if the deleted take was selected,
                    // assuming there is another take to select.
                    if (Context.TakeSelection.IsSelected(take))
                    {
                        Context.TakeSelection.Unselect(take);
                        StopEditingTextToMotionTake();
                    }

                    takesLibrary.Remove(take);
                }

                void OnRequestedDuplicateLibraryItem(LibraryModel library, LibraryItemModel item) { }

                void OnRequestedDeleteSelectedLibraryItems(LibraryModel library, SelectionModel<LibraryItemModel> selectionModel) { }

                void UpdateStateTransition()
                {
                    switch (Context.Authoring.Mode)
                    {
                        case AuthoringModel.AuthoringMode.Unknown:
                            m_NextTransition = Transition.None();
                            break;

                        case AuthoringModel.AuthoringMode.Timeline:
                            m_NextTransition = Transition.Inner<AuthorTimeline>(Context.TimelineContext);
                            break;

                        case AuthoringModel.AuthoringMode.TextToMotionTake:
                            m_NextTransition = Transition.Inner<AuthorTextToMotion>(Context.TextToMotionTakeContext);
                            break;

                        case AuthoringModel.AuthoringMode.ConvertMotionToTimeline:
                            m_NextTransition = Transition.Inner<AuthorMotionToTimeline>(Context.MotionToTimelineContext);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                
                /// <summary>
                /// Initiate export animation export.
                /// </summary>
                /// <param name="exportType">The type of file to export.</param>
                /// <param name="item">The item to export. If null, then we export either the working timeline
                /// or the selected take.</param>
                /// <remarks>
                /// The app calls this method when the user requests an export. It gathers the data to be exported and
                /// triggers the platform to start the export process. The platform will call <see cref="OnExport"/>
                /// when it is ready to receive data.
                /// </remarks>
                void StartExport(ExportType exportType, LibraryItemModel item = null)
                {
                    // TODO: This will be removed once platform passes export type
                    if (exportType == ExportType.None)
                    {
                        Debug.LogWarning($"Cannot start export if no export type has been set");
                        return;
                    }

                    if (exportType is ExportType.HumanoidAnimation)
                    {
                        Instance.PublishMessage(new ExportAnimationMessage(exportType: exportType,
                            exportData: CollectExportData(item)));
                    }
                }

                // OnExport is called when the PLATFORM calls export.
                byte[] OnExport()
                {
                    // TODO: This will be removed once platform passes export type
                    if (m_LastExportType == ExportType.None)
                    {
                        Debug.LogWarning("Cannot perform export if the export type is not set");
                        return Array.Empty<byte>();
                    }

                    try
                    {
                        if (m_LastExportType == ExportType.FBX)
                        {
                            if (FBXExport.TryGetExportData(out byte[] data))
                            {
                                return data;
                            }

                            // TODO: Make exception not USD specific
                            throw new IOException("FBX Export failed");
                        }

                        throw new ArgumentOutOfRangeException(nameof(m_LastExportType), "Unsupported export type.");
                    }
                    catch (Exception e)
                    {
                        var toast = Toast.Build(Context.RootUI, e.Message, NotificationDuration.Short);

                        toast.SetStyle(NotificationStyle.Negative);
                        toast.SetAnimationMode(AnimationMode.Slide);
                        toast.Show();
                    }

                    m_LastExportType = ExportType.None;
                    return Array.Empty<byte>();
                }

                ExportData CollectExportData(LibraryItemModel itemToExport = null)
                {
                    if (Context.TimelineContext.TimelineBakingLogic.IsRunning)
                        throw new InvalidOperationException("Cannot export while animation is still baking.");

                    var actors = new ExportData.ActorExportData[Context.Stage.NumActors];
                    for (var i = 0; i < actors.Length; ++i)
                    {
                        var exportTargetActor = Context.Stage.GetActorModel(i);
                        var actorDefinitionComponent = Context.Stage.GetActorInstance(exportTargetActor.ID);
                        var posingArmature = Context.PoseAuthoringLogic.GetPosingArmature(exportTargetActor.EntityID);

                        actors[i] = new ExportData.ActorExportData(actorDefinitionComponent, exportTargetActor, i, posingArmature);
                    }

                    var props = new ExportData.PropExportData[Context.Stage.NumProps];
                    for (var i = 0; i < props.Length; ++i)
                    {
                        var exportTargetProp = Context.Stage.GetPropModel(i);
                        var propDefinitionComponent = Context.Stage.GetPropInstance(exportTargetProp.ID);

                        props[i] = new ExportData.PropExportData(Context.Stage, exportTargetProp, propDefinitionComponent, i);
                    }

                    // If we didn't explicitly pass in an item to export, we have a few fallbacks:
                    // Fallback 1: export the "working" timeline if we didn't specify which take to export
                    BakedTimelineModel contextBakedTimeline = Context.TimelineContext.BakedTimeline;
                    
                    // Fallback 2: if we have a take selected, export that one.
                    if (Context.TakeSelection.HasSelection)
                    {
                        itemToExport ??= Context.TakeSelection.GetSelection(0);
                    }
                    
                    if (itemToExport is TextToMotionTake take)
                    {
                        contextBakedTimeline = take.BakedTimelineModel;
                    }

                    return new ExportData(contextBakedTimeline, actors, props);
                }
                
                /// <summary>
                /// Write the working timeline into the currently selected take.
                /// </summary>
                void OnRequestSaveTimeline()
                {
                    if (Context.Authoring.Timeline.IsDirty && Context.Authoring.ActiveTake is KeySequenceTake take)
                    {
                        // TODO: Sometimes this doesn't update the thumbnail in the takes library. Why?
                        take.SetTimeline(Context.TimelineContext.Timeline);
                        Context.Authoring.Timeline.IsDirty = false;
                    }
                }

                void OnTextToMotionServiceStateChanged(TextToMotionService service, TextToMotionService.Status state)
                {
                    // TODO
                }

                void OnTextToMotionServiceRequestFailed(TextToMotionRequest request, string error)
                {
                    // Remove incomplete take from the library
                    // TODO: This logic will need to change if we are able to generate takes
                    Context.TakesLibrary.Remove(request.Target);
                }
            }
        }
    }
}
