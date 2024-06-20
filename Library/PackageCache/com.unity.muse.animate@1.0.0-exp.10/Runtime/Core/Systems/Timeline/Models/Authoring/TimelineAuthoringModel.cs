using System;
using UnityEngine;

namespace Unity.Muse.Animate
{
    class TimelineAuthoringModel
    {
        // Events meant to be used by both UI Models and Author.Timeline States
        public event Action OnModeChanged;

        // Events meant to be used by the UI Models
        public event Action OnChanged;

        // Events meant to be used by Author.Timeline States
        // - Image to Pose
        public event Action OnRequestedPoseEstimation;

        // - Timeline - Preview
        public event Action OnRequestedPreview;
        
        // - Timeline - Playback
        public event Action<float> OnRequestedSeekToFrame;
        
        // - Timeline - Keys
        public event Action OnRequestedAddKey;
        public event Action<int> OnRequestedSelectKeyIndex;
        public event Action<TimelineModel.SequenceKey> OnRequestedSelectKey;
        public event Action<TimelineModel.SequenceKey> OnRequestedSeekToKey;
        public event Action<int> OnRequestedEditKeyIndex;
        public event Action<TimelineModel.SequenceKey> OnRequestedEditKey;
        public event Action<TimelineModel.SequenceKey> OnRequestedDeleteKey;
        public event Action<TimelineModel.SequenceKey> OnRequestedPreviewKey;
        public event Action OnRequestedDeleteSelectedKeys;
        public delegate void RequestedInsertKeyWithEffectorRecovery(int bakedFrameIndex, int keyIndex, float progress, out TimelineModel.SequenceKey key);
        public event RequestedInsertKeyWithEffectorRecovery OnRequestedInsertKeyWithEffectorRecovery;
        public delegate void RequestedInsertKey(int keyIndex, out TimelineModel.SequenceKey key);
        public event RequestedInsertKey OnRequestedInsertKey;
        public delegate void RequestedMoveKey(int fromIndex, int toIndex);
        public event RequestedMoveKey OnRequestedMoveKey;
        public delegate TimelineModel.SequenceKey RequestedDuplicateKey(int fromIndex, int toIndex);
        public event RequestedDuplicateKey OnRequestedDuplicateKey;
        
        // - Timeline - Transitions
        public event Action<TimelineModel.SequenceTransition> OnRequestedSelectTransition;
        public event Action<TimelineModel.SequenceTransition> OnRequestedSeekToTransition;
        public event Action<TimelineModel.SequenceTransition> OnRequestedEditTransition;
        public event Action<TimelineModel.SequenceTransition> OnRequestedPreviewTransition;

        public event Action OnRequestSaveTimeline;
        
        // Timeline - Entities
        public event Action OnRequestedDeleteSelectedEntities;

        // - Posing interactions
        public event Action OnRequestedCopyPose;
        public event Action OnRequestedDisableSelectedEffectors;

        /// <summary>
        /// Authoring modes
        /// </summary>
        public enum AuthoringMode
        {
            /// <summary>
            /// No authoring mode set
            /// </summary>
            Unknown,

            /// <summary>
            /// Previewing final animation
            /// </summary>
            Preview,

            /// <summary>
            /// Authoring a key
            /// </summary>
            EditKey,
            
            /// <summary>
            /// Authoring a transition
            /// </summary>
            EditTransition
        }

        public enum SelectionType
        {
            Entity,
            Effector,
            SequenceKey,
            SequenceTransition
        }

        public SelectionType LastSelectionType { get; set; }
        
        public AuthoringMode Mode
        {
            get => m_Mode;
            set
            {
                if (m_Mode == value)
                    return;

                m_Mode = value;
                OnModeChanged?.Invoke();
                OnChanged?.Invoke();
            }
        }
        
        public bool CanCopyPose
        {
            get => m_CanCopyPose;
            set
            {
                if (m_CanCopyPose == value)
                    return;

                m_CanCopyPose = value;
                OnChanged?.Invoke();
            }
        }

        public bool CanDeleteSelectedEntities
        {
            get => m_CanDeleteSelectedEntities;
            set
            {
                if (m_CanDeleteSelectedEntities == value)
                    return;

                m_CanDeleteSelectedEntities = value;
                OnChanged?.Invoke();
            }
        }

        public bool CanDisableSelectedEffectors
        {
            get => m_CanDisableSelectedEffectors;
            set
            {
                if (m_CanDisableSelectedEffectors == value)
                    return;

                m_CanDisableSelectedEffectors = value;
                OnChanged?.Invoke();
            }
        }

        public bool CanEstimatePose
        {
            get => m_CanEstimatePose;
            set
            {
                if (m_CanEstimatePose == value)
                    return;

                m_CanEstimatePose = value;
                OnChanged?.Invoke();
            }
        }
        
        /// <summary>
        /// Gets or sets whether the timeline has been modified.
        /// </summary>
        public bool IsDirty { get; internal set; }

        AuthoringMode m_Mode = AuthoringMode.Unknown;

        bool m_CanCopyPose;
        bool m_CanEstimatePose;
        bool m_CanDeleteSelectedEntities;
        bool m_CanDisableSelectedEffectors;

        public void RequestCopyPose()
        {
            OnRequestedCopyPose?.Invoke();
        }

        public void RequestPoseEstimation()
        {
            OnRequestedPoseEstimation?.Invoke();
        }

        public void RequestDeleteSelectedEntities()
        {
            OnRequestedDeleteSelectedEntities?.Invoke();
        }
        
        public void RequestDisableSelectedEffectors()
        {
            UndoRedoLogic.Instance.Prime();
            OnRequestedDisableSelectedEffectors?.Invoke();
        }
        
        public void RequestPreview()
        {
            OnRequestedPreview?.Invoke();
        }

        // Keys Requests
        public void RequestDeleteKey(TimelineModel.SequenceKey key)
        {
            OnRequestedDeleteKey?.Invoke(key);
        }
        
        public void RequestMoveKey(int fromIndex, int toIndex)
        {
            OnRequestedMoveKey?.Invoke(fromIndex, toIndex);
        }
        
        public void RequestSeekToFrame(float frame)
        {
            OnRequestedSeekToFrame?.Invoke(frame);
        }
        
        public TimelineModel.SequenceKey RequestDuplicateKey(int fromIndex, int toIndex)
        {
            return OnRequestedDuplicateKey?.Invoke(fromIndex, toIndex);
        }
        
        public void RequestPreviewKey(TimelineModel.SequenceKey key)
        {
            OnRequestedPreviewKey?.Invoke(key);
        }

        public void RequestEditKey(TimelineModel.SequenceKey key)
        {
            OnRequestedEditKey?.Invoke(key);
        }

        public void RequestEditKeyIndex(int index)
        {
            OnRequestedEditKeyIndex?.Invoke(index);
        }

        public void RequestAddKey()
        {
            OnRequestedAddKey?.Invoke();
        }
        
        public void RequestInsertKeyWithEffectorRecovery(int bakedFrameIndex, int keyIndex, float progress, out TimelineModel.SequenceKey key)
        {
            key = null;
            OnRequestedInsertKeyWithEffectorRecovery?.Invoke(bakedFrameIndex, keyIndex, progress, out key);
        }

        public void RequestInsertKey(int keyIndex, out TimelineModel.SequenceKey key)
        {
            key = null;
            OnRequestedInsertKey?.Invoke(keyIndex, out key);
        }

        public void RequestSelectKey(TimelineModel.SequenceKey key)
        {
            OnRequestedSelectKey?.Invoke(key);
        }

        public void RequestSeekToKey(TimelineModel.SequenceKey key)
        {
            OnRequestedSeekToKey?.Invoke(key);
        }

        public void RequestSelectKeyIndex(int index)
        {
            OnRequestedSelectKeyIndex?.Invoke(index);
        }

        public void RequestDeleteSelectedKeys()
        {
            OnRequestedDeleteSelectedKeys?.Invoke();
        }
        
        // Transitions
        
        public void RequestEditTransition(TimelineModel.SequenceTransition transition)
        {
            OnRequestedEditTransition?.Invoke(transition);
        }
        
        public void RequestPreviewTransition(TimelineModel.SequenceTransition key)
        {
            OnRequestedPreviewTransition?.Invoke(key);
        }

        public void RequestSelectTransition(TimelineModel.SequenceTransition transition)
        {
            OnRequestedSelectTransition?.Invoke(transition);
        }
        
        public void RequestSeekToTransition(TimelineModel.SequenceTransition transition)
        {
            OnRequestedSeekToTransition?.Invoke(transition);
        }

        public void RequestSaveTimeline()
        {
            OnRequestSaveTimeline?.Invoke();
        }
    }
}
