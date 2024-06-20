using System.Diagnostics;
using UnityEngine.UIElements;

namespace Unity.Muse.Animate
{
    class LibraryItemUI: VisualElement
    {
        const string k_BaseUssClassName = "deeppose-library-item";
        
        public LibraryItemUIModel Model => m_Model;
        
        string m_UssClassNameSelected;
        string m_UssClassNameHighlighted;
        string m_UssClassNameEditing;
        
        protected LibraryItemUIModel m_Model;

        protected internal LibraryItemUI(string ussClassName = null, string ussClassNameSelected = null,
            string ussClassNameHighlighted = null, string ussClassNameEditing = null)
        {
            m_UssClassNameSelected = ussClassNameSelected;
            m_UssClassNameHighlighted = ussClassNameHighlighted;
            m_UssClassNameEditing = ussClassNameEditing;
            
            if (!string.IsNullOrWhiteSpace(k_BaseUssClassName))
            {
                AddToClassList(k_BaseUssClassName);
            }
            
            if (!string.IsNullOrWhiteSpace(ussClassName))
            {
                AddToClassList(ussClassName);
            }
            
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);
            
            focusable = true;
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            // TODO: we should stop using RegisterCallback to handle user interactions.
            // These are too low level, and don't handle multiple input modalities.
            // The recommended method is to use Manipulators, such as Clickable or
            // ContextualMenuManipulator. However, the latter doesn't seem to be working in the
            // runtime.
            RegisterCallback<PointerUpEvent>(OnClicked);
        }

        void OnDetachedFromPanel(DetachFromPanelEvent evt)
        {
            UnregisterCallback<PointerUpEvent>(OnClicked);
        }

        protected void SetModel(LibraryItemUIModel viewModel)
        {
            DevLogger.LogSeverity(TraceLevel.Verbose, $"LibraryItemUI -> SetModel({viewModel})");
            
            if (m_Model != null)
                UnregisterModel();

            m_Model = viewModel;

            RegisterModel();
            Update();
        }

        protected virtual void Update()
        {
            DevLogger.LogSeverity(TraceLevel.Verbose, $"LibraryItemUI -> Update()");
            
            style.display = m_Model.IsVisible ? DisplayStyle.Flex : DisplayStyle.None;

            if (m_Model.IsEditing && !string.IsNullOrWhiteSpace(m_UssClassNameEditing))
            {
                AddToClassList(m_UssClassNameEditing);
                RemoveFromClassList(m_UssClassNameHighlighted);
                RemoveFromClassList(m_UssClassNameSelected);
            }
            else if (m_Model.IsHighlighted && !string.IsNullOrWhiteSpace(m_UssClassNameHighlighted))
            {
                AddToClassList(m_UssClassNameHighlighted);
                RemoveFromClassList(m_UssClassNameEditing);
                RemoveFromClassList(m_UssClassNameSelected);
            }
            else if (m_Model.IsSelected && !string.IsNullOrWhiteSpace(m_UssClassNameSelected))
            {
                AddToClassList(m_UssClassNameSelected);
                RemoveFromClassList(m_UssClassNameEditing);
                RemoveFromClassList(m_UssClassNameHighlighted);
            }
            else
            {
                RemoveFromClassList(m_UssClassNameHighlighted);
                RemoveFromClassList(m_UssClassNameEditing);
                RemoveFromClassList(m_UssClassNameSelected);
            }
        }

        void RegisterModel()
        {
            if (m_Model == null)
            {
                DevLogger.LogError($"LibraryItemUI -> RegisterModel() failed, m_Model is null");
                return;
            }
            
            DevLogger.LogSeverity(TraceLevel.Verbose, $"LibraryItemUI -> RegisterModel()");
            
            m_Model.OnChanged += OnModelChanged;
        }

        void UnregisterModel()
        {
            if (m_Model == null)
            {
                DevLogger.LogError($"LibraryItemUI -> UnregisterModel() failed, m_Model is null");
                return;
            }

            DevLogger.LogSeverity(TraceLevel.Verbose, $"LibraryItemUI -> UnregisterModel()");

            m_Model.OnChanged -= OnModelChanged;
            m_Model = null;
        }

        void OnModelChanged(LibraryItemUIModel item)
        {
            DevLogger.LogSeverity(TraceLevel.Verbose, $"LibraryItemUI -> OnModelChanged({item})");
            
            Update();
        }

        void OnClicked(IPointerEvent evt)
        {
            DevLogger.LogSeverity(TraceLevel.Verbose, $"LibraryItemUI -> OnClicked({evt.button})");
            m_Model?.Clicked(evt.button);
            
            // TODO: Right-clicking does not change the selection, so show some visual indicator that the item has focus
        }
    }
}
