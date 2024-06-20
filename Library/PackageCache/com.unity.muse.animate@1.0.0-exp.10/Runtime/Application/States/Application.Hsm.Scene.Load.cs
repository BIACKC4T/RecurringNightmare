using Hsm;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Muse.AppUI.UI;
using Unity.AppUI.Core;

namespace Unity.Muse.Animate
{
    partial class Application
    {
        partial class ApplicationHsm
        {
            public class LoadScene : ApplicationState<ApplicationContext>
            {
                string m_JsonToLoad;
                bool m_SceneLoaded;

                public override Transition GetTransition()
                {
                    if (m_SceneLoaded)
                        return Transition.Sibling<SceneLoaded>(Context);

                    return Transition.None();
                }

                public override void OnEnter(object[] args)
                {
                    Assert.AreEqual(2, args.Length);

                    base.OnEnter(new []{ args[0] });

                    m_JsonToLoad = (string)args[1];
                    m_SceneLoaded = false;
                }

                public override void Update(float aDeltaTime)
                {
                    base.Update(aDeltaTime);

                    if (m_SceneLoaded)
                        return;

                    var success = Context.Stage.Load(m_JsonToLoad);

                    if (success)
                    {
                        RestoreCameraViewpoint();
                        
                        foreach (var key in Context.Stage.Timeline.Keys)
                        {
                            Context.ThumbnailsService.RequestThumbnail(key.Key.Thumbnail, key.Key, Context.Camera.Position, Context.Camera.Rotation);
                        }

                        foreach (var take in Context.Stage.TakesLibrary.Takes)
                        {
                            take.RequestThumbnailUpdate(Context.ThumbnailsService, Context.Camera);
                        }

                        var toast = Toast.Build(Context.RootVisualElement, "Scene data loaded",
                            NotificationDuration.Short);
                        toast.SetStyle(NotificationStyle.Positive);
                        toast.SetAnimationMode(AnimationMode.Slide);
                        toast.Show();
                    }
                    else
                    {
                        var toast = Toast.Build(Context.RootVisualElement, "Failed to load scene data",
                            NotificationDuration.Short);
                        toast.SetStyle(NotificationStyle.Warning);
                        toast.SetAnimationMode(AnimationMode.Slide);
                        toast.Show();
                    }

                    //Loading is done
                    m_SceneLoaded = true;
                }

                void RestoreCameraViewpoint()
                {
                    if (Context.Stage.NumCameraViewpoints == 0)
                        return;

                    var cameraCoordinates = Context.Stage.GetCameraViewpoint(0);
                    Context.CameraMovement.SetCoordinates(cameraCoordinates.Pivot, cameraCoordinates.CameraPosition);
                }
            }
        }
    }
}
