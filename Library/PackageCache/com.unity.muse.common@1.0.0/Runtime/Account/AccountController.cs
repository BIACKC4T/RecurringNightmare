using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Muse.AppUI.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.Common.Account
{
    delegate AccountState StateTransition(AccountState toState, AccountState fromState);

    class AccountController : IDisposable
    {
        static List<AccountController> s_Controllers = new();
        public static IEnumerable<AccountController> controllers => s_Controllers;

        /// <summary>
        /// Registers an EditorWindow to the account controller.
        ///
        /// This will make the window show the various relevant dialogs (signed in, start trial, data opt-in, etc...)
        ///
        /// The state change logic can be intercepted and modified if desired to add various conditions through an optional
        /// `transition` argument. This method will receive a `fromState` and a `toState` and should return the new desired state.
        ///
        /// eg:
        /// <example>
        /// <code>
        /// AccountController.Register(myEditorWindow, (toState, fromState) =>
        /// {
        ///     // Remove support for sign-in state
        ///     if (fromState == AccountState.SignIn)
        ///         return AccountState.Default;
        ///
        ///     return toState;
        /// });
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="window">The EditorWindow to be registered.</param>
        /// <param name="transition">An option method that can modify the state change logic if desired.</param>
        /// <returns></returns>
        public static AccountController Register(EditorWindow window, StateTransition transition = null, bool allowNoAccount = false)
        {
            var controller = new AccountController(window, false, allowNoAccount);
            controller.OnStateTransition += transition;
            controller.Init();

            return controller;
        }

        static void Clear(AccountController controller)
        {
            controller.Dispose();
            s_Controllers.Remove(controller);     // Window has been destroyed.
        }

        public static bool IsAnyWindowRegistered()
        {
            foreach (var controller in s_Controllers.ToList())
            {
                if (controller.IsInvalid)
                    Clear(controller);
            }

            return s_Controllers.Any();
        }

        /// <summary>
        /// Get the account controller that a visual element is part of
        /// </summary>
        /// <param name="anyElementInWindow"></param>
        /// <returns></returns>
        public static AccountController Get(VisualElement anyElementInWindow)
        {
            return s_Controllers.Find(accountController => accountController.element == anyElementInWindow.GetFirstAncestorOfType<Panel>());
        }

        /// <summary>
        /// Refresh all editor windows's state
        /// </summary>
        public static void Refresh()
        {
            foreach (var controller in s_Controllers.ToList())
                controller.StateChanged();
        }

        AccountState m_State = AccountState.Default;
        public AccountState State => m_State;

        TrialForm m_TrialForm;
        public StateTransition OnStateTransition;
        public bool skipInternalStateChange;
        public AccountDropdown AccountDropdown => m_Window.rootVisualElement.Q<AccountDropdown>();
        public bool IsInvalid => m_Window == null || element == null;

        public VisualElement element => m_Window.rootVisualElement.Q<Panel>(); // An arbitrary UI element inside the UI panel.
        readonly EditorWindow m_Window;
        Modal m_Modal;  // The currently displayed modal dialog
        AccountDialog m_Dialog;
        bool m_HasAttached;
        public bool allowNoAccount { get; protected set; }

        public AccountController(EditorWindow window, bool init = true, bool allowNoAccount = false)
        {
            s_Controllers.Add(this);
            this.allowNoAccount = allowNoAccount;

            m_Window = window;
            if (element is null)
                throw new Exception("The window must have a visual element with a Panel type to be apple to display modal dialogs.");

            element.styleSheets.Add(ResourceManager.Load<StyleSheet>(PackageResources.museTheme));
            element.styleSheets.Add(ResourceManager.Load<StyleSheet>(PackageResources.accountStyleSheet));

            AccountInfo.Instance.OnOrganizationChanged += StateChanged;
            AccountInfo.Instance.OnLegalConsentChanged += StateChanged;
            SignInUtility.OnChanged += StateChanged;
            window.rootVisualElement.RegisterCallback<AttachToPanelEvent>(AttachToPanel);

            if (init)
                Init();
        }

        public virtual void Dispose()
        {
            AccountInfo.Instance.OnOrganizationChanged -= StateChanged;
            AccountInfo.Instance.OnLegalConsentChanged -= StateChanged;
            SignInUtility.OnChanged -= StateChanged;
        }

        public void Init()
        {
            StateChanged();
        }

        void AttachToPanel(AttachToPanelEvent evt)
        {
            m_HasAttached = true;
            Apply();
        }

        bool ShouldSkipStateChange()
        {
            // Skip any update if the window has been destroyed.
            // Only clear from registered windows if the element in it has at least been attached once.
            if (IsInvalid && m_HasAttached)
                Clear(this);

            return IsInvalid;
        }

        public void StateChanged()
        {
            if (ShouldSkipStateChange())
                return;

            var updateToState = ResolveCurrentState();                                          // Apply internal state change logic
            m_State = OnStateTransition?.Invoke(updateToState, m_State) ?? updateToState;       // Apply External client's state change logic
            Apply();                                                                            // Apply new state
        }

        // Always allow all usage. Used mainly for testing purposes.
        internal static bool ForceAllowUsage { get; set; }

        // Always show sign-in dialog if requested
        bool IsSignedOut => SignInUtility.state == SignInState.SignedOut;

        bool AlwaysAllowUseWithoutEntitlements => allowNoAccount && GlobalPreferences.trialDialogShown;

        // If the user has a trial or ever had one
        bool IsRegistered => AccountInfo.Instance.IsRegistered;

        // Opt-out of the subscription flow if the app is usable without an entitlement.
        bool AllowsUseWithoutEntitlements => IsRegistered || AlwaysAllowUseWithoutEntitlements;

        // If the user does not have seats but his organization is determined to have available ones.
        bool ShouldRequestSeats => AccountInfo.Instance.RequestSeat;

        // If the user is currently filling up dialogs
        bool IsFillingForm => m_TrialForm is not null;

        bool IsEntitled => AccountInfo.Instance.IsEntitled;

        // Keep in current state if processing trial form dialogs (eg: clicked start trial)
        // Don't change state until we have entitlements+legal information
        // Otherwise we run the risk of displaying partial information
        //  eg: Entitlement is set, so dialog disappear but legal consent is not yet received so consent dialog is shown again briefly
        bool IsProcessingTrialForm => m_TrialForm?.processing ?? false;

        AccountState TrialFormState
        {
            get => m_TrialForm.state;
            set
            {
                m_TrialForm.state = value;
                StateChanged();
            }
        }

        // Resolve current state based on current user/account information
        protected virtual AccountState ResolveCurrentState()
        {
            if (ForceAllowUsage)
                return AccountState.Default;
            if (IsSignedOut)
                return AccountState.SignIn;
            if (IsProcessingTrialForm)
                return m_State;
            if (AllowsUseWithoutEntitlements)
                return AccountState.Default;

            if (IsEntitled)
            {
                if (AccountInfo.Instance.LegalConsent.HasConsented)
                    return AccountState.Default;
                else
                    return AccountState.TrialConfirm;
            }
            else
            {
                if (ShouldRequestSeats)
                    return AccountState.RequestSeat;
                if (IsFillingForm)
                    return TrialFormState;

                return AccountState.Trial;
            }
        }

        /// <summary>
        /// Apply current state
        /// </summary>
        protected virtual void Apply()
        {
            if (State == AccountState.Default)
            {
                // If started trial form and changed organization to a valid one, ensure trialForm is reset
                // Otherwise changing back to an organization that's not entitled would pursue the old form.
                m_TrialForm = null;
                TryDismissCurrentModal();
            }
            else if (State == AccountState.Trial)
            {
                m_TrialForm = new() {startTrial = !AccountInfo.Instance.IsEntitled};
                DisplayStartTrial();
            }
            else if (State == AccountState.TrialConfirm)
            {
                // startTrial will be false since in this case we only need legal consent and opt-in
                m_TrialForm ??= new()
                {
                    organization = AccountInfo.Instance.Organization,
                    state = AccountState.TrialConfirm
                };
                DisplayStartTrialConfirm();
            }
            else if (State == AccountState.DataOptIn)
                DisplayDataOptIn();
            else if (State == AccountState.TrialStarted)
                DisplayTrialStarted();
            else if (State == AccountState.SignIn)
                DisplaySignIn();
            else if (State == AccountState.RequestSeat)
                DisplayRequestSeat();
        }

        public void TryDismissCurrentModal()
        {
            if (m_Modal != null)
            {
                m_Modal.Dismiss();
                m_Modal = null;
                m_Dialog = null;
            }
        }

        public virtual void DisplayStartTrial()
        {
            ShowModal(new StartTrialDialog(allowNoAccount)
            {
                OnAccept = org =>
                {
                    m_TrialForm.organization = org;
                    m_TrialForm.state = AccountState.TrialConfirm;

                    // Apply the organization change
                    if (m_TrialForm.organization is not null)
                        AccountInfo.Instance.Organization = m_TrialForm.organization;

                    StateChanged();
                },
                OnClose = () =>
                {
                    GlobalPreferences.trialDialogShown = true;
                    StateChanged();
                }
            });
        }

        public virtual void DisplayStartTrialConfirm() => ShowModal(new StartTrialConfirmDialog(m_TrialForm.organization)
            {
                OnAccept = () =>
                {
                    m_TrialForm.legalConsent.terms_of_service_legal_info = true;
                    m_TrialForm.legalConsent.privacy_policy_gen_ai = true;

                    if (AccountInfo.Instance.LegalConsent.HasConsented)
                        ProcessTrialForm(m_TrialForm);      // Start trial without showing usage opt-in if the user has already consented to the legal terms
                    else
                        TrialFormState = AccountState.DataOptIn;
                },
                OnClose = () => TrialFormState = AccountState.Trial
            });

        public virtual void DisplayDataOptIn() => ShowModal(new DataOpInDialog {OnAccept = usage =>
            {
                m_TrialForm.legalConsent.content_usage_data_training = usage;
                ProcessTrialForm(m_TrialForm);
            }
        });

        public virtual void DisplayTrialStarted() => ShowModal(new SubscriptionStartedDialog
        {
            OnAccept = StateChanged
        });
        public virtual void DisplaySignIn() => ShowModal(new SignInDialog());
        public virtual void DisplayRequestSeat() => ShowModal(new RequestSeatDialog());

        void ShowModal(AccountDialog dialog)
        {
            TryDismissCurrentModal();
            m_Modal = dialog.CreateModal(element);
            m_Dialog = dialog;
            m_Modal.Show();
        }

        void ProcessTrialForm(TrialForm trialForm)
        {
            AsyncUtils.SafeExecute(ProcessTrialFormAsync(trialForm));
        }

        async Task ProcessTrialFormAsync(TrialForm trialForm)
        {
            m_Dialog.SetProcessing();       // Block dialog buttons
            await trialForm.Apply();
            m_TrialForm = null;

            // Normally there should have been a refresh at the correct event, but this acts
            // as a failsafe to ensure the state is always refreshed once all information is known.
            StateChanged();
        }
    }
}
