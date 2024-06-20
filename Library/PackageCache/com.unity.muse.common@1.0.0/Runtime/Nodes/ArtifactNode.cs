using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.Common
{
    internal class ArtifactNode : VisualElement
    {
        const string k_MainUssClassName = "muse-canvas-node";

        const string k_PreviewUssClassName = k_MainUssClassName + "__preview";

        Artifact m_Artifact;

        public Artifact artifact
        {
            get => m_Artifact;
            set
            {
                if (m_Artifact == value)
                    return;

                m_Artifact = value;
                UpdateView();
            }
        }

        public ArtifactNode()
        {
            usageHints = UsageHints.DynamicTransform;
            AddToClassList(k_MainUssClassName);
        }

        public void UpdateView()
        {
            Clear();

            if (artifact is null)
                return;

            var preview = artifact.CreateCanvasView();
            preview.AddToClassList(k_PreviewUssClassName);
            Add(preview);
        }
    }
}
