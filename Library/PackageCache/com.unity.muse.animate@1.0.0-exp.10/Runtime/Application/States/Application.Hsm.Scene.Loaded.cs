using Hsm;
using UnityEngine;
using AppUI = Unity.Muse.AppUI.UI;
using AppCore = Unity.AppUI.Core;

namespace Unity.Muse.Animate
{
    partial class Application
    {
        partial class ApplicationHsm
        {
            public class SceneLoaded : ApplicationState<ApplicationContext>
            {
                public override Transition GetTransition()
                {
                    return Transition.None();
                }
            }
        }
    }
}
