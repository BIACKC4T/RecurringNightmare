using System;
using Hsm;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Muse.AppUI.UI;

namespace Unity.Muse.Animate
{
    partial class Application
    {
        partial class ApplicationHsm
        {
            public class AuthorTimelineKeyLoop : ApplicationState<AuthorTimelineKeyLoopContext>, IPointerClickHandler, IKeyDownHandler
            {
                ActionButton m_TranslateActionButton;
                ActionButton m_RotateActionButton;

                Transition m_NextTransition = Transition.None();

                public override Transition GetTransition()
                {
                    return m_NextTransition;
                }

                public override void OnEnter(object[] args)
                {
                    base.OnEnter(args);
                    UndoRedoLogic.Instance.SetInitialCheckpoint();

                    SetupUI();
                    RegisterEvents();
                    Context.LoopAuthoringLogic.IsVisible = true;

                    // If no tool is currently selected, select the default tool
                    if (Context.AuthoringModel.LoopTool == AuthoringModel.LoopToolType.None)
                        ChangeTool(AuthoringModel.LoopToolType.Translate);

                    // Reflect the currently selected tool
                    UpdateTool();
                }

                public override void OnExit()
                {
                    base.OnExit();

                    CloseUI();
                    UnregisterEvents();
                    Context.LoopAuthoringLogic.IsVisible = false;
                }

                void RegisterEvents()
                {
                    Context.LoopKeyToolbar.OnRequestedTool += ChangeTool;
                    Context.AuthoringModel.OnLoopToolChanged += OnToolChanged;
                }

                void UnregisterEvents()
                {
                    Context.LoopKeyToolbar.OnRequestedTool -= ChangeTool;
                    Context.AuthoringModel.OnLoopToolChanged -= OnToolChanged;
                }

                public void OnPointerClick(PointerEventData eventData)
                {
                    if (ApplicationConstants.DebugStatesInputEvents) Log($"OnPointerClick({eventData.button})");

                    Context.EntitySelection.Clear();
                    eventData.Use();
                }

                public void OnKeyDown(KeyPressEvent eventData)
                {
                    if (ApplicationConstants.DebugStatesInputEvents) Log($"OnKeyDown({eventData.KeyCode})");

                    switch (eventData.KeyCode)
                    {
                        case KeyCode.W:
                            ChangeTool(AuthoringModel.LoopToolType.Translate);
                            eventData.Use();
                            break;

                        case KeyCode.E:
                            ChangeTool(AuthoringModel.LoopToolType.Rotate);
                            eventData.Use();
                            break;
                    }
                }

                void SetupUI()
                {
                    Context.AuthoringModel.Title = "Editing Loop Key";

                    // Show the toolbars used
                    Context.LoopKeyToolbar.IsVisible = true;
                    Context.UndoRedoToolbar.IsVisible = true;
                }

                void CloseUI()
                {
                    // Hide the toolbars used
                    Context.LoopKeyToolbar.IsVisible = false;
                    Context.UndoRedoToolbar.IsVisible = false;
                }

                void UpdateToolbar()
                {
                    Context.LoopKeyToolbar.SelectedTool = Context.AuthoringModel.LoopTool;
                }

                void OnToolChanged()
                {
                    UpdateTool();
                }

                void UpdateTool()
                {
                    UpdateToolbar();

                    switch (Context.AuthoringModel.LoopTool)
                    {
                        case AuthoringModel.LoopToolType.None:
                            m_NextTransition = Transition.None();
                            break;

                        case AuthoringModel.LoopToolType.Rotate:
                            m_NextTransition = Transition.Inner<AuthorTimelineKeyLoopRotate>(Context);
                            break;

                        case AuthoringModel.LoopToolType.Translate:
                            m_NextTransition = Transition.Inner<AuthorTimelineKeyLoopTranslate>(Context);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(Context.AuthoringModel.LoopTool), Context.AuthoringModel.LoopTool, null);
                    }
                }

                void ChangeTool(AuthoringModel.LoopToolType toolType)
                {
                    Context.AuthoringModel.LoopTool = toolType;
                }
            }
        }
    }
}
