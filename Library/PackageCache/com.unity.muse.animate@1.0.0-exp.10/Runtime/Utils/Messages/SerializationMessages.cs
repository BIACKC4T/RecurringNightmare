using Unity.Muse.Animate.Usd;

namespace Unity.Muse.Animate
{
    readonly struct LoadSessionMessage
    {
        public readonly string JsonData;
        public LoadSessionMessage(string jsonData)
        {
            JsonData = jsonData;
        }
    }
    
    readonly struct SaveSessionMessage
    {
    }
    
    readonly struct ExportAnimationMessage
    {
        public readonly ExportData ExportData;
        public readonly Application.ApplicationHsm.Author.ExportType ExportType;

        public ExportAnimationMessage(Application.ApplicationHsm.Author.ExportType exportType, ExportData exportData)
        {
            ExportType = exportType;
            ExportData = exportData;
        }
    }
}
