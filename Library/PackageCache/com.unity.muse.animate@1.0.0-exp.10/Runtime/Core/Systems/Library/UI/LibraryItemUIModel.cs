using System;

namespace Unity.Muse.Animate
{
    abstract class LibraryItemUIModel
    {
        public Action<LibraryItemUIModel> OnLeftClicked;
        public Action<LibraryItemUIModel> OnRightClicked;
        public Action<LibraryItemUIModel> OnChanged;

        [Flags]
        enum ItemState
        {
            Visible = 1,
            Selected = 2,
            Highlighted = 4,
            Editing = 8
        }

        public LibraryItemModel Target
        {
            get => m_Target;
            set => SetTarget(value);
        }

        public bool IsVisible
        {
            get => m_State.HasFlag(ItemState.Visible);
            set => SetState(ItemState.Visible, value);
        }

        public bool IsSelected
        {
            get => m_State.HasFlag(ItemState.Selected);
            set => SetState(ItemState.Selected, value);
        }

        public bool IsHighlighted
        {
            get => m_State.HasFlag(ItemState.Highlighted);
            set => SetState(ItemState.Highlighted, value);
        }

        public bool IsEditing
        {
            get => m_State.HasFlag(ItemState.Editing);
            set => SetState(ItemState.Editing, value);
        }

        LibraryItemModel m_Target;
        ItemState m_State = ItemState.Visible;

        void SetState(ItemState state, bool value)
        {
            if (value == m_State.HasFlag(state))
                return;

            if (value)
            {
                m_State |= state;
            }
            else
            {
                m_State &= ~state;
            }

            InvokeChanged();
        }

        internal void SetTarget(LibraryItemModel value)
        {
            if (m_Target != null)
                UnregisterCallbacks();
            
            m_Target = value;

            if (m_Target != null)
                RegisterCallbacks();

            InvokeChanged();
        }
        
        protected virtual void RegisterCallbacks()
        {
            // Override to register/unregister to the item's specific models.
            // See SequenceKeyViewModel.cs for an example
            m_Target.OnItemChanged += OnItemChanged;
        }
        
        protected virtual void UnregisterCallbacks()
        {
            // Override to register/unregister to the item's specific models.
            // See SequenceKeyViewModel.cs for an example
            m_Target.OnItemChanged -= OnItemChanged;
        }

        public void Clicked(int button = 0)
        {
            if (button == 0)
            {
                OnLeftClicked?.Invoke(this);
                return;
            }
            
            OnRightClicked?.Invoke(this);
        }

        protected void InvokeChanged()
        {
            OnChanged?.Invoke(this);
        }
        
        void OnItemChanged(ILibraryItem.Property property)
        {
            InvokeChanged();
        }
    }
}
