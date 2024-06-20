using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.Animate.Editor
{
    [CustomEditor(typeof(MuseAnimateSession))]
    class MuseAnimateSessionInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            var label = new Label("Muse Animate Session");
            root.Add(label);
            return root;
        }
    }
}