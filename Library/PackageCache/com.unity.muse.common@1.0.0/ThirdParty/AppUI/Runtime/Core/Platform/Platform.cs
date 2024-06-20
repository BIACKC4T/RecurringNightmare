#define APPUI_PLATFORM_EDITOR_ONLY
using System;
using UnityEngine;

namespace Unity.AppUI.Core
{
    /// <summary>
    /// Utility methods and properties related to the Target Platform.
    /// </summary>
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    public static class Platform
    {
        static IPlatformImpl s_Impl;
        
        static Platform()
        {
            Initialize();
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Initialize()
        {
            if (s_Impl != null)
                return;
            
#if APPUI_PLATFORM_EDITOR_ONLY
            
#if UNITY_EDITOR_OSX
            s_Impl = new OSXPlatformImpl();
#elif UNITY_EDITOR_WIN
            s_Impl = new WindowsPlatformImpl();
#else 
            s_Impl = new PlatformImpl();
#endif
            
#else // APPUI_PLATFORM_EDITOR_ONLY

#if UNITY_IOS && !UNITY_EDITOR
            s_Impl = new IOSPlatformImpl();
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            s_Impl = new OSXPlatformImpl();
#elif UNITY_ANDROID && !UNITY_EDITOR
            s_Impl = new AndroidPlatformImpl();
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            s_Impl = new WindowsPlatformImpl();
#else 
            s_Impl = new PlatformImpl();
#endif
            
#endif // APPUI_PLATFORM_EDITOR_ONLY
        }
        
        /// <summary>
        /// The base DPI value used in <see cref="UnityEngine.UIElements.PanelSettings"/>.
        /// </summary>
        public const float baseDpi = 96f;

        /// <summary>
        /// The DPI value that should be used in UI-Toolkit PanelSettings
        /// <see cref="UnityEngine.UIElements.PanelSettings.referenceDpi"/>.
        /// <para>
        /// This value is the value of <see cref="Screen.dpi"/> divided by the main screen scale factor.
        /// </para>
        /// </summary>
        public static float referenceDpi => s_Impl.referenceDpi;

        /// <summary>
        /// The main screen scale factor.
        /// <remarks>
        /// The "main" screen is the current screen used at highest priority to display the application window.
        /// </remarks>
        /// </summary>
        public static float mainScreenScale => s_Impl.mainScreenScale;

        /// <summary>
        /// Event triggered when the system theme changes.
        /// </summary>
        public static event Action<string> systemThemeChanged
        {
            add => s_Impl.systemThemeChanged += value;
            remove => s_Impl.systemThemeChanged -= value;
        }

        /// <summary>
        /// Polls the system theme and triggers the <see cref="systemThemeChanged"/> event if the theme has changed.
        /// </summary>
        internal static void PollSystemTheme()
        {
            s_Impl.PollSystemTheme();
        }

        internal static void PollGestures()
        {
            s_Impl.PollGestures();
        }

        /// <summary>
        /// Event triggered when a pan gesture is received.
        /// </summary>
        public static event Action<PanGesture> panGestureChanged
        {
            add => s_Impl.panGestureChanged += value;
            remove => s_Impl.panGestureChanged -= value;
        }
        
        /// <summary>
        /// Event triggered when a magnification gesture is received.
        /// </summary>
        public static event Action<MagnificationGesture> magnificationGestureChanged
        {
            add => s_Impl.magnificationGestureChanged += value;
            remove => s_Impl.magnificationGestureChanged -= value;
        }
        
        /// <summary>
        /// Whether the pan gesture has changed this frame.
        /// </summary>
        public static bool panGestureChangedThisFrame => s_Impl.panGestureChangedThisFrame;
        
        /// <summary>
        /// Whether the magnification gesture has changed this frame.
        /// </summary>
        public static bool magnificationGestureChangedThisFrame => s_Impl.magnificationGestureChangedThisFrame;

        /// <summary>
        /// The pan gesture data.
        /// </summary>
        public static PanGesture panGesture => s_Impl.panGesture;

        /// <summary>
        /// The magnification gesture data.
        /// </summary>
        public static MagnificationGesture magnificationGesture => s_Impl.magnificationGesture;

        /// <summary>
        /// Whether the current platform supports touch gestures.
        /// </summary>
        public static bool isTouchGestureSupported => s_Impl.isTouchGestureSupported;

        /// <summary>
        /// The current system theme.
        /// </summary>
        public static string systemTheme => s_Impl.systemTheme;

        /// <summary>
        /// Run a haptic feedback on the current platform.
        /// </summary>
        /// <param name="feedbackType">The type of haptic feedback to trigger.</param>
        public static void RunHapticFeedback(HapticFeedbackType feedbackType)
        {
            s_Impl.RunHapticFeedback(feedbackType);
        }
        
        /// <summary>
        /// Handle an native message coming from a native App UI plugin.
        /// </summary>
        /// <param name="message"> The message to handle.</param>
        internal static void HandleNativeMessage(string message)
        {
            s_Impl.HandleNativeMessage(message);
        }
    }
}
