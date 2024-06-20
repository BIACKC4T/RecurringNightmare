using System;
using Unity.Muse.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.Common
{
#if ENABLE_UXML_SERIALIZED_DATA
    [UxmlElement]
#endif
    internal partial class Canvas : VisualElement
    {
#if ENABLE_UXML_TRAITS
        internal new class UxmlFactory : UxmlFactory<Canvas, UxmlTraits> { }
#endif

        Model m_CurrentModel;

        CanvasManipulator m_CurrentToolManipulator;

        readonly AppUI.UI.Canvas m_Canvas;

        Artifact m_RefinedArtifact;

        VisualElement m_ControlContent;
        VisualElement m_ControlTopContent;
        VisualElement m_ControlMiddleContent;
        VisualElement m_ControlBottomContent;

        IVisualElementScheduledItem m_ScheduledFrame;
        
        const int k_FrameTopOffset = 42;

        public Artifact refinedArtifact
        {
            get => m_RefinedArtifact;
            private set
            {
                if (m_RefinedArtifact == value)
                    return;

                m_RefinedArtifact = value;
                UpdateView();
            }
        }

        public override VisualElement contentContainer => m_Canvas.contentContainer;

        void SetFrameContainer(Rect frameContainer)
        {
            m_Canvas.frameContainer = frameContainer;
        }

        public AppUI.UI.CanvasManipulator primaryManipulator
        {
            get => m_Canvas.primaryManipulator;
            set => m_Canvas.primaryManipulator = value;
        }

        public Canvas()
        {
            m_Canvas = new AppUI.UI.Canvas
            {
                frameMargin = 24f,
            };
            
            m_Canvas.controlScheme = GlobalPreferences.canvasControlScheme;
            GlobalPreferences.preferencesChanged += OnPreferencesChanged;
            
            hierarchy.Add(m_Canvas);
            m_Canvas.StretchToParentSize();
            this.StretchToParentSize();

            this.RegisterContextChangedCallback<Model>(context => SetModel(context.context));

            m_ControlContent = new VisualElement()
            {
                name = "control-content",
                pickingMode = PickingMode.Ignore,
                style =
                {
                    flexGrow = 1f
                }
            };

            m_ControlTopContent = new VisualElement()
            {
                name = "control-top-content",
            };
            
            m_ControlMiddleContent = new VisualElement()
            {
                name = "control-middle-content",
                pickingMode = PickingMode.Ignore,
                style =
                {
                    flexGrow = 1f
                }
            };
            
            m_ControlBottomContent = new VisualElement()
            {
                name = "control-bottom-content",
            };
            
            m_ControlContent.Add(m_ControlTopContent);
            m_ControlContent.Add(m_ControlMiddleContent);
            m_ControlContent.Add(m_ControlBottomContent);
            
            hierarchy.Add(m_ControlContent);
        }

        void OnPreferencesChanged()
        {
            m_Canvas.controlScheme = GlobalPreferences.canvasControlScheme;
        }

        void FrameArtifact(Artifact artifact)
        {
            // we just have one node in the canvas so we just need to check the first child
            if (m_Canvas.childCount == 0 || m_Canvas.ElementAt(0) is not ArtifactNode node || node.artifact != artifact)
                return;

            FrameAll();
        }

        public void FrameAll()
        {
            //Scheduling since it can be done too quickly when Unity editor is opened while the
            //window was open before the editor was closed.
            schedule.Execute(() =>
            {
                UpdateCanvasFrameContainer();
                m_Canvas.FrameAll();
            });
        }
        public void SetModel(Model model)
        {
            UnSubscribeToModelEvents();
            m_CurrentModel = model;
            SubscribeToModelEvents();
        }
        
        public void UpdateCanvasFrameContainer()
        {
            if (m_CurrentModel == null)
                return;
            
            var leftOverlay = m_CurrentModel.LeftOverlay.resolvedStyle;
            var rightOverlay = m_CurrentModel.RightOverlay.resolvedStyle;
            
            var width = resolvedStyle.width - 
                        leftOverlay.width - 
                        leftOverlay.marginLeft -
                        leftOverlay.marginRight -
                        rightOverlay.width -
                        rightOverlay.marginLeft -
                        rightOverlay.marginRight;
            var x = leftOverlay.width + 
                    leftOverlay.marginLeft + 
                    leftOverlay.marginRight;
            const int y = k_FrameTopOffset;
            var height = resolvedStyle.height - y;
            
            SetFrameContainer(new Rect(x, y, width, height));
        }

        void SubscribeToModelEvents()
        {
            if (m_CurrentModel == null)
                return;

            m_CurrentModel.OnDispose += OnModelDispose;
            m_CurrentModel.OnActiveToolChanged += OnActiveToolChanged;
            m_CurrentModel.OnFrameArtifactRequested += FrameArtifact;
            m_CurrentModel.OnDispose += UnSubscribeToModelEvents;
            m_CurrentModel.OnCanvasRefineArtifact += OnRefineArtifact;
            m_CurrentModel.OnRefineArtifact += OnRefineArtifact;
            m_CurrentModel.OnArtifactSelected += OnArtifactSelected;
            m_CurrentModel.OnFinishRefineArtifact += OnFinishRefineArtifact;
            m_CurrentModel.OnLeftOverlayChanged += OnLeftOverlayChanged;
            m_CurrentModel.OnRightOverlayChanged += OnRightOverlayChanged;
        }

        void OnLeftOverlayChanged(VisualElement overlay)
        {
            UpdateCanvasFrameContainer();
        }
        
        void OnRightOverlayChanged(VisualElement overlay)
        {
            UpdateCanvasFrameContainer();
        }

        private void OnFinishRefineArtifact(Artifact obj)
        {
            refinedArtifact = null;
            UpdateView();
        }

        void OnArtifactSelected(Artifact artifact)
        {
            if (m_CurrentModel == null || !m_CurrentModel.isRefineMode)
                return;

            OnActiveToolChanged(m_CurrentModel.ActiveTool);
            OnRefineArtifact(artifact);
        }
        
        void OnRefineArtifact(Artifact artifact)
        {
            if (m_CurrentModel == null)
                return;

            refinedArtifact = artifact;
        }

        void UnSubscribeToModelEvents()
        {
            if (m_CurrentModel == null)
                return;

            m_CurrentModel.OnDispose -= OnModelDispose;
            m_CurrentModel.OnActiveToolChanged -= OnActiveToolChanged;
            m_CurrentModel.OnFrameArtifactRequested -= FrameArtifact;
            m_CurrentModel.OnCanvasRefineArtifact -= OnRefineArtifact;
            m_CurrentModel.OnRefineArtifact -= OnRefineArtifact;
            m_CurrentModel.OnArtifactSelected -= OnArtifactSelected;
            m_CurrentModel.OnFinishRefineArtifact -= OnFinishRefineArtifact;
            m_CurrentModel.OnLeftOverlayChanged -= OnLeftOverlayChanged;
            m_CurrentModel.OnRightOverlayChanged -= OnRightOverlayChanged;
        }

        void OnModelDispose()
        {
            SetModel(null);
        }

        void OnPanChanged(bool panEnabled)
        {
            foreach (var item in m_Canvas.Children())
            {
                item.SetEnabled(!panEnabled);
                item.pickingMode = panEnabled ? PickingMode.Ignore : PickingMode.Position;
                item.EnableInClassList("cursor--grab", panEnabled);
            }
            
            primaryManipulator = panEnabled ? AppUI.UI.CanvasManipulator.Pan : AppUI.UI.CanvasManipulator.None;
        }

        void OnActiveToolChanged(ICanvasTool tool)
        {
            if (m_CurrentToolManipulator != null)
                m_Canvas.RemoveManipulator(m_CurrentToolManipulator);
            
            OnPanChanged(tool is PanTool);
            
            if (tool == null)
                return;

            m_CurrentToolManipulator = tool.GetToolManipulator();
            if (m_CurrentToolManipulator != null)
                m_Canvas.AddManipulator(m_CurrentToolManipulator);
        }

        public void UpdateView()
        {
            Clear();
            if (m_CurrentModel == null || refinedArtifact is null)
                return;

            var node = new ArtifactNode
            {
                artifact = refinedArtifact
            };
            Add(node);
        }
    }
}
