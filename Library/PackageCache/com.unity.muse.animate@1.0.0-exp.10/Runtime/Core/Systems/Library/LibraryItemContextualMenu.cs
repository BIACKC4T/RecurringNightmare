using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.Muse.Animate.PlatformUtils;

namespace Unity.Muse.Animate
{
    class LibraryItemContextualMenu
    {
        const string k_CopyLabel = "Copy";
        const string k_CopyIcon = "";
        const string k_CopyShortcut = "C";
        const string k_PasteAndReplaceLabel = "Paste and Replace";
        const string k_PasteAndReplaceIcon = "";
        const string k_PasteAndReplaceShortcut = "V";
        const string k_PasteLabel = "Paste";
        const string k_PasteIcon = "";
        const string k_PasteShortcut = "";
        const string k_DuplicateLabel = "Duplicate";
        const string k_DuplicateIcon = "";
        const string k_DuplicateShortcut = "D";
        
        const string k_DeleteLabel = "Delete";
        const string k_DeleteIcon = "";
        const string k_DeleteShortcut = "";
        
        const string k_ExportLabel = "Export";
        const string k_UsePromptLabel = "Use This Prompt";
        
        public Action<ActionType, ClipboardService, SelectionModel<LibraryItemModel>, LibraryItemUIModel> OnMenuAction;

        public enum ActionType
        {
            Copy,
            PasteAndReplace,
            Paste,
            Duplicate,
            Delete,
            Export,
            UsePrompt,
        }

        LibraryModel m_Library;
        SelectionModel<LibraryItemModel> m_SelectionModel;
        ClipboardService m_ClipboardService;
        LibraryItemUIModel m_Target;

        List<ContextMenu.ActionArgs> m_ActionArgs;
        ContextMenu.ActionArgs m_PasteAndReplaceAction;
        ContextMenu.ActionArgs m_PasteAction;

        ContextMenu.ActionArgs m_DeleteAction;
        ContextMenu.ActionArgs m_UsePromptAction;

        /// <summary>
        /// A contextual menu for a given library item.
        /// </summary>
        public LibraryItemContextualMenu()
        {
            // TODO: Handle keyboard shortcuts correctly, since these are shared with other menus.
            var copyAction = CreateAction(ActionType.Copy, k_CopyLabel, k_CopyIcon, GetCommandLabel(k_CopyShortcut));
            m_PasteAndReplaceAction = CreateAction(ActionType.PasteAndReplace, k_PasteAndReplaceLabel, k_PasteAndReplaceIcon, GetCommandLabel(k_PasteAndReplaceShortcut));
            m_PasteAction = CreateAction(ActionType.Paste, k_PasteLabel, k_PasteIcon, GetCommandLabel(k_PasteShortcut));
            var duplicateAction = CreateAction(ActionType.Duplicate, k_DuplicateLabel, k_DuplicateIcon, GetCommandLabel(k_DuplicateShortcut));

            m_DeleteAction = CreateAction(ActionType.Delete, k_DeleteLabel, k_DeleteIcon, k_DeleteShortcut);
            m_UsePromptAction = CreateAction(ActionType.UsePrompt, k_UsePromptLabel, "", "");
            
            // TODO: Add menu items when we can support those actions
            m_ActionArgs = new List<ContextMenu.ActionArgs>
            {
                CreateAction(ActionType.Export, k_ExportLabel, "", ""),
                m_UsePromptAction,
                m_DeleteAction
            };
        }
        
        /// <summary>
        /// Opens the contextual menu for a given Library's item.
        /// </summary>
        /// <param name="library">The target library model targeted by the menu.</param>
        /// <param name="clipboardService">The clipboard service used by the menu.</param>
        /// <param name="selectionModel">The selection model of the menu.</param>
        /// <param name="targetItem">The target library item, for which the context menu is opened.</param>
        /// <param name="anchor">An anchor for the contextual menu.</param>
        public void Open(LibraryModel library,
            ClipboardService clipboardService,
            SelectionModel<LibraryItemModel> selectionModel,
            LibraryItemUIModel targetItem,
            VisualElement anchor)
        {
            m_Target = targetItem;
            m_Library = library;
            m_ClipboardService = clipboardService;
            m_SelectionModel = selectionModel;

            // Enable / Disable actions
            m_PasteAndReplaceAction.IsClickable = m_ClipboardService.CanPaste(m_Target.Target);
            m_PasteAction.IsClickable = m_ClipboardService.CanPaste(m_Target.Target);
            m_UsePromptAction.IsClickable = m_Target.Target is TextToMotionTake;
            
            // Open the menu
            ContextMenu.OpenContextMenu(anchor, m_ActionArgs);
        }

        ContextMenu.ActionArgs CreateAction(ActionType actionType, string label, string icon, string shortcut)
        {
            return new ContextMenu.ActionArgs((int)actionType, label, icon, () => { InvokeMenuAction(actionType); }, shortcut);
        }

        void InvokeMenuAction(ActionType type)
        {
            int itemIndex = -1;
            
            if (m_SelectionModel.HasSelection)
            {
                itemIndex = m_Library.IndexOf(m_SelectionModel.GetSelection(0));
            }

            DeepPoseAnalytics.SendKeyAction(Enum.GetName(typeof(ActionType), type), itemIndex);
            
            OnMenuAction?.Invoke(type, m_ClipboardService, m_SelectionModel, m_Target);
        }
    }
}
