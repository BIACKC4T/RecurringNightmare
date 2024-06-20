using Unity.Muse.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;
using Button = Unity.Muse.AppUI.UI.Button;

namespace Unity.Muse.Common
{
    /// <summary>
    /// Common UI for Canvas Tools
    /// </summary>
    internal class MuseToolbar : VisualElement
    {
        /// <summary>
        /// Pan Button
        /// </summary>
        public ActionButton PanBtn { get; private set; }
        /// <summary>
        /// Paint Button
        /// </summary>
        public ActionButton PaintBtn { get; private set; }
        /// <summary>
        /// Erase Button
        /// </summary>
        public ActionButton EraseBtn { get; private set; }
        /// <summary>
        /// Delete Button
        /// </summary>
        public ActionButton DeleteBtn { get; private set; }
        /// <summary>
        /// Size Slider
        /// </summary>
        public TouchSliderFloat SizeSlider { get; private set; }
        
        public MuseToolbar()
        {
            InitializeVisualTree();
        }

        void InitializeVisualTree()
        {
            var styleSheet = ResourceManager.Load<StyleSheet>(PackageResources.toolbarStyleSheet);
            styleSheets.Add(styleSheet);

            var actionGroup = new ActionGroup()
            {
                compact = true,
                justified = false,
                style =
                {
                    flexGrow = 0f
                }
            };

            PanBtn = new ActionButton()
            {
                name = "PanBtn",
                tooltip = "Pan (1 or P)",
                icon = "pan"
            };
            actionGroup.Add(PanBtn);

            PaintBtn = new ActionButton()
            {
                name = "PaintBtn",
                tooltip = "Paint (2 or B)",
                icon = "paint-brush"
            };
            actionGroup.Add(PaintBtn);

            EraseBtn = new ActionButton()
            {
                name = "EraseBtn",
                tooltip = "Erase (3 or E)",
                icon = "eraser"
            };
            actionGroup.Add(EraseBtn);

            Add(actionGroup);

            SizeSlider = new TouchSliderFloat()
            {
                label = "Size",
                value = 5,
                lowValue = 0,
                highValue = 10,
                style =
                {
                    width = 138
                }
            };
            Add(SizeSlider);

            DeleteBtn = new ActionButton
            {
                icon = "delete",
                tooltip = "Clear"
            };

            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                DeleteBtn.tooltip += " (Command+Delete)";
            }
            else
            {
                DeleteBtn.tooltip += " (Del)";
            }

            Add(DeleteBtn);

            PanBtn.clickable.clicked += OnPanClicked;
            PaintBtn.clickable.clicked += OnPaintClicked;
            EraseBtn.clickable.clicked += OnEraseClicked;

            SelectButton(PanBtn);
        }

        /// <summary>
        /// Set the button's selected state
        /// </summary>
        /// <param name="button">The specific button</param>
        public void SelectButton(ActionButton button)
        {
            PaintBtn.EnableInClassList(Styles.selectedUssClassName, button == PaintBtn);
            EraseBtn.EnableInClassList(Styles.selectedUssClassName, button == EraseBtn);
            PanBtn.EnableInClassList(Styles.selectedUssClassName, button == PanBtn);

            DeleteBtn.SetEnabled(button == PaintBtn || button == EraseBtn);
            SizeSlider.SetEnabled(button == PaintBtn || button == EraseBtn);
        }

        internal bool IsPaintButtonSelected()
        {
            return PaintBtn.ClassListContains(Styles.selectedUssClassName);
        }

        internal bool IsEraserButtonSelected()
        {
            return EraseBtn.ClassListContains(Styles.selectedUssClassName);
        }

        void OnPaintClicked()
        {
            SelectButton(PaintBtn);
        }

        void OnEraseClicked()
        {
            SelectButton(EraseBtn);
        }

        void OnPanClicked()
        {
            SelectButton(PanBtn);
        }
    }
}