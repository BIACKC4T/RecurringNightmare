using UnityEngine;
using Unity.AppUI.Core;
using UnityEngine.UIElements;
using Unity.Muse.AppUI.UI;

namespace Unity.Muse.Animate
{
    class AddEntitiesToolbarView : UITemplateContainer, IUITemplate
    {
        public const string defaultName = "add-entities-toolbar";
        
        const string k_AddPropsButtonName = "add-props";
        const string k_AddActorsButtonName = "add-actors";

        AddEntitiesToolbarViewModel m_Model;
        ActionButton m_AddPropsButton;
        ActionButton m_AddActorsButton;
        Popover m_Popover;
        Menu m_AddPropsMenu;
        Menu m_AddActorsMenu;

        bool m_AddingActors;
        bool m_AddingProps;

        public new class UxmlTraits : VisualElement.UxmlTraits { }
        public new class UxmlFactory : UxmlFactory<AddEntitiesToolbarView, UxmlTraits> { }

        public AddEntitiesToolbarView()
            : base("deeppose-toolbar") { }

        public void InitComponents()
        {
            m_AddPropsMenu = new();
            m_AddPropsMenu.AddToClassList("deeppose-drop-menu");
            m_AddPropsMenu.style.width = 230;
            m_AddPropsMenu.style.height = 400;

            m_AddActorsMenu = new();
            m_AddActorsMenu.AddToClassList("deeppose-drop-menu");
            m_AddActorsMenu.style.width = 230;
            m_AddActorsMenu.style.height = 110;
        }
        
        public void FindComponents()
        {
            m_AddPropsButton = this.Q<ActionButton>(k_AddPropsButtonName);
            m_AddActorsButton = this.Q<ActionButton>(k_AddActorsButtonName);
        }

        public void RegisterComponents()
        {
            m_AddPropsButton.RegisterCallback<ClickEvent>(OnAddPropsButtonClicked);
            m_AddActorsButton.RegisterCallback<ClickEvent>(OnAddActorsButtonClicked);
        }

        public void UnregisterComponents()
        {
            m_AddPropsButton.UnregisterCallback<ClickEvent>(OnAddPropsButtonClicked);
            m_AddActorsButton.UnregisterCallback<ClickEvent>(OnAddActorsButtonClicked);
        }

        public void SetModel(AddEntitiesToolbarViewModel model)
        {
            UnregisterModel();
            m_Model = model;
            RegisterModel();
            Update();
        }

        void RegisterModel()
        {
            if (m_Model == null)
                return;

            m_Model.OnChanged += OnChanged;
            m_Model.OnRequestedAddPropsMenu += OnRequestedAddPropsMenu;
            m_Model.OnRequestedAddActorsMenu += OnRequestedAddActorsMenu;
            m_Model.OnCreatedPropFromMenu += OnCreatedPropFromMenu;
            m_Model.OnCreatedActorFromMenu += OnCreatedActorFromMenu;

            // Right now the menus are only generated once,
            // but if we were to update the registries at runtime we would have to call those methods again
            BuildPropsMenu();
            BuildActorsMenu();
        }

        void UnregisterModel()
        {
            if (m_Model == null)
                return;

            m_Model.OnChanged -= OnChanged;
            m_Model.OnRequestedAddPropsMenu -= OnRequestedAddPropsMenu;
            m_Model.OnRequestedAddActorsMenu -= OnRequestedAddActorsMenu;
            m_Model.OnCreatedPropFromMenu -= OnCreatedPropFromMenu;
            m_Model.OnCreatedActorFromMenu -= OnCreatedActorFromMenu;

            m_Model = null;
        }

        public void Update()
        {
            if (m_Model == null)
                return;

            if (!IsAttachedToPanel)
                return;
            
            if (!m_Model.IsVisible)
            {
                parent.style.display = DisplayStyle.None;
                return;
            }

            parent.style.display = DisplayStyle.Flex;

            m_AddActorsButton.SetEnabled(!m_AddingActors);
            m_AddActorsButton.SetSelectedWithoutNotify(m_AddingActors);
            m_AddPropsButton.SetEnabled(!m_AddingProps);
            m_AddPropsButton.SetSelectedWithoutNotify(m_AddingProps);
        }

        void OnChanged()
        {
            Update();
        }

        void OnAddPropsButtonClicked(ClickEvent evt)
        {
            m_Model.RequestAddPropsMenu();
        }

        void OnAddActorsButtonClicked(ClickEvent evt)
        {
            m_Model.RequestAddActorsMenu();
        }

        void OnRequestedAddActorsMenu()
        {
            OpenAddActorsDialog();
        }

        void OnRequestedAddPropsMenu()
        {
            OpenAddPropsDialog();
        }

        void OnCreatedActorFromMenu()
        {
            m_Popover?.Dismiss(DismissType.Action);
            Update();
        }

        void OnCreatedPropFromMenu()
        {
            m_Popover?.Dismiss(DismissType.Action);
            Update();
        }

        void BuildPropsMenu()
        {
            m_AddPropsMenu.Clear();

            // Add the items to the menu
            foreach (var definition in m_Model.Stage.PropRegistry.VisibleEntries)
            {
                AddMenuItem(m_AddPropsMenu.contentContainer, definition.Label, definition.Thumbnail,
                    ev => { m_Model.CreatePropFromMenu(definition); });
            }
        }

        void BuildActorsMenu()
        {
            m_AddActorsMenu.Clear();

            // Add the items to the menu
            foreach (var definition in m_Model.Stage.ActorRegistry.VisibleEntries)
            {
                AddMenuItem(m_AddActorsMenu.contentContainer, definition.Label, definition.Thumbnail,
                    ev => { m_Model.CreateActorFromMenu(definition); });
            }
        }

        void OpenAddPropsDialog()
        {
            // Close the previous Popover (if any)
            ClosePreviousPopover();

            // Build and Show the Popover
            Popover(m_AddPropsButton, m_AddPropsMenu);

            // Set flag
            m_AddingProps = true;

            // Update the visual appearance (disabled button)
            Update();
        }

        void OpenAddActorsDialog()
        {
            // Close the previous Popover (if any)
            ClosePreviousPopover();

            // Build and Show the Popover
            Popover(m_AddActorsButton, m_AddActorsMenu);

            // Set flag
            m_AddingActors = true;

            // Update the visual appearance (disabled button)
            Update();
        }

        void Popover(VisualElement referenceView, VisualElement contentView)
        {
            m_Popover = AppUI.UI.Popover.Build(referenceView, contentView);
            m_Popover.SetPlacement(PopoverPlacement.BottomEnd);
            m_Popover.SetArrowVisible(false);
            m_Popover.SetContainerPadding(4);
            m_Popover.Show();
            m_Popover.dismissed += OnPopoverDismissed;
        }

        void ClosePreviousPopover()
        {
            if (m_Popover == null)
                return;

            m_Popover.Dismiss(DismissType.Consecutive);
            m_Popover.dismissed -= OnPopoverDismissed;
        }

        void OnPopoverDismissed(Popover popover, DismissType type)
        {
            m_AddingProps = false;
            m_AddingActors = false;

            Update();
        }

        static void AddMenuItem(VisualElement container, string label, Texture thumbnail, EventCallback<ClickEvent> callback)
        {
            // TODO: If we ever build the menu at runtime more than once, we should re-use menu items
            var menuItem = new MenuItem();
            menuItem.RegisterCallback(callback);
            menuItem.label = label;

            var icon = menuItem.Q<Icon>("appui-menuitem__icon");
            icon.image = thumbnail;
            icon.style.display = DisplayStyle.Flex;
            icon.style.width = 32;
            icon.style.height = 32;

            // Prevent the label from extending outside the menu
            var textElement = menuItem.Q<TextElement>("appui-menuitem__label");
            textElement.style.flexShrink = 1;

            // Add the created item to the menu
            container.Add(menuItem);
        }
    }
}
