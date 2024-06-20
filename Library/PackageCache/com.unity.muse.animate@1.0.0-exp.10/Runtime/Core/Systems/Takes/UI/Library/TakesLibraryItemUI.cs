using System;
using System.Diagnostics;
using UnityEngine;
using Unity.Muse.AppUI.UI;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace Unity.Muse.Animate
{
    /// <summary>
    /// UI Component responsible for displaying a take in a takes library.
    /// </summary>
    class TakesLibraryItemUI : LibraryItemUI
    {
        TakeModel Take => m_Model?.Target as TakeModel;

        const string k_UssClassName = "deeppose-library-item";
        const string k_UssClassNameSelected = "deeppose-library-item-selected";
        const string k_UssClassNameHighlighted = "deeppose-library-item-highlighted";
        const string k_UssClassNameEditing = "deeppose-library-item-editing";
        const string k_ThumbnailUssClassName = "deeppose-library-item-thumbnail";
        const string k_CircularProgressUssClassName = "deeppose-library-item-circular-progress";
        const string k_TypeIconUssClassName = "deeppose-library-item-type-icon";
        
        static readonly string[] k_TakeTypeIcons = { "", "recorder" };
        
        Image m_ThumbnailElement;
        Icon m_TypeIcon;
        CircularProgress m_CircularProgress;

        public TakesLibraryItemUI()
            :
            base(k_UssClassName, k_UssClassNameSelected, k_UssClassNameHighlighted, k_UssClassNameEditing)
        {
            name = "library-item-take";
            InitComponents();
        }
        
        public void SetModel(TakesLibraryItemUIModel viewModel)
        {
            base.SetModel(viewModel);
        }

        protected override void Update()
        {
            base.Update();
           
            if (m_Model?.Target == null)
            {
                Debug.LogError($"TakesLibraryItemUI -> Update() failed, model Target is null.");
                return;
            }
            
            DevLogger.LogSeverity(TraceLevel.Verbose, $"TakesLibraryItemUI -> Update()");
            
            UpdateThumbnail();
            UpdateTypeIcon();
            UpdateProgress();
        }

        void InitComponents()
        {
            DevLogger.LogSeverity(TraceLevel.Verbose, $"TakesLibraryItemUI -> InitComponents()");
            
            m_ThumbnailElement = new Image() { name = "take-thumbnail" };
            m_ThumbnailElement.pickingMode = PickingMode.Ignore;
            m_ThumbnailElement.AddToClassList(k_ThumbnailUssClassName);
            Add(m_ThumbnailElement);
            
            m_TypeIcon = new Icon() { name = "take-type-icon" };
            m_TypeIcon.pickingMode = PickingMode.Ignore;
            m_TypeIcon.AddToClassList(k_TypeIconUssClassName);
            Add(m_TypeIcon);
            
            m_CircularProgress = new CircularProgress(){ name = "take-circular-progress" };;
            m_CircularProgress.variant = Progress.Variant.Indeterminate;
            m_CircularProgress.bufferValue = 1f;
            m_CircularProgress.pickingMode = PickingMode.Ignore;
            m_CircularProgress.size = Size.L;
            m_CircularProgress.AddToClassList(k_CircularProgressUssClassName);
            Add(m_CircularProgress);
        }
        
        void UpdateThumbnail()
        {
            DevLogger.LogSeverity(TraceLevel.Verbose, $"TakesLibraryItemUI -> UpdateThumbnail()");
            
            if (m_Model?.Target?.Thumbnail == null)
            {
                if (m_ThumbnailElement != null)
                {
                    m_ThumbnailElement.style.display = DisplayStyle.None;
                }
                
                return;
            }
            
            m_ThumbnailElement.image = m_Model.Target.Thumbnail.Texture;
            m_ThumbnailElement.scaleMode = ScaleMode.StretchToFill;
            m_ThumbnailElement.style.display = DisplayStyle.Flex;
        }

        void UpdateTypeIcon()
        {
            DevLogger.LogSeverity(TraceLevel.Verbose, $"TakesLibraryItemUI -> UpdateTypeIcon()");

            m_TypeIcon.iconName = k_TakeTypeIcons[(int)Take.Type];
        }

        void UpdateProgress()
        {
            if (Take == null)
                return;
            
            DevLogger.LogSeverity(TraceLevel.Verbose, $"TakesLibraryItemUI -> UpdateProgress("+Take.Progress+")");
            
            m_CircularProgress.value = Take.Progress;
            m_CircularProgress.style.display = Take.Progress >= 1 ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
