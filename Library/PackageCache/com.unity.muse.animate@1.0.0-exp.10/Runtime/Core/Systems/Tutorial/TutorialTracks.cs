using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.Muse.Animate
{
    static class TutorialTracks
    {
        const string k_TextureBasePath = "Packages/com.unity.muse.animate/Runtime/PackageResources/Textures/Tutorial/TrackBasics";
        const string k_TrackBasics = "basics";
        const string k_TrackBasics00 = "step00";
        const string k_TrackBasics01 = "step01";
        const string k_TrackBasics02 = "step02";
        const string k_TrackBasics03 = "step03";

        static TutorialLogic s_Logic;

        public static void Init(TutorialLogic logic)
        {
            s_Logic = logic;
            s_Logic.OnStepAction += OnStepAction;
            s_Logic.OnStepOpened += OnStepOpened;

            InitBasics();
        }

        public static void StartTutorial()
        {
            s_Logic.OpenStep(k_TrackBasics, k_TrackBasics00);
        }

        static void InitBasics()
        {
            s_Logic.AddTrack(k_TrackBasics, "The Basics", $"Tutorial for {ApplicationConstants.ApplicationName}");
            s_Logic.AddStep(k_TrackBasics,
                new TutorialTrackStepData()
                {
                    StepId = k_TrackBasics00,
                    Title = "The Basics",
                    Body = $"Pose your character using <b><color=orange>Effectors</color></b>.\n{ApplicationConstants.ApplicationName} uses AI to generate\nnatural motion between pose keyframes.",
                    TargetUI = "",
                    NextButtonLabel = "Next tip",
                    ShowDismissButton = true,
                    DismissButtonLabel = "Let's animate",
                    BackgroundImage = LoadTexture(k_TextureBasePath + "/step00.png"),
                }
            );
            
            s_Logic.AddStep(k_TrackBasics,
                new TutorialTrackStepData()
                {
                    StepId = k_TrackBasics01,
                    Title = "Effectors",
                    Body = $"Activate an effector by moving\nor rotating it. Select multiple\neffectors with the Shift key.\nDeactivate effectors with the\nDelete key, delete icon, or right-\nclick menu. {ApplicationConstants.ApplicationName} works\nbetter with fewer effectors.",
                    TargetUI = "",
                    PreviousButtonLabel = "Previous tip",
                    NextButtonLabel = "Next tip",
                    ShowDismissButton = true,
                    DismissButtonLabel = "Let's animate",
                    BackgroundImage = LoadTexture(k_TextureBasePath + "/step01.png"),
                }
            );
            
            s_Logic.AddStep(k_TrackBasics,
                new TutorialTrackStepData()
                {
                    StepId = k_TrackBasics02,
                    Title = "Oops?",
                    Body = "Did you get a little carried away with\nyour effectors? It's easy to reset your\npose and start again. Just right-click\non your character for this and more\nhelpful pose options.",
                    TargetUI = "",
                    PreviousButtonLabel = "Previous tip",
                    NextButtonLabel = "Final tip",
                    ShowDismissButton = true,
                    DismissButtonLabel = "Let's animate",
                    BackgroundImage = LoadTexture(k_TextureBasePath + "/step02.png"),
                }
            );
            
            s_Logic.AddStep(k_TrackBasics,
                new TutorialTrackStepData()
                {
                    StepId = k_TrackBasics03,
                    Title = "Keyframes",
                    Body = $"Create several pose keyframes,\nand {ApplicationConstants.ApplicationName} will generate\nnatural motion in between.\nRight-click keyframes for more\noptions. {ApplicationConstants.ApplicationName} works\nbetter with fewer keyframes.",
                    TargetUI = "",
                    PreviousButtonLabel = "Previous tip",
                    ShowAcceptButton = true,
                    AcceptButtonLabel = "Let's animate",
                    ShowDismissButton = false,
                    BackgroundImage = LoadTexture(k_TextureBasePath + "/step03.png"),
                }
            );
        }

        static Texture2D LoadTexture(string path)
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
#else
            return null;
#endif
        }
        
        static void OnStepOpened(in TutorialTrackData trackData, in TutorialTrackStepData stepData)
        {
            switch (trackData.TrackId)
            {
                case k_TrackBasics:
                    DeepPoseAnalytics.SendTutorialStepOpenedEvent(
                        stepData.StepId, 
                        trackData.NbSteps > 0 ? (float)(stepData.Index+1)/trackData.NbSteps : -1
                    );
                    return;
            }
        }
        
        static void OnStepAction(in TutorialTrackData trackData, in TutorialTrackStepData stepData, TutorialLogic.ActionType actionType)
        {
            switch (trackData.TrackId)
            {
                case k_TrackBasics:
                    OnBasicsAction(stepData, actionType);
                    return;
            }
        }

        static void OnBasicsAction(in TutorialTrackStepData stepData, TutorialLogic.ActionType actionType)
        {
            if (stepData.StepId == k_TrackBasics03 && actionType == TutorialLogic.ActionType.Accept)
            {
                s_Logic.RunShrinkAnimation();
            }
        }
    }
}
