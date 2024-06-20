using UnityEngine;

namespace Unity.Muse.Animate
{
    class CameraContext
    {
        public CameraModel Camera => m_CameraModel;
        public CameraMovementModel CameraMovement => m_CameraMovementModel;

        readonly CameraModel m_CameraModel;
        readonly CameraMovementModel m_CameraMovementModel;

        public CameraContext(Camera camera)
        {
            m_CameraModel = new CameraModel(camera);
            m_CameraMovementModel = new CameraMovementModel(m_CameraModel);
        }
        
        public void SetViewportOffset(Vector2 offset)
        {
            m_CameraMovementModel.SetViewportOffset(offset);
        }
    }
}
