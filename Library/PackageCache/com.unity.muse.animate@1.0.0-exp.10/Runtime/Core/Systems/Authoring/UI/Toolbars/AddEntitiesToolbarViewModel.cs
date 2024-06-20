using DefaultNamespace;
using Unity.Muse.Animate.Prop;
using UnityEngine;

namespace Unity.Muse.Animate
{
    class AddEntitiesToolbarViewModel
    {
        public delegate void Changed();
        public event Changed OnChanged;

        public delegate void RequestedAddPropsMenu();
        public event RequestedAddPropsMenu OnRequestedAddPropsMenu;

        public delegate void RequestedAddActorsMenu();
        public event RequestedAddActorsMenu OnRequestedAddActorsMenu;

        public delegate void CreatedActor();
        public event CreatedActor OnCreatedActorFromMenu;

        public delegate void CreatedProp();
        public event CreatedProp OnCreatedPropFromMenu;

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

        public StageModel Stage => m_StageModel;

        StageModel m_StageModel;
        bool m_IsVisible;

        public AddEntitiesToolbarViewModel(StageModel stageModel)
        {
            m_StageModel = stageModel;
        }

        public void RequestAddActorsMenu()
        {
            OnRequestedAddActorsMenu?.Invoke();
        }

        public void RequestAddPropsMenu()
        {
            OnRequestedAddPropsMenu?.Invoke();
        }

        public void CreatePropFromMenu(PropDefinition definition)
        {
            m_StageModel.CreateProp(definition.ID, Vector3.zero, Quaternion.identity);
            OnCreatedPropFromMenu?.Invoke();
        }

        public void CreateActorFromMenu(ActorDefinition definition)
        {
            m_StageModel.CreateActor(definition.ID, Vector3.zero, Quaternion.identity);
            OnCreatedActorFromMenu?.Invoke();
        }
    }
}
