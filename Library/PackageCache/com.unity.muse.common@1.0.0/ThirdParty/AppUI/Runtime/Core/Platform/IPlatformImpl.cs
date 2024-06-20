using System;

namespace Unity.AppUI.Core
{
    interface IPlatformImpl
    {
        event Action<string> systemThemeChanged;
        event Action<PanGesture> panGestureChanged;
        event Action<MagnificationGesture> magnificationGestureChanged;
        
        float referenceDpi { get; }
        float mainScreenScale { get; }
        bool panGestureChangedThisFrame { get; }
        bool magnificationGestureChangedThisFrame { get; }
        PanGesture panGesture { get; }
        MagnificationGesture magnificationGesture { get; }
        bool isTouchGestureSupported { get; }
        string systemTheme { get; }

        void PollSystemTheme();
        void PollGestures();
        void RunHapticFeedback(HapticFeedbackType feedbackType);
        void HandleNativeMessage(string message);
    }
}
