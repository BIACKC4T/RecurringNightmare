using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Muse.AppUI.UI;
using UnityEngine.UIElements;

namespace Unity.Muse.Animate
{
    static class ContextMenu
    {
        private static MenuBuilder s_CurrentMenu;
        private static VisualElement s_Anchor;

        public class ActionArgs
        {
            public ActionArgs()
            {
            }

            public ActionArgs(int keyIndex, string displayText, string iconName, Action action,
                string shortcutText = "", bool isClickable = true)
            {
                KeyIndex = keyIndex;
                DisplayText = displayText;
                IconName = iconName;
                Action = action;
                ShortcutText = shortcutText;
                IsClickable = isClickable;
            }
            
            public int KeyIndex;
            public string DisplayText;
            public string ShortcutText;
            public string IconName;
            public Action Action;
            public bool IsClickable = true;
        }

        public static void OpenContextMenu(VisualElement parent, Vector2 screenPosition, IEnumerable<ActionArgs> actions)
        {
            if (s_Anchor == null)
            {
                s_Anchor = new VisualElement();
                s_Anchor.name = "entity-context-menu-anchor";
                s_Anchor.style.position = new StyleEnum<Position>(Position.Absolute);
            }

            var localPosition = screenPosition;
            
            s_Anchor.style.left = localPosition.x;
            s_Anchor.style.top =  parent.layout.height - localPosition.y;
            
            parent.Add(s_Anchor);

            OpenContextMenu(s_Anchor, actions);
        }
        
        public static MenuBuilder OpenContextMenu(VisualElement anchor, IEnumerable<ActionArgs> actions)
        {
            if (s_CurrentMenu != null)
            {
                s_CurrentMenu.Dismiss();
            }

            var menuBuilder = MenuBuilder.Build(anchor);
            
            foreach (var action in actions)
            {
                var item = new MenuItem
                {
                    label = action.DisplayText,
                    icon = action.IconName,
                    userData = action.KeyIndex,
                    shortcut = action.ShortcutText,
                };

                item.SetEnabled(action.IsClickable);
                
                item.RegisterCallback<ClickEvent>(_ =>
                {
                    action.Action();
                    // close the menu after action.
                    menuBuilder.Dismiss();
                });

                menuBuilder.currentMenu.Add(item);
            }

            menuBuilder.Show();
            s_CurrentMenu = menuBuilder;
            return menuBuilder;
        }
    }
}
