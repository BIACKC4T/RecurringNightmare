using System;
using Hsm;
using Unity.AppUI.Core;
using Unity.Muse.AppUI.UI;
using Unity.DeepPose.Core;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using AppUI = Unity.Muse.AppUI.UI;

namespace Unity.Muse.Animate
{
    partial class Application
    {
        partial class ApplicationHsm
        {
            public class AuthorTimeline : ApplicationState<AuthorTimelineContext>, IKeyDownHandler, IPointerClickHandler
            {
                Transition m_NextTransition = Transition.None();
                float m_NextTick;
                const float k_TickInterval = 1f;

                public override Transition GetTransition()
                {
                    return m_NextTransition;
                }

                public override void OnEnter(object[] args)
                {
                    base.OnEnter(args);

                    // Clear entity selection
                    Context.EntitySelection.Clear();

                    // Add the two first keys if none is present
                    if (Context.Timeline.KeyCount == 0)
                        CreateStartingKeys();

                    // Set the title
                    Context.AuthoringModel.Title = Context.AuthoringModel.TargetName;

                    // Show the animation
                    // Note: We do not need to show the baked timeline view at this point because
                    // we're going straight to keyframe pose authoring.
                    // Context.BakedTimelineViewLogic.IsVisible = true;
                    Context.BakedTimelineViewLogic.ResetAllLoopOffsets();
                    
                    // Stop the playback
                    Context.Playback.CurrentFrame = 0;
                    Context.Playback.Stop();
                    
                    // Setup the UI
                    SetupUI();
                    
                    // Register to models
                    RegisterEvents();

                    if (Context.BakedTimeline.FramesCount == 0)
                    {
                        // Queue the baking of the timeline (run motion synthesis)
                        // Note: Forcing update to allow Context.TimelineBakingLogic.BakedTimelineMapping to be populated
                        Context.TimelineBakingLogic.QueueBaking(true);
                    }
                    
                    Context.AuthoringModel.Timeline.Mode = TimelineAuthoringModel.AuthoringMode.Unknown;
                    
                    // Enter Posing Mode on the first key
                    StartEditing(Context.Timeline.GetKey(0));
                }

                public override void OnExit()
                {
                    base.OnExit();
                    
                    // Save the working timeline.
                    Context.AuthoringModel.Timeline.RequestSaveTimeline();

                    // Stop the animation
                    Context.Playback.Stop();
                    Context.TimelineBakingLogic.Cancel();
                    
                    Context.Timeline.Clear();
                    Context.BakedTimelineMapping.Clear();
                    Context.BakedTimeline.Clear();
                    
                    Context.AuthoringModel.Timeline.Mode = TimelineAuthoringModel.AuthoringMode.Unknown;
                    
                    // Hide the animation
                    Context.BakedTimelineViewLogic.IsVisible = false;
        
                    // Close the UI
                    CloseUI();
                    
                    // Unregister from models
                    UnregisterEvents();
                }

                void SetupUI()
                {
                    // Show the Inspectors Panel
                    Context.InspectorsPanelViewModel.IsVisible = true;
                    
                    // Show the overlay UI
                    Context.Overlay.IsVisible = true;

                    // Refresh the UI
                    UpdateUI();
                }

                void CloseUI()
                {
                    // Hide the overlay UI
                    Context.Overlay.IsVisible = false;
                    
                    // Hide the Timeline UI
                    Context.TimelineUI.IsVisible = false;
                    
                    // Hide the toolbars used by the state
                    Context.SelectedEntitiesToolbar.IsVisible = false;
                    Context.UndoRedoToolbar.IsVisible = false;
                    Context.AddEntitiesToolbarViewModel.IsVisible = false;
                    
                    // Hide the Inspectors Panel
                    Context.InspectorsPanelViewModel.IsVisible = false;
                }
                
                void RegisterEvents()
                {
                    //----------------------
                    // TIMELINE UI REQUESTS
                    //----------------------

                    Context.TimelineUI.OnRequestedAddKey += OnTimelineUIRequestedAddKey;
                    Context.TimelineUI.OnRequestedDeleteSelectedKeys += OnTimelineUIRequestedDeleteSelectedKeys;
                    Context.TimelineUI.OnRequestedEditKey += OnTimelineUIRequestedEditKey;
                    Context.TimelineUI.OnRequestedEditTransition += OnTimelineUIRequestedEditTransition;
                    Context.TimelineUI.OnRequestedInsertKey += OnTimelineUIRequestedInsertKey;
                    Context.TimelineUI.OnRequestedInsertKeyWithEffectorRecovery += OnTimelineUIRequestedInsertKeyWithEffectorRecovery;
                    Context.TimelineUI.OnRequestedKeyToggle += OnTimelineUIRequestedKeyToggle;
                    Context.TimelineUI.OnRequestedSelectTransition += OnTimelineUIRequestedSelectTransition;
                    Context.TimelineUI.OnRequestedTransitionToggle += OnTimelineUIRequestedTransitionToggle;
                    Context.TimelineUI.OnRequestedSeekToKey += OnTimelineUIRequestedSeekToKey;
                    Context.TimelineUI.OnRequestedSeekToTransition += OnTimelineUIRequestedSeekToTransition;
                    Context.TimelineUI.OnRequestedSeekToFrame += OnTimelineUIRequestedSeekToFrame;
                    Context.TimelineUI.OnRequestedMoveKey += OnTimelineUIRequestedMoveKey;
                    Context.TimelineUI.OnRequestedDuplicateKey += OnTimelineUIRequestedDuplicateKey;
                    Context.TimelineUI.OnRequestedDeleteKey += OnTimelineUIRequestedDeleteKey;
                    
                    // Export Request
                    Context.Overlay.OnRequestExport += OnRequestedExport;

                    //--------------------
                    // AUTHORING REQUESTS
                    //--------------------

                    // Entities Requests
                    Context.AuthoringModel.Timeline.OnRequestedDeleteSelectedEntities += OnRequestedDeleteSelectedEntities;
                    
                    // Playback Requests
                    Context.AuthoringModel.Timeline.OnRequestedSeekToFrame += OnRequestedSeekToFrame;
                    
                    // Keys Authoring Requests
                    Context.AuthoringModel.Timeline.OnRequestedAddKey += OnRequestedAddKey;
                    Context.AuthoringModel.Timeline.OnRequestedDeleteSelectedKeys += OnRequestedDeleteSelectedKeys;
                    Context.AuthoringModel.Timeline.OnRequestedEditKey += OnRequestedEditKey;
                    Context.AuthoringModel.Timeline.OnRequestedEditKeyIndex += OnRequestedEditKeyIndex;
                    Context.AuthoringModel.Timeline.OnRequestedPreviewKey += OnRequestedPreviewKey;
                    Context.AuthoringModel.Timeline.OnRequestedSeekToKey += OnRequestedSeekToKey;
                    Context.AuthoringModel.Timeline.OnRequestedInsertKeyWithEffectorRecovery += OnRequestedInsertKeyWithEffectorRecovery;
                    Context.AuthoringModel.Timeline.OnRequestedInsertKey += OnRequestedInsertKey;
                    Context.AuthoringModel.Timeline.OnRequestedMoveKey += OnRequestedMoveKey;
                    Context.AuthoringModel.Timeline.OnRequestedDuplicateKey += OnRequestedDuplicateKey;
                    Context.AuthoringModel.Timeline.OnRequestedDeleteKey += OnRequestedDeleteKey;
                    
                    // Transitions Authoring Requests
                    Context.AuthoringModel.Timeline.OnRequestedSelectTransition += OnRequestedSelectTransition;
                    Context.AuthoringModel.Timeline.OnRequestedEditTransition += OnRequestedEditTransition;
                    Context.AuthoringModel.Timeline.OnRequestedPreviewTransition += OnRequestedPreviewTransition;
                    Context.AuthoringModel.Timeline.OnRequestedSeekToTransition += OnRequestedSeekToTransition;

                    // Posing Requests
                    Context.AuthoringModel.Timeline.OnRequestedCopyPose += OnRequestedCopyPose;

                    // Pose Estimation Requests
                    Context.AuthoringModel.Timeline.OnRequestedPoseEstimation += OnRequestedPoseEstimation;

                    //----------------
                    // EVENTS
                    //----------------

                    // Authoring Events
                    Context.AuthoringModel.Timeline.OnChanged += OnTimelineAuthoringChanged;
                    Context.AuthoringModel.Timeline.OnModeChanged += OnTimelineAuthoringModeChanged;

                    // Posing Events
                    Context.PoseAuthoringLogic.OnSolveFinished += OnPosingSolveFinished;
                    Context.PoseAuthoringLogic.OnEffectorSelectionChanged += OnEffectorSelectionChanged;

                    // Playback Events
                    Context.Playback.OnChanged += OnPlaybackChanged;

                    // Timeline Events
                    Context.Timeline.OnChanged += OnTimelineChanged;
                    Context.Timeline.OnKeyAdded += OnTimelineKeyAdded;
                    Context.Timeline.OnKeyChanged += OnTimelineKeyChanged;
                    Context.Timeline.OnKeyRemoved += OnTimelineKeyRemoved;
                    Context.Timeline.OnTransitionRemoved += OnTimelineTransitionRemoved;

                    // Timeline Baking Events
                    Context.BakedTimeline.OnChanged += OnBakedTimelineChanged;

                    // Selection Events
                    Context.EntitySelection.OnSelectionChanged += OnEntitySelectionChanged;
                    Context.TakeSelection.OnSelectionChanged += OnTakeSelectionChanged;
                }

                void UnregisterEvents()
                {
                    //----------------------
                    // TIMELINE UI REQUESTS
                    //----------------------

                    Context.TimelineUI.OnRequestedAddKey -= OnTimelineUIRequestedAddKey;
                    Context.TimelineUI.OnRequestedDeleteSelectedKeys -= OnTimelineUIRequestedDeleteSelectedKeys;
                    Context.TimelineUI.OnRequestedEditKey -= OnTimelineUIRequestedEditKey;
                    Context.TimelineUI.OnRequestedEditTransition -= OnTimelineUIRequestedEditTransition;
                    Context.TimelineUI.OnRequestedInsertKey -= OnTimelineUIRequestedInsertKey;
                    Context.TimelineUI.OnRequestedInsertKeyWithEffectorRecovery -= OnTimelineUIRequestedInsertKeyWithEffectorRecovery;
                    Context.TimelineUI.OnRequestedKeyToggle -= OnTimelineUIRequestedKeyToggle;
                    Context.TimelineUI.OnRequestedSelectTransition -= OnTimelineUIRequestedSelectTransition;
                    Context.TimelineUI.OnRequestedTransitionToggle -= OnTimelineUIRequestedTransitionToggle;
                    Context.TimelineUI.OnRequestedSeekToKey -= OnTimelineUIRequestedSeekToKey;
                    Context.TimelineUI.OnRequestedSeekToTransition -= OnTimelineUIRequestedSeekToTransition;
                    Context.TimelineUI.OnRequestedSeekToFrame -= OnTimelineUIRequestedSeekToFrame;
                    Context.TimelineUI.OnRequestedMoveKey -= OnTimelineUIRequestedMoveKey;
                    Context.TimelineUI.OnRequestedDuplicateKey -= OnTimelineUIRequestedDuplicateKey;
                    Context.TimelineUI.OnRequestedDeleteKey -= OnTimelineUIRequestedDeleteKey;

                    //--------------------
                    // AUTHORING REQUESTS
                    //--------------------

                    // Entities Requests
                    Context.AuthoringModel.Timeline.OnRequestedDeleteSelectedEntities -= OnRequestedDeleteSelectedEntities;
                    
                    // Playback Requests
                    Context.AuthoringModel.Timeline.OnRequestedSeekToFrame -= OnRequestedSeekToFrame;
                    
                    // Keys Authoring Requests
                    Context.AuthoringModel.Timeline.OnRequestedAddKey -= OnRequestedAddKey;
                    Context.AuthoringModel.Timeline.OnRequestedDeleteSelectedKeys -= OnRequestedDeleteSelectedKeys;
                    Context.AuthoringModel.Timeline.OnRequestedEditKey -= OnRequestedEditKey;
                    Context.AuthoringModel.Timeline.OnRequestedEditKeyIndex -= OnRequestedEditKeyIndex;
                    Context.AuthoringModel.Timeline.OnRequestedPreviewKey -= OnRequestedPreviewKey;
                    Context.AuthoringModel.Timeline.OnRequestedSeekToKey -= OnRequestedSeekToKey;
                    Context.AuthoringModel.Timeline.OnRequestedInsertKeyWithEffectorRecovery -= OnRequestedInsertKeyWithEffectorRecovery;
                    Context.AuthoringModel.Timeline.OnRequestedInsertKey -= OnRequestedInsertKey;
                    Context.AuthoringModel.Timeline.OnRequestedMoveKey -= OnRequestedMoveKey;
                    Context.AuthoringModel.Timeline.OnRequestedDuplicateKey -= OnRequestedDuplicateKey;
                    Context.AuthoringModel.Timeline.OnRequestedDeleteKey -= OnRequestedDeleteKey;

                    // Transitions Authoring Requests
                    Context.AuthoringModel.Timeline.OnRequestedSelectTransition -= OnRequestedSelectTransition;
                    Context.AuthoringModel.Timeline.OnRequestedEditTransition -= OnRequestedEditTransition;
                    Context.AuthoringModel.Timeline.OnRequestedPreviewTransition -= OnRequestedPreviewTransition;
                    Context.AuthoringModel.Timeline.OnRequestedSeekToTransition -= OnRequestedSeekToTransition;

                    // Posing Requests
                    Context.AuthoringModel.Timeline.OnRequestedCopyPose -= OnRequestedCopyPose;

                    // Pose Estimation Requests
                    Context.AuthoringModel.Timeline.OnRequestedPoseEstimation -= OnRequestedPoseEstimation;

                    //----------------
                    // EVENTS
                    //----------------

                    // Authoring Events
                    Context.AuthoringModel.Timeline.OnChanged -= OnTimelineAuthoringChanged;
                    Context.AuthoringModel.Timeline.OnModeChanged -= OnTimelineAuthoringModeChanged;

                    // Posing Events
                    Context.PoseAuthoringLogic.OnSolveFinished -= OnPosingSolveFinished;
                    Context.PoseAuthoringLogic.OnEffectorSelectionChanged -= OnEffectorSelectionChanged;

                    // Playback Events
                    Context.Playback.OnChanged -= OnPlaybackChanged;

                    // Timeline Events
                    Context.Timeline.OnChanged -= OnTimelineChanged;
                    Context.Timeline.OnKeyAdded -= OnTimelineKeyAdded;
                    Context.Timeline.OnKeyChanged -= OnTimelineKeyChanged;
                    Context.Timeline.OnKeyRemoved -= OnTimelineKeyRemoved;
                    Context.Timeline.OnTransitionRemoved -= OnTimelineTransitionRemoved;

                    // Timeline Baking Events
                    Context.BakedTimeline.OnChanged -= OnBakedTimelineChanged;

                    // Selection Events
                    Context.EntitySelection.OnSelectionChanged -= OnEntitySelectionChanged;
                    Context.TakeSelection.OnSelectionChanged -= OnTakeSelectionChanged;
                }

                void CreateStartingKeys()
                {
                    Context.Timeline.AddKey();
                    Context.Timeline.AddKey();

                    for (var i = 0; i < Context.Timeline.KeyCount; i++)
                    {
                        var key = Context.Timeline.Keys[i];

                        // Load the key
                        Context.PoseAuthoringLogic.RestorePosingStateFromKey(key.Key);

                        // Solve the physics for the key
                        Context.PoseAuthoringLogic.SolvePhysicsFully();

                        // Apply the solved pose on the key
                        Context.PoseAuthoringLogic.ApplyPosingStateToKey(key.Key);

                        // Request and render the thumbnail right away
                        RequestKeyThumbnail(key.Key.Thumbnail, key.Key);

                        Context.ThumbnailsService.Update();
                    }
                }

                void StartEditing(TimelineModel.SequenceKey key)
                {
                    SelectKey(key);
                    SeekToKey(key);
                    Context.PoseAuthoringLogic.RestorePosingStateFromKey(key.Key);
                    EnableUndoRedoForKey(key.Key);

                    // This logic looks a bit strange, since we are leaving the state and entering it again.
                    // Basically, we need this to trigger a state transition,
                    // i.e. change the sub-state to the correct type.
                    Context.AuthoringModel.Timeline.Mode = TimelineAuthoringModel.AuthoringMode.Unknown;
                    Context.AuthoringModel.Timeline.Mode = TimelineAuthoringModel.AuthoringMode.EditKey;
                }

                void StartEditing(TimelineModel.SequenceTransition transition)
                {
                    SelectTransition(transition);
                    SeekToTransition(transition);
                    Context.AuthoringModel.Timeline.Mode = TimelineAuthoringModel.AuthoringMode.Unknown;
                    Context.AuthoringModel.Timeline.Mode = TimelineAuthoringModel.AuthoringMode.EditTransition;
                }

                void SelectKey(TimelineModel.SequenceKey key)
                {
                    Context.KeySelection.SetSelection(key);
                    Context.AuthoringModel.LastSelectionType = AuthoringModel.SelectionType.SequenceKey;
                }

                void SelectTransition(TimelineModel.SequenceTransition transition)
                {
                    Context.TransitionSelection.Clear();
                    Context.TransitionSelection.Select(transition);
                }

                void StartPreviewing()
                {
                    Context.AuthoringModel.Timeline.Mode = TimelineAuthoringModel.AuthoringMode.Unknown;
                    Context.AuthoringModel.Timeline.Mode = TimelineAuthoringModel.AuthoringMode.Preview;
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

                public override void Update(float aDeltaTime)
                {
                    base.Update(aDeltaTime);
                    
                    if (Time.realtimeSinceStartup > m_NextTick)
                    {
                        Tick();
                        m_NextTick = Time.realtimeSinceStartup + k_TickInterval;
                    }
                    // Context.Playback.Update(aDeltaTime);
                }

                void Tick()
                {
                    Context.AuthoringModel.Timeline.RequestSaveTimeline();
                }

                public override bool UpdateBaking(float delta)
                {
                    if (base.UpdateBaking(delta))
                        return true;

                    // Note: I still dont do baking of the timeline at this level because
                    // I think it is used outside of the timeline editing context.
                    // See Author state, 1 level above.

                    /*
                    if (Context.TimelineContext.TimelineBakingLogic.NeedToUpdate)
                    {
                        Context.TimelineContext.TimelineBakingLogic.Update(false);
                    }
                    */

                    return false;
                }

                void UpdateStateTransition()
                {
                    switch (Context.AuthoringModel.Timeline.Mode)
                    {
                        case TimelineAuthoringModel.AuthoringMode.Unknown:
                            m_NextTransition = Transition.None();
                            break;

                        case TimelineAuthoringModel.AuthoringMode.Preview:
                            m_NextTransition = Transition.Inner<AuthorTimelinePreview>(Context.PreviewContext);
                            break;

                        case TimelineAuthoringModel.AuthoringMode.EditKey:
                        {
                            if (!Context.KeySelection.HasSelection)
                            {
                                m_NextTransition = Transition.Inner<AuthorTimelinePreview>(Context.PreviewContext);
                                break;
                            }

                            var selectedKey = Context.KeySelection.GetSelection(0);

                            m_NextTransition = selectedKey.Key.Type switch
                            {
                                KeyData.KeyType.Empty => Transition.Inner<AuthorTimelinePreview>(Context.PreviewContext),
                                KeyData.KeyType.FullPose => Transition.Inner<AuthorTimelineKeyPose>(Context.KeyPoseContext),
                                KeyData.KeyType.Loop => Transition.Inner<AuthorTimelineKeyLoop>(Context.KeyLoopContext),
                                _ => throw new ArgumentOutOfRangeException()
                            };
                            break;
                        }

                        case TimelineAuthoringModel.AuthoringMode.EditTransition:
                        {
                            if (!Context.TransitionSelection.HasSelection)
                            {
                                m_NextTransition = Transition.Inner<AuthorTimelinePreview>(Context.PreviewContext);
                                break;
                            }

                            m_NextTransition = Transition.Inner<AuthorTimelineTransition>(Context.TransitionContext);
                            break;
                        }

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                void OnSeekedToFrame()
                {
                    Context.BakedTimelineViewLogic.ResetAllLoopOffsets();

                    if (Context.AuthoringModel.Timeline.Mode == TimelineAuthoringModel.AuthoringMode.EditKey || Context.AuthoringModel.Timeline.Mode == TimelineAuthoringModel.AuthoringMode.EditTransition)
                    {
                        StartPreviewing();
                    }
                }

                void FrameCamera()
                {
                    if (!Context.EntitySelection.HasSelection)
                    {
                        Context.CameraMovement.Frame(Context.Timeline.GetWorldBounds());
                        return;
                    }

                    var entityID = Context.EntitySelection.GetSelection(0);
                    var bounds = GetEntityBounds(entityID);
                    for (var i = 1; i < Context.EntitySelection.Count; i++)
                    {
                        entityID = Context.EntitySelection.GetSelection(i);
                        var actorBounds = GetEntityBounds(entityID);
                        bounds.Encapsulate(actorBounds);
                    }

                    DeepPoseAnalytics.SendActionOfInterestEvent(DeepPoseAnalytics.ActionOfInterest.FrameCameraOnEntitySelection);
                    Context.CameraMovement.Frame(bounds);
                }

                void RequestKeyThumbnail(ThumbnailModel target, KeyModel key)
                {
                    Context.ThumbnailsService.RequestThumbnail(target, key, Context.Camera.Position, Context.Camera.Rotation);
                }

                void UpdatePlaybackDisplay()
                {
                    Context.TimelineUI.PlaybackViewModel.EmphasizeTransition = Context.AuthoringModel.Timeline.Mode != TimelineAuthoringModel.AuthoringMode.Preview;
                    Context.TimelineUI.PlaybackViewModel.ShowPlusButton = Context.AuthoringModel.Timeline.Mode == TimelineAuthoringModel.AuthoringMode.Preview;
                }

                void UpdateUIVisibility()
                {
                    switch (Context.AuthoringModel.Timeline.Mode)
                    {
                        default:
                        case TimelineAuthoringModel.AuthoringMode.Unknown:
                            Context.TimelineUI.IsVisible = false;
                            break;

                        case TimelineAuthoringModel.AuthoringMode.Preview:
                        case TimelineAuthoringModel.AuthoringMode.EditKey:
                        case TimelineAuthoringModel.AuthoringMode.EditTransition:
                            Context.TimelineUI.IsVisible = true;
                            break;
                    }


                    Context.UndoRedoToolbar.IsVisible = true;
                    
                    // NOTE: disabling props and multiple characters for the time being
                    // Context.AddEntitiesToolbarViewModel.IsVisible = true;
                    // Context.SelectedEntitiesToolbar.IsVisible = Context.AuthoringModel.Timeline.Mode == TimelineAuthoringModel.AuthoringMode.EditKey;
                    Context.SelectedEntitiesToolbar.IsVisible = false;
                    Context.AddEntitiesToolbarViewModel.IsVisible = false;
                }
                
                Bounds GetEntityBounds(EntityID entityID)
                {
                    var viewGameObject = Context.PoseAuthoringLogic.GetPosingGameObject(entityID);
                    var bounds = viewGameObject.GetRenderersWorldBounds();
                    return bounds;
                }

                public void OnKeyDown(KeyPressEvent eventData)
                {
                    if (ApplicationConstants.DebugStatesInputEvents) Log($"OnKeyDown({eventData.KeyCode})");

                    switch (eventData.KeyCode)
                    {
                        case KeyCode.F:
                            FrameCamera();
                            eventData.Use();
                            break;

                        case KeyCode.Delete:
                        {
                            if (Context.EntitySelection.Count > 0 && Context.AuthoringModel.LastSelectionType == AuthoringModel.SelectionType.Entity)
                            {
                                Context.AuthoringModel.Timeline.RequestDeleteSelectedEntities();
                                eventData.Use();
                            }
                            else if (Context.AuthoringModel.LastSelectionType == AuthoringModel.SelectionType.SequenceKey)
                            {
                                Context.AuthoringModel.Timeline.RequestDeleteSelectedKeys();
                                eventData.Use();
                            }

                            break;
                        }
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
                        case KeyCode.Space:
                            DoTogglePlayback();
                            eventData.Use();
                            break;
                        case KeyCode.LeftArrow:
                            Context.TimelineUI.SequenceViewModel.SelectPreviousItem();
                            eventData.Use();
                            break;
                        case KeyCode.RightArrow:
                            Context.TimelineUI.SequenceViewModel.SelectNextItem();
                            eventData.Use();
                            break;
                    }
                }

                void OnTimelineAuthoringModeChanged()
                {
                    RefreshCanCopyPose();
                    RefreshCanDeleteSelectedEntities();
                    RefreshCanDisableSelectedEffectors();
                    UpdateUI();
                    UpdateStateTransition();
                }

                void OnTimelineAuthoringChanged()
                {
                    RefreshCanCopyPose();
                    RefreshCanDeleteSelectedEntities();
                    RefreshCanDisableSelectedEffectors();
                    UpdateUI();
                    RefreshSelectedEntitiesToolbar();
                }

                void OnPlaybackChanged(PlaybackModel model, PlaybackModel.Property property)
                {
                    UpdateUI();

                    // Go to preview mode if started playing
                    if (property != PlaybackModel.Property.IsPlaying)
                        return;

                    if (!model.IsPlaying)
                        return;

                    StartPreviewing();
                }

                void OnBakedTimelineChanged(BakedTimelineModel model)
                {
                    Context.Playback.MaxFrame = model.FramesCount - 1;
                }

                void InitializeKeyPoses(ActorModel actorModel)
                {
                    for (var i = 0; i < Context.Timeline.KeyCount; i++)
                    {
                        var key = Context.Timeline.Keys[i];
                        Context.PoseAuthoringLogic.ApplyPosingStateToKey(actorModel.EntityID, key.Key);
                    }
                }

                void DoDuplicateKey(bool left = false)
                {
                    if (Context.KeySelection.Count != 1)
                        return;

                    var selectedKey = Context.KeySelection.GetSelection(0);
                    var oldIndex = Context.Timeline.IndexOf(selectedKey);

                    var toIndex = left ? oldIndex : oldIndex + 1;
                    var newKey = Context.Timeline.DuplicateKey(oldIndex, toIndex);

                    Context.KeySelection.Clear();
                    Context.KeySelection.Select(newKey);
                }

                void DoMoveKey(bool left = false)
                {
                    if (Context.KeySelection.Count != 1)
                        return;

                    var keyModel = Context.KeySelection.GetSelection(0);
                    var keyIndex = Context.Timeline.IndexOf(keyModel);
                    var toIndex = left ? keyIndex - 1 : keyIndex + 1;
                    Context.Timeline.MoveKey(keyIndex, toIndex);
                    Context.KeySelection.Clear();
                    Context.KeySelection.Select(Context.Timeline.GetKey(toIndex));
                }

                void DoTogglePlayback()
                {
                    if (Context.Playback.IsPlaying)
                    {
                        Context.Playback.Pause();
                    }
                    else
                    {
                        Context.Playback.Play(Context.Playback.CurrentFrame >= Context.Playback.MaxFrame);
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

                void DoCopyKey()
                {
                    if (Context.KeySelection.Count != 1)
                        return;

                    var selectedKey = Context.KeySelection.GetSelection(0);
                    Context.Clipboard.Copy(selectedKey.Key);

                    if (selectedKey.OutTransition != null)
                        Context.Clipboard.Copy(selectedKey.OutTransition.Transition);
                }

                void DoDeleteKey()
                {
                    // Note: There is no multiple selection allowed on keys for the moment
                    if (Context.KeySelection.Count != 1 || !CanDeleteSelectedKeys())
                        return;

                    var keyModel = Context.KeySelection.GetSelection(0);
                    var oldIndex = Context.Timeline.IndexOf(keyModel);
                    Context.Timeline.RemoveKey(keyModel);
                    Context.KeySelection.Clear();
                    StartEditing(Context.Timeline.GetKey(Mathf.Min(oldIndex, Context.Timeline.KeyCount - 1)));
                }

                void DoCopyPose()
                {
                    if (Context.KeySelection.Count != 1 || Context.EntitySelection.Count != 1)
                        return;

                    var selectedKey = Context.KeySelection.GetSelection(0);
                    var selectedEntityID = Context.EntitySelection.GetSelection(0);
                    if (!selectedKey.Key.TryGetKey(selectedEntityID, out var entityKeyModel))
                        return;

                    Context.Clipboard.Copy(entityKeyModel);
                    DeepPoseAnalytics.SendActionOfInterestEvent(DeepPoseAnalytics.ActionOfInterest.CopyPose);
                }

                bool CanPastePose()
                {
                    if (Context.EntitySelection.Count != 1 || Context.KeySelection.Count != 1)
                        return false;

                    var selectedEntityID = Context.EntitySelection.GetSelection(0);
                    var selectedKey = Context.KeySelection.GetSelection(0);
                    if (!selectedKey.Key.TryGetKey(selectedEntityID, out var entityKeyModel))
                        return false;

                    var canPasteKey = Context.Clipboard.CanPaste(entityKeyModel);
                    return canPasteKey;
                }

                bool CanPasteKey()
                {
                    if (Context.KeySelection.Count != 1)
                        return false;

                    var keyModel = Context.KeySelection.GetSelection(0);
                    return Context.Clipboard.CanPaste(keyModel.Key);
                }

                bool CanMoveKey(bool left = false)
                {
                    if (Context.KeySelection.Count != 1)
                        return false;

                    var keyModel = Context.KeySelection.GetSelection(0);
                    var keyIndex = Context.Timeline.IndexOf(keyModel);
                    return left ? keyIndex > 0 : keyIndex < Context.Timeline.KeyCount - 1;
                }

                bool CanDeleteKey() => Context.KeySelection.Count == 1 && Context.Timeline.KeyCount > 1;
                bool CanDeleteSelectedKeys() => Context.KeySelection.Count < Context.Timeline.KeyCount;

                void DoPasteAny()
                {
                    DoPasteReplaceKey();
                    DoPastePose();
                }

                void DoPasteReplaceKey()
                {
                    if (Context.KeySelection.Count != 1)
                        return;

                    var selectedKey = Context.KeySelection.GetSelection(0);
                    Context.Clipboard.Paste(selectedKey.Key);

                    if (selectedKey.OutTransition != null)
                        Context.Clipboard.Paste(selectedKey.OutTransition.Transition);

                    Context.PoseAuthoringLogic.RestorePosingStateFromKey(selectedKey.Key);
                }

                void DoPasteKey(bool left = false)
                {
                    var selectedKey = Context.KeySelection.GetSelection(0);
                    var keyIndex = Context.Timeline.IndexOf(selectedKey);
                    var toIndex = left ? keyIndex : keyIndex + 1;

                    // Create a key to paste into (doesn't matter what it contains)
                    var newKey = Context.Timeline.DuplicateKey(keyIndex, toIndex);
                    Context.Clipboard.Paste(newKey.Key);

                    Context.KeySelection.Clear();
                    Context.KeySelection.Select(newKey);
                }

                void DoPastePose()
                {
                    if (Context.KeySelection.Count != 1 || Context.EntitySelection.Count != 1)
                        return;

                    var selectedKey = Context.KeySelection.GetSelection(0);
                    var selectedEntityID = Context.EntitySelection.GetSelection(0);
                    if (!selectedKey.Key.TryGetKey(selectedEntityID, out var entityKeyModel))
                        return;

                    Context.Clipboard.Paste(entityKeyModel);
                    Context.PoseAuthoringLogic.RestorePosingStateFromKey(selectedKey.Key);
                    UndoRedoLogic.Instance.Prime();
                    UndoRedoLogic.Instance.Push();

                    DeepPoseAnalytics.SendActionOfInterestEvent(DeepPoseAnalytics.ActionOfInterest.PastePose);
                }

                void DoResetPose()
                {
                    if (Context.KeySelection.Count != 1)
                        return;

                    var selectedKey = Context.KeySelection.GetSelection(0);

                    for (var i = 0; i < Context.EntitySelection.Count; i++)
                    {
                        var selectedEntity = Context.EntitySelection.GetSelection(i);

                        if (!Context.Stage.TryGetActorModel(selectedEntity, out var actorModel))
                            continue;

                        if (!Context.PoseLibrary.TryGetDefaultPose(actorModel, out var sourceEntityKey))
                            continue;

                        if (!selectedKey.Key.TryGetKey(selectedEntity, out var destinationEntityKey))
                            continue;

                        sourceEntityKey.CopyTo(destinationEntityKey);
                    }

                    Context.PoseAuthoringLogic.RestorePosingStateFromKey(selectedKey.Key);
                    UndoRedoLogic.Instance.Prime();
                    UndoRedoLogic.Instance.Push();

                    DeepPoseAnalytics.SendActionOfInterestEvent(DeepPoseAnalytics.ActionOfInterest.ResetPose);
                }

                void DoAddKey()
                {
                    // Add a new key to the timeline
                    var sequenceKey = Context.Timeline.AddKey();

                    // Save the current pose to the newly created key
                    Context.PoseAuthoringLogic.ApplyPosingStateToKey(sequenceKey.Key);

                    // Edit the new key
                    Context.AuthoringModel.Timeline.RequestEditKey(sequenceKey);

                    DeepPoseAnalytics.SendActionOfInterestEvent(DeepPoseAnalytics.ActionOfInterest.AddKey);
                }

                void RefreshCanCopyPose()
                {
                    Context.AuthoringModel.Timeline.CanCopyPose = CanCopyPose();
                }

                void RefreshCanDeleteSelectedEntities()
                {
                    Context.AuthoringModel.Timeline.CanDeleteSelectedEntities = CanDeleteSelectedEntities();
                }

                void RefreshCanDisableSelectedEffectors()
                {
                    // HACK: Only the current editing state knows if the selected effectors can be disabled, but there
                    // is no way to query that state from here. For now the states shall push the correct value
                    // to the CanDisableSelectedEffectors property.
                    // Context.AuthoringModel.Timeline.CanDisableSelectedEffectors = CanDisableSelectedEffectors();
                }

                bool CanCopyPose()
                {
                    return TryGetPoseCopyHumanoidAnimator(out var _);
                }

                bool CanDeleteSelectedEntities()
                {
                    return Context.EntitySelection.HasSelection;
                }

                bool CanDisableSelectedEffectors()
                {
                    return Context.EntitySelection.HasSelection
                        && Context.AuthoringModel.Timeline.Mode == TimelineAuthoringModel.AuthoringMode.EditKey
                        && Context.KeySelection.HasSelection
                        && Context.KeySelection.GetSelection(0).Key.Type == KeyData.KeyType.FullPose
                        && Context.PoseAuthoringLogic.EffectorSelectionCount != 0;
                }

                void DeleteSelectedEntities()
                {
                    using var toRemoveList = TempList<EntityID>.Allocate();

                    for (var i = 0; i < Context.EntitySelection.Count; i++)
                    {
                        var entityID = Context.EntitySelection.GetSelection(i);
                        toRemoveList.Add(entityID);
                    }

                    foreach (var entityID in toRemoveList.List)
                    {
                        Context.Stage.RemoveEntity(entityID);
                    }
                }

                void CopyPose()
                {
                    if (TryGetPoseCopyHumanoidAnimator(out var animator))
                    {
                        var jsonData = ExportUtils.GetHumanoidPoseAsJson(animator.gameObject);

                        // Commented out with the new CloudLab pacakge migration. The clipboard functionality will be
                        // removed completely soon from the UI as well in https://jira.unity3d.com/browse/LABSCL-1852.
                        //WebGLClipboardHandler.SetClipboard(jsonData);
                        ShowPoseCopiedToast();
                    }
                }

                void ShowDeleteSelectedEntitiesPrompt(string title, string description, Action callback)
                {
                    var dialog = new AlertDialog
                    {
                        title = title,
                        description = description,
                        variant = AlertSemantic.Destructive
                    };

                    dialog.SetPrimaryAction(99, "Delete", callback);
                    dialog.SetCancelAction(1, "Cancel");

                    var modal = Modal.Build(Context.RootUI, dialog);
                    modal.Show();
                }

                void ShowPoseEstimationPrompt()
                {
                    // Commented out with the new CloudLab package migration. The clipboard functionality will be
                    // removed completely soon from the UI as well in https://jira.unity3d.com/browse/LABSCL-1852.
                    //WebGLBrowseFileDialog.Instance.OpenFileDialog(QueuePoseEstimation);
                }

                void ShowPoseEstimationReadyToast()
                {
                    Toast.Build(Context.RootUI, "Pose Estimation is Ready", NotificationDuration.Short)
                        .SetStyle(NotificationStyle.Positive)
                        .SetAnimationMode(AnimationMode.Slide)
                        .Show();
                }

                void ShowPoseEstimationFailedToast()
                {
                    Toast.Build(Context.RootUI, "Pose Estimation is unavailable", NotificationDuration.Short)
                        .SetStyle(NotificationStyle.Negative)
                        .SetAnimationMode(AnimationMode.Slide)
                        .Show();
                }

                void ShowPoseCopiedToast()
                {
                    Toast.Build(Context.RootUI, "Copied to Clipboard", NotificationDuration.Short)
                        .SetStyle(NotificationStyle.Positive)
                        .SetAnimationMode(AnimationMode.Slide)
                        .Show();
                }

                void SeekToTransition(TimelineModel.SequenceTransition transition)
                {
                    Context.BakedTimelineViewLogic.ResetAllLoopOffsets();

                    if (Context.BakedTimelineMapping.TryGetBakedTransitionSegment(
                            Context.Timeline.IndexOf(transition),
                            out var startBakedFrameIndex, out var endBakedFrameIndex))
                    {
                        Context.Playback.CurrentFrame = (startBakedFrameIndex + endBakedFrameIndex) / 2f;
                    }
                }

                void SeekToKey(TimelineModel.SequenceKey key)
                {
                    Context.BakedTimelineViewLogic.ResetAllLoopOffsets();

                    if (Context.BakedTimelineMapping.TryGetBakedKeyIndex(
                            Context.Timeline.IndexOf(key),
                            out var bakedFrameIndex))
                    {
                        Context.Playback.CurrentFrame = bakedFrameIndex;
                    }
                }

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

                bool TryGetPoseEstimationHumanoidAnimator(EntityID entityID, out Animator animator)
                {
                    var posingArmature = Context.PoseAuthoringLogic.GetPosingArmature(entityID);
                    return posingArmature.gameObject.TryGetHumanoidAnimator(out animator);
                }

                bool TryGetPoseCopyHumanoidAnimator(out Animator animator)
                {
                    animator = null;
                    if (Context.EntitySelection.Count != 1)
                        return false;

                    var entityID = Context.EntitySelection.GetSelection(0);

                    var armature = GetCurrentViewArmature(entityID);
                    if (armature == null)
                        return false;

                    return armature.gameObject.TryGetHumanoidAnimator(out animator);
                }

                ArmatureMappingComponent GetCurrentViewArmature(EntityID entityID)
                {
                    switch (Context.AuthoringModel.Timeline.Mode)
                    {
                        case TimelineAuthoringModel.AuthoringMode.Unknown:
                            return null;

                        case TimelineAuthoringModel.AuthoringMode.Preview:
                        case TimelineAuthoringModel.AuthoringMode.EditTransition:
                            return Context.BakedTimelineViewLogic.GetPreviewArmature(entityID);

                        case TimelineAuthoringModel.AuthoringMode.EditKey:
                        {
                            if (!Context.KeySelection.HasSelection)
                                return null;

                            var selectedKey = Context.KeySelection.GetSelection(0);
                            return selectedKey.Key.Type switch
                            {
                                KeyData.KeyType.Empty => null,
                                KeyData.KeyType.FullPose => Context.PoseAuthoringLogic.GetViewArmature(entityID),
                                KeyData.KeyType.Loop => null,
                                _ => throw new ArgumentOutOfRangeException()
                            };
                        }

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                void RefreshSelectedEntitiesToolbar()
                {
                    Context.SelectedEntitiesToolbar.CanCopyPose = Context.AuthoringModel.Timeline.CanCopyPose;
                    Context.SelectedEntitiesToolbar.CanDeleteSelectedEntities = Context.AuthoringModel.Timeline.CanDeleteSelectedEntities;
                    Context.SelectedEntitiesToolbar.CanEstimatePose = Context.AuthoringModel.Timeline.CanEstimatePose;
                }

                public void OnPointerClick(PointerEventData eventData)
                {
                    if (ApplicationConstants.DebugStatesInputEvents) Log($"OnPointerClick({eventData.button})");
                    
                    Context.EntitySelection.Clear();
                    eventData.Use();
                }

                // Playback Requests Handlers

                void OnRequestedSeekToFrame(float frame)
                {
                    Context.Playback.CurrentFrame = frame;
                    Context.BakedTimelineViewLogic.ResetAllLoopOffsets();
                    
                    if (Context.AuthoringModel.Timeline.Mode == TimelineAuthoringModel.AuthoringMode.EditKey || Context.AuthoringModel.Timeline.Mode == TimelineAuthoringModel.AuthoringMode.EditTransition)
                    {
                        StartPreviewing();
                    }
                }

                void UpdateUI()
                {
                    Context.TimelineUI.IsEditingKey = Context.AuthoringModel.Timeline.Mode == TimelineAuthoringModel.AuthoringMode.EditKey;
                    Context.TimelineUI.IsEditingTransition = Context.AuthoringModel.Timeline.Mode == TimelineAuthoringModel.AuthoringMode.EditTransition;
                    Context.TimelineUI.CurrentFrame = Context.Playback.CurrentFrame;
                    Context.TimelineUI.IsPlaying = Context.Playback.IsPlaying;

                    UpdatePlaybackDisplay();
                    UpdateUIVisibility();

                    var index = Mathf.FloorToInt(Context.Playback.CurrentFrame);

                    if (Context.BakedTimelineMapping.TryGetKeyIndex(index, out var keyIndex))
                    {
                        Context.TimelineUI.CurrentKeyIndex = keyIndex;
                        Context.TimelineUI.CurrentTransitionIndex = keyIndex;
                        
                        SelectKey(Context.Timeline.GetKey(keyIndex));
                    }
                    else if (Context.BakedTimelineMapping.TryGetTransitionIndex(index, out var transitionIndex))
                    {
                        Context.TimelineUI.CurrentKeyIndex = transitionIndex;
                        Context.TimelineUI.CurrentTransitionIndex = transitionIndex;
                        
                        SelectKey(Context.Timeline.GetKey(transitionIndex));
                    }
                    else
                    {
                        Context.TimelineUI.CurrentKeyIndex = -1;
                        Context.TimelineUI.CurrentTransitionIndex = -1;
                    }
                }

                // Keys Authoring Requests Handlers

                void OnRequestedAddKey()
                {
                    DoAddKey();
                }

                void OnRequestedDeleteKey(TimelineModel.SequenceKey key)
                {
                    Context.Timeline.RemoveKey(key);
                }

                TimelineModel.SequenceKey OnRequestedDuplicateKey(int fromIndex, int toIndex)
                {
                    return Context.Timeline.DuplicateKey(fromIndex, toIndex);
                }

                void OnRequestedMoveKey(int fromIndex, int toIndex)
                {
                    Context.Timeline.MoveKey(fromIndex, toIndex);
                }

                void OnRequestedEditKeyIndex(int index)
                {
                    StartEditing(Context.Timeline.GetKey(index));
                }

                void OnRequestedEditKey(TimelineModel.SequenceKey key)
                {
                    StartEditing(key);
                }

                void OnRequestedPreviewKey(TimelineModel.SequenceKey key)
                {
                    SelectKey(key);
                    SeekToKey(key);
                    StartPreviewing();
                }

                void OnRequestedSeekToKey(TimelineModel.SequenceKey key)
                {
                    SeekToKey(key);
                }

                void OnRequestedInsertKey(int keyIndex, out TimelineModel.SequenceKey sequenceKey)
                {
                    sequenceKey = Context.Timeline.InsertKey(keyIndex);
                    Context.PoseAuthoringLogic.ApplyPosingStateToKey(sequenceKey.Key);
                }

                /// <summary>
                /// Insert or replace a key with a key automatically built from effectors recovery.
                /// The key pose is built from the current timeline baked output animation, at the given bakedFrameIndex.
                /// </summary>
                /// <param name="bakedFrameIndex">The frame to use from the current timeline baked output animation.</param>
                /// <param name="keyIndex">The key index to insert the new key at.</param>
                /// <param name="progress">A 0 to 1 ratio representing the position in time of the inserted key in relation it's previous and next keys.</param>
                /// <param name="key">The key built from effectors recovery.</param>
                void OnRequestedInsertKeyWithEffectorRecovery(int bakedFrameIndex, int keyIndex, float progress, out TimelineModel.SequenceKey key)
                {
                    // Check if there is a key directly at the bakedFrameIndex
                    if (Context.BakedTimelineMapping.TryGetKeyIndex(bakedFrameIndex, out var existingKeyIndex))
                    {
                        key = Context.Timeline.GetKey(existingKeyIndex);
                    }
                    else
                    {
                        key = Context.Timeline.InsertKey(keyIndex, true, progress);
                    }
                    
                    RecoverEffectorsFromBakedTimeline(bakedFrameIndex);
                    Context.PoseAuthoringLogic.ApplyPosingStateToKey(key.Key);
                    
                    Context.AuthoringModel.RequestGenerateKeyThumbnail(key.Thumbnail, key.Key);
                    
                    // A bake step has to be performed here, to update the Context.BakedTimelineMapping
                    Context.TimelineBakingLogic.QueueBaking(true);
                    
                    Context.AuthoringModel.Timeline.RequestEditKeyIndex(keyIndex);

                    // TODO: figure out why TryGetKeyIndex is not working here
#if false
                    if (Context.BakedTimelineMapping.TryGetKeyIndex(bakedFrameIndex, out var newKeyIndex))
                    {
                        Context.AuthoringModel.Timeline.RequestEditKeyIndex(newKeyIndex);
                    }
                    else
                    {
                        Debug.LogError("Could not locate key index at frame "+bakedFrameIndex+" after effectors recovery.");
                    }
#endif
                }

                void OnRequestedDeleteSelectedKeys()
                {
                    DoDeleteKey();
                }

                // Transitions Authoring Requests Handlers

                void OnRequestedSelectTransition(TimelineModel.SequenceTransition transition)
                {
                    SelectTransition(transition);
                }

                void OnRequestedEditTransition(TimelineModel.SequenceTransition transition)
                {
                    StartEditing(transition);
                }

                void OnRequestedPreviewTransition(TimelineModel.SequenceTransition transition)
                {
                    SeekToTransition(transition);
                    StartPreviewing();
                }

                void OnRequestedSeekToTransition(TimelineModel.SequenceTransition transition)
                {
                    SeekToTransition(transition);
                }

                // Pose Estimation Requests Handlers

                void OnRequestedPoseEstimation()
                {
                    ShowPoseEstimationPrompt();
                }

                // Selection Events Handlers

                void OnEntitySelectionChanged(SelectionModel<EntityID> model)
                {
                    if (model.HasSelection)
                    {
                        Context.AuthoringModel.LastSelectionType = AuthoringModel.SelectionType.Entity;
                    }

                    RefreshCanCopyPose();
                    RefreshCanDeleteSelectedEntities();
                    RefreshCanDisableSelectedEffectors();

                    // Exit preview mode if selecting an entity
                    if (model.HasSelection && Context.AuthoringModel.Timeline.Mode == TimelineAuthoringModel.AuthoringMode.Preview)
                    {
                        var currentFrame = Mathf.FloorToInt(Context.Playback.CurrentFrame);
                        if (Context.BakedTimelineMapping.TryGetFirstKeyBefore(currentFrame, out _, out var keyTimelineIndex))
                        {
                            var key = Context.Timeline.GetKey(keyTimelineIndex);
                            Context.AuthoringModel.Timeline.RequestEditKey(key);
                        }
                    }
                }

                void OnRequestedCopyPose()
                {
                    CopyPose();
                }

                void OnRequestedDeleteSelectedEntities()
                {
                    // 26th June Release: disabling multiple characters and props
#if false
                    if (!Context.EntitySelection.HasSelection)
                        return;

                    ShowDeleteSelectedEntitiesPrompt(
                        $"Delete {Context.EntitySelection.Count.ToString()} entities",
                        "The selected entities will be deleted.",
                        DeleteSelectedEntities
                    );
#endif
                }
                
                void OnPosingSolveFinished(PoseAuthoringLogic logic)
                {
                    if (!Context.KeySelection.HasSelection)
                        return;

                    Assert.AreEqual(1, Context.KeySelection.Count);

                    var sequenceKey = Context.KeySelection.GetSelection(0);
                    Context.PoseAuthoringLogic.ApplyPosingStateToKey(sequenceKey.Key);
                }

                // -------------------------------------------
                // Selection Events Handlers
                // -------------------------------------------

                void OnEffectorSelectionChanged(PoseAuthoringLogic logic)
                {
                    RefreshCanDisableSelectedEffectors();
                }

                // -------------------------------------------
                // Timeline UI Requests Handlers
                // -------------------------------------------

                void OnTimelineUIRequestedKeyToggle(TimelineModel.SequenceKey key)
                {
                    if (Context.AuthoringModel.Timeline.Mode == TimelineAuthoringModel.AuthoringMode.Preview)
                    {
                        Context.AuthoringModel.Timeline.RequestEditKey(key);
                    }
                    else if (Context.AuthoringModel.Timeline.Mode == TimelineAuthoringModel.AuthoringMode.EditKey)
                    {
                        Context.AuthoringModel.Timeline.RequestPreviewKey(key);
                    }
                }

                void OnTimelineUIRequestedDeleteKey(TimelineModel.SequenceKey key)
                {
                    Context.AuthoringModel.Timeline.RequestDeleteKey(key);
                }

                TimelineModel.SequenceKey OnTimelineUIRequestedDuplicateKey(int fromIndex, int toIndex)
                {
                    return Context.AuthoringModel.Timeline.RequestDuplicateKey(fromIndex, toIndex);
                }

                void OnTimelineUIRequestedMoveKey(int fromIndex, int toIndex)
                {
                    Context.AuthoringModel.Timeline.RequestMoveKey(fromIndex, toIndex);
                }

                void OnTimelineUIRequestedEditKey(TimelineModel.SequenceKey key)
                {
                    Context.AuthoringModel.Timeline.RequestEditKey(key);
                }

                void OnTimelineUIRequestedSeekToFrame(float frame)
                {
                    Context.AuthoringModel.Timeline.RequestSeekToFrame(frame);
                }

                void OnTimelineUIRequestedTransitionToggle(TimelineModel.SequenceTransition transition)
                {
                    if (Context.AuthoringModel.Timeline.Mode == TimelineAuthoringModel.AuthoringMode.Preview)
                    {
                        Context.AuthoringModel.Timeline.RequestEditTransition(transition);
                    }
                    else if (Context.AuthoringModel.Timeline.Mode == TimelineAuthoringModel.AuthoringMode.EditTransition)
                    {
                        Context.AuthoringModel.Timeline.RequestPreviewTransition(transition);
                    }
                }

                void OnTimelineUIRequestedSelectTransition(TimelineModel.SequenceTransition transition)
                {
                    Context.AuthoringModel.Timeline.RequestSelectTransition(transition);
                }

                void OnTimelineUIRequestedDeleteSelectedKeys()
                {
                    Context.AuthoringModel.Timeline.RequestDeleteSelectedKeys();
                }

                void OnTimelineUIRequestedAddKey()
                {
                    Context.AuthoringModel.Timeline.RequestAddKey();
                }

                void OnTimelineUIRequestedInsertKey(int keyIndex, float transitionProgress)
                {
                    Context.AuthoringModel.Timeline.RequestInsertKey(keyIndex, out var key);
                    Context.AuthoringModel.Timeline.RequestEditKey(key);
                }

                void OnTimelineUIRequestedEditTransition(TimelineModel.SequenceTransition transition)
                {
                    Context.AuthoringModel.Timeline.RequestEditTransition(transition);
                }

                void OnTimelineUIRequestedSeekToTransition(TimelineModel.SequenceTransition transition)
                {
                    Context.AuthoringModel.Timeline.RequestSeekToTransition(transition);
                }

                void OnTimelineUIRequestedSeekToKey(TimelineModel.SequenceKey key)
                {
                    Context.AuthoringModel.Timeline.RequestSeekToKey(key);
                }

                void OnTimelineUIRequestedInsertKeyWithEffectorRecovery(int currentFrameIndex, int keyIndex, float transitionProgress)
                {
                    Context.AuthoringModel.Timeline.RequestInsertKeyWithEffectorRecovery(currentFrameIndex, keyIndex, transitionProgress, out var key);
                }

                // -------------------------------------------
                // Timeline Events Handlers
                // -------------------------------------------

                void OnTimelineChanged(TimelineModel model, TimelineModel.Property property)
                {
                    if (property != TimelineModel.Property.AnimationData)
                        return;

                    Context.TimelineBakingLogic.QueueBaking(false);
                    Context.AuthoringModel.Timeline.IsDirty = true;
                }

                void OnTimelineTransitionRemoved(TimelineModel model, TimelineModel.SequenceTransition transition)
                {
                    //Context.TransitionSelection.Unselect(transition);
                }

                void OnTimelineKeyChanged(TimelineModel model, KeyModel key, KeyModel.Property property)
                {
                    switch (property)
                    {
                        case KeyModel.Property.Type:
                            OnTimelineAuthoringModeChanged();
                            break;
                        case KeyModel.Property.EntityKey or KeyModel.Property.EntityList:
                            RequestKeyThumbnail(key.Thumbnail, key);
                            break;
                    }
                    
                    Context.TimelineBakingLogic.QueueBaking(false);
                    Context.AuthoringModel.Timeline.IsDirty = true;
                }

                void OnTimelineKeyRemoved(TimelineModel model, TimelineModel.SequenceKey key)
                {
                    Context.KeySelection.Unselect(key);
                    Context.ThumbnailsService.CancelRequestOf(key.Key.Thumbnail);
                }

                void OnTimelineKeyAdded(TimelineModel model, TimelineModel.SequenceKey key)
                {
                    RequestKeyThumbnail(key.Thumbnail, key.Key);
                }

                void OnRequestedExport()
                {
                    // Forward the export request to the Authoring logic/model, one level higher in the HSM
                    Context.AuthoringModel.RequestExportLibraryItem(null, null);
                }

                void OnTakeSelectionChanged(SelectionModel<LibraryItemModel> _)
                {
                    // Context.AuthoringModel.Timeline.RequestSaveTimeline();
                }
            }
        }
    }
}
