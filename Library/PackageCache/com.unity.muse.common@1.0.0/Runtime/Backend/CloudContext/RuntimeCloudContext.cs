using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Scripting;

namespace Unity.Muse.Common
{
    [Preserve]
    [Serializable]
    internal class RuntimeCloudContext : MonoBehaviour, ICloudContext
    {
        public double TimeSinceStartup => Time.unscaledTimeAsDouble;
        List<ICloudContext.Callback> m_DeferredCallbackList = new();
        HashSet<ICloudContext.Callback> m_PerTickCallbackList = new();

        /// <summary>
        /// Access token for cloud authorization.
        /// </summary>
        [SerializeField]
        public string accessToken;

        void OnEnable()
        {
            CloudContextFactory.SetCloudContext(this);
        }

        void OnDisable()
        {
            CloudContextFactory.SetCloudContext(null);
        }

        public void RegisterNextFrameCallback(ICloudContext.Callback cb)
        {
            m_DeferredCallbackList.Add(cb);
        }

        public void RegisterForTickCallback(ICloudContext.Callback cb)
        {
            m_PerTickCallbackList.Add(cb);
        }

        public void UnregisterForTickCallback(ICloudContext.Callback cb)
        {
            m_PerTickCallbackList.Remove(cb);
        }

        void Update()
        {
            var tempList = ListPool<ICloudContext.Callback>.Get();

            tempList.AddRange(m_DeferredCallbackList);
            m_DeferredCallbackList.Clear();

            foreach (var callback in tempList)
            {
                callback();
            }

            tempList.Clear();
            tempList.AddRange(m_PerTickCallbackList);

            foreach (var perTickUpdate in tempList)
            {
                perTickUpdate();
            }
        }
    }
}
