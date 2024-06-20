using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Unity.AppUI.Core
{
    enum GestureType
    {
        Unknown,
        Pan,
        Mag
    }
    
    [StructLayout(LayoutKind.Sequential)]
    struct PlatformTouchEvent 
    {
        public byte touchId;
        public byte phase;
        public float normalizedX;
        public float normalizedY;
        public float deviceWidth;
        public float deviceHeight;
    }
    
    class PlatformImpl : IPlatformImpl
    {
        const float k_RecognitionThreshold = 0.05f;

        const float k_PanRecognitionDotThreshold = 0.9f;
        
        public PlatformImpl() {}

        event Action<string> systemThemeChangedInternal;

        public event Action<string> systemThemeChanged
        {
            add
            {
                systemThemeChangedInternal += value;
                if (systemThemeChangedInternal != null)
                    m_ThemePollingEnabled = true;
            }
            remove
            {
                systemThemeChangedInternal -= value;
                if (systemThemeChangedInternal == null)
                    m_ThemePollingEnabled = false;
            }
        }

        public event Action<PanGesture> panGestureChanged;

        public event Action<MagnificationGesture> magnificationGestureChanged;

        public virtual float referenceDpi => Platform.baseDpi;

        public virtual float mainScreenScale => 1f;

        public bool panGestureChangedThisFrame { get; protected set; }

        public bool magnificationGestureChangedThisFrame { get; protected set; }

        public PanGesture panGesture
        {
            get => m_PanGesture;
            protected set
            {
                if (m_PanGesture != value)
                {
                    m_PanGesture = value;
                    panGestureChangedThisFrame = true;
                    panGestureChanged?.Invoke(m_PanGesture);
                }
            }
        }

        public MagnificationGesture magnificationGesture 
        {
            get => m_MagnificationGesture;
            protected set
            {
                if (m_MagnificationGesture != value)
                {
                    m_MagnificationGesture = value;
                    magnificationGestureChangedThisFrame = true;
                    magnificationGestureChanged?.Invoke(m_MagnificationGesture);
                }
            }
        }

        public virtual bool isTouchGestureSupported { get; protected set; }

        public virtual string systemTheme => m_LastSystemTheme;

        PanGesture m_PanGesture;
        
        MagnificationGesture m_MagnificationGesture;

        bool m_ThemePollingEnabled;
        
        double m_LastThemePollTime = 0;
        
        string m_LastSystemTheme = "dark";

        readonly List<TrackPadTouch> k_FrameTouches = new List<TrackPadTouch>();

        readonly Dictionary<int, TrackPadTouch> k_PrevTouches = new Dictionary<int, TrackPadTouch>();

        int m_LastGesturePollFrame;
        
        float m_LastGesturePollTime;
        
        TrackPadTouch m_Touch0;
        
        TrackPadTouch m_Touch1;
        
        bool m_TwoFingersUsed;
        
        Vector2 m_StartPos0;
        
        Vector2 m_StartPos1;
        
        Vector2 m_DeltaPos0;
        
        Vector2 m_DeltaPos1;
        
        GestureType m_Gesture;
        
        float m_LastMag;
        
        float m_StartDistance;
        
        Vector2 m_LastPanPos;

#if UNITY_EDITOR
        UnityEditor.EditorWindow m_LastEditorWindow;
#endif
        
        public void PollSystemTheme()
        {
            if (!m_ThemePollingEnabled)
                return;
            
#if UNITY_EDITOR
            var currentTime = UnityEditor.EditorApplication.timeSinceStartup;
#else 
            var currentTime = Time.realtimeSinceStartupAsDouble;
#endif
            
            if (currentTime <= 2.0)
                return;
            
            if (currentTime - m_LastThemePollTime > 2.0)
            {
                m_LastThemePollTime = currentTime;
                
                var newTheme = systemTheme;
                if (m_LastSystemTheme != newTheme)
                {
                    systemThemeChangedInternal?.Invoke(newTheme);
                    m_LastSystemTheme = newTheme;
                }
            }
        }
        
        public void PollGestures()
        {
            panGestureChangedThisFrame = false;
            magnificationGestureChangedThisFrame = false;
            
            var touches = GetTrackpadTouches();
            
            isTouchGestureSupported = isTouchGestureSupported || touches.Count > 0;
            
            foreach (var touch in touches)
            {
                if (touch.phase == TouchPhase.Began)
                {
                    // check if we already have another touch
                    if (m_Touch0.fingerId == -1 || m_Touch0.fingerId == touch.fingerId)
                    {
                        m_Touch0 = touch;
                    }
                    else if (m_Touch1.fingerId == -1 || m_Touch1.fingerId == touch.fingerId)
                    {
                        if (touch.fingerId == m_Touch0.fingerId)
                            Debug.LogError("The second finger ID can't be the same as the first one");
                        m_Touch1 = touch;
                        m_TwoFingersUsed = true;
                        m_StartPos0 = m_Touch0.position;
                        m_StartPos1 = m_Touch1.position;
                        m_DeltaPos0 = Vector2.zero;
                        m_DeltaPos1 = Vector2.zero;
                    }
                    
                    if (m_Touch0.fingerId != touch.fingerId && m_Touch1.fingerId != touch.fingerId && m_TwoFingersUsed)
                    {
                        // we can deactivate here since more than 2 fingers are used
                        AbortGesture();
                    }
                }
                else if (touch.phase is TouchPhase.Canceled or TouchPhase.Ended)
                {
                    // check if the touch is one of the tracked ones
                    if (touch.fingerId == m_Touch0.fingerId || touch.fingerId == m_Touch1.fingerId)
                        AbortGesture(touch.phase);
                }

                if (m_TwoFingersUsed && touch.phase is TouchPhase.Moved or TouchPhase.Stationary)
                {
                    // store touch data if its one of the tracked ones
                    if (touch.fingerId == m_Touch0.fingerId)
                    {
                        m_Touch0 = touch;
                    }
                    else if (touch.fingerId == m_Touch1.fingerId)
                    {
                        m_Touch1 = touch;
                    }
                }
            }

            if (m_TwoFingersUsed)
            {
                // compute deltas
                m_DeltaPos0 = m_Touch0.position - m_StartPos0;
                m_DeltaPos1 = m_Touch1.position - m_StartPos1;

                if (m_Gesture == GestureType.Unknown)
                {
                    // need to recognize the movement
                    if (m_DeltaPos0.magnitude > k_RecognitionThreshold && m_DeltaPos1.magnitude > k_RecognitionThreshold)
                    {
                        m_LastMag = 1f;
                        m_StartDistance = Vector2.Distance(m_Touch0.position, m_Touch1.position);
                        m_LastPanPos = m_Touch0.position;

                        // can recognize
                        if (Vector2.Dot(m_DeltaPos0.normalized, m_DeltaPos1.normalized) > k_PanRecognitionDotThreshold)
                        {
                            // pan
                            m_Gesture = GestureType.Pan;
                            panGesture = new PanGesture(m_Touch0.position, Vector2.zero, TouchPhase.Began);
                        }
                        else 
                        {
                            // mag
                            m_Gesture = GestureType.Mag;
                            magnificationGesture = new MagnificationGesture(0f, TouchPhase.Began);
                        }
                    }
                }
                else if (m_Gesture == GestureType.Mag)
                {
                    var d = Vector2.Distance(m_Touch0.position, m_Touch1.position);
                    var mag = d / m_StartDistance;
                    var magDelta = mag - m_LastMag;
                    m_LastMag = mag;
                    magnificationGesture = new MagnificationGesture(magDelta, TouchPhase.Moved);
                }
                else if (m_Gesture == GestureType.Pan)
                {
                    var pos = m_Touch0.position;
                    var panDelta = pos - m_LastPanPos;
                    m_LastPanPos = pos;
                    panGesture = new PanGesture(pos, panDelta, TouchPhase.Moved);
                }
            }
        }

        List<TrackPadTouch> GetTrackpadTouches() 
        {
            m_LastGesturePollTime = Time.unscaledTime;
            if ((Application.isPlaying && m_LastGesturePollFrame != Time.frameCount) || !Application.isPlaying)
            {
                m_LastGesturePollFrame = Time.frameCount;
                
                k_PrevTouches.Clear();
                foreach (var touch in k_FrameTouches)
                {
                    k_PrevTouches[touch.fingerId] = touch;
                }

                k_FrameTouches.Clear();
                
                PlatformTouchEvent e;
                e.touchId = 0;
                e.phase = 0;
                e.normalizedX = 0;
                e.normalizedY = 0;
                e.deviceWidth = 0;
                e.deviceHeight = 0;

#if UNITY_EDITOR
                var currentWindow = UnityEditor.EditorWindow.focusedWindow;
                if (currentWindow != m_LastEditorWindow && currentWindow)
                    SetupFocusedTrackingObject();
                m_LastEditorWindow = currentWindow;
                
                if (currentWindow)
#endif
                {
                    while (ReadTouch(ref e))
                    {
                        var screenPos = new Vector2(e.normalizedX, e.normalizedY);
                        var deltaPos = new Vector2(0, 0);

                        if (k_PrevTouches.TryGetValue(e.touchId, out var prevTouch))
                            deltaPos = screenPos - prevTouch.position;

                        var timeDelta = Time.unscaledTime - m_LastGesturePollTime;
                        var phase = e.phase switch 
                        {
                            0 => TouchPhase.Began,
                            1 => TouchPhase.Moved,
                            2 => TouchPhase.Ended,
                            3 => TouchPhase.Canceled,
                            4 => TouchPhase.Stationary,
                            _ => TouchPhase.Ended
                        };
                        var newTouch = new TrackPadTouch(e.touchId, screenPos, 1, deltaPos, timeDelta, phase);
                        k_FrameTouches.Add(newTouch);
                    }
                }

                m_LastGesturePollTime = Time.unscaledTime;
            }
            
            return k_FrameTouches;
        }

        void AbortGesture(TouchPhase phase = TouchPhase.Canceled)
        {
            m_TwoFingersUsed = false;
            m_Gesture = GestureType.Unknown;
            m_Touch0 = new TrackPadTouch(-1, Vector2.zero, 0, Vector2.zero, 0, TouchPhase.Canceled);
            m_Touch1 = new TrackPadTouch(-1, Vector2.zero, 0, Vector2.zero, 0, TouchPhase.Canceled);
            panGesture = new PanGesture(Vector2.zero, Vector2.zero, phase);
            magnificationGesture = new MagnificationGesture(0f, phase);
        }
        
        protected virtual void SetupFocusedTrackingObject() {}
        
        protected virtual bool ReadTouch(ref PlatformTouchEvent e) => false;

        public virtual void RunHapticFeedback(HapticFeedbackType feedbackType)
        {
            Debug.LogWarning(Application.isEditor
                ? "Haptic Feedbacks are not supported in the Editor."
                : "Haptic Feedbacks are not supported on the current platform.");
        }
        
        public virtual void HandleNativeMessage(string message) {}
    }
}
