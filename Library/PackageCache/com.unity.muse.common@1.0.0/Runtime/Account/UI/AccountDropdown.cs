using System;
using Unity.Muse.AppUI.UI;
using Unity.Muse.Common.Account;
using Unity.Muse.Common.Utils;
using UnityEngine;
using UnityEngine.UIElements;
using Button = Unity.Muse.AppUI.UI.Button;

namespace Unity.Muse.Common
{
#if ENABLE_UXML_SERIALIZED_DATA
    [UxmlElement]
#endif
    partial class AccountDropdown : VisualElement
    {
#if ENABLE_UXML_TRAITS
        internal new class UxmlFactory : UxmlFactory<AccountDropdown, UxmlTraits> { }
#endif

        Popover m_SubscriptionStartModal;
        static Action s_UpdateUsage;
        Text m_UsageExceeded;
        Image m_Bar;
        LinearProgress m_UsageProgress;
        Text m_Usage;
        bool UsageExceeded => AccountInfo.Instance.Usage.Exceeded;

        static Action UpdateUsage => s_UpdateUsage ??= EventServices.IntervalDebounce(AccountInfo.Instance.UpdateUsage, 5f);


        public AccountDropdown()
        {
            styleSheets.Add(ResourceManager.Load<StyleSheet>(PackageResources.accountStyleSheet));

            var dropdown = new Button
            {
                title = TextContent.museTitle,
                leadingIcon = "muse-logo",
                trailingIcon = "caret-down--fill"
            };
            dropdown.AddToClassList("muse-account-dropdown");

            Add(dropdown);

            dropdown.clicked += ShowMuseAccountSettings;
            RegisterCallback<AttachToPanelEvent>(AttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(DetachFromPanel);
            ShowSubscriptionStartMessage();

            UpdateUsage();
        }

        void AttachToPanel(AttachToPanelEvent evt)
        {
            AccountInfo.Instance.OnOrganizationChanged += ShowSubscriptionStartMessage;
            AccountInfo.Instance.OnUsageChanged += RefreshUsage;
        }

        void RefreshUsage()
        {
            if (m_Bar is null || m_UsageExceeded is null || m_UsageProgress is null || m_Usage is null)
                return;

            m_Bar.SetDisplay(UsageExceeded);
            m_UsageExceeded.SetDisplay(UsageExceeded);
            m_UsageProgress.SetDisplay(!UsageExceeded);
            m_Usage.text = AccountInfo.Instance.Usage.Label;
            m_Usage.tooltip = AccountInfo.Instance.Usage.Tooltip;
        }

        void DetachFromPanel(DetachFromPanelEvent evt)
        {
            AccountInfo.Instance.OnOrganizationChanged -= ShowSubscriptionStartMessage;
            AccountInfo.Instance.OnUsageChanged -= RefreshUsage;
        }

        void ShowMuseAccountSettings()
        {
            Popover modal = null;

            void GoToAccountClick()
            {
                modal?.Dismiss();
                AccountUtility.GoToMuseAccount();
            }

            var content = new VisualElement();
            content.AddToClassList("muse-account-settings");

            var hasNeverUsedMuse = AccountInfo.Instance is {IsEntitled: false, IsExpired: false};
            var controller = AccountController.Get(this);
            if (hasNeverUsedMuse && controller is {allowNoAccount: true})
            {
                var startTrial = new Button {title = TextContent.tryMuse};
                startTrial.pickingMode = PickingMode.Position;
                startTrial.style.marginLeft = 0;
                startTrial.AddToClassList("account-dropdown-start-subscription");
                startTrial.quiet = true;
                startTrial.clicked += () =>
                {
                    modal?.Dismiss();
                    GlobalPreferences.trialDialogShown = false;
                    controller.StateChanged();
                };
                content.Add(startTrial);
            }

            var usageGroup = new VisualElement {name = "muse-account-usage-group"};

            m_UsageExceeded = new Text {name = "muse-usage-exceeded", text = TextContent.subUsageExceeded};
            usageGroup.Add(m_UsageExceeded);

            var usageRow = new VisualElement {name = "muse-account-usage-row"};
            var usageLabel = new Text {text = TextContent.subUsageUsed};
            usageRow.Add(usageLabel);
            m_Usage = new();
            m_Usage.AddToClassList("usage");
            usageRow.Add(m_Usage);

            usageGroup.Add(usageRow);

            m_Bar = new Image
            {
                image = ResourceManager.Load<Texture2D>(PackageResources.accountUsageExceededBar),
                scaleMode = ScaleMode.StretchToFill
            };
            m_Bar.AddToClassList("muse-usage-progress");
            usageGroup.Add(m_Bar);

            m_UsageProgress = new LinearProgress
            {
                value = AccountInfo.Instance.Usage.Progress,
                variant = Progress.Variant.Determinate
            };
            m_UsageProgress.AddToClassList("muse-usage-progress");
            m_UsageProgress.colorOverride = new Color(0.9215f, 0.2549f, 0.47843f);
            usageGroup.Add(m_UsageProgress);

            if (!hasNeverUsedMuse)
                content.Add(usageGroup);

            var goToAccountRow = new VisualElement();
            goToAccountRow.AddToClassList("row");
            goToAccountRow.Add(new Text {text = TextContent.goToMuseAccount});
            goToAccountRow.Add(new IconButton
            {
                icon = "arrow-square-out",
                quiet = true,
                clickable = new Pressable(GoToAccountClick)
            });
            goToAccountRow.AddManipulator(new Pressable(GoToAccountClick));

            content.Add(goToAccountRow);

            RefreshUsage();

            modal = Popover.Build(this, content);
            modal.SetAnchor(this);
            modal.SetPlacement(PopoverPlacement.Bottom);
            modal.Show();
        }

        public void ShowSubscriptionStartMessage()
        {
            if (panel == null)
                return;
            if (AccountInfo.Instance.SubscriptionStartDisplayed)
                return;
            if (!AccountInfo.Instance.IsEntitled)
                return;

            AccountInfo.Instance.SubscriptionStartDisplayed = true;

            var message = new VisualElement();
            message.AddToClassList("muse-subscription-message");
            message.AddToClassList("muse-subscription-start-message");

            var titleRow = new VisualElement {name = "muse-message-title-row"};
            titleRow.Add(new Text {text = TextContent.subStartTitle, name = "muse-message-title"});
            titleRow.Add(new IconButton {icon = "x", quiet = true, clickable = new Pressable(() => m_SubscriptionStartModal?.Dismiss())});
            message.Add(titleRow);

            message.Add(new Text {text = TextContent.subStartDescription, name = "muse-message-description", enableRichText = true});
            message.Add(new Button(() =>
            {
                m_SubscriptionStartModal?.Dismiss();
                GlobalPreferences.trialDialogShown = false;
                AccountController.Refresh();
            })
            {
                name = "muse-message-learn-more-button",
                title = TextContent.subStartLearnMore,
                variant = ButtonVariant.Accent
            });

            m_SubscriptionStartModal = Popover.Build(this, message);
            m_SubscriptionStartModal.view.AddToClassList("muse-subscription-start-popover");
            m_SubscriptionStartModal.SetAnchor(this);
            m_SubscriptionStartModal.SetPlacement(PopoverPlacement.Bottom);
            // Changing passMask and using `--background-color` on the modal's ExVisualElement is necessary to have
            // a larger border radius on the dialog then the default. Otherwise ghosting on the dialog's edges occurs.
            if (m_SubscriptionStartModal.view.Q<ExVisualElement>("appui-popover__shadow-element") is ExVisualElement exVisualElement)
                exVisualElement.passMask = ExVisualElement.Passes.Clear | ExVisualElement.Passes.BackgroundColor | ExVisualElement.Passes.OutsetShadows;
            m_SubscriptionStartModal.Show();
        }
    }
}
