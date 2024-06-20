using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Muse.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;
using AppUI = Unity.Muse.AppUI.UI;

namespace Unity.Muse.Animate
{
    class LibraryUI : UITemplateContainer, IUITemplate
    {
        const string k_UssClassName = "deeppose-library";
        
        static readonly string titleClassName = $"{k_UssClassName}__title";
        static readonly string gridviewUssClassName = k_UssClassName + "__gridview";
        static readonly string thumbnailSizeSliderUssClassName = k_UssClassName + "__thumbnail-slider";
        static readonly string contextMenuAnchorUssClassName = k_UssClassName + "__context-menu-anchor";
        static readonly string exportButtonUssClassName = k_UssClassName + "__export-button";

        const float k_DefaultThumbnailSize = 110;

        LibraryUIModel m_Model;
        LibraryItemContextualMenu m_LibraryItemContextualMenu;

        GridView m_GridView;
        
        bool m_Inited;

        SliderFloat m_ThumbnailSizeSlider;
        SearchBar m_SearchBar;
        VisualElement m_ContextMenuAnchor;
        Text m_Title;

        string m_CurrentMode;
        int m_CountPerRow = 2;

        VisualElement m_VerticalScrollerDragContainer;
        public VisualElement content;

        AppUI.UI.Button m_ExportButton;
        
        List<LibraryItemUI> m_Items = new();
        List<LibraryItemUI> m_FilteredItemsList = new();
        
        float m_ResultsTraySize = 1f;

        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public new class UxmlFactory : UxmlFactory<LibraryUI, UxmlTraits> { }

        public LibraryUI()
            : base(k_UssClassName) { }

        public void InitComponents()
        {
            m_LibraryItemContextualMenu = new LibraryItemContextualMenu();
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        public void FindComponents()
        {
            m_ContextMenuAnchor = this.Q<VisualElement>(contextMenuAnchorUssClassName);
            
            m_GridView = this.Q<GridView>(gridviewUssClassName);
            m_GridView.makeItem = MakeItemView;
            m_GridView.bindItem = BindGridItem;
            
            m_ThumbnailSizeSlider = this.Q<SliderFloat>(thumbnailSizeSliderUssClassName);
            m_ThumbnailSizeSlider.lowValue = 0.5f;
            m_ThumbnailSizeSlider.highValue = 1f;
            
            m_Title = this.Q<Text>(classes: titleClassName);
            m_ExportButton = this.Q<AppUI.UI.Button>(className: exportButtonUssClassName);
            m_SearchBar = this.Q<SearchBar>();
            
            RefreshThumbnailSize();
        }
        
        void BindGridItem(VisualElement el, int index)
        {
            el.Clear();
            el.Add(m_FilteredItemsList[index]);
        }
        
        VisualElement MakeItemView()
        {
            return new VisualElement();
        }
        
        public void RegisterComponents()
        {
            m_LibraryItemContextualMenu.OnMenuAction += OnItemContextualMenuAction;
            m_ThumbnailSizeSlider.RegisterValueChangingCallback(OnThumbnailSizeSliderChanged);
            
            m_ThumbnailSizeSlider.value = m_ResultsTraySize;
            m_ThumbnailSizeSlider.tooltip = "Thumbnail size";
            
            m_SearchBar.RegisterValueChangingCallback(OnSearchFieldChanging);
            m_SearchBar.RegisterValueChangedCallback(OnSearchFieldChanged);
            
            m_SearchBar.trailingIconName = "x--regular";
            m_SearchBar.trailingElement.pickingMode = PickingMode.Position;
            m_SearchBar.trailingElement.RegisterCallback<PointerDownEvent>(_ => CancelSearch());
            m_SearchBar.trailingElement.style.display = DisplayStyle.None;
        }
        
        public void UnregisterComponents()
        {
            m_LibraryItemContextualMenu.OnMenuAction -= OnItemContextualMenuAction;
            m_ThumbnailSizeSlider.UnregisterValueChangingCallback(OnThumbnailSizeSliderChanged);
            m_SearchBar.UnregisterValueChangingCallback(OnSearchFieldChanging);
            m_SearchBar.UnregisterValueChangedCallback(OnSearchFieldChanged);
        }
        
        protected void SetModel(LibraryUIModel model)
        {
            DevLogger.LogSeverity(TraceLevel.Verbose, $"LibraryUI -> SetModel({model})");
            
            UnregisterModel();
            m_Model = model;
            RegisterModel();
            Update();
        }

        void RegisterModel()
        {
            if (m_Model == null)
                return;

            m_Model.OnItemsChanged += OnItemsChanged;
            m_Model.OnRequestedItemContextualMenu += OnRequestedItemContextualMenu;
            m_Model.OnRequestedScrollToItem += OnRequestedScrollToItem;
            m_Model.OnSelectionChanged += SelectionChanged;
        }

        void UnregisterModel()
        {
            if (m_Model == null)
                return;

            m_Model.OnItemsChanged -= OnItemsChanged;
            m_Model.OnRequestedItemContextualMenu -= OnRequestedItemContextualMenu;
            m_Model.OnRequestedScrollToItem += OnRequestedScrollToItem;
            m_Model.OnSelectionChanged -= SelectionChanged;
        }

        public void Update()
        {
            DevLogger.LogSeverity(TraceLevel.Verbose, "LibraryUI-> Update()");
            
            if(m_Model == null)
                return;
            
            if (!IsAttachedToPanel)
                return;
            
            // Create new item views if required.
            for (var i = 0; i < m_Model.Items.Count; i++)
            {
                if (i >= m_Items.Count)
                    CreateItem(i);
            }

            FilterItemsSource();
        }
        
        void FilterItemsSource()
        {
            var search = m_SearchBar?.value?.ToLower();
            m_FilteredItemsList.Clear();
            
            foreach (var item in m_Items)
            {
                if (item is not TakesLibraryItemUI { Model: { IsVisible: true } model } take)
                {
                    continue;
                }

                var prompt = model.Target switch
                {
                    TextToMotionTake motionTake => motionTake.Prompt.ToLower(),
                    KeySequenceTake keysTake => keysTake.Title.ToLower(),
                    _ => ""
                };

                if (string.IsNullOrEmpty(search) || prompt.Contains(search) || prompt.Equals(search))
                    m_FilteredItemsList.Add(take);
            }
            
            m_FilteredItemsList.Reverse();
            m_GridView.itemsSource = m_FilteredItemsList;
            m_GridView.Refresh();
        }
        
        protected void RegisterItem(LibraryItemUI item)
        {
            DevLogger.LogSeverity(TraceLevel.Verbose, $"LibraryUI-> RegisterItem({item})");
            
            m_Items.Add(item);
        }

        protected virtual void CreateItem(int index)
        {
            DevLogger.LogSeverity(TraceLevel.Verbose, $"LibraryUI-> CreateItem({index})");
            
            var item = new LibraryItemUI();
            RegisterItem(item);
        }
        
        void OnSearchFieldChanged(ChangeEvent<string> evt)
        {
            OnSearch();
        }

        void OnSearchFieldChanging(ChangingEvent<string> evt)
        {
            OnSearch();
        }

        void OnSearch()
        {
            m_SearchBar.trailingElement.style.display = string.IsNullOrEmpty(m_SearchBar.value) ? DisplayStyle.None : DisplayStyle.Flex;
            FilterItemsSource();
        }

        void CancelSearch()
        {
            m_SearchBar.value = string.Empty;
            FilterItemsSource();
        }

        void OnThumbnailSizeSliderChanged(ChangingEvent<float> evt)
        {
            m_ResultsTraySize = evt.newValue;
            RefreshThumbnailSize();
        }

        void RefreshThumbnailSize()
        {
            var size = m_ThumbnailSizeSlider.value * k_DefaultThumbnailSize;
            var sizeAndMargin = size;
            var width = m_GridView.resolvedStyle.width;
            var newCountPerRow = Mathf.FloorToInt(width / sizeAndMargin);

            newCountPerRow = Mathf.Max(1, newCountPerRow);

            if (newCountPerRow != m_CountPerRow)
            {
                m_CountPerRow = newCountPerRow;
                m_GridView.columnCount = m_CountPerRow;
            }

            var itemHeight = Mathf.FloorToInt(width / m_CountPerRow);

            if (!Mathf.Approximately(itemHeight, m_GridView.itemHeight))
                m_GridView.itemHeight = itemHeight;
        }
        
        void OnItemContextualMenuAction(LibraryItemContextualMenu.ActionType type, ClipboardService clipboard, SelectionModel<LibraryItemModel> selectionModel, LibraryItemUIModel target)
        {
            DevLogger.LogSeverity(TraceLevel.Verbose, $"LibraryUI -> OnItemContextualMenuAction({type}, {clipboard}, {selectionModel}, {target})");
            
            m_Model.RequestItemContextMenuAction(type, clipboard, selectionModel, target);
        }

        void OnRequestedItemContextualMenu(LibraryModel library, LibraryItemUIModel item)
        {
            // TODO: Finish hooking up the buttons
            
            DevLogger.LogSeverity(TraceLevel.Verbose, $"LibraryUI -> OnRequestedItemContextualMenu({library}, {item})");
            
            if (m_Model == null)
                return;
            
            LibraryItemUI matchingView = null;

            foreach (var view in m_Items)
            {
                if (view.Model == item)
                {
                    matchingView = view;
                    break;
                }
            }

            m_LibraryItemContextualMenu.Open(library, m_Model.ClipboardService, m_Model.SelectionModel, item, matchingView);

        }

        void OnRequestedScrollToItem(LibraryModel library, LibraryItemUIModel item)
        {
            DevLogger.LogSeverity(TraceLevel.Verbose, $"LibraryUI -> OnRequestedScrollToItem("+item+")");
            
            for (int i = 0; i < m_Items.Count; i++)
            {
                if (m_Items[i].Model == item)
                {
                    m_GridView.ScrollToItem(i);
                    return;
                }
            }
        }

        void SelectionChanged()
        {
            DevLogger.LogSeverity(TraceLevel.Verbose, $"LibraryUI -> SelectionChanged()");
            
            Update();
        }

        void OnItemsChanged()
        {
            DevLogger.LogSeverity(TraceLevel.Verbose, $"LibraryUI -> OnItemsChanged()");
            
            Update();
        }
        
        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (panel == null || float.IsNaN(evt.newRect.width) || Mathf.Approximately(0, evt.newRect.width))
                return;

            RefreshThumbnailSize();
        }
    }
}
