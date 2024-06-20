using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.Muse.Common
{
    internal static class AvailableToolsFactory
    {
        static Dictionary<string, HashSet<Type>> s_AvailableTools;

        public static void RegisterTool<T>(string mode) where T: ICanvasTool, new()
        {
            s_AvailableTools ??= new();
            if (!s_AvailableTools.TryGetValue(mode, out var tools))
                s_AvailableTools[mode] = tools = new HashSet<Type>();

            tools.Add(typeof(T));
        }

        public static IEnumerable<ICanvasTool> GetAvailableTools(Model model)
        {
            var tools = new List<ICanvasTool>();
            
            if (!model || string.IsNullOrEmpty(model.CurrentMode))
                return tools;
            
            var availableTools = s_AvailableTools[model.CurrentMode];

            foreach (var instance in availableTools.Select(tool => (ICanvasTool)Activator.CreateInstance(tool)))
            {
                instance.SetModel(model);
                tools.Add(instance);
            }
            return tools;
        }
    }
}
