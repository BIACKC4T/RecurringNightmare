
using System.Collections.Generic;
using Unity.Muse.Common;
using UnityEditor;
using UnityEngine;

namespace Unity.GenerativeAI.Editor
{

    internal class EditorCloudContext : ICloudContext
    {
        [InitializeOnLoadMethod]
        static void InjectEditorContext()
        {
            CloudContextFactory.InjectCloudContextType<EditorCloudContext>();
        }

        public double TimeSinceStartup => EditorApplication.timeSinceStartup;

        static Dictionary<ICloudContext.Callback, EditorApplication.CallbackFunction> s_CallbackDelegateTrackingTable = new();

        void ICloudContext.RegisterNextFrameCallback(ICloudContext.Callback cb)
        {
            EditorApplication.delayCall += () => cb();
        }

        public void RegisterForTickCallback(ICloudContext.Callback cb)
        {
            void CallbackFunction() => cb();
            if (s_CallbackDelegateTrackingTable.TryAdd(cb, CallbackFunction))
            {
                EditorApplication.update += CallbackFunction;
            }
        }

        public void UnregisterForTickCallback(ICloudContext.Callback cb)
        {
            if (s_CallbackDelegateTrackingTable.TryGetValue(cb, out var editorCallbackDelegate))
            {
                EditorApplication.update -= editorCallbackDelegate;
                s_CallbackDelegateTrackingTable.Remove(cb);
            }
        }
    }
}
