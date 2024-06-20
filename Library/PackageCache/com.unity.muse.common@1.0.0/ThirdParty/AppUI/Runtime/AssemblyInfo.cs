using System.Runtime.CompilerServices;
#if UNITY_EDITOR
using UnityEditor.UIElements;
#endif

[assembly: InternalsVisibleTo("Unity.AppUI.Tests")]
[assembly: InternalsVisibleTo("Unity.AppUI.Editor.Tests")]
[assembly: InternalsVisibleTo("Unity.Muse.AppUI.Editor")]
#if UNITY_EDITOR
[assembly: UxmlNamespacePrefix("Unity.Muse.AppUI.UI", "appui")]
#endif
