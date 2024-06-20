using System;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Unity.Muse.Common.Account
{
    /// <summary>
    /// The client status provides the status of a specific client, for instance if it is scheduled to be deprecated
    /// or if updates are available.
    /// </summary>
    class ClientStatus
    {
        static ClientStatus s_Instance;
        public static ClientStatus Instance => s_Instance ??= new();

        public event Action<ClientStatusResponse> OnClientStatusChanged;

        public bool IsClientUsable => AccountInfo.Instance.IsEntitled && !Status.IsDeprecated && NetworkState.IsAvailable;
        public PackageInfo packageInfo = PackageInfo.FindForAssetPath("Packages/com.unity.muse.common/package.json");
        public string apiVersion = GenerativeAIBackend.TexturesUrl.version;

        public ClientStatusResponse Status
        {
            get => AccountStatus.instance.status;
            internal set
            {
                AccountStatus.instance.status = value;
                OnClientStatusChanged?.Invoke(value);
            }
        }

        public void UpdateStatus()
        {
            if (!UnityConnectUtils.GetIsLoggedIn())
                return;

            var requestData = new ClientStatusRequest(packageInfo, apiVersion);
            GenerativeAIBackend.GetStatus(requestData, (result, error) =>
            {
                AccountStatus.instance.statusChecked = true;

                if (!string.IsNullOrEmpty(error))
                    return;

                Status = result;
            });
        }
    }
}
