using System;
using Unity.Muse.Common;
using UnityEngine;

namespace Unity.Muse.Texture
{
    internal static class PrimitiveObjectsProvider
    {
        public static GameObject GetPrimitiveInstance(PrimitiveObjectTypes primitiveType)
        {
            var path = string.Empty;
            switch (primitiveType)
            {
                case PrimitiveObjectTypes.Sphere:
                    path = PackageResources.spherePreviewModel;
                    break; 
                case PrimitiveObjectTypes.Cube:
                    path = PackageResources.cubePreviewModel;
                    break;
                case PrimitiveObjectTypes.Plane:
                    path = PackageResources.planePreviewModel;
                    break;
                case PrimitiveObjectTypes.Cylinder:
                    path = PackageResources.cylinderPreviewModel;
                    break;
                case PrimitiveObjectTypes.Custom:
                    #if UNITY_EDITOR
                    path = UnityEditor.EditorUtility.OpenFilePanel("Select custom object", "", "fbx");
                    #endif
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(primitiveType), primitiveType, null);
            }

            return GameObject.Instantiate( ResourceManager.Load<GameObject>(path));
        }
    }
}