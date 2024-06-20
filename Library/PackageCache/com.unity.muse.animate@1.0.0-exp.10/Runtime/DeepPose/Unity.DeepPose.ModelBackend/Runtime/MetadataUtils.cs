using Unity.Sentis;
using UnityEngine;

#if UNITY_STANDALONE_WIN && DEEPPOSE_ONNXRUNTIME
using Unity.ONNXRuntime;
#endif

namespace Unity.DeepPose.ModelBackend
{
    internal static partial class MetadataUtils
    {
        public static bool TryReadMetaData<T>(this ModelAsset model, out T metadata)
        {
#if UNITY_STANDALONE_WIN && DEEPPOSE_ONNXRUNTIME
            if (model is NNModelONNXRuntime onnxModel)
            {
                return TryReadMetaDataOnnxRuntime(onnxModel, out metadata);
            }
#endif

            // Load data only, no weights
            var loadedModel = ModelLoader.Load(model);
            return TryGetMetaData(loadedModel, out metadata);
        }

        static bool TryGetMetaData<T>(this Model model, out T metadata)
        {
            Debug.LogError("This version of Sentis does not serialize model metadata");
            metadata = default;
            return false;
        }
    }
}
