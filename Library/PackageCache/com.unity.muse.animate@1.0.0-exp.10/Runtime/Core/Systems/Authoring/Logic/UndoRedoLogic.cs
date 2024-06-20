using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.Muse.Animate
{
    sealed class UndoRedoLogic
    {
        public static UndoRedoLogic Instance { get; } = new();
        public bool CanUndo => m_UndoRedoStack.CanUndo;
        public bool CanRedo => m_UndoRedoStack.CanRedo;

        public event Action StackStateChanged;

        readonly UndoRedoStack m_UndoRedoStack = new();
        readonly List<ITrackedObject> m_TrackedObjects = new();
        bool m_Primed;
        bool m_HasPendingChanges;

        UndoRedoLogic() { }

        public void SetInitialCheckpoint()
        {
            Clear();
            SetCheckpoint();
        }

        /// <summary>
        /// To prevent an excess of undo steps, we only push a change to the undo stack if the user
        /// has interacted with the UI. This method primes the undo stack to accept a change, or push
        /// a change if there is one pending.
        /// </summary>
        public void Prime()
        {
            m_Primed = true;
            if (m_HasPendingChanges)
            {
                Push();
            }
        }

        /// <summary>
        /// Push a change to the undo stack. If we are primed, this will push the change to the stack.
        /// If we are not primed, the next call to <see cref="Prime"/> will push the change to the stack.
        /// </summary>
        /// <remarks>
        /// The state change may be finalized either before or after the user action is triggered, so we need this bit of
        /// complex logic to handle both cases. There might be a better way to do this.
        /// </remarks>
        public void Push()
        {
            m_HasPendingChanges = true;

            if (!m_Primed) return;

            m_Primed = false;
            m_HasPendingChanges = false;

            var undoActions = m_TrackedObjects.Select(o => o.GetUndoAction()).ToList();
            var redoActions = m_TrackedObjects.Select(o => o.GetRedoAction()).ToList();
            var command = new UndoRedoStack.Command(
                undo: () =>
                {
                    foreach (var undoAction in undoActions)
                    {
                        undoAction();
                    }
                },
                execute: () =>
                {
                    foreach (var redoAction in redoActions)
                    {
                        redoAction();
                    }
                }
            );

            SetCheckpoint();

            m_UndoRedoStack.Push(command);
            m_Primed = false;
            StackStateChanged?.Invoke();
        }

        void SetCheckpoint()
        {
            foreach (var trackedObject in m_TrackedObjects)
            {
                trackedObject.SetCheckpoint();
            }
        }

        public void Undo()
        {
            if (!CanUndo) return;

            m_UndoRedoStack.Undo();

            foreach (var trackedObject in m_TrackedObjects)
            {
                trackedObject.NotifyChanged();
            }

            m_Primed = false;
            m_HasPendingChanges = false;

            SetCheckpoint();
            StackStateChanged?.Invoke();
        }

        public void Redo()
        {
            if (!CanRedo) return;

            m_UndoRedoStack.Redo();

            foreach (var trackedObject in m_TrackedObjects)
            {
                trackedObject.NotifyChanged();
            }

            m_Primed = false;
            m_HasPendingChanges = false;

            SetCheckpoint();
            StackStateChanged?.Invoke();
        }

        /// <summary>
        /// Clear the undo/redo stack.
        /// </summary>
        public void Clear()
        {
            m_Primed = false;
            m_HasPendingChanges = false;
            m_UndoRedoStack.Clear();
            StackStateChanged?.Invoke();
        }

        /// <summary>
        /// Clear the undo/redo stack and forget all tracked objects.
        /// </summary>
        public void Reset()
        {
            Clear();
            m_TrackedObjects.Clear();
        }

        public void TrackModel<T>(T model, Action<T> onChanged = null) where T : ICopyable<T>
        {
            m_TrackedObjects.Add(new TrackedCopyableModel<T>(model, onChanged));
        }
    }
}
