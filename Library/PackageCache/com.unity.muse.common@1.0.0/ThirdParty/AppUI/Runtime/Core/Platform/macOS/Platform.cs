#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Unity.AppUI.Core
{
    class OSXPlatformImpl : PlatformImpl
    {
        [DllImport("AppUIMuseNativePlugin")]
        static extern float _NSAppUIScaleFactor();

        [DllImport("AppUIMuseNativePlugin")]
        static extern int _NSCurrentAppearance();

        [DllImport("AppUIMuseNativePlugin")]
        static extern bool _NSReadTouchEvent(ref PlatformTouchEvent e);

        [DllImport("AppUIMuseNativePlugin")]
        static extern void _NSSetupFocusedTrackingObject();
        
        public override float referenceDpi => Screen.dpi / mainScreenScale;

        public override float mainScreenScale => _NSAppUIScaleFactor();
        
        public override string systemTheme => _NSCurrentAppearance() == 2 ? "dark" : "light";

        protected override void SetupFocusedTrackingObject()
        {
            _NSSetupFocusedTrackingObject();
        }

        protected override bool ReadTouch(ref PlatformTouchEvent e)
        {
            if (AppUI.settings && AppUI.settings.enableMacOSGestureRecognition)
                return _NSReadTouchEvent(ref e);
            
            return false;
        }
    }
}
#endif
