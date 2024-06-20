using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Unity.Muse.Animate
{
    /// <summary>
    /// Represents a transition
    /// </summary>
    [Serializable]
    class TransitionModel
    {
        public enum Property
        {
            Duration,
            Type
        }

        [SerializeField]
        TransitionData m_Data;

        /// <summary>
        /// Checks if the transition is in a valid state
        /// </summary>
        public bool IsValid => m_Data.Duration > 0;

        /// <summary>
        /// Duration of the transition, in frames
        /// </summary>
        public int Duration
        {
            get => m_Data.Duration;
            set
            {
                var correctedValue = Mathf.Max(1, value);
                if (correctedValue == m_Data.Duration)
                    return;

                m_Data.Duration = correctedValue;
                OnChanged?.Invoke(this, Property.Duration);
            }
        }

        /// <summary>
        /// The type of the transition
        /// </summary>
        public TransitionData.TransitionType Type
        {
            get => m_Data.Type;
            set
            {
                if (value == m_Data.Type)
                    return;

                m_Data.Type = value;
                OnChanged?.Invoke(this, Property.Type);
            }
        }

        public delegate void Changed(TransitionModel model, Property property);
        public event Changed OnChanged;

        /// <summary>
        /// Creates a new transition
        /// </summary>
        public TransitionModel()
        {
            m_Data.Duration = 30;
            m_Data.Type = TransitionData.TransitionType.MotionSynthesis;
        }

        [JsonConstructor]
        public TransitionModel(TransitionData m_Data)
        {
            this.m_Data = m_Data;
        }

        /// <summary>
        /// Copies the transition to another transition
        /// </summary>
        /// <param name="other">The other transition to which the state will be copied</param>
        public void CopyTo(TransitionModel other)
        {
            other.Duration = Duration;
            other.Type = Type;
        }

        /// <summary>
        /// Merge a source transition into that transition.
        /// </summary>
        /// <param name="source">The source transition to merge. Left unchanged.</param>
        public void Fuse(TransitionModel source)
        {
            Duration += source.Duration;
            // Type is left unchanged for now until we have to deal with it
        }

        /// <summary>
        /// Splits a transition to a target transition.
        /// This transition will be updated to store the first half and the target transition the second half
        /// </summary>
        /// <param name="target">The target transition that will store the second half of the split result</param>
        /// <param name="timePercent">An indication of where to perform the split. Must be between 0 and 1.</param>
        public void Split(TransitionModel target, float timePercent = 0.5f)
        {
            if (timePercent < 0f || timePercent > 1f)
                AssertUtils.Fail($"Invalid splitting percent: {timePercent}. Must be between 0 and 1.");

            target.Type = Type;
            var remainder = Mathf.RoundToInt((1f-timePercent) * Duration);
            target.Duration = remainder;
            Duration -= remainder;

            target.Type = Type;
        }
    }
}
