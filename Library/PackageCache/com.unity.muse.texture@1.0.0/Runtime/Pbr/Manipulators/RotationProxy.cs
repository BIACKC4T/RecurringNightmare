using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.Texture
{
    internal class RotationProxy : Manipulator
    {
        public bool active { get; set; } = true;
        RotationManipulator m_RotationManipulator;

        public RotationProxy(RotationManipulator rotationManipulator)
        {
            m_RotationManipulator = rotationManipulator;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (!active)
                return;

            RotationManipulator.OnMouseMove(evt, false);
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!active)
                return;
            
            target.ReleasePointer(evt.pointerId);
            m_RotationManipulator.OnPointerUp(evt);
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if(!active)
                return;
            m_RotationManipulator.OnPointerMove(evt, false, false);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (!active || evt.button != 0)
                return;

            target.CapturePointer(evt.pointerId);
            m_RotationManipulator.OnPointerDown(evt, false, false);
        }
    }
}