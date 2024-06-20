
using UnityEngine;

namespace Unity.Muse.Animate
{
    class SequenceKeyInspectorViewModel : SequenceItemInspectorViewModel<TimelineModel.SequenceKey>
    {
        public override TimelineModel.SequenceKey Target
        {
            get => m_Target;
            set
            {
                if (m_Target != null)
                {
                    m_Target.Key.OnChanged -= OnTargetModelChanged;
                }

                base.Target = value;

                if (m_Target != null)
                {
                    m_Target.Key.OnChanged += OnTargetModelChanged;
                }
            }
        }

        TransitionModel InTransition => m_Target?.InTransition?.Transition;
        TransitionModel OutTransition => m_Target?.OutTransition?.Transition;

        public bool HasTransition => OutTransition != null;
        public float TransitionSpeed => OutTransition?.Duration ?? 0f;
        public bool CanLoop => InTransition != null && OutTransition == null;

        public bool CanExtrapolate => InTransition != null;

        public bool IsLooping => HasTarget && Target.Key.Type == KeyData.KeyType.Loop;
        public bool IsExtrapolating => HasTarget && Target.Key.Type == KeyData.KeyType.Empty;

        public SequenceKeyInspectorViewModel(InspectorsPanelViewModel inspectorsPanel)
            : base(inspectorsPanel) {}

        void OnTargetModelChanged(KeyModel keyModel, KeyModel.Property property)
        {
            NotifyTargetChange();
        }

        public void SetTransitionSpeed(float speed)
        {
            OutTransition.Duration = Mathf.RoundToInt(speed);
        }

        public void SetLooping(bool isLooping)
        {
            if (isLooping)
            {
                Target.Key.Type = KeyData.KeyType.Loop;
                Target.Key.Loop.StartFrame = 0;
                Target.Key.Loop.NumBakingLoopbacks = 1;
            }
            else
            {
                Target.Key.Type = KeyData.KeyType.FullPose;
            }
        }

        public void SetExtrapolating(bool isExtrapolating)
        {
            Target.Key.Type = isExtrapolating ? KeyData.KeyType.Empty : KeyData.KeyType.FullPose;
        }
    }
}
