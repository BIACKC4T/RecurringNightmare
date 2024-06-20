using System;
using UnityEngine;
using Application = UnityEngine.Device.Application;

namespace Unity.Muse.Common.Account
{
    static class AccountUtility
    {
        public static void GoToAccount()
        {
            var organizationId = AccountInfo.Instance.Organization?.Id;
            if (string.IsNullOrEmpty(organizationId))
                Application.OpenURL("https://id.unity.com/account/edit");
            else
                Application.OpenURL($"https://id.unity.com/organizations/{organizationId}");
        }

        public static void GoToMuseAccount()
        {
            Application.OpenURL("https://muse.unity.com/explore");
        }

        public static void UpdateMusePackages()
        {
#if UNITY_EDITOR
            UnityEditor.PackageManager.UI.Window.Open("com.unity.muse.common");
#endif
        }
    }
}