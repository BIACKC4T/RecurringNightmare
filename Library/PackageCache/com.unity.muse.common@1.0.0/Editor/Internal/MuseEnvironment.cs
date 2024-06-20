using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Unity.Muse.Common.Editor
{
    class MuseEnvironment : MonoBehaviour
    {
        const string k_TestDefineSymbol = "UNITY_MUSE_CLOUD_TEST";
        const string k_StagingDefineSymbol = "UNITY_MUSE_CLOUD_STAGING";

        internal enum TestEnvironment
        {
            Production,
            Staging,
            Test
        }

        [MenuItem("internal:Muse/Internals/Set Muse Test Environment", false, 904)]
        static void SetTestEnvironment()
        {
            var currentEnvironment = GetCurrentEnvironment();

            var title = "Choose the Muse Test Environment.";
            var message = "Closing the window will set the environment to production." +
                "\nCurrent environment : " + currentEnvironment;

            var choice = EditorUtility.DisplayDialogComplex(title, message, TestEnvironment.Staging.ToString(), TestEnvironment.Production.ToString(), TestEnvironment.Test.ToString());

            if (choice == 0) // Ok - Staging
            {
                SetTestEnvironment(TestEnvironment.Staging);
            }
            else if (choice == 2) // Alt - Test
            {
                SetTestEnvironment(TestEnvironment.Test);
            }
            else // Cancel - Prod
            {
                SetTestEnvironment(TestEnvironment.Production);
            }
        }

        internal static void SetTestEnvironment(TestEnvironment environment)
        {
            RemoveDefineSymbols(k_TestDefineSymbol);
            RemoveDefineSymbols(k_StagingDefineSymbol);

            if (environment == TestEnvironment.Staging)
            {
                AddDefineSymbols(k_StagingDefineSymbol);
            }
            else if (environment == TestEnvironment.Test)
            {
                AddDefineSymbols(k_TestDefineSymbol);
            }
        }

        internal static TestEnvironment GetCurrentEnvironment()
        {
            if (ContainsDefineSymbol(k_StagingDefineSymbol))
                return TestEnvironment.Staging;
            if (ContainsDefineSymbol(k_TestDefineSymbol))
                return TestEnvironment.Test;

            return TestEnvironment.Production;
        }

        internal static void AddDefineSymbols(string environmentSymbol)
        {
            var namedBuildTarget = GetCurrentNamedBuildTarget();
            PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget, out var defines);

            if (!ContainsDefineSymbol(environmentSymbol))
            {
                var newSymbols = new List<string>(defines) { environmentSymbol };
                PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, newSymbols.ToArray());
            }
        }

        internal static void RemoveDefineSymbols(string environmentSymbol)
        {
            var namedBuildTarget = GetCurrentNamedBuildTarget();
            PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget, out var defines);

            var index = Array.IndexOf(defines, environmentSymbol);
            if (index > -1)
            {
                var newSymbols = new List<string>(defines);
                newSymbols.RemoveAt(index);
                PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, newSymbols.ToArray());
            }
        }

        internal static bool ContainsDefineSymbol(string environmentSymbol)
        {
            var namedBuildTarget = GetCurrentNamedBuildTarget();
            PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget, out var defines);

            var index = Array.IndexOf(defines, environmentSymbol);
            return index != -1;
        }

        static NamedBuildTarget GetCurrentNamedBuildTarget()
        {
            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);

            return namedBuildTarget;
        }
    }
}
