﻿using System;
using UnityEngine;

namespace Unity.Muse.Animate
{
    class TimelineViewModel
    {
        public delegate void Changed(Property property);

        public event Changed OnChanged;
        public event Action<TimelineModel.SequenceKey> OnRequestedKeyToggle;
        public event Action<TimelineModel.SequenceTransition> OnRequestedTransitionToggle;
        public event Action<TimelineModel.SequenceTransition> OnRequestedSelectTransition;
        public event Action<TimelineModel.SequenceKey> OnRequestedEditKey;
        public event Action<TimelineModel.SequenceKey> OnRequestedDeleteKey;
        public delegate void RequestedMoveKey(int fromIndex, int toIndex);
        public event RequestedMoveKey OnRequestedMoveKey;
        public event Action<TimelineModel.SequenceKey> OnRequestedSeekToKey;
        public event Action<TimelineModel.SequenceTransition> OnRequestedSeekToTransition;
        public delegate TimelineModel.SequenceKey RequestedDuplicateKey(int fromIndex, int toIndex);
        public event RequestedDuplicateKey OnRequestedDuplicateKey;
        public event Action<float> OnRequestedSeekToFrame;
        public event Action OnRequestedDeleteSelectedKeys;
        public event Action OnRequestedAddKey;
        public event SequenceViewModel.RequestedInsertKey OnRequestedInsertKey;
        public event Action<int,int,float> OnRequestedInsertKeyWithEffectorRecovery;
        public event Action<TimelineModel.SequenceTransition> OnRequestedEditTransition;

        public enum Property
        {
            Visibility,
            IsReadOnly,
            IsPlaying,
            IsEditingKey,
            IsEditingTransition,
            CurrentFrame,
            CurrentFrameIndex,
            CurrentKeyIndex,
            CurrentTransitionIndex
        }

        public SequenceViewModel SequenceViewModel => m_SequenceViewModel;
        public BakingViewModel BakingViewModel => m_BakingViewModel;
        public PlaybackViewModel PlaybackViewModel => m_PlaybackViewModel;

        public bool IsPlaying
        {
            get => m_IsPlaying;

            set
            {
                if (m_IsPlaying == value)
                    return;

                m_IsPlaying = value;
                m_SequenceViewModel.IsPlaying = m_IsPlaying;
                OnChanged?.Invoke(Property.IsPlaying);
            }
        }

        public bool IsEditingKey
        {
            get => m_IsEditingKey;

            set
            {
                if (m_IsEditingKey == value)
                    return;

                m_IsEditingKey = value;
                m_SequenceViewModel.IsEditingKey = m_IsEditingKey;
                OnChanged?.Invoke(Property.IsEditingKey);
            }
        }
        
        public bool IsEditingTransition
        {
            get => m_IsEditingTransition;

            set
            {
                if (m_IsEditingTransition == value)
                    return;

                m_IsEditingTransition = value;
                m_SequenceViewModel.IsEditingTransition = m_IsEditingTransition;
                OnChanged?.Invoke(Property.IsEditingTransition);
            }
        }

        public float CurrentFrame
        {
            get => m_CurrentFrame;
            set
            {
                if (Math.Abs(value - m_CurrentFrame) < Mathf.Epsilon)
                    return;

                m_CurrentFrame = value;
                CurrentFrameIndex = Mathf.RoundToInt(m_CurrentFrame);
                OnChanged?.Invoke(Property.CurrentFrame);
            }
        }

        int CurrentFrameIndex
        {
            get => m_CurrentFrameIndex;
            set
            {
                if (value == m_CurrentFrameIndex)
                    return;

                m_CurrentFrameIndex = value;
                UpdateCurrentIndices();
                OnChanged?.Invoke(Property.CurrentFrameIndex);
            }
        }

        public int CurrentKeyIndex
        {
            get => m_CurrentKeyIndex;
            set
            {
                if (value == m_CurrentKeyIndex)
                    return;

                m_CurrentKeyIndex = value;
                m_SequenceViewModel.CurrentKeyIndex = m_CurrentKeyIndex;
                OnChanged?.Invoke(Property.CurrentKeyIndex);
            }
        }

        public int CurrentTransitionIndex
        {
            get => m_CurrentTransitionIndex;
            set
            {
                if (value == m_CurrentTransitionIndex)
                {
                    return;
                }

                m_CurrentTransitionIndex = value;
                m_SequenceViewModel.CurrentTransitionIndex = m_CurrentTransitionIndex;
                OnChanged?.Invoke(Property.CurrentTransitionIndex);
            }
        }

        public bool IsVisible
        {
            get => m_IsVisible;
            set
            {
                if (value == m_IsVisible)
                    return;

                m_IsVisible = value;
                m_SequenceViewModel.IsVisible = m_IsVisible;
                OnChanged?.Invoke(Property.Visibility);
            }
        }

        public bool IsReadOnly
        {
            get => m_IsReadOnly;
            set
            {
                if (value == m_IsReadOnly)
                    return;

                m_IsReadOnly = value;
                OnChanged?.Invoke(Property.IsReadOnly);
            }
        }

        SequenceViewModel m_SequenceViewModel;
        BakingViewModel m_BakingViewModel;
        PlaybackViewModel m_PlaybackViewModel;
        BakedTimelineMappingModel m_BakedTimelineMappingModel;
        PlaybackModel m_PlaybackModel;
        AuthoringModel m_AuthoringModel;
        TimelineModel m_TimelineModel;

        readonly SelectionModel<TimelineModel.SequenceKey> m_KeySelectionModel;
        readonly SelectionModel<TimelineModel.SequenceTransition> m_TransitionSelectionModel;

        float m_CurrentFrame;
        int m_CurrentFrameIndex;
        int m_CurrentKeyIndex;
        int m_CurrentTransitionIndex;
        bool m_IsVisible;
        bool m_IsPlaying;
        bool m_SeekToSelectedKey;
        bool m_IsReadOnly;
        bool m_IsEditingKey;
        bool m_IsEditingTransition;

        public TimelineViewModel(
            AuthoringModel authoringModel,
            TimelineModel timelineModel,
            PlaybackModel playbackModel,
            BakingLogic bakingLogic,
            BakedTimelineMappingModel bakedTimelineMappingModel,
            SelectionModel<TimelineModel.SequenceKey> keySelectionModel,
            SelectionModel<TimelineModel.SequenceTransition> transitionSelectionModel,
            ClipboardService clipboardService,
            InspectorsPanelViewModel inspectorsPanel)
        {
            // Models
            m_AuthoringModel = authoringModel;
            m_TimelineModel = timelineModel;
            m_PlaybackModel = playbackModel;
            m_BakedTimelineMappingModel = bakedTimelineMappingModel;
            m_KeySelectionModel = keySelectionModel;
            m_TransitionSelectionModel = transitionSelectionModel;

            // Views
            m_SequenceViewModel = new SequenceViewModel(timelineModel, keySelectionModel, transitionSelectionModel, clipboardService, inspectorsPanel);
            m_PlaybackViewModel = new PlaybackViewModel(playbackModel, timelineModel, bakedTimelineMappingModel);
            m_BakingViewModel = new BakingViewModel(bakingLogic);

            RegisterEvents();
        }

        void RegisterEvents()
        {
            // Sequencer UI Requests
            m_SequenceViewModel.OnRequestedKeyContextualMenuAction += OnRequestedKeyContextualMenuAction;
            m_SequenceViewModel.OnRequestedKeyToggle += OnSequenceRequestedKeyToggle;
            m_SequenceViewModel.OnRequestedTransitionToggle += OnSequenceRequestedTransitionToggle;
            m_SequenceViewModel.OnRequestedAddKey += OnSequenceRequestedAddKey;
            m_SequenceViewModel.OnRequestedInsertKey += OnSequenceRequestedInsertKey;
            m_SequenceViewModel.OnRequestedSelectKey += OnSequenceRequestedEditKey;
            m_SequenceViewModel.OnRequestedSelectTransition += OnSequenceRequestedSelectTransition;
            m_SequenceViewModel.OnRequestedDeleteSelectedKeys += OnSequenceRequestedDeleteSelectedKeys;
            
            // Playback UI Requests
            m_PlaybackViewModel.OnRequestedInsertKey += OnPlaybackRequestedInsertKey;
            m_PlaybackViewModel.OnRequestedSeekToFrame += OnPlaybackRequestedSeekToFrame;
        }

        TimelineModel.SequenceKey RequestDuplicateKey(int fromIndex, bool toLeft)
        {
            var toIndex = toLeft ? fromIndex : fromIndex + 1;
            return OnRequestedDuplicateKey?.Invoke(fromIndex, toIndex);
        }

        void OnRequestedKeyContextualMenuAction(SequenceKeyContextualMenu.ActionType type, ClipboardService clipboard, SelectionModel<TimelineModel.SequenceKey> selectionModel, SequenceItemViewModel<TimelineModel.SequenceKey> target)
        {
            var oldIndex = m_TimelineModel.IndexOf(target.Target);
            var previouslySelected = selectionModel.IsSelected(target.Target);

            void HandleSelection(TimelineModel.SequenceKey keyToSelect)
            {
                if (previouslySelected)
                {
                    OnRequestedEditKey?.Invoke(keyToSelect);
                }
            }

            void Duplicate(bool toLeft)
            {
                var duplicatedKey = RequestDuplicateKey(oldIndex, toLeft);
                HandleSelection(duplicatedKey);
            }

            void Paste(Func<TimelineModel.SequenceKey> getSequenceKey)
            {
                if (!clipboard.CanPaste(target.Target.Key))
                {
                    return;
                }

                var sequenceKey = getSequenceKey();
                clipboard.Paste(sequenceKey.Key);
                HandleSelection(sequenceKey);
            }

            void Move(bool toLeft)
            {
                var toIndex = toLeft ? oldIndex - 1 : oldIndex + 1;
                OnRequestedMoveKey?.Invoke(oldIndex, toIndex);
                HandleSelection(m_TimelineModel.GetKey(toIndex));
            }

            void Delete()
            {
                OnRequestedDeleteKey?.Invoke(m_TimelineModel.GetKey(oldIndex));
                HandleSelection(m_TimelineModel.GetKey(Mathf.Min(oldIndex, m_TimelineModel.KeyCount - 1)));
            }

            switch (type)
            {
                case SequenceKeyContextualMenu.ActionType.Copy:
                    clipboard.Copy(target.Target.Key);
                    break;

                case SequenceKeyContextualMenu.ActionType.PasteAndReplace:
                    Paste(() => target.Target);
                    break;

                case SequenceKeyContextualMenu.ActionType.PasteLeft:
                    Paste(() => RequestDuplicateKey(oldIndex, toLeft: true));
                    break;

                case SequenceKeyContextualMenu.ActionType.PasteRight:
                    Paste(() => RequestDuplicateKey(oldIndex, toLeft: false));
                    break;

                case SequenceKeyContextualMenu.ActionType.DuplicateLeft:
                    Duplicate(toLeft: true);
                    break;

                case SequenceKeyContextualMenu.ActionType.DuplicateRight:
                    Duplicate(toLeft: false);
                    break;

                case SequenceKeyContextualMenu.ActionType.MoveLeft:
                    Move(toLeft: true);
                    break;

                case SequenceKeyContextualMenu.ActionType.MoveRight:
                    Move(toLeft: false);
                    break;

                case SequenceKeyContextualMenu.ActionType.Delete:
                    Delete();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        void UpdateCurrentIndices()
        {
            if (m_BakedTimelineMappingModel.TryGetKeyIndex(m_CurrentFrameIndex, out var keyIndex))
            {
                CurrentKeyIndex = keyIndex;
            }
            else
            {
                CurrentKeyIndex = -1;
            }

            if (m_BakedTimelineMappingModel.TryGetTransitionIndex(m_CurrentFrameIndex, out var transitionIndex))
            {
                CurrentTransitionIndex = transitionIndex;
            }
            else
            {
                CurrentTransitionIndex = -1;
            }
        }

        // -------------------------------------------
        // Sequencer Requests Events Handlers
        // -------------------------------------------
        
        void OnSequenceRequestedKeyToggle(SequenceItemViewModel<TimelineModel.SequenceKey> key)
        {
            OnRequestedKeyToggle?.Invoke(key.Target);
        }

        void OnSequenceRequestedTransitionToggle(SequenceItemViewModel<TimelineModel.SequenceTransition> transition)
        {
            OnRequestedTransitionToggle?.Invoke(transition.Target);
        }

        void OnSequenceRequestedSelectTransition(TimelineModel.SequenceTransition transition)
        {
            OnRequestedSelectTransition?.Invoke(transition);
        }

        void OnSequenceRequestedEditKey(TimelineModel.SequenceKey key)
        {
            OnRequestedEditKey?.Invoke(key);
        }

        void OnSequenceRequestedDeleteSelectedKeys()
        {
            OnRequestedDeleteSelectedKeys?.Invoke();
        }

        void OnSequenceRequestedAddKey()
        {
            OnRequestedAddKey?.Invoke();
        }

        void OnSequenceRequestedInsertKey(int keyIndex, float transitionProgress)
        {
            OnRequestedInsertKey?.Invoke(keyIndex, transitionProgress);
        }
        
        void OnSequenceRequestedEditTransition(TimelineModel.SequenceTransition transition)
        {
            OnRequestedEditTransition?.Invoke(transition);
        }
        
        void OnSequenceRequestedSeekToKey(TimelineModel.SequenceKey key)
        {
            OnRequestedSeekToKey?.Invoke(key);
        }
        
        void OnSequenceRequestedSeekToTransition(TimelineModel.SequenceTransition transition)
        {
            OnRequestedSeekToTransition?.Invoke(transition);
        }

        // -------------------------------------------
        // Playback Requests Handlers
        // -------------------------------------------

        void OnPlaybackRequestedSeekToFrame(float frame)
        {
            OnRequestedSeekToFrame?.Invoke(frame);
        }
        
        void OnPlaybackRequestedInsertKey(int keyIndex, float transitionProgress)
        {
            OnRequestedInsertKeyWithEffectorRecovery?.Invoke(m_CurrentFrameIndex, keyIndex, transitionProgress);
        }
    }
}
