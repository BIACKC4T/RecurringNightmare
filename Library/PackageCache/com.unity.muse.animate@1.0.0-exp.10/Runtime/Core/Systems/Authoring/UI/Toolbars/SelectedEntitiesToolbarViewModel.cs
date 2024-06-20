using System;
using UnityEngine;

using AppUI = Unity.Muse.AppUI.UI;
using AppCore = Unity.AppUI.Core;

namespace Unity.Muse.Animate
{
    class SelectedEntitiesToolbarViewModel
    {
        public delegate void Changed();
        public event Changed OnChanged;

        public bool IsVisible
        {
            get => m_IsVisible;
            set
            {
                if (value == m_IsVisible)
                    return;

                m_IsVisible = value;
                OnChanged?.Invoke();
            }
        }

        public bool CanCopyPose
        {
            get => m_CanCopyPose;
            set
            {
                if (value == m_CanCopyPose)
                    return;

                m_CanCopyPose = value;
                OnChanged?.Invoke();
            }
        }

        public bool CanDeleteSelectedEntities
        {
            get => m_CanDeleteSelectedEntities;
            set
            {
                if (value == m_CanDeleteSelectedEntities)
                    return;

                m_CanDeleteSelectedEntities = value;
                OnChanged?.Invoke();
            }
        }

        public bool CanEstimatePose
        {
            get => m_CanEstimatePose;
            set
            {
                if (value == m_CanEstimatePose)
                    return;

                m_CanEstimatePose = value;
                OnChanged?.Invoke();
            }
        }

        bool m_IsVisible;
        bool m_CanCopyPose;
        bool m_CanDeleteSelectedEntities;
        bool m_CanEstimatePose;

        readonly AuthoringModel m_AuthoringModel;


        public SelectedEntitiesToolbarViewModel(AuthoringModel authoringModel)
        {
            m_AuthoringModel = authoringModel;
            m_AuthoringModel.OnChanged += OnAuthoringChanged;

            RefreshProperties();
        }

        void RefreshProperties()
        {
            RefreshCanCopyPose();
            RefreshCanEstimatePose();
            RefreshCanDelete();
        }

        void RefreshCanCopyPose()
        {
            CanCopyPose = m_AuthoringModel.Timeline.CanCopyPose;
        }

        void RefreshCanDelete()
        {
            CanDeleteSelectedEntities = m_AuthoringModel.Timeline.CanDeleteSelectedEntities;
        }

        void RefreshCanEstimatePose()
        {
            CanEstimatePose = m_AuthoringModel.Timeline.CanEstimatePose;
        }

        public void RequestPoseEstimation()
        {
            m_AuthoringModel.Timeline.RequestPoseEstimation();
        }

        public void RequestDeleteSelectedEntities()
        {
            m_AuthoringModel.Timeline.RequestDeleteSelectedEntities();
        }

        public void RequestCopyPose()
        {
            m_AuthoringModel.Timeline.RequestCopyPose();
        }

        void OnAuthoringChanged()
        {
            RefreshProperties();
        }
    }
}
