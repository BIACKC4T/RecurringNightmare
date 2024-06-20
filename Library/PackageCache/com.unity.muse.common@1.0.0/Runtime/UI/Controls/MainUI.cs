using System;
using System.Linq;
using Unity.Muse.AppUI.UI;
using Unity.Muse.Common.Account;
using UnityEngine;
using UnityEngine.UIElements;
using Button = Unity.Muse.AppUI.UI.Button;

#pragma warning disable 0067

namespace Unity.Muse.Common
{
#if ENABLE_UXML_SERIALIZED_DATA
    [UxmlElement]
#endif
    internal partial class MainUI : VisualElement, IControl
    {
        [Serializable]
        class UISize : IModelData
        {
            public event Action OnModified;
            public event Action OnSaveRequested;

            [SerializeField]
            float m_NodeListWidth = k_NodeListMinWidth;

            [SerializeField]
            float m_NodeListRefineWidth = k_NodeListMinWidth;

            [SerializeField]
            float m_AssetListRefineWidth = k_AssetListMinWidth;

            public float nodeListWidth
            {
                get => m_NodeListWidth;
                set
                {
                    m_NodeListWidth = value;
                    OnModified?.Invoke();
                }
            }

            public float nodeListRefineWidth
            {
                get => m_NodeListRefineWidth;
                set
                {
                    m_NodeListRefineWidth = value;
                    OnModified?.Invoke();
                }
            }

            public float assetListRefineWidth
            {
                get => m_AssetListRefineWidth;
                set
                {
                    m_AssetListRefineWidth = value;
                    OnModified?.Invoke();
                }
            }
        }

#if ENABLE_UXML_TRAITS
        internal new class UxmlFactory : UxmlFactory<MainUI, UxmlTraits> { }
#endif

        bool m_Initialized;
        Canvas m_Canvas;
        ControlToolbar m_ControlToolbar;
        NodesList m_NodesList;
        AssetsList m_AssetsList;
        ScopeToolbar m_ScopeToolbar;
        AccountDropdown m_AccountDropdown;

        int m_Mode;

        IUIMode m_UIMode;

        const float k_AssetListLeftMargin = 10f;
        const float k_NodeListMinWidth = 300;
        const float k_AssetListMinWidth = 200;
        UISize m_UISize;

        Artifact m_ArtifactToBeRefined;

        IVisualElementScheduledItem m_ScheduledFrameSelectedArtifact;

        public MainUI()
        {
            this.AddManipulator(new MuseShortcutHandler());
            this.RegisterContextChangedCallback<Model>(context =>
            {
                if (context.context != null)
                    SetModel(context.context);
            });

#if UNITY_EDITOR
            if(UnityEditor.EditorGUIUtility.isProSkin)
                AddToClassList("dark-mode");
#endif
        }

        void CreateAccountDropdown()
        {
            if (model is null || m_AccountDropdown is not null)
                return;

            m_AccountDropdown = new AccountDropdown();
            model.AddToToolbar(m_AccountDropdown, 1, ToolbarPosition.Right);
        }

        public void SetModel(Model model)
        {
            if (model == this.model)
                return;

            Init();
            this.model = model;
            this.model.OnModeChanged += OnModeChanged;
            model.ModeChanged(ModesFactory.GetModeIndexFromKey(model.CurrentMode));
            schedule.Execute(CreateAccountDropdown); // Wait until the controltoolbar is hooked up to the model events
            UpdateView();
        }

        void OnModeChanged(int newMode)
        {
            if (newMode == m_Mode)
                return;

            m_Mode = newMode;

            UpdateView();
        }

        public Canvas canvas => m_Canvas;
        public ControlToolbar controlToolbar => m_ControlToolbar;
        public NodesList nodesList => m_NodesList;
        public AssetsList assetsList => m_AssetsList;
        public ScopeToolbar scopeToolbar => m_ScopeToolbar;
        public Model model { get; private set; }

        public void UpdateView()
        {
            PreUpdateView();

            m_UIMode?.Deactivate();
            m_UIMode = UIModeFactory.GetUIMode(ModesFactory.GetModeKeyFromIndex(m_Mode));
            m_UIMode?.Activate(this);

            PostUpdateView();
        }


        void PreUpdateView()
        {
            UnregisterCallback<GeometryChangedEvent>(OnMainUIGeometryChanged);

            assetsList.OnResized -= AssetListResized;
            nodesList.OnResized -= NodeListResized;
            nodesList.UnregisterCallback<GeometryChangedEvent>(OnNodesListGeometryChanged);

            RemoveModelListeners();
        }

        void PostUpdateView()
        {
            m_UISize = model.GetData<UISize>();
            AddModelListeners();

            assetsList.content.style.minWidth = k_AssetListMinWidth;
            model.SetLeftOverlay(nodesList.content);
            model.SetRightOverlay(assetsList.content);

            MaximiseAssetList();
            assetsList.MarkDirtyRepaint();

            RegisterCallback<GeometryChangedEvent>(OnMainUIGeometryChanged);
            assetsList.OnResized += AssetListResized;
            nodesList.OnResized += NodeListResized;
            nodesList.RegisterCallback<GeometryChangedEvent>(OnNodesListGeometryChanged);

            UpdateCanvasVisibility();
        }

        void Init()
        {
            if (m_Initialized) return;
            m_Canvas = this.Q<Canvas>();
            m_ControlToolbar = this.Q<ControlToolbar>();
            m_NodesList = this.Q<NodesList>();
            m_AssetsList = this.Q<AssetsList>();
            m_ScopeToolbar = this.Q<ScopeToolbar>();
            m_Initialized = true;
        }

        void AddModelListeners()
        {
            model.OnRefineArtifact += OnRefineArtifact;
            model.OnFinishRefineArtifact += OnFinishRefineArtifact;
            model.OnDispose += OnDispose;
            model.OnArtifactSelected += OnArtifactSelected;
            model.OnModeChanged += OnModeChanged;
            model.OnServerError += OnServerError;
            GenerativeAIBackend.OnServerError += OnServerError;
        }

        void RemoveModelListeners()
        {
            model.OnRefineArtifact -= OnRefineArtifact;
            model.OnFinishRefineArtifact -= OnFinishRefineArtifact;
            model.OnDispose -= OnDispose;
            model.OnArtifactSelected -= OnArtifactSelected;
            model.OnModeChanged -= OnModeChanged;
            model.OnServerError -= OnServerError;
            GenerativeAIBackend.OnServerError -= OnServerError;
        }

        void OnMainUIGeometryChanged(GeometryChangedEvent evt)
        {
            if (!model.isRefineMode)
                MaximiseAssetList();
            canvas.UpdateCanvasFrameContainer();
        }

        void UpdateCanvasVisibility()
        {
            var enabled = model.isRefineMode;
            canvas.style.display = enabled ? DisplayStyle.Flex : DisplayStyle.None;
            EnableInClassList("muse--refinement-mode", enabled);
            if (enabled)
            {
                m_ScheduledFrameSelectedArtifact?.Pause();
                m_ScheduledFrameSelectedArtifact = schedule.Execute(() =>
                {
                    canvas.FrameAll();
                });
            }
        }

        void RefineModeAssetList()
        {
            assetsList.content.style.width = m_UISize.assetListRefineWidth;
            nodesList.style.width = m_UISize.nodeListRefineWidth;
            nodesList.draggerElement.RemoveFromClassList(Styles.hiddenUssClassName);
        }

        void MaximiseAssetList()
        {
            assetsList.content.style.maxWidth = resolvedStyle.width - 300 - k_AssetListLeftMargin;
            assetsList.content.style.width = resolvedStyle.width - m_UISize.nodeListWidth - k_AssetListLeftMargin;
            nodesList.style.width = m_UISize.nodeListWidth;
            nodesList.draggerElement.AddToClassList(Styles.hiddenUssClassName);
        }

        void OnNodesListGeometryChanged(GeometryChangedEvent evt)
        {
            canvas.UpdateCanvasFrameContainer();
        }

        void NodeListResized()
        {
            if (!model.isRefineMode)
            {
                m_UISize.nodeListWidth = nodesList.style.width.value.value;
                MaximiseAssetList();
            }
            else
            {
                m_UISize.nodeListRefineWidth = nodesList.style.width.value.value;
            }
        }

        void AssetListResized()
        {
            var assetListWidth = assetsList.content.style.width.value.value;
            if (model.isRefineMode)
            {
                m_UISize.assetListRefineWidth = assetListWidth;
            }
            else
            {
                m_UISize.nodeListWidth = Mathf.Min(nodesList.resolvedStyle.maxWidth.value,
                    resolvedStyle.width - k_AssetListLeftMargin - assetListWidth);
                MaximiseAssetList();
            }
        }

        void OnRefineArtifact(Artifact artifact)
        {
            model.CanvasRefineArtifact(artifact);
            RefineModeAssetList();
            UpdateCanvasVisibility();
        }

        void OnFinishRefineArtifact(Artifact artifact)
        {
            MaximiseAssetList();
            UpdateCanvasVisibility();
        }

        void OnArtifactSelected(Artifact artifact)
        {
            if (artifact is null)
                return;

            if (model.isRefineMode && canvas.refinedArtifact?.Guid != artifact.Guid)
                model.CanvasRefineArtifact(artifact);
        }

        void OnDispose()
        {
            PreUpdateView();
        }

        bool OnServerError(long code, string error)
        {
            switch (code)
            {
                case 429:
                    Debug.LogWarning("Your last request was rate-limited because of too many requests in a short amount of time. Please try again later.");
                    return true;
            }

            return false;
        }
    }
}