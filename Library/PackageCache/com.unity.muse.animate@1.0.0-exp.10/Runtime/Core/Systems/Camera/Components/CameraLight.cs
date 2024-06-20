using UnityEngine;

namespace Unity.Muse.Animate
{
    class CameraLight : MonoBehaviour
    {
        public Transform LightA;
        public Transform LightB;
    
        public Quaternion OffsetA;
        public Quaternion OffsetB;
    
        // Start is called before the first frame update
        void Start()
        {
            if (LightA != null)
            {
                OffsetA = LightA.rotation;
            }

            if (LightB != null)
            {
                OffsetB = LightB.rotation;
            }
        }

        // Update is called once per frame
        void LateUpdate()
        {
            if (LightA != null)
            {
                LightA.rotation = transform.rotation * OffsetA;
            }
        
            if (LightB != null)
            {
                LightB.rotation = transform.rotation * OffsetB;
            }
        }
    }
}