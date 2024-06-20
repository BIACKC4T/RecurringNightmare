using System;
using Unity.Muse.AppUI.UI;
using UnityEngine.UIElements;

namespace Unity.Muse.Common.Account
{
    class StartTrialDialog : AccountDialog
    {
        public Action<OrganizationInfo> OnAccept;

        public StartTrialDialog(bool allowNoAccount = false)
        {
            OrganizationInfo selected = AccountInfo.Instance.Organization;
            AddToClassList("muse-subscription-dialog-start");

            dialogDescription.Add(new Text {text = TextContent.subTitle, name="muse-description-title"});

            if (AccountInfo.Instance.Organizations.Count > 1)
            {
                var organizationSelection = new VisualElement {name = "muse-organization-selection"};
                organizationSelection.AddToClassList("muse-description-section");
                organizationSelection.Add(new Text {name = "muse-description-text-secondary", text = TextContent.subConfirmSelectOrganization, enableRichText = true});
                var organizationDropDown = new OrganizationDropdown(org =>
                {
                    selected = org;
                    OnOrganizationChanged(selected);
                });
                selected = organizationDropDown.Selected;
                organizationDropDown.AddToClassList("muse-trial-organization-dropdown");
                organizationSelection.Add(organizationDropDown);
                dialogDescription.Add(organizationSelection);
            }
            else
                dialogDescription.Add(new Text {text = TextContent.subDescription1, name = "muse-description-secondary", enableRichText = true});

            AddCancelButton(TextContent.subViewOrganizations, AccountLinks.ViewOrganizations);
            if (allowNoAccount)
                AddCloseButton();

            AddPrimaryButton(TextContent.subStartTrial, () => OnAccept?.Invoke(selected));
            OnOrganizationChanged(selected);

            // Check entitlements on focus as long as the trial dialogs are shown.
            AccountInfo.Instance.ShouldCheckEntitlementsOnFocus = true;
        }

        void OnOrganizationChanged(OrganizationInfo selection)
        {
            if (selection is {Status: SubscriptionStatus.NotEntitled})
                m_PrimaryButton.title = TextContent.subStartTrial;
            else if (selection is {Status: SubscriptionStatus.FreeTrial})
                m_PrimaryButton.title = TextContent.subStartJoinTrial;
            else if (selection is {Status: SubscriptionStatus.Entitled})
                m_PrimaryButton.title = TextContent.subStartJoinSubscription;
            else if (selection is {IsEntitled: true})
                m_PrimaryButton.title = TextContent.subStartUsing;
        }
    }
}
