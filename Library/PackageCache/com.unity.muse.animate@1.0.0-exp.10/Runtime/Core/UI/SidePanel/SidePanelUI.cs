using System;
using UnityEngine;
using AppUI = Unity.Muse.AppUI.UI;
using UnityEngine.UIElements;

namespace Unity.Muse.Animate
{
    class SidePanelUI : TogglePanel, IUITemplate
    {
        const string k_UssClassName = "deeppose-inspectors-panel";

        SidePanelUIModel m_Model;
        
        public SidePanelUI()
            : base(k_UssClassName) { }

        void IUITemplate.InitComponents()
        {
            base.InitComponents();
            
            var pageTypes = Enum.GetValues(typeof(SidePanelUtils.PageType));
            
            // NOTE: Disabling the other pages until they are needed
            
            for (var i = 0; i < /*pageTypes.Length*/ 1; i++)
            {
                var pageType = (SidePanelUtils.PageType)i;
                AddPage(pageType.GetTitle(), pageType.GetIcon());
                AddButton(pageType.GetIcon(), pageType.GetTooltip());
            }
        }

        public void SetModel(SidePanelUIModel model)
        {
            m_Model = model;
            base.SetModel(model);
            Update();
        }
        
        public override void ClickedLeft()
        {
            SelectPage(-1);
        }
        
        public new class UxmlFactory : UxmlFactory<SidePanelUI, UxmlTraits> { }
    }
}
