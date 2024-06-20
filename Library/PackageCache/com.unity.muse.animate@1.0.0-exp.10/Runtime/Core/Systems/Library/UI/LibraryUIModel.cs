using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Muse.Animate
{
    abstract class LibraryUIModel
    {
        public delegate void RequestedScrollToItem(LibraryModel library, LibraryItemUIModel item);
        public delegate void RequestedItemContextualMenuAction(LibraryModel library, LibraryItemContextualMenu.ActionType type, ClipboardService clipboard, SelectionModel<LibraryItemModel> selectionModel, LibraryItemUIModel target);
        public delegate void RequestedItemContextualMenu(LibraryModel library, LibraryItemUIModel item);
        public delegate void RequestedAddItem(LibraryModel library);
        public delegate void RequestedDeleteItem(LibraryModel library, LibraryItemModel item);
        public delegate void RequestedEditItem(LibraryModel library, LibraryItemModel item);
        public delegate void RequestedSelectItem(LibraryModel library, SelectionModel<LibraryItemModel> selectionModel, LibraryItemModel item);
        public delegate void RequestedDuplicateItem(LibraryModel library, LibraryItemModel item);
        public delegate void RequestedExportItem(LibraryModel library, LibraryItemModel item);
        public delegate void RequestedDeleteSelectedItems(LibraryModel library, SelectionModel<LibraryItemModel> selectionModel);

        public event Action OnItemsChanged;
        public event Action OnSelectionChanged;
        public event RequestedScrollToItem OnRequestedScrollToItem;
        public event RequestedItemContextualMenu OnRequestedItemContextualMenu;
        public event RequestedAddItem OnRequestedAddItem;
        public event RequestedSelectItem OnRequestedSelectItem;
        public event RequestedEditItem OnRequestedEditItem;
        public event RequestedDuplicateItem OnRequestedDuplicateItem;
        public event RequestedExportItem OnRequestedExportItem;
        public event RequestedDeleteItem OnRequestedDeleteItem;
        public event RequestedDeleteSelectedItems OnRequestedDeleteSelectedItems;

        public event Action<string> OnRequestedSetPrompt;

        public IReadOnlyList<LibraryItemUIModel> Items => m_Items;
        public LibraryItemModel SelectedItem => m_SelectionModel.HasSelection ? m_SelectionModel.GetSelection(0) : null;
        public ClipboardService ClipboardService => m_ClipboardService;
        public LibraryModel Library => m_Library;
        public SelectionModel<LibraryItemModel> SelectionModel => m_SelectionModel;
        protected AuthoringModel AuthoringModel => m_AuthoringModel;

        readonly ClipboardService m_ClipboardService;
        readonly SelectionModel<LibraryItemModel> m_SelectionModel;
        readonly AuthoringModel m_AuthoringModel;
        
        List<LibraryItemUIModel> m_Items = new();
        LibraryModel m_Library;

        protected LibraryUIModel(AuthoringModel authoringModel, SelectionModel<LibraryItemModel> selectionModel,
            ClipboardService clipboardService)
        {
            m_AuthoringModel = authoringModel;
            m_SelectionModel = selectionModel;
            m_SelectionModel.OnSelectionChanged += OnSelectionModelChanged;
            m_ClipboardService = clipboardService;
        }

        protected void SetTarget(LibraryModel target)
        {
            Log($"SetTarget("+target+")");
            m_Library = target;
            m_Library.OnChanged += OnLibraryChanged;
            UpdateItems();
        }

        void OnSelectionModelChanged(SelectionModel<LibraryItemModel> model)
        {
            Log($"OnSelectionModelChanged({model})");
            UpdateItems();
            OnSelectionChanged?.Invoke();
        }

        void OnLibraryChanged()
        {
            Log($"OnLibraryChanged()");
            UpdateItems();
        }

        void HideItem(int index)
        {
            Log($"HideItem({index})");
            m_Items[index].IsVisible = false;
        }

        protected void RegisterItem(LibraryItemUIModel item)
        {
            Log($"RegisterItem("+item+")");
            m_Items.Add(item);
            item.OnLeftClicked += OnItemLeftClicked;
            item.OnRightClicked += OnItemRightClicked;
        }

        protected abstract void CreateItem(int index);

        void UpdateItems()
        {
            Log($"UpdateItems()");
            
            if (m_Library == null)
            {
                Debug.LogError("UpdateItems() failed, no library");
                
                // Hide all items
                for (var i = 0; i < m_Items.Count; i++)
                {
                    HideItem(i);
                }

                return;
            }
            
            // Add new items
            for (var i = m_Items.Count; i < m_Library.ItemCount; i++)
            {
                CreateItem(i);
            }

            // Update or Hide extra items.
            for (var i = 0; i < m_Items.Count; i++)
            {
                if (i < m_Library.ItemCount)
                {
                    m_Items[i].SetTarget(m_Library.Items[i]);
                    UpdateItem(m_Items[i]);
                }
                else
                {
                    HideItem(i);
                }
            }

            OnItemsChanged?.Invoke();
        }

        void UpdateItem(LibraryItemUIModel item)
        {
            Log($"UpdateItem({item})");
            
            item.IsVisible = true;
            item.IsSelected = IsSelected(item);
            item.IsEditing = IsEditing(item);
        }

        bool IsEditing(LibraryItemUIModel item)
        {
            return IsSelected(item);
        }

        bool IsSelected(LibraryItemUIModel key)
        {
            var selected = m_SelectionModel.IsSelected(key.Target);
            Log($"IsSelected({key}) -> {selected}");
            return selected;
        }

        public void RequestAddItem()
        {
            Log($"RequestAddItem()");
            OnRequestedAddItem?.Invoke(m_Library);
        }

        public void RequestDeleteSelectedItems()
        {
            Log($"RequestDeleteSelectedItems()");
            OnRequestedDeleteSelectedItems?.Invoke(m_Library, m_SelectionModel);
        }

        void RequestSelectItem(LibraryItemModel item)
        {
            Log($"RequestSelectItem({item})");
            OnRequestedSelectItem?.Invoke(m_Library, m_SelectionModel, item);
        }
        
        public void RequestScrollToItem(LibraryItemModel item)
        {
            Log($"RequestScrollToItem({item})");
            for (int i = 0; i < m_Items.Count; i++)
            {
                if (m_Items[i].Target == item)
                {
                    OnRequestedScrollToItem?.Invoke(m_Library, m_Items[i]);
                    return;
                }
            }
        }

        void RequestEditItem(LibraryItemModel item)
        {
            Log($"RequestEditItem({item})");
            OnRequestedEditItem?.Invoke(m_Library, item);
        }
        
        void RequestItemContextualMenu(LibraryItemUIModel item)
        {
            Log($"RequestItemContextualMenu({item})");
            OnRequestedItemContextualMenu?.Invoke(m_Library, item);
        }

        public void RequestItemContextMenuAction(LibraryItemContextualMenu.ActionType type, ClipboardService clipboard, SelectionModel<LibraryItemModel> selectionModel, LibraryItemUIModel target)
        {
            Log($"RequestItemContextMenuAction({type}, {clipboard}, {selectionModel}, {target})");
            switch (type)
            {
                case LibraryItemContextualMenu.ActionType.Delete:
                    OnRequestedDeleteItem?.Invoke(m_Library, target.Target);
                    break;
                case LibraryItemContextualMenu.ActionType.Export:
                    OnRequestedExportItem?.Invoke(m_Library, target.Target);
                    break;
                case LibraryItemContextualMenu.ActionType.Duplicate:
                    OnRequestedDuplicateItem?.Invoke(m_Library, target.Target);
                    break;
                case LibraryItemContextualMenu.ActionType.UsePrompt when target.Target is TextToMotionTake take:
                    OnRequestedSetPrompt?.Invoke(take.Prompt);
                    break;
            }
        }
        
        void OnItemLeftClicked(LibraryItemUIModel item)
        {
            Log($"OnItemLeftClicked({item})");
            
            // If already selected, switch authoring mode between posing and previewing
            if (item.IsSelected)
            {
                return;
            }

            RequestSelectItem(item.Target);
            RequestEditItem(item.Target);
        }

        void OnItemRightClicked(LibraryItemUIModel item)
        {
            Log($"OnItemRightClicked({item})");
            
            RequestSelectItem(item.Target);
            RequestEditItem(item.Target);
            RequestItemContextualMenu(item);
        } 
        
        #region Debugging
        
        void Log(string msg)
        {
            if (!ApplicationConstants.DebugLibraryUI)
                return;

            Debug.Log(GetType().Name + " -> " + msg);
        }
        
        #endregion
    }
}
