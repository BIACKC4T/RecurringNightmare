using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Unity.Muse.Common.Editor
{
    [ScriptedImporter(1, new []{"musemode"},new [] {"json"})]
    internal class MuseModeImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var asset = ScriptableObject.CreateInstance<MuseModeAsset>();
            var json = File.ReadAllText(ctx.assetPath);
            asset.modes = JsonUtility.FromJson<Modes>(json);
            asset.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
            ctx.AddObjectToAsset("MuseModeAsset", asset);
            ctx.SetMainObject(asset);
        }

        [MenuItem("internal:Muse/Internals/Update Modes", false, 111)]
        static void LoadMuseMode()
        {
            ModesFactory.LoadMuseModes();
        }

        [InitializeOnLoadMethod]
        static void RegisterAvailableTools()
        {
           OperatorsFactory.RegisterDefaultOperators();
        }
    }
}
