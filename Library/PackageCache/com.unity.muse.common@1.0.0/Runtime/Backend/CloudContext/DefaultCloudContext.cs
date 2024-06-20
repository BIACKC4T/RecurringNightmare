using System;
using UnityEngine;

namespace Unity.Muse.Common
{
    internal class DefaultCloudContext : ICloudContext
    {
        public void RegisterNextFrameCallback(ICloudContext.Callback cb) => throw new System.NotImplementedException();
        public void RegisterForTickCallback(ICloudContext.Callback cb) => throw new System.NotImplementedException();
        public void UnregisterForTickCallback(ICloudContext.Callback cb) => throw new System.NotImplementedException();

        public double TimeSinceStartup => throw new NotImplementedException();
    }
}
