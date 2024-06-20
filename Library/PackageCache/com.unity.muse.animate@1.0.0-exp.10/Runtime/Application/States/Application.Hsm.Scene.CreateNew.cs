using Hsm;
using UnityEngine;

namespace Unity.Muse.Animate
{
    partial class Application
    {
        partial class ApplicationHsm
        {
            public class CreateNewScene : ApplicationState<ApplicationContext>
            {
                bool m_SceneLoaded;

                public override Transition GetTransition()
                {
                    if (m_SceneLoaded)
                        return Transition.Sibling<SceneLoaded>(Context);

                    return Transition.None();
                }

                public override void OnEnter(object[] args)
                {
                    base.OnEnter(args);

                    m_SceneLoaded = false;
                }

                public override void Update(float aDeltaTime)
                {
                    base.Update(aDeltaTime);

                    if (m_SceneLoaded)
                        return;

                    // Destroying any existing scene
                    Context.Stage.Clear();

                    // Add a few actors
                    Context.Stage.CreateActor("biped", Vector3.zero, Quaternion.identity);
                    //Context.Stage.CreateActor("biped", Vector3.forward, Quaternion.AngleAxis(180f, Vector3.up));
                    //Context.Stage.CreateActor("quadruped", new Vector3(1f, 0f, 0.5f), Quaternion.AngleAxis(90f, Vector3.up));

                    // Add a few props
                    //Context.Stage.CreateProp("cube_50", new Vector3(-1f, 0f, 0.5f), Quaternion.AngleAxis(45f, Vector3.up));

                    // Set camera position
                    var cameraRotation = Quaternion.Euler(13f, 128f, 0f);
                    Context.CameraMovement.SetPivotAndOrbit(Vector3.zero, cameraRotation);
                    Context.CameraMovement.Center(new Vector3(0f, 0.7f, 0f), 3.5f, true);

                    // Loading is done
                    m_SceneLoaded = true;
                    
                    TutorialTracks.Init(Context.Tutorial);
                    
                    StartTutorialIfUserHasNotSeenIt();
                }

                private void StartTutorialIfUserHasNotSeenIt()
                {
                    var hasUserSeenTutorial 
                        = PlayerPrefs.GetInt(ApplicationConstants.TutorialSeenPlayerPrefsKey, defaultValue: 0) != 0;
                    if (hasUserSeenTutorial) return;
                    
                    TutorialTracks.StartTutorial();

                    PlayerPrefs.SetInt(ApplicationConstants.TutorialSeenPlayerPrefsKey, 1);
                }
            }
        }
    }
}
