using System;
using System.Diagnostics;
using Hsm;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using Unity.Muse.AppUI.UI;

namespace Unity.Muse.Animate
{
    partial class Application
    {
        partial class ApplicationHsm
        {
            public abstract class ApplicationState<T> : State
            {
                protected T Context => m_Context;

                T m_Context;

                public override void OnEnter()
                {
                    base.OnEnter();
                    throw new Exception("States must be passed a context");
                }

                public override void OnEnter(object[] aArgs)
                {
                    base.OnEnter(aArgs);

                    Assert.IsTrue(aArgs != null && aArgs.Length == 1, "Invalid state args");
                    m_Context = (T)aArgs[0];
                }

                internal void Log(string msg)
                {
                    DevLogger.LogSeverity(TraceLevel.Info, GetType().Name + " -> " + msg);
                }
            }

            public class Root : StateWithOwner<Application>
            {
                protected ApplicationContext Context { get; private set; }

                static KeyCode[] s_AllKeyCodes = (KeyCode[])System.Enum.GetValues(typeof(KeyCode));
                bool m_WasAnyKey;

                Transition m_NextTransition;

                MenuItemModel[] m_AppMenuItems;

                public override Transition GetTransition()
                {
                    if (IsInInnerState<SceneLoaded>())
                        m_NextTransition = Transition.Inner<Author>(Context.CreateAuthoringContext());

                    return m_NextTransition;
                }

                public override void OnEnter()
                {
                    Context = Owner.ApplicationContext;
                    m_NextTransition = Transition.None();

                    var uiApplication = Context.RootVisualElement.Q<Panel>();
                    uiApplication.scale = "medium";

                    // Handle 3D Viewport Background Click
                    Context.CameraMovementViewModel.OnClickedWithoutDragging += OnBackgroundClicked;

                    if (EventSystem.current == null)
                    {
                        DevLogger.LogSeverity(TraceLevel.Verbose, "EventSystem.current = null");
                    }
                    else
                    {
                        DevLogger.LogSeverity(TraceLevel.Verbose, "EventSystem.current = " + EventSystem.current.name);
                    }

                    m_AppMenuItems = new MenuItemModel[]
                    {
                        new("Save Session", () => Instance.PublishMessage<SaveSessionMessage>())
                    };

                    Context.ApplicationMenuUIModel.AddItems(m_AppMenuItems);

                    Instance.SubscribeToMessage<LoadSessionMessage>(LoadSession);

                    NewSession();
                }

                public override void OnExit()
                {
                    Context.CameraMovementViewModel.OnClickedWithoutDragging -= OnBackgroundClicked;
                    
                    Context.ApplicationMenuUIModel.RemoveItems(m_AppMenuItems);

                    Instance.UnsubscribeFromMessage<LoadSessionMessage>(LoadSession);
                }

                public override void Update(float aDeltaTime)
                {
                    base.Update(aDeltaTime);
                    UpdateKeyPresses();
                    UpdateCamera(aDeltaTime);
                }

                void NewSession()
                {
                    m_NextTransition = Transition.Inner<CreateNewScene>(Context);
                }

                void LoadSession(LoadSessionMessage message)
                {
                    LoadSession(message.JsonData);
                }

                void LoadSession(string jsonData)
                {
                    if (!string.IsNullOrEmpty(jsonData))
                    {
                        m_NextTransition = Transition.Inner<LoadScene>(Context, jsonData);
                    }
                }

                void UpdateKeyPresses()
                {
                    // Disabling this if we receive the inputs from the editor
#if UNITY_EDITOR
                    // Note: this is far from the ideal solution but this is what we have for now...
                    if (!Input.anyKey && !Input.anyKeyDown && !m_WasAnyKey)
                        return;
                    
                    for (var i = 0; i < s_AllKeyCodes.Length; i++)
                    {
                        var keyCode = s_AllKeyCodes[i];
                        if (keyCode == KeyCode.None)
                            continue;

                        if (Input.GetKeyDown(keyCode))
                        {
                            KeyPressEvent.Pool.Get(out var ev);
                            ev.KeyCode = keyCode;
                            StateMachine.SendKeyDownEvent(ev);
                            KeyPressEvent.Pool.Release(ev);
                        }
                        else if (Input.GetKey(keyCode))
                        {
                            KeyPressEvent.Pool.Get(out var ev);
                            ev.KeyCode = keyCode;
                            StateMachine.SendKeyHoldEvent(ev);
                            KeyPressEvent.Pool.Release(ev);
                        }
                        else if (Input.GetKeyUp(keyCode))
                        {
                            KeyPressEvent.Pool.Get(out var ev);
                            ev.KeyCode = keyCode;
                            StateMachine.SendKeyUpEvent(ev);
                            KeyPressEvent.Pool.Release(ev);
                        }
                    }

                    m_WasAnyKey = Input.anyKey || Input.anyKeyDown;
#endif
                }

                void UpdateCamera(float deltaTime)
                {
                    // Mouse wheel zoom
                    Context.CameraMovement.Update(deltaTime);
                    SaveCameraViewpoint();
                }

                void OnBackgroundClicked(CameraMovementViewModel model, PointerEventData eventData)
                {
                    if (ApplicationConstants.DebugStatesInputEvents)
                        Log("OnBackgroundClicked()");

                    StateMachine.SendPointerClickEvent(eventData);
                }

                void SaveCameraViewpoint()
                {
                    var cameraViewpoint = Context.Stage.NumCameraViewpoints == 0
                        ? Context.Stage.AddCameraViewpoint()
                        : Context.Stage.GetCameraViewpoint(0);

                    cameraViewpoint.SetCoordinates(Context.CameraMovement.Pivot, Context.Camera.Position);
                }

                void Log(string msg)
                {
                    DevLogger.LogSeverity(TraceLevel.Info, GetType().Name + " -> " + msg);
                }
            }
        }
    }
}
