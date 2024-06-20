using UnityEngine;

namespace Unity.Muse.Animate
{
    static class PhysicsUtils
    {
        public static void InitializeRigidBodies(this GameObject gameObject, bool isKinematic, bool resetVelocities)
        {
            if (gameObject == null)
                return;

            var rigidBodies = gameObject.GetComponentsInChildren<Rigidbody>();
            for (var i = 0; i < rigidBodies.Length; i++)
            {
                var rigidBody = rigidBodies[i];

                rigidBody.isKinematic = isKinematic;

                if (resetVelocities && !rigidBody.isKinematic)
                {
                    rigidBody.velocity = Vector3.zero;
                    rigidBody.angularVelocity = Vector3.zero;
                }
            }
        }

        public static void SetLayerCollisionMatrix()
        {
            // First disable all collisions between Muse Animate layers
            for (var i = 0; i < 32; i++)
            {
                Physics.IgnoreLayerCollision(ApplicationLayers.LayerEnvironment, i, true);
                Physics.IgnoreLayerCollision(ApplicationLayers.LayerHandles, i, true);
                Physics.IgnoreLayerCollision(ApplicationLayers.LayerPosing, i, true);
                Physics.IgnoreLayerCollision(ApplicationLayers.LayerBaking, i, true);
                Physics.IgnoreLayerCollision(ApplicationLayers.LayerThumbnail, i, true);
            }
            
            // Then enable collisions between Muse Animate layers
            Physics.IgnoreLayerCollision(ApplicationLayers.LayerPosing, ApplicationLayers.LayerPosing, false);
            Physics.IgnoreLayerCollision(ApplicationLayers.LayerPosing, ApplicationLayers.LayerEnvironment, false);
            Physics.IgnoreLayerCollision(ApplicationLayers.LayerBaking, ApplicationLayers.LayerEnvironment, false);
        }
    }
}
