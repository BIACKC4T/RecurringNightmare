using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Muse.AppUI.UI;
using Unity.Muse.Common.Utils;
using UnityEngine;
using UnityEngine.UIElements;
using Button = Unity.Muse.AppUI.UI.Button;
using Dragger = Unity.Muse.Common.Baryon.UI.Manipulators.Dragger;

namespace Unity.Muse.Common
{
#if ENABLE_UXML_SERIALIZED_DATA
    [UxmlElement]
#endif
    internal partial class AssetsList : ExVisualElement, IControl
    {
        const string ussClassName = "muse-assetslist";
        static readonly string dragBarUssClassName = ussClassName + "__dragbar";
        static readonly string gridviewUssClassName = ussClassName + "__gridview";
        static readonly string thumbnailSizeSliderUssClassName = ussClassName + "__thumbnail-slider";
        static readonly string contextMenuAnchorUssClassName = ussClassName + "__context-menu-anchor";
        static readonly string titleClassName = $"{ussClassName}__title";
        static readonly string exportButtonUssClassName = ussClassName + "__export-button";
        static readonly string bookmarkFilterButtonUssClassName = ussClassName + "__bookmark-filter-button";

        const float k_DefaultThumbnailSize = 240;

        Model m_CurrentModel;

        List<Artifact> m_ItemsList = new();
        List<Artifact> m_FilteredItemsList = new();

        bool m_Inited;

        AppUI.UI.GridView m_GridView;

        Dragger m_HorizontalDraggable;

        VisualElement m_DraggableContainer;

        SliderFloat m_ThumbnailSizeSlider;

        SearchBar m_SearchBar;

        VisualElement m_ContextMenuAnchor;
        Text m_Title;

        string m_CurrentMode;
        public event Action OnResized;

        readonly List<MuseShortcut> m_DeleteShortcuts;

        int m_CountPerRow = 2;

        VisualElement m_VerticalScrollerDragContainer;
        public VisualElement content;

        Button m_ExportButton;
        ActionButton m_BookmarkFilterButton;
        BookmarkManager m_BookmarkManager;

        Dictionary<Artifact, ArtifactView> m_ViewCache;

        IEnumerable<int> m_PreviousSelectedIndices;

        public AssetsList()
        {
            this.ApplyTemplate(PackageResources.assetsListTemplate);
            Init();

            m_DeleteShortcuts = new List<MuseShortcut>
            {
                new("Delete Artifact", DeleteSelected, KeyCode.Delete, source: this) { requireFocus = true }
            };

            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                m_DeleteShortcuts.Add( new MuseShortcut("Delete Artifact", DeleteSelected, KeyCode.Backspace, KeyModifier.Action, this) { requireFocus = true });
            }
        }

        void OnAttachToPanel(AttachToPanelEvent evt) => MuseShortcuts.AddShortcuts(m_DeleteShortcuts);
        void OnDetachFromPanel(DetachFromPanelEvent evt) => MuseShortcuts.RemoveShortcuts(m_DeleteShortcuts);

        void OnResizeBarDrag(Dragger manipulator)
        {
            var width = content.resolvedStyle.width;
            width -= manipulator.deltaPos.x;
            content.style.width = width;
            OnResized?.Invoke();
        }

        public void SetModel(Model model) => SetModel(model, false);

        void SetModel(Model model, bool force)
        {
            if (model == m_CurrentModel && !force)
                return;

            UnSubscribeToModelEvents();
            m_CurrentModel = model;
            m_CurrentMode = m_CurrentModel ? m_CurrentModel.CurrentMode : ModesFactory.GetModeKeyFromIndex(0);
            SubscribeToModelEvents();

            if (m_CurrentModel != null)
            {
                m_BookmarkManager = m_CurrentModel.GetData<BookmarkManager>();
                m_BookmarkFilterButton.selected = m_BookmarkManager?.isFilterEnabled ?? false;

                SetArtifacts(m_CurrentModel.AssetsData);
                OnArtifactSelected(m_CurrentModel.SelectedArtifact);
            }
            else
                SetArtifacts(null);

            FilterItemsSource();
            UpdateExportButton();
        }

        void SubscribeToModelEvents()
        {
            if(m_CurrentModel == null) return;

            m_CurrentModel.OnArtifactAdded += OnArtifactAdded;
            m_CurrentModel.OnArtifactRemoved += OnArtifactRemoved;
            m_CurrentModel.OnDeselectAll += OnDeselectAll;
            m_CurrentModel.OnArtifactSelected += OnArtifactSelected;
            m_CurrentModel.OnModeChanged += OnModeChanged;
            m_CurrentModel.OnRefineArtifact += OnRefineArtifactChanged;
            m_CurrentModel.OnFinishRefineArtifact += OnRefineArtifactChanged;
            m_CurrentModel.OnGenerateButtonClicked += OnGenerateButtonClicked;
            m_CurrentModel.OnDispose += OnDisposeModel;
            m_CurrentModel.OnModified += OnModelDataModified;
        }

        void OnRefineArtifactChanged(Artifact artifact)
        {
            m_Title.text = m_CurrentModel.isRefineMode ? "Refinements" : "Generations";
            // Simulate a switch to another model to trigger a full asset refresh.
            // The switch needs a separate frame to execute in order to let the canvas
            // frame properly.
            var model = m_CurrentModel;
            SetModel(null);
            schedule.Execute(() =>
            {
                SetModel(model);
                UpdateView();

                schedule.Execute(() =>
                {
                    OnArtifactSelected(artifact);
                });
            });
        }

        void UnSubscribeToModelEvents()
        {
            if(m_CurrentModel == null) return;

            m_CurrentModel.OnArtifactAdded -= OnArtifactAdded;
            m_CurrentModel.OnArtifactRemoved -= OnArtifactRemoved;
            m_CurrentModel.OnDeselectAll -= OnDeselectAll;
            m_CurrentModel.OnArtifactSelected -= OnArtifactSelected;
            m_CurrentModel.OnModeChanged -= OnModeChanged;
            m_CurrentModel.OnRefineArtifact -= OnRefineArtifactChanged;
            m_CurrentModel.OnFinishRefineArtifact -= OnRefineArtifactChanged;
            m_CurrentModel.OnGenerateButtonClicked -= OnGenerateButtonClicked;
            m_CurrentModel.OnDispose -= OnDisposeModel;
            m_CurrentModel.OnModified -= OnModelDataModified;
        }

        void OnDisposeModel()
        {
            SetModel(null);
        }

        void OnGenerateButtonClicked()
        {
            SetFilter(false);       // Cancel the filter since it will hide newly generated item which would be confusing
            CancelSearch();
        }

        void OnModeChanged(int mode)
        {
            m_CurrentMode = ModesFactory.GetModeKeyFromIndex(mode);
            FilterItemsSource();
        }

        void Init()
        {
            m_ViewCache = new(new ArtifactComparer());
            passMask = Passes.Clear | Passes.OutsetShadows | Passes.BackgroundColor;
            content = this.Q<VisualElement>(classes: ussClassName);
            m_ContextMenuAnchor = this.Q<VisualElement>(contextMenuAnchorUssClassName);
            m_PreviousSelectedIndices = new List<int>();
            m_GridView = this.Q<AppUI.UI.GridView>(gridviewUssClassName);
            m_GridView.allowNoSelection = true;
            m_GridView.makeItem = MakeItemView;
            m_GridView.bindItem = BindGridItem;
            m_GridView.operationMask &= ~AppUI.UI.GridView.GridOperations.Cancel;
            m_GridView.columnCount = 2;
            m_GridView.itemHeight = (int)k_DefaultThumbnailSize;
            m_GridView.selectionType = SelectionType.Multiple;
            m_GridView.selectionChanged += OnGridViewSelectionChanged;
            m_GridView.contextClicked += OnGridViewContextClick;
            m_GridView.doubleClicked += OnGridViewDoubleClick;
            m_GridView.dragStarted += OnGridViewDragStarted;
            m_GridView.dragUpdated += OnGridViewDragUpdated;
            m_GridView.dragFinished += OnGridViewDragFinished;
            m_GridView.dragCanceled += OnGridViewDragCanceled;
            m_GridView.scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            m_GridView.scrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;
            m_GridView.scrollView.verticalScroller.style.opacity = 0;
            m_VerticalScrollerDragContainer = m_GridView.scrollView.verticalScroller.slider.Q(classes:BaseSlider<float>.dragContainerUssClassName);
            m_DraggableContainer = this.Q<VisualElement>(dragBarUssClassName);
            m_ThumbnailSizeSlider = this.Q<SliderFloat>(thumbnailSizeSliderUssClassName);
            m_ThumbnailSizeSlider.RegisterValueChangingCallback(OnThumbnailSizeSliderChanged);
            m_ThumbnailSizeSlider.value = Preferences.resultsTraySize;
            m_ThumbnailSizeSlider.tooltip = TextContent.thumbnailSizeSliderTooltip;
            m_Title = this.Q<Text>(classes: titleClassName);

            m_ExportButton = this.Q<Button>(className: exportButtonUssClassName);
            m_ExportButton.clicked += () =>
                PerformAction(null, (int)Actions.Save, new List<object>(m_GridView.selectedItems.Where(selected => GetArtifactView((Artifact)selected) != null)));
            m_ExportButton.tooltip = TextContent.saveTooltip;

            m_BookmarkFilterButton = this.Q<ActionButton>(className:bookmarkFilterButtonUssClassName);
            m_BookmarkFilterButton.clicked += OnToggleFilter;
            m_BookmarkFilterButton.tooltip = TextContent.bookmarkTooltip;

            m_SearchBar = this.Q<SearchBar>();
            m_SearchBar.RegisterValueChangingCallback(OnSearchFieldChanging);
            m_SearchBar.RegisterValueChangedCallback(OnSearchFieldChanged);

            m_SearchBar.trailingIconName = "x--regular";
            m_SearchBar.trailingElement.pickingMode = PickingMode.Position;
            m_SearchBar.trailingElement.RegisterCallback<PointerDownEvent>(_ => CancelSearch());
            m_SearchBar.trailingElement.style.display = DisplayStyle.None;

            RegisterCallback<PointerEnterEvent>(OnPointerEnter);
            RegisterCallback<PointerLeaveEvent>(OnPointerLeave);

            m_HorizontalDraggable = new Dragger(() => { }, OnResizeBarDrag, _ => { }, _ => { });
            m_DraggableContainer.AddManipulator(m_HorizontalDraggable);

            content.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            this.RegisterContextChangedCallback<Model>(context => SetModel(context.context));

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnToggleFilter()
        {
            SetFilter(!m_BookmarkManager.isFilterEnabled);
        }

        void SetFilter(bool value)
        {
            m_BookmarkManager.SetFilter(value);
            FilterItemsSource();
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

        void FilterItemsSource()
        {
            var selectedItems = m_GridView.selectedItems.ToArray();

            // After a domain reload, refresh selection if it is present on the model
            if (!selectedItems.Any() && m_CurrentModel && m_CurrentModel.SelectedArtifact is not null)
            {
                m_CurrentModel.ArtifactSelected(m_CurrentModel.SelectedArtifact, true);
            }

            var search = m_SearchBar?.value?.ToLower();
            var type = ArtifactFactory.GetTypeForMode(m_CurrentMode);
            m_FilteredItemsList.Clear();
            foreach (var item in m_ItemsList)
            {
                if (item.GetType() == type)
                {
                    var prompt = item.GetOperator<PromptOperator>()?.GetPrompt()?.ToLower();
                    if (prompt != null && (string.IsNullOrEmpty(search) || prompt.Contains(search)))
                        m_FilteredItemsList.Add(item);
                }
            }

            var isBookmarkFilterEnabled = m_BookmarkManager.isFilterEnabled;
            m_BookmarkFilterButton.selected = isBookmarkFilterEnabled;

            m_FilteredItemsList = isBookmarkFilterEnabled ? m_FilteredItemsList.Where(a => m_BookmarkManager.IsBookmarked(a)).ToList() : m_FilteredItemsList;

            m_GridView.itemsSource = m_FilteredItemsList;
        }

        void OnGridViewSelectionChanged(IEnumerable<object> selectedItems)
        {
            if (m_CurrentModel == null)
                return;

            //Don't unselect items in refine mode
            m_GridView.allowNoSelection = !m_CurrentModel.isRefineMode;

            Artifact selectedArtifact = null;
            foreach (var selectedItem in selectedItems)
            {
                selectedArtifact = (Artifact)selectedItem;
                break;
            }

            m_CurrentModel.ArtifactSelected(selectedArtifact);

            UpdateExportButton();

            m_PreviousSelectedIndices = new List<int>(m_GridView.selectedIndices);
        }

        readonly List<VisualElement> m_ClonedElements = new();

        /// <summary>
        /// Get the list of artifact views from the assets list.
        /// </summary>
        internal IEnumerable<ArtifactView> GetViews => m_GridView.Query<ArtifactView>().ToList();
        /// <summary>
        /// Get the related view for a given artifact.
        /// </summary>
        /// <param name="artifact">Artifact to get a view from.</param>
        /// <returns>The artifact view.</returns>
        internal ArtifactView GetView(Artifact artifact)
        {
            return GetViews.FirstOrDefault(view =>  ReferenceEquals(view.Artifact, artifact));
        }

        void OnGridViewDragStarted(PointerMoveEvent evt)
        {
            if (m_CurrentModel == null)
                return;

            var selection = new List<object>(m_GridView.selectedItems);

            if (selection.Count == 0)
                return;

            var artifacts = new List<Artifact>();
            foreach (var item in m_GridView.selectedIndices)
            {
                var artifact = m_FilteredItemsList[item];
                artifacts.Add(artifact);

                var artifactView = GetView(artifact);
                if(artifactView == null)
                    continue;

                var clonedElement = new Image
                {
                    image = artifactView.Preview,
                    pickingMode = PickingMode.Ignore,
                    style =
                    {
                        position = Position.Absolute,
                        width = artifactView.worldBound.width,
                        height = artifactView.worldBound.height,
                        opacity = 0.5f
                    },
                    transform =
                    {
                        position = selection.Count > 1 ? artifactView.worldBound.position : (Vector2)evt.position - artifactView.worldBound.size * 0.5f
                    },
                    userData = artifactView
                };

                m_ClonedElements.Add(clonedElement);
                if(artifactView.panel != null)
                    artifactView.panel.visualTree.Add(clonedElement);
            }

            m_CurrentModel.DragStart(artifacts);
        }

        void OnGridViewDragUpdated(PointerMoveEvent evt)
        {
            var isInsideCanvas = panel.visualTree.worldBound.Contains(evt.position);
            if (isInsideCanvas)
            {
                if (m_ClonedElements.Count == 0)
                    OnGridViewDragStarted(evt);
                foreach (var clonedElement in m_ClonedElements)
                {
                    clonedElement.transform.position += evt.deltaPosition;
                }
            }
            else
            {
                if (Application.isEditor)
                {
                    switch (m_CurrentModel.DraggedArtifacts.Count)
                    {
                        case 1:
                            var artifactView = GetArtifactView(m_CurrentModel.DraggedArtifacts[0]);
                            artifactView.UpdateView();
                            artifactView.DragEditor();
                            break;
                        case > 1:
                            var draggedElements = m_CurrentModel.DraggedArtifacts.Select(a =>
                            {
                                var view = GetArtifactView(a);
                                view.UpdateView();
                                return view;
                            }).ToList();
                            m_CurrentModel.EditorStartMultiDrag(GetArtifacts(draggedElements));
                            break;
                    }
                }
                m_GridView.CancelDrag();
            }
        }

        static IList<(string name, IList<Artifact> artifacts)> GetArtifacts(List<ArtifactView> artifactViews)
        {
            var result = new List<(string name, IList<Artifact> artifacts)>();

            foreach (var artifactView in artifactViews)
                result.Add(artifactView.GetArtifactsAndType());

            return result;
        }

        void OnGridViewDragFinished(PointerUpEvent evt)
        {
            var selection = new List<ArtifactView>();
            foreach (var clonedElement in m_ClonedElements)
            {
                selection.Add((ArtifactView)clonedElement.userData);
                clonedElement.RemoveFromHierarchy();
            }
            m_ClonedElements.Clear();

            if (selection.Count == 0)
                return;

            var artifacts = new List<Artifact>();

            foreach (var artifactView in selection)
            {
                artifacts.Add(artifactView.Artifact);
            }

            m_CurrentModel.DropItems(artifacts, evt.position);
            m_CurrentModel.DragEnd();
        }

        void OnGridViewDragCanceled()
        {
            foreach (var clonedElement in m_ClonedElements)
            {
                clonedElement.RemoveFromHierarchy();
            }
            m_ClonedElements.Clear();
            m_CurrentModel.DragEnd();
        }

        void OnGridViewDoubleClick(int indexUnderCursor)
        {
            if (m_CurrentModel == null)
                return;

            if (indexUnderCursor < 0 || indexUnderCursor >= m_GridView.itemsSource.Count)
                return;

            var artifactView = GetView(m_GridView.itemsSource[indexUnderCursor] as Artifact);
            artifactView?.TryGoToRefineMode();
        }

        void OnGridViewContextClick(PointerDownEvent evt)
        {
            if (m_CurrentModel == null)
                return;

            var selection = new List<object>(m_GridView.selectedItems);

            if (selection.Count == 0)
                return;

            m_ContextMenuAnchor.transform.position = content.WorldToLocal(evt.position);

            var menuBuilder = MenuBuilder.Build(m_ContextMenuAnchor);

            AddActionsToMenu(selection, menuBuilder);

            if (menuBuilder.currentMenu.childCount == 0)
            {
                menuBuilder.Dismiss();
            }
            else
            {
                menuBuilder.Show();
            }
        }

        void AddActionsToMenu(IList<object> selection, MenuBuilder menuBuilder)
        {
            var views = selection.Cast<Artifact>().Select(artifact =>
            {
                var view = GetArtifactView(artifact);
                view.UpdateView();
                return view;
            }).ToList();
            var context = new ActionContext(views);
            var actionIds = new List<int>();
            foreach (var view in views)
            {
                var availableActions = view.GetAvailableActions(context);
                if(!availableActions.Any())
                    continue;

                foreach (var availableAction in availableActions)
                {
                    if (!actionIds.Contains(availableAction.id))
                    {
                        actionIds.Add(availableAction.id);
                        menuBuilder.AddAction(
                            availableAction.id,
                            availableAction.label,
                            availableAction.icon,
                            availableAction.shortcut,
                            evt => PerformAction(evt, availableAction.id, selection));
                        menuBuilder.currentMenu
                            .ElementAt(menuBuilder.currentMenu.childCount - 1)
                            .SetEnabled(availableAction.enabled);
                    }
                }
            }
        }

        void PerformAction(IPointerEvent evt, int actionId, IList<object> selection)
        {
            var views = selection.Cast<Artifact>().Select(GetView).ToList();
            var context = new ActionContext(views);

            switch (actionId)
            {
                case (int) Actions.Save:
                    switch (selection.Count)
                    {
                        case 1:
                        {
                            var artifactView = GetArtifactView((Artifact)selection[0]);
                            artifactView.PerformAction(actionId, context, evt);
                            break;
                        }
                        case > 1:
                            m_CurrentModel.MultiExport(selection.Select(s => GetArtifactView((Artifact)s)).ToList());
                            break;
                    }
                    break;
                case (int) Actions.Delete:
                    DeleteSelected();
                    break;
                default:
                    foreach (var selectedItem in selection)
                    {
                        var artifactView = GetArtifactView((Artifact)selectedItem);
                        artifactView.PerformAction(actionId, context, evt);
                    }
                    break;

            }
        }

        void DeleteSelected()
        {
            if (!m_GridView.selectedItems.Any())
                return;
            if(GlobalPreferences.deleteWithoutWarning)
                DoDeleteSelected();
            else
            {
                var checkbox = new Checkbox
                {
                    label = TextContent.deleteDialogOkDontShowAgain,
                    style =
                    {
                        flexGrow = 1
                    }
                };
                var dialog = new AlertDialog
                {
                    title = TextContent.deleteDialogTitle,
                    description = TextContent.deleteDialogMessage,
                    variant = AlertSemantic.Destructive
                };
                dialog.SetPrimaryAction(2, TextContent.deleteDialogOk, () =>
                {
				    if (checkbox.value == CheckboxState.Checked)
				    {
                        GlobalPreferences.deleteWithoutWarning = true;
				    }
				    DoDeleteSelected();
                });
                dialog.SetCancelAction(0, TextContent.cancel);
                dialog.actionContainer.Insert(0, checkbox);
                var modal = Modal.Build(m_GridView, dialog);
                modal.Show();
            }
        }

        void DoDeleteSelected()
        {
            var artifacts = m_GridView.selectedItems.Cast<Artifact>();
            m_CurrentModel.RemoveAssets(artifacts.ToArray());

            UpdateExportButton();
        }

        void OnPointerLeave(PointerLeaveEvent evt)
        {
            var scroller = m_GridView.scrollView.verticalScroller;

            if (m_VerticalScrollerDragContainer.HasPointerCapture(evt.pointerId))
                return;

            scroller.experimental.animation
                .Start(scroller.resolvedStyle.opacity, 0, 120,
                    (element, f) => element.style.opacity = f);
        }

        void OnPointerEnter(PointerEnterEvent evt)
        {
            if (m_DraggableContainer.HasPointerCapture(evt.pointerId))
                return;

            var scroller = m_GridView.scrollView.verticalScroller;
            scroller.experimental.animation
                .Start(scroller.resolvedStyle.opacity, 1, 120,
                    (element, f) => element.style.opacity = f);
        }

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (panel == null || float.IsNaN(evt.newRect.width) || Mathf.Approximately(0, evt.newRect.width))
                return;

            RefreshThumbnailSize();
        }

        void BindGridItem(VisualElement el, int index)
        {
            var data = m_FilteredItemsList[index];
            var view = GetArtifactView(data);
            view.UpdateView();
            el.Clear();
            el.Add(view);
        }

        VisualElement MakeItemView()
        {
            return new VisualElement();
        }

        void OnThumbnailSizeSliderChanged(ChangingEvent<float> evt)
        {
            Preferences.resultsTraySize = evt.newValue;
            RefreshThumbnailSize();
        }

        void RefreshThumbnailSize()
        {
            var size = m_ThumbnailSizeSlider.value * k_DefaultThumbnailSize;

            var sizeAndMargin = size;

            var width = m_GridView.scrollView.contentContainer.resolvedStyle.width;
            var newCountPerRow = Mathf.FloorToInt(width / sizeAndMargin);

            newCountPerRow = Mathf.Max(1, newCountPerRow);

            if (newCountPerRow != m_CountPerRow)
            {
                m_CountPerRow = newCountPerRow;
                m_GridView.columnCount = m_CountPerRow;
            }

            var itemHeight = Mathf.FloorToInt(width / m_CountPerRow);

            if (Math.Abs(itemHeight - m_GridView.itemHeight) > 1)
                m_GridView.itemHeight = itemHeight;
        }

        void OnArtifactRemoved(Artifact[] artifacts)
        {
            foreach (var artifact in artifacts)
                m_ItemsList.RemoveAll(item => item == (object) artifact);

            FilterItemsSource();
        }

        void OnArtifactAdded(Artifact artifact)
        {
            m_ItemsList.Insert(0, artifact);
            FilterItemsSource();
        }

        ArtifactView GetArtifactView(Artifact artifact)
        {
            if (m_ViewCache.TryGetValue(artifact, out var view))
                return view;

            var result = artifact.CreateView();
            ObjectUtils.Retain(result.Preview, panel);
            m_ViewCache.Add(artifact, result);
            return result;
        }

        void SetArtifacts(IEnumerable<Artifact> artifacts)
        {
            m_ItemsList.Clear();
            if (artifacts != null)
            {
                foreach (var artifact in artifacts)
                {
                    //if we have an invalid artifact, we retry to generate it
                    if (!artifact.IsValid())
                    {
                        artifact.RetryGenerate(m_CurrentModel);
                    }
                    m_ItemsList.Add(artifact);
                }
            }

            m_ItemsList.Reverse();
            FilterItemsSource();
        }

        public void UpdateView()
        {
            foreach (var artifactView in GetViews)
                artifactView.UpdateView();
        }

        void OnDeselectAll()
        {
            m_GridView.SetSelectionWithoutNotify(new int[] {});
        }

        void OnArtifactSelected(Artifact artifact)
        {
            if (artifact is null)
            {
                m_GridView.SetSelectionWithoutNotify(new int[] {});
                return;
            }

            if (m_GridView.selectedItems.Cast<Artifact>().Any(selected => selected == artifact))
                return;

            for (var idx = 0; idx < m_FilteredItemsList.Count; idx++)
            {
                var item = m_FilteredItemsList[idx];
                if (item == artifact)
                {
                    m_GridView.SetSelectionWithoutNotify(new[] {idx});
                    m_GridView.ScrollToItem(idx);
                    break;
                }
            }
        }
        public void ScrollToItem(int index)
        {
            m_GridView.ScrollToItem(index);
        }

        public void ScrollToItem(string guid)
        {
            var index = m_FilteredItemsList.FindIndex(x => x.Guid == guid);
            if(index > 0)
                ScrollToItem(index);
        }

        void OnModelDataModified()
        {
            UpdateExportButton();
        }

        void UpdateExportButton()
        {
            var canExport = m_GridView.selectedItems.Any(s => s is Artifact artifact && ArtifactCache.IsInCache(artifact));
            m_ExportButton.style.display = canExport ? DisplayStyle.Flex : DisplayStyle.None;
        }

#if ENABLE_UXML_TRAITS
        internal new class UxmlFactory : UxmlFactory<AssetsList, UxmlTraits> { }
#endif
    }
}