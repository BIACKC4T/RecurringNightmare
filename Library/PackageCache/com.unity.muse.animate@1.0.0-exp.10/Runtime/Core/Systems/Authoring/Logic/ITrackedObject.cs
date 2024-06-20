using System;

namespace Unity.Muse.Animate
{
    interface ITrackedObject
    {
        void SetCheckpoint();
        void NotifyChanged();

        /// <summary>
        /// The action returned by this method reverts the tracked object to the state it had
        /// when <see cref="SetCheckpoint"/> was called.
        /// </summary>
        public Action GetUndoAction();
        /// <summary>
        /// The action returned by this method reverts the tracked object to the current state.
        /// </summary>
        public Action GetRedoAction();
    }
    
    class TrackedCopyableModel<T>: ITrackedObject where T : ICopyable<T>
    {
        readonly T m_TrackedModel;
        T m_PreviousState;
        readonly Action<T> m_OnChanged;
        
        public TrackedCopyableModel(T model, Action<T> onChanged)
        {
            m_TrackedModel = model;
            m_OnChanged = onChanged;
            SetCheckpoint();
        }
        
        public void SetCheckpoint()
        {
            if (m_PreviousState == null)
            {
                m_PreviousState = m_TrackedModel.Clone();
            }
            else
            {
                m_TrackedModel.CopyTo(m_PreviousState);
            }
        }
        
        public void NotifyChanged()
        {
            m_OnChanged?.Invoke(m_TrackedModel);
        }

        public Action GetUndoAction()
        {
            var previousCopy = m_PreviousState.Clone();
            return () => previousCopy.CopyTo(m_TrackedModel);
        }

        public Action GetRedoAction()
        {
            var currentCopy = m_TrackedModel.Clone();
            return () => currentCopy.CopyTo(m_TrackedModel);
        }
    }
}
