using System;
using UnityEngine;

namespace Unity.Muse.Animate
{
    /// <summary>
    /// A data structure that holds a stack of commands that can be undone and redone.
    /// </summary>
    class UndoRedoStack
    {
        public class Command
        {
            public Command(Action execute, Action undo)
            {
                Execute = execute;
                Undo = undo;
            }

            public Action Execute { get; }
            public Action Undo { get; }
            public int Id { get; } = s_NextId++;
            
            static int s_NextId = 0;
        }

        CircularStack<Command> m_UndoHistory;
        CircularStack<Command> m_RedoHistory;

        public bool CanUndo => m_UndoHistory.Count > 0;
        public bool CanRedo => m_RedoHistory.Count > 0;

        public UndoRedoStack(int historyLength = 25)
        {
            m_UndoHistory = new CircularStack<Command>(historyLength);
            m_RedoHistory = new CircularStack<Command>(historyLength);
        }

        public void Undo()
        {
            if (!CanUndo)
            {
                return;
            }

            var command = m_UndoHistory.Pop();
            m_RedoHistory.Push(command);
            command.Undo();
        }

        public void Redo()
        {
            if (!CanRedo)
            {
                return;
            }

            var command = m_RedoHistory.Pop();
            m_UndoHistory.Push(command);
            command.Execute();
        }

        public void Push(Command command)
        {
            m_RedoHistory.Clear();
            m_UndoHistory.Push(command);
        }
        
        public void Clear()
        {
            m_UndoHistory.Clear();
            m_RedoHistory.Clear();
        }
    }
}
