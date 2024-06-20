using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Muse.Animate
{
    class PlaybackViewModel
    {
        public delegate void Changed();
        public event Changed OnChanged;
        public delegate void RequestedInsertKey(int keyIndex, float transitionProgress);
        public event RequestedInsertKey OnRequestedInsertKey;
        public event Action<float> OnRequestedSeekToFrame;

        public int CurrentFrame => Mathf.RoundToInt(m_PlaybackModel.CurrentFrame);
        public int MinFrame => Mathf.FloorToInt(m_PlaybackModel.MinFrame);
        public int MaxFrame => Mathf.CeilToInt(m_PlaybackModel.MaxFrame);

        public bool EmphasizeTransition
        {
            get => m_EmphasizeTransition;
            set
            {
                if (m_EmphasizeTransition == value)
                    return;

                m_EmphasizeTransition = value;
                OnChanged?.Invoke();
            }
        }

        public bool ShowPlusButton
        {
            get => m_ShowPlusButton && !IsPlaying;
            set
            {
                if (m_ShowPlusButton == value)
                    return;

                m_ShowPlusButton = value;
                OnChanged?.Invoke();
            }
        }

        public bool IsPlaying => m_PlaybackModel.IsPlaying;
        public bool IsLooping => m_PlaybackModel.IsLooping;
        public int FramesPerSecond => Mathf.RoundToInt(m_PlaybackModel.FramesPerSecond);
        public float PlaybackSpeed => m_PlaybackModel.PlaybackSpeed;
        public int KeyCount => m_TimelineModel.KeyCount;
        public bool ShowTransition => m_ShowTransition;
        public int TransitionStart => m_TransitionStart;
        public int TransitionEnd => m_TransitionEnd;
        public List<int> KeyFrames => m_KeyFrames;

        PlaybackModel m_PlaybackModel;
        TimelineModel m_TimelineModel;
        BakedTimelineMappingModel m_BakedTimelineMappingModel;
        List<int> m_KeyFrames = new();

        bool m_ShowTransition;
        int m_TransitionStart;
        int m_TransitionEnd;
        bool m_EmphasizeTransition;
        bool m_ShowPlusButton;

        public PlaybackViewModel(PlaybackModel playbackModel,
            TimelineModel timelineModel,
            BakedTimelineMappingModel mappingModel)
        {
            m_PlaybackModel = playbackModel;
            m_TimelineModel = timelineModel;
            m_BakedTimelineMappingModel = mappingModel;

            m_PlaybackModel.OnChanged += OnPlaybackChanged;
            m_BakedTimelineMappingModel.OnChanged += OnBakedTimelineMappingChanged;

            UpdateKeyFrames();
            UpdateTransition();
        }

        public void Play()
        {
            DeepPoseAnalytics.SendTimelineAction(DeepPoseAnalytics.TimelineAction.Play);

            if (CurrentFrame == MaxFrame)
                m_PlaybackModel.GoToStart();

            m_PlaybackModel.Play();
        }

        public void Pause()
        {
            DeepPoseAnalytics.SendTimelineAction(DeepPoseAnalytics.TimelineAction.Pause);
            
            m_PlaybackModel.Pause();
        }

        public void ToggleLooping()
        {
            m_PlaybackModel.IsLooping = !m_PlaybackModel.IsLooping;

            DeepPoseAnalytics.SendTimelineAction((m_PlaybackModel.IsLooping) ? 
                DeepPoseAnalytics.TimelineAction.LoopEnable :
                DeepPoseAnalytics.TimelineAction.LoopDisable);
        }

        public void RequestSeekToFrame(float frame)
        {
            if (Math.Abs(m_PlaybackModel.CurrentFrame - frame) < Mathf.Epsilon)
                return;

            OnRequestedSeekToFrame?.Invoke(frame);
        }

        public void GoToPrevKey()
        {
            if (m_TimelineModel.KeyCount == 0)
                return;

            var currentFrameIdx = Mathf.FloorToInt(m_PlaybackModel.CurrentFrame);
            if (!m_BakedTimelineMappingModel.TryGetFirstKeyBefore(currentFrameIdx, out var keyBakedFrameIndex, out var keyTimelineIndex, true))
                return;

            DeepPoseAnalytics.SendTimelineKeyAction(DeepPoseAnalytics.TimelineKeyAction.GoToPreviousKey, keyTimelineIndex);
       
            RequestSeekToFrame(keyBakedFrameIndex);
        }

        public void GoToNextKey()
        {
            if (m_TimelineModel.KeyCount == 0)
                return;

            var currentFrameIdx = Mathf.FloorToInt(m_PlaybackModel.CurrentFrame);
            if (!m_BakedTimelineMappingModel.TryGetFirstKeyAfter(currentFrameIdx, out var keyBakedFrameIndex, out var keyTimelineIndex, true))
                return;

            DeepPoseAnalytics.SendTimelineKeyAction(DeepPoseAnalytics.TimelineKeyAction.GoToNextKey, keyTimelineIndex);
            
            RequestSeekToFrame(keyBakedFrameIndex);
        }

        public void SetNextPlaybackSpeed()
        {
            var currentPlaybackSpeed = m_PlaybackModel.PlaybackSpeed;
            var nextPlaybackSpeed = currentPlaybackSpeed.Equals(0.25f) ? 0.5f
                : currentPlaybackSpeed.Equals(0.5f) ? 1f
                : currentPlaybackSpeed + 1f;

            if (currentPlaybackSpeed.Equals(3))
            {
                nextPlaybackSpeed = 0.25f;
            }

            m_PlaybackModel.PlaybackSpeed = nextPlaybackSpeed;

            DeepPoseAnalytics.SendTimelineSetPlaybackSpeed(nextPlaybackSpeed);
        }

        public void SetFramesPerSecond(int framesPerSecond)
        {
            m_PlaybackModel.FramesPerSecond = framesPerSecond;
        }

        void OnPlaybackChanged(PlaybackModel model, PlaybackModel.Property property)
        {
            UpdateTransition();
            OnChanged?.Invoke();
        }

        void OnBakedTimelineMappingChanged(BakedTimelineMappingModel model)
        {
            UpdateKeyFrames();
            UpdateTransition();
            OnChanged?.Invoke();
        }

        void UpdateKeyFrames()
        {
            m_KeyFrames.Clear();

            // Note: we skip first and last key for display
            for (var i = 0; i < m_TimelineModel.KeyCount; i++)
            {
                if (!m_BakedTimelineMappingModel.TryGetBakedKeyIndex(i, out var bakedFrameIndex))
                    continue;

                m_KeyFrames.Add(bakedFrameIndex);
            }
        }

        void UpdateTransition()
        {
            m_ShowTransition = false;
            
            // Get the closest key in the past
            if (!m_BakedTimelineMappingModel.TryGetFirstKeyBefore(CurrentFrame, out var startBakedFrameIndex, out var keyTimelineIndex, false))
                return;

            if (m_TimelineModel.KeyCount == 0)
                return;
            
            // Get the out transition of that key
            var key = m_TimelineModel.GetKey(keyTimelineIndex);
            var transition = key.OutTransition;

            // Special case for last key
            if (transition == null)
            {
                m_ShowTransition = true;
                m_TransitionStart = startBakedFrameIndex;
                m_TransitionEnd = startBakedFrameIndex + 1;
                return;
            }

            // Get the index of the out transition
            var transitionIdx = m_TimelineModel.IndexOf(transition);
            if (transitionIdx < 0)
                return;

            // Get the baked range for the transition
            if (!m_BakedTimelineMappingModel.TryGetBakedTransitionSegment(transitionIdx, out startBakedFrameIndex, out var endBakedFrameIndex))
                return;

            m_ShowTransition = true;
            m_TransitionStart = startBakedFrameIndex;
            m_TransitionEnd = endBakedFrameIndex;
        }

        public void InsertKeyAtCurrentFrame()
        {
            m_BakedTimelineMappingModel.GetBakedKeyProgressAt(m_PlaybackModel.CurrentFrame, out var keyIndex, out var transitionProgress);
            Debug.Log($"GetBakedKeyProgressAt({m_PlaybackModel.CurrentFrame} -> {keyIndex}, {transitionProgress})");

            DeepPoseAnalytics.SendTimelineKeyAction(DeepPoseAnalytics.TimelineKeyAction.InsertKey, keyIndex + 1);

            OnRequestedInsertKey?.Invoke(keyIndex+1, transitionProgress);
        }
    }
}
