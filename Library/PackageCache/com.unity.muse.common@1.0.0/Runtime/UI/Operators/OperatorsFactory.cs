using System;
using System.Collections.Generic;
using UnityEngine.Scripting;

#if !UNITY_EDITOR
using UnityEngine;
#endif

namespace Unity.Muse.Common
{
    internal static class OperatorsFactory
    {
        static Dictionary<string, Type> s_AvailableOperatorTypes;

        public static bool RegisterOperator<T>() where T : IOperator, new()
        {
            var operatorType = typeof(T);
            var operatorInstance = (IOperator)Activator.CreateInstance(operatorType);

            s_AvailableOperatorTypes ??= new Dictionary<string, Type>();
            return s_AvailableOperatorTypes.TryAdd(operatorInstance.OperatorName, operatorType);
        }

        public static IOperator GetOperatorInstance(string operatorName)
        {
            if (s_AvailableOperatorTypes == null || !s_AvailableOperatorTypes.TryGetValue(operatorName, out var operatorType))
                return null;

            return (IOperator)Activator.CreateInstance(operatorType);
        }

#if !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        [Preserve]
        public static void RegisterDefaultOperators()
        {
            RegisterOperator<GenerateOperator>();
            RegisterOperator<LoraOperator>();
            RegisterOperator<MaskOperator>();
            RegisterOperator<PromptOperator>();
            RegisterOperator<ReferenceOperator>();
            RegisterOperator<UpscaleOperator>();
        }
    }
}
