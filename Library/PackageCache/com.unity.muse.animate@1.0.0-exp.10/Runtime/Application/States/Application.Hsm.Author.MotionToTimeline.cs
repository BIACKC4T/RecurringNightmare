using System;
using Hsm;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Muse.AppUI.UI;
using Unity.AppUI.Core;

namespace Unity.Muse.Animate
{
    partial class Application
    {
        partial class ApplicationHsm
        {
            /// <summary>
            /// The user enters text. Presses a button. A text to motion ML model is run. The result is shown in 3d.
            /// </summary>
            public class AuthorMotionToTimeline : ApplicationState<AuthorMotionToTimelineContext>, IKeyDownHandler, IPointerClickHandler
            {
                public override Transition GetTransition()
                {
                    return Transition.None();
                }

                public override void OnEnter(object[] args)
                {
                    base.OnEnter(args);
                    
                    // Show the UI
                    Context.UI.IsVisible = true;
                    
                    // Set the title
                    Context.AuthoringModel.Title = "Converting Motion To Keys";
                    
                    // Clear entity selection
                    Context.EntitySelectionModel.Clear();
                    
                    // Start with looping on
                    Context.Playback.IsLooping = true;
                    
                    // Register Events
                    RegisterEvents();
                    
                    // Show the currently selected take
                    ActivateSelectedTake();
                }

                public override void OnExit()
                {
                    base.OnExit();
                    
                    // Reset the authoring step to "None"
                    Context.Model.Step = MotionToTimelineAuthoringModel.AuthoringStep.None;
                    
                    // Pause the playback
                    if (Context.Playback.IsPlaying)
                        Context.Playback.Pause();
                    
                    // Stop any baking or parsing
                    if(Context.MotionToKeysSampling.IsRunning) 
                        Context.MotionToKeysSampling.Cancel();
                    
                    Context.OutputTimelineBaking.Cancel();

                    // Hide the Views
                    Context.OutputBakedTimelineViewLogic.IsVisible = false;
                    Context.InputBakedTimelineViewLogic.IsVisible = false;

                    // Hide the UI
                    Context.UI.IsVisible = false;
                    
                    // Unregister events
                    UnregisterEvents();
                }

                void RegisterEvents()
                {
                    // Track the baking and sampling to show it on the UI
                    Context.BakingTaskStatusUI.TrackBakingLogics(Context.OutputTimelineBaking);
                    Context.BakingTaskStatusUI.TrackSamplingLogics(Context.MotionToKeysSampling);
                    
                    // Model Events
                    Context.Model.OnChanged += OnModelChanged;

                    // Model User Requests
                    Context.Model.OnRequestConfirm += RequestConfirm;
                    Context.Model.OnRequestPreview += RequestPreview;
                    Context.Model.OnRequestCancel += RequestCancel;

                    // Timeline events
                    Context.OutputTimeline.OnKeyAdded += OnTimelineKeyAdded;
                    Context.OutputTimeline.OnKeyChanged += OnTimelineKeyChanged;
                    Context.OutputTimeline.OnKeyRemoved += OnTimelineKeyRemoved;

                    // Playback events
                    Context.Playback.OnChanged += OnPlaybackChanged;
                    Context.UI.PlaybackUIModel.OnSeekedToFrame += OnPlaybackSeekedToFrame;

                    // Motion to keys sampling events
                    Context.MotionToKeysSampling.OnCompleted += OnMotionToKeysSamplingComplete;
                    Context.MotionToKeysSampling.OnFailed += OnMotionToKeysSamplingFailed;

                    // Baking events
                    Context.OutputTimelineBaking.OnBakingCompleted += OnOutputTimelineBakingCompleted;
                    Context.OutputTimelineBaking.OnBakingFailed += OnOutputTimelineBakingFailed;

                    Context.TakeSelection.OnSelectionChanged += OnTakeSelectionChanged;
                }
                
                void UnregisterEvents()
                {
                    // Stop tracking the baking and sampling
                    Context.BakingTaskStatusUI.UntrackBakingLogics(Context.OutputTimelineBaking);
                    Context.BakingTaskStatusUI.UntrackSamplingLogics(Context.MotionToKeysSampling);
                    
                    // Model Events
                    Context.Model.OnChanged -= OnModelChanged;
                    
                    // Model User Requests
                    Context.Model.OnRequestConfirm -= RequestConfirm;
                    Context.Model.OnRequestPreview -= RequestPreview;
                    Context.Model.OnRequestCancel -= RequestCancel;
                    
                    // Timeline Events
                    Context.OutputTimeline.OnKeyAdded -= OnTimelineKeyAdded;
                    Context.OutputTimeline.OnKeyChanged -= OnTimelineKeyChanged;
                    Context.OutputTimeline.OnKeyRemoved -= OnTimelineKeyRemoved;
                    
                    // Playback Events
                    Context.Playback.OnChanged -= OnPlaybackChanged;
                    Context.UI.PlaybackUIModel.OnSeekedToFrame -= OnPlaybackSeekedToFrame;
                    // Sampling Events
                    Context.MotionToKeysSampling.OnCompleted -= OnMotionToKeysSamplingComplete;
                    Context.MotionToKeysSampling.OnFailed -= OnMotionToKeysSamplingFailed;
                    // Baking Events
                    Context.OutputTimelineBaking.OnBakingCompleted -= OnOutputTimelineBakingCompleted;
                    Context.OutputTimelineBaking.OnBakingFailed -= OnOutputTimelineBakingFailed;
                    // Selection Events
                    Context.TakeSelection.OnSelectionChanged -= OnTakeSelectionChanged;
                }
                
                public override void Update(float aDeltaTime)
                {
                    base.Update(aDeltaTime);
                    
                    if (Context.Model.Step == MotionToTimelineAuthoringModel.AuthoringStep.NoPreview || Context.Model.Step == MotionToTimelineAuthoringModel.AuthoringStep.PreviewIsAvailable)
                    {
                        Context.Playback.Update(aDeltaTime);
                    }
                }

                /// <summary>
                /// Update the various baking logics of this state.
                /// </summary>
                /// <param name="delta"></param>
                /// <returns>Returns true if is interrupting further bakes deeper down the states tree.</returns>
                public override bool UpdateBaking(float delta)
                {
                    if (base.UpdateBaking(delta))
                        return true;

                    if (Context.OutputTimelineBaking.NeedToUpdate)
                    {
                        Context.OutputTimelineBaking.Update(delta, false);
                        return true;
                    }
                    
                    if (Context.MotionToKeysSampling.NeedToUpdate)
                    {
                        Context.MotionToKeysSampling.Update(delta, false);
                        return true;
                    }

                    return false;
                }

                void UpdateViews()
                {
                    // Input
                    if (Context.Model.Step != MotionToTimelineAuthoringModel.AuthoringStep.NoPreview)
                    {
                        Context.InputBakedTimelineViewLogic.IsVisible = false;
                    }
                    else
                    {
                        Context.InputBakedTimelineViewLogic.IsVisible = true;
                        Context.InputBakedTimelineViewLogic.DisplayFrame(Context.Playback.CurrentFrame);
                    }
                    
                    // Output
                    if (Context.Model.Step != MotionToTimelineAuthoringModel.AuthoringStep.PreviewIsAvailable)
                    {
                        Context.OutputBakedTimelineViewLogic.IsVisible = false;
                    }
                    else
                    {
                        Context.OutputBakedTimelineViewLogic.IsVisible = true;
                        Context.OutputBakedTimelineViewLogic.DisplayFrame(Context.Playback.CurrentFrame);
                    }
                }
                
                void DoMotionToKeys()
                {
                    Context.Playback.Stop();
                    Context.Model.Step = MotionToTimelineAuthoringModel.AuthoringStep.PreviewIsSamplingMotionToKeys;
                    Context.UI.IsSamplingMotionToKeys = true;
                    Context.MotionToKeysSampling.QueueBaking(Context.Model.KeyFrameSamplingSensitivity,
                        Context.Model.UseMotionCompletion);
                    UpdateViews();
                }
                
                void FrameCamera()
                {
                    if (!Context.EntitySelectionModel.HasSelection)
                    {
                        Context.CameraMovement.Frame(Context.InputBakedTimeline.GetWorldBounds());
                        return;
                    }

                    var entityID = Context.EntitySelectionModel.GetSelection(0);
                    var bounds = GetEntityBounds(entityID);
                    for (var i = 1; i < Context.EntitySelectionModel.Count; i++)
                    {
                        entityID = Context.EntitySelectionModel.GetSelection(i);
                        var actorBounds = GetEntityBounds(entityID);
                        bounds.Encapsulate(actorBounds);
                    }

                    Context.CameraMovement.Frame(bounds);
                }

                void OnModelChanged(MotionToTimelineAuthoringModel.Property obj)
                {
                    switch(obj)
                    {
                        case MotionToTimelineAuthoringModel.Property.Target:
                            UpdateUI();
                            break;
                        case MotionToTimelineAuthoringModel.Property.Step:
                            UpdateUI();
                            break;
                        case MotionToTimelineAuthoringModel.Property.FrameDensity:
                            Context.Model.IsPreviewObsolete = true;
                            break;
                        case MotionToTimelineAuthoringModel.Property.UseMotionCompletion:
                            Context.Model.IsPreviewObsolete = true;
                            break;
                        
                        case MotionToTimelineAuthoringModel.Property.IsPreviewObsolete:
                            if (Context.Model.IsPreviewObsolete)
                            {
                                Context.Model.Step = MotionToTimelineAuthoringModel.AuthoringStep.PreviewIsObsolete;
                            }
                            
                            UpdateUI();
                            break;
                        
                        default:
                            throw new ArgumentOutOfRangeException(nameof(obj), obj, null);
                    }
                }

                void OnPlaybackSeekedToFrame()
                {
                    ResetLoopOffsets();
                }

                void ResetLoopOffsets()
                {
                    Context.InputBakedTimelineViewLogic.ResetAllLoopOffsets();
                    Context.OutputBakedTimelineViewLogic.ResetAllLoopOffsets();
                }
                
                void OnPlaybackChanged(PlaybackModel model, PlaybackModel.Property property)
                {
                    switch (property)
                    {
                        case PlaybackModel.Property.IsPlaying:
                        case PlaybackModel.Property.IsLooping:
                            ResetLoopOffsets();
                            break;

                        case PlaybackModel.Property.CurrentTime:
                            UpdateViews();
                            break;

                        case PlaybackModel.Property.MinTime:
                        case PlaybackModel.Property.MaxTime:
                        case PlaybackModel.Property.FramesPerSecond:
                        case PlaybackModel.Property.PlaybackSpeed:
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(property), property, null);
                    }
                }

                public void OnPointerClick(PointerEventData eventData)
                {
                    if (ApplicationConstants.DebugStatesInputEvents) Log("OnPointerClick()");
                    
                    Context.EntitySelectionModel.Clear();
                    eventData.Use();
                }

                public void OnKeyDown(KeyPressEvent eventData)
                {
                    switch (eventData.KeyCode)
                    {
                        case KeyCode.F:
                            FrameCamera();
                            eventData.Use();
                            break;
                    }
                }
                
                void RequestConfirm()
                {
                    var newTake = new KeySequenceTake(Context.OutputTimeline, "Keys Take");
                    CreateTakeThumbnail(newTake);
                    Context.TakesLibraryModel.Add(newTake);
                    
                    var toast = Toast.Build(Context.RootUI, "Success! A new Editable Take was created.", NotificationDuration.Short);
                                        toast.SetStyle(NotificationStyle.Positive);
                                        toast.SetAnimationMode(AnimationMode.Slide);
                                        toast.Show();
                                        
                    Context.AuthoringModel.RequestEditLibraryItem(Context.TakesLibraryModel, newTake);
                }
                
                void RequestPreview()
                {
                    DoMotionToKeys();
                }
                
                void RequestCancel()
                {
                    if (Context.TakeSelection.HasSelection)
                    {
                        Context.AuthoringModel.RequestEditLibraryItem(Context.TakesLibraryModel, Context.TakeSelection.GetSelection(0));
                        return;
                    }

                    if (Context.TakesLibraryModel.TakesCount == 0)
                        return;
                    
                    Context.AuthoringModel.RequestEditLibraryItem(Context.TakesLibraryModel, Context.TakesLibraryModel.Takes[0]);
                }
                
                void OnMotionToKeysSamplingComplete(MotionToKeysSamplingLogic logic)
                {
                    Context.Model.Step = MotionToTimelineAuthoringModel.AuthoringStep.PreviewIsBakingTimelineOutput;
                    Context.OutputTimelineBaking.QueueBaking(false);
                }

                void OnOutputTimelineBakingCompleted(BakingLogic logic)
                {
                    Context.Model.IsPreviewObsolete = false;
                    Context.Playback.MaxFrame = Context.OutputBakedTimeline.FramesCount;
                    Context.Playback.Play(true);
                    FrameCamera();
                    Context.Model.Step = MotionToTimelineAuthoringModel.AuthoringStep.PreviewIsAvailable;
                    UpdateUI();
                }
                
                void OnOutputTimelineBakingFailed(BakingLogic logic, string error)
                {
                    Context.Model.IsPreviewObsolete = true;
                    Context.Model.Step = MotionToTimelineAuthoringModel.AuthoringStep.NoPreview;
                    Context.UI.IsBakingOutputTimeline = false;
                }

                void OnMotionToKeysSamplingFailed(MotionToKeysSamplingLogic logic, string error)
                {
                    Context.Model.IsPreviewObsolete = true;
                    Context.Model.Step = MotionToTimelineAuthoringModel.AuthoringStep.NoPreview;
                    Context.UI.IsSamplingMotionToKeys = false;
                }

                Bounds GetEntityBounds(EntityID entityID)
                {
                    var viewGameObject = Context.InputBakedTimelineViewLogic.GetPreviewGameObject(entityID);
                    var bounds = viewGameObject.GetRenderersWorldBounds();
                    return bounds;
                }

                void CreateTakeThumbnail(KeySequenceTake take)
                {
                    var timeline = take.TimelineModel;
                    if (timeline.KeyCount == 0) return;

                    // var targetKeyIndex = timeline.KeyCount / 2;
                    var targetKey = timeline.GetKey(0);
                    
                    Context.AuthoringModel.RequestGenerateKeyThumbnail(targetKey.Key.Thumbnail, targetKey.Key);
                }
                
                void OnTakeSelectionChanged(SelectionModel<LibraryItemModel> model)
                {
                    ActivateSelectedTake();
                }

                void ResetTake()
                {
                    // Set the preview to be obsolete, shows the convert button as enabled
                    Context.Model.IsPreviewObsolete = true;

                    // Reset the Views
                    Context.InputBakedTimelineViewLogic.IsVisible = false;
                    Context.InputBakedTimelineViewLogic.ResetAllLoopOffsets();
                    Context.OutputBakedTimelineViewLogic.IsVisible = false;
                    Context.OutputBakedTimelineViewLogic.ResetAllLoopOffsets();

                    // Set the authoring step
                    Context.Model.Step = MotionToTimelineAuthoringModel.AuthoringStep.None;
                }
                
                void ActivateSelectedTake()
                {
                    ResetTake();
                    
                    if (!Context.TakeSelection.HasSelection)
                        return;
                    
                    var take = Context.TakeSelection.Selection[0];

                    if (take is TextToMotionTake textToMotionTake)
                    {
                        ActivateTake(textToMotionTake);
                    }
                }
                
                void ActivateTake(TextToMotionTake take)
                {
                    Context.OutputTimeline.Clear();
                    Context.OutputBakedTimeline.Clear();
                    take.BakedTimelineModel.CopyTo(Context.InputBakedTimeline);
                    Context.Model.Step = MotionToTimelineAuthoringModel.AuthoringStep.NoPreview;
                    Context.Playback.MaxFrame = Context.InputBakedTimeline.FramesCount-1;
                    Context.Playback.Play(true);
                    
                    UpdateUI();
                    UpdateViews();
                }

                void UpdateUI()
                {
                    Context.UI.IsSamplingMotionToKeys = Context.MotionToKeysSampling.IsRunning;
                    Context.UI.IsBakingOutputTimeline = Context.OutputTimelineBaking.IsRunning;
                    Context.UI.Refresh();
                }
                
                void OnTimelineKeyChanged(TimelineModel model, KeyModel key, KeyModel.Property property)
                {
                    if (property is not (KeyModel.Property.EntityKey or KeyModel.Property.EntityList))
                        return;

                    RequestKeyThumbnail(key.Thumbnail, key);
                }

                void OnTimelineKeyRemoved(TimelineModel model, TimelineModel.SequenceKey key)
                {
                    Context.KeySelectionModel.Unselect(key);
                    Context.ThumbnailsService.CancelRequestOf(key.Key.Thumbnail);
                }

                void OnTimelineKeyAdded(TimelineModel model, TimelineModel.SequenceKey key)
                {
                    RequestKeyThumbnail(key.Thumbnail, key.Key);
                }
                
                void RequestKeyThumbnail(ThumbnailModel target, KeyModel key)
                {
                    Context.ThumbnailsService.RequestThumbnail(target, key, Context.Camera.Position, Context.Camera.Rotation);
                }
            }
        }
    }
}
