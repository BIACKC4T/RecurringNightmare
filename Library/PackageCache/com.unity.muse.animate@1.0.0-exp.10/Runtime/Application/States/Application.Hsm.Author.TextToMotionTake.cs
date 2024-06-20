using System;
using Hsm;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Unity.Muse.Animate
{
    partial class Application
    {
        partial class ApplicationHsm
        {
            /// <summary>
            /// The user enters text. Presses a button. A text to motion ML model is run. The result is shown in 3d.
            /// </summary>
            public class AuthorTextToMotion : ApplicationState<AuthorTextToMotionTakeContext>, IKeyDownHandler, IPointerClickHandler
            {
                TextToMotionTake m_Take;
                
                public override Transition GetTransition()
                {
                    return Transition.None();
                }

                public override void OnEnter(object[] args)
                {
                    base.OnEnter(args);

                    // Clear entity selection
                    Context.EntitySelection.Clear();

                    Context.AuthoringModel.Title = "Text to Motion";
                    Context.OutputBakedTimelineViewLogic.IsVisible = true;
                    Context.OutputBakedTimelineViewLogic.ResetAllLoopOffsets();

                    Context.UI.IsVisible = true;

                    // Start stopped, with looping on
                    Context.Playback.IsLooping = true;
                    
                    if (Context.Playback.IsPlaying)
                        Context.Playback.Stop();
                    
                    // Register Events
                    RegisterEvents();
                    
                    // Edit the current target take
                    EditTake(Context.Model.Target);
                }

                void OnModelChanged(TextToMotionAuthoringModel.Property property)
                {
                    switch (property)
                    {
                        case TextToMotionAuthoringModel.Property.Target:
                            EditTake(Context.Model.Target);
                            break;
                        case TextToMotionAuthoringModel.Property.RequestPrompt:
                        case TextToMotionAuthoringModel.Property.RequestSeed:
                        case TextToMotionAuthoringModel.Property.RequestTakesAmount:
                        case TextToMotionAuthoringModel.Property.RequestTakesCounter:
                        default:
                            break;
                    }
                }

                void OnOutputChanged(BakedTimelineModel model)
                {
                    UpdateViews();
                }

                public override void OnExit()
                {
                    base.OnExit();
                    
                    ClearTake();
                    

                    Context.OutputBakedTimelineViewLogic.IsVisible = false;
                    Context.UI.IsVisible = false;
                    
                    UnregisterEvents();
                }

                void RegisterEvents()
                {
                    Context.Model.OnRequestShuffle += OnRequestedShuffle;
                    Context.Model.OnRequestExtractKeys += OnRequestedExtractKeys;
                    Context.Model.OnRequestExport += OnRequestedExport;
                    Context.Model.OnRequestDelete += OnRequestedDelete;
                    Context.Model.OnChanged += OnModelChanged;
                    Context.OutputBakedTimeline.OnChanged += OnOutputChanged;
                    Context.Playback.OnChanged += OnPlaybackChanged;
                    Context.PlaybackUI.OnSeekedToFrame += OnPlaybackSeekedToFrame;
                }
                
                void UnregisterEvents()
                {
                    Context.Model.OnRequestShuffle -= OnRequestedShuffle;
                    Context.Model.OnRequestExtractKeys -= OnRequestedExtractKeys;
                    Context.Model.OnRequestExport -= OnRequestedExport;
                    Context.Model.OnRequestDelete -= OnRequestedDelete;
                    Context.Model.OnChanged -= OnModelChanged;
                    Context.OutputBakedTimeline.OnChanged -= OnOutputChanged;
                    Context.Playback.OnChanged -= OnPlaybackChanged;
                    Context.PlaybackUI.OnSeekedToFrame -= OnPlaybackSeekedToFrame;
                }
                
                public override void Update(float aDeltaTime)
                {
                    base.Update(aDeltaTime);
                    Context.Playback.Update(aDeltaTime);
                }

                void UpdateViews()
                {
                    if (Context.OutputBakedTimeline.FramesCount <= 0 || Context.Playback.MaxFrame <= Context.Playback.MinFrame)
                    {
                        Context.OutputBakedTimelineViewLogic.IsVisible = false;
                    }
                    else
                    {
                        Context.OutputBakedTimelineViewLogic.IsVisible = true;
                        Context.OutputBakedTimelineViewLogic.DisplayFrame(Context.Playback.CurrentFrame);
                    }
                }

                void UpdateUI()
                {
                    Context.UI.IsBakingCurrentTake = m_Take?.IsBaking ?? false;
                    Context.UI.IsBusy = m_Take?.IsBaking ?? false;

                    if (Context.OutputBakedTimeline.FramesCount <= 0 || Context.Playback.MaxFrame <= Context.Playback.MinFrame)
                    {
                        Context.UI.CanMakeEditable = false;
                        Context.UI.CanExport = false;
                    }
                    else
                    {
                        Context.UI.CanMakeEditable = true;
                        Context.UI.CanExport = true;
                    }
                }

                void EditTake(TextToMotionTake take)
                {
                    // Unload and hide the previous take
                    ClearTake();

                    m_Take = take;

                    if (m_Take != null)
                    {
                        Context.UI.Prompt = m_Take.Prompt;
                        Context.UI.Seed = m_Take.Seed;
                        
                        m_Take.OnBakingComplete += OnTakeBakingComplete;
                        m_Take.OnBakingFailed += OnTakeBakingFailed;
                        
                        // Only track the baking logic if it is currently baking this specific take
                        if (m_Take.IsBaking)
                        {
                            // Previously, we start tracking the baking logic here to display in the UI.
                            // However, this logic didn't work as intended since we can't track individual generations.
                            // Also, we need to be able to display errors even when a take is not selected. Therefore,
                            // we hook up the tracking instead in Hsm.Author when the context is created.
                            
                            // Show a notice
                            Context.BakingNoticeUI.Show("Solving Animation...");
                        }
                        
                        // Copy the baked timeline from the take to the output
                        m_Take.BakedTimelineModel.CopyTo(Context.OutputBakedTimeline);
                        
                        // Adjust the playback to match the new output
                        if (Context.OutputBakedTimeline.FramesCount > 0)
                        {
                            Context.Playback.MaxFrame = Context.OutputBakedTimeline.FramesCount - 1;
                            
                            // Play the output
                            Context.Playback.Play(true);
                            
                            FrameCamera();
                        }
                        else 
                        {
                            // Stop the output playback
                            Context.Playback.MaxFrame = 0;
                            Context.Playback.Stop();
                        }
                    }
                    else
                    {
                        Context.UI.Prompt = "";
                        Context.UI.Seed = 0;
                        Context.Playback.MaxFrame = 0;
                        Context.OutputBakedTimeline.Clear();
                        Context.Playback.Stop();
                    }

                    UpdateViews();
                    UpdateUI();
                }

                void ClearTake()
                {
                    if (m_Take != null)
                    {
                        m_Take.OnBakingComplete -= OnTakeBakingComplete;
                        m_Take.OnBakingFailed -= OnTakeBakingFailed;
                        
                        // Hide the notice
                        Context.BakingNoticeUI.Hide();
                        
                        Context.UI.Prompt = "";
                        Context.UI.Seed = 0;
                        
                        Context.Playback.MaxFrame = 0;
                        
                        if (Context.Playback.IsPlaying)
                            Context.Playback.Stop();

                        Context.OutputBakedTimeline.Clear();
                        
                        m_Take = null;
                    }
                }
                
                void FrameCamera()
                {
                    if (!Context.EntitySelection.HasSelection)
                    {
                        Context.CameraMovement.Frame(Context.OutputBakedTimeline.GetWorldBounds());
                        return;
                    }
                    
                    Context.CameraMovement.Frame(GetSelectedEntitiesBounds());
                }

                void OnPlaybackSeekedToFrame()
                {
                    Context.OutputBakedTimelineViewLogic.ResetAllLoopOffsets();
                }

                void OnPlaybackChanged(PlaybackModel model, PlaybackModel.Property property)
                {
                    switch (property)
                    {
                        case PlaybackModel.Property.IsPlaying:
                        case PlaybackModel.Property.IsLooping:
                            Context.OutputBakedTimelineViewLogic.ResetAllLoopOffsets();
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
                    if (ApplicationConstants.DebugStatesInputEvents) Log($"OnPointerClick({eventData.button})");
                    
                    Context.EntitySelection.Clear();
                    eventData.Use();
                }

                public void OnKeyDown(KeyPressEvent eventData)
                {
                    if (ApplicationConstants.DebugStatesInputEvents) Log($"OnKeyDown({eventData.KeyCode})");
                    
                    if (Context.TakesUI.IsWriting)
                    {
                        eventData.Use();
                        return;
                    }

                    switch (eventData.KeyCode)
                    {
                        case KeyCode.F:
                            FrameCamera();
                            eventData.Use();
                            break;
                    }
                }

                void OnRequestedExtractKeys(TextToMotionTake take)
                {
                    Context.AuthoringModel.RequestConvertMotionToKeys(Context.Model.Target);
                }

                void OnRequestedExport(TextToMotionTake take)
                {
                    // Export requests are handled by the authoring model
                    Context.AuthoringModel.RequestExportLibraryItem(Context.TakesLibraryModel, take);
                }

                void OnRequestedDelete(TextToMotionTake take) { }

                void OnRequestedShuffle(TextToMotionTake take) { }

                Bounds GetEntityBounds(EntityID entityID)
                {
                    var viewGameObject = Context.OutputBakedTimelineViewLogic.GetPreviewGameObject(entityID);
                    var bounds = viewGameObject.GetRenderersWorldBounds();
                    return bounds;
                }

                Bounds GetSelectedEntitiesBounds()
                {
                    var entityID = Context.EntitySelection.GetSelection(0);
                    var bounds = GetEntityBounds(entityID);

                    for (var i = 1; i < Context.EntitySelection.Count; i++)
                    {
                        entityID = Context.EntitySelection.GetSelection(i);
                        var actorBounds = GetEntityBounds(entityID);
                        bounds.Encapsulate(actorBounds);
                    }

                    return bounds;
                }
                
                void OnTakeBakingComplete()
                {
                    // Copy the baked take onto the output
                    Context.Model.Target.BakedTimelineModel.CopyTo(Context.OutputBakedTimeline);
                    
                    // Adjust to playback to the length of the output and play
                    Context.Playback.MaxFrame = Context.OutputBakedTimeline.FramesCount - 1;
                    Context.Playback.Play(true);
                    
                    // Hide the baking notice
                    Context.BakingNoticeUI.Hide();
                    
                    // Update the output views and the UI state
                    UpdateViews();
                    UpdateUI();
                    
                    // Automatically Frame the camera
                    FrameCamera();
                }
                
                void OnTakeBakingFailed()
                {
                    // Hide the baking notice
                    Context.BakingNoticeUI.Hide();
                    
                    // Remove the take from the library
                    ClearTake();
                    
                    // Update the output views and the UI state
                    UpdateViews();
                    UpdateUI();
                    
                    Context.AuthoringModel.RequestDeleteLibraryItem(Context.TakesLibraryModel, m_Take);
                }
            }
        }
    }
}
