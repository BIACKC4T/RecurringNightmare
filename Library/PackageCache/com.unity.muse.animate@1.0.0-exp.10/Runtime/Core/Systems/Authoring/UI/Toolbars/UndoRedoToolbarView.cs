using UnityEngine;
using UnityEngine.UIElements;
using Unity.Muse.AppUI.UI;

namespace Unity.Muse.Animate
{
    class UndoRedoToolbarView : UITemplateContainer, IUITemplate
    {
        public const string defaultName = "undo-redo-toolbar";
        
        const string k_UndoButtonName = "undo";
        const string k_RedoButtonName = "redo";

        ActionButton m_UndoButton;
        ActionButton m_RedoButton;
        
        bool m_IsVisible;
        bool m_IsDisabled;

        public bool IsVisible
        {
            get => m_IsVisible;
            set
            {
                m_IsVisible = value;
                Update();
            }
        }
        
        public bool IsDisabled
        {
            get => m_IsDisabled;
            set
            {
                m_IsDisabled = value;
                Update();
            }
        }
        
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public new class UxmlFactory : UxmlFactory<UndoRedoToolbarView, UxmlTraits> { }

        public UndoRedoToolbarView()
            : base("deeppose-toolbar") { }

        public void FindComponents()
        {
            m_UndoButton = this.Q<ActionButton>(k_UndoButtonName);
            m_RedoButton = this.Q<ActionButton>(k_RedoButtonName);
        }

        public void RegisterComponents()
        {
            m_UndoButton.clicked += OnUndoClicked;
            m_RedoButton.clicked += OnRedoClicked;

            UndoRedoLogic.Instance.StackStateChanged += Update;
        }

        public void UnregisterComponents()
        {
            m_UndoButton.clicked -= OnUndoClicked;
            m_RedoButton.clicked -= OnRedoClicked;

            UndoRedoLogic.Instance.StackStateChanged -= Update;
        }

        public void Update()
        {
            if (!m_IsVisible)
            {
                parent.style.display = DisplayStyle.None;
                return;
            }
            
            parent.style.display = DisplayStyle.Flex;
            
            m_UndoButton.SetEnabled(!m_IsDisabled && UndoRedoLogic.Instance.CanUndo);
            m_RedoButton.SetEnabled(!m_IsDisabled && UndoRedoLogic.Instance.CanRedo);
        }

        static void OnUndoClicked()
        {
            UndoRedoLogic.Instance.Undo();
        }

        static void OnRedoClicked()
        {
            UndoRedoLogic.Instance.Redo();
        }
    }
}
