using System;
using System.Diagnostics;
using UnityEngine;

namespace Unity.Muse.Animate
{
    static class ApplicationConstants
    {
        #region Debugging

        public static bool DebugRaycastEvents = false;
        public static bool DebugViewportPointerEvents = false;
        public static bool DebugViewportKeyboardEvents = false;
        public static bool DebugViewportManipulatorEvents = false;
        public static bool DebugCameraMovement = false;
        public static bool DebugStatesInputEvents = false;
        public static bool DebugLibraryUI = false;
        public static bool DebugTakesUI = false;
        public static bool DebugMotionToKeysSampling = false;
        public static TraceLevel TraceLevel { get; set; } = TraceLevel.Warning;
        #endregion

        public const int BackgroundCameraOrder = -999;
        public const int EntitiesRaycastOrder = 0;
        public const int EntityEffectorRaycastOrder = 1;
        public const int PosingEffectorRaycastOrder = 2;
        public const int HandleRaycastOrder = 3;
        public const int ForegroundCameraOrder = 999;

        public const float ToleranceToRadiusFactor = 15f;

        /// <summary>
        /// If the camera has smooth motion, allowing the user to "flick" the camera around.
        /// </summary>
        public const bool UseCameraPhysics = false;

        /// <summary>
        /// Defines if we are using the Cloud Inference pipeline for motion synthesis or not
        /// </summary>
        public const bool UseMotionCloudInference = true;

#if LOCAL_CLOUD_INFERENCE
        /// <summary> Host address for the Cloud Inference</summary>
        public const string CloudInferenceHost = "http://localhost:5000";

        /// <summary>Authorization token for the Cloud Inference</summary>
        public const string CloudInferenceAuthorizationToken = "Bearer 123456789";
#else // PRODUCTION
        /// <summary> Host address for the Cloud Inference</summary>
        public const string CloudInferenceHost = "https://musetools-prd-fndabjhyf7dscuby.z01.azurefd.net/";

        /// <summary>Authorization token for the Cloud Inference</summary>
        public const string CloudInferenceAuthorizationToken = "";
#endif

        /// <summary>
        /// MaxNumberOfFrames that a request can generate
        /// </summary>
        public const int MaxCloudInferenceFrames = 5000;

        /// <summary>
        /// The time budget at each frame that can be used when generating an animation
        /// </summary>
        public const float BakingTimeBudget = 0.05f;

        /// <summary>
        /// The time budget allocated for baking when the baking process is throttled
        /// </summary>
        public const float BakingTimeBudgetThrottled = 0.01f;

        /// <summary>
        /// Number of frames per second for authoring animation
        /// IMPORTANT: that should match the FPS of machine learning models
        /// </summary>
        public const float FramesPerSecond = 30f;

        public static Material LoopGhostMaterial = null;
        
        public const string PackagePath = "Packages/com.unity.deeppose-samples";
        public const string AssemblyName = "Unity.Muse.Animate";

        public const string TutorialSeenPlayerPrefsKey = "TutorialSeen";

        /// <summary>
        /// Controls if motion synthesis is enabled. Currently motion synthesis is
        /// quite heavy process on WebGL builds so this serves as the
        /// fallback option for disabling it for the initial release.
        ///
        /// If false, plain motion interpolation is used as the fallback.
        /// </summary>
        public static bool MotionSynthesisEnabled => true;

        /// <summary>
        /// Controls if "AI assisted sampling" is allowed when parsing key poses from a baked timeline.
        /// </summary>
        public static bool AllowAIKeysSampling = false;

        /// <summary>
        /// The official application name.
        /// </summary>
        public const string ApplicationName = "Muse Animate";

        /// <summary>
        /// Defines if we are allowing any direction to be facing the target instead of only the forward one.
        /// </summary>
        public const bool EnableGeneralizedLookAt = false;

        public const bool IsUSDExportEnabled = true;
        public const bool IsFBXExportEnabled = true;

        public const string AuthorizationHeaderName = "Authorization";
        public const string GenesisIdHeaderName = "Genesis-Id";
        
        /// <summary>
        /// Temporary: Newer versions of Unity introduce bugs in the application. We should prohibit
        /// the application from running on these versions until the bugs are fixed.
        /// </summary>
        public static readonly Version MaxUnityVersion = new Version(2023, 2);
        
        public static readonly LightingSettings DefaultLightingSettings = new(
                new Color(0.1843137f, 0.1843137f, 0.1843137f, 1f),
                0.12f,
                new Color(0.7529412f, 0.7529412f, 0.7529412f, 1f),
                new Color(0.2470588f, 0.2470588f, 0.2470588f, 1f),
                new Color(0, 0, 0, 1f));
    }
}
