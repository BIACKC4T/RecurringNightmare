using System;
using UnityEngine;

namespace Unity.AppUI.Core
{
    /// <summary>
    /// A pan gesture received from a magic trackpad.
    /// </summary>
    public struct PanGesture : IEquatable<PanGesture>
    {
        /// <summary>
        /// The delta position of the touch since the last frame.
        /// </summary>
        public Vector2 deltaPos { get; }

        /// <summary>
        /// The phase of the gesture.
        /// </summary>
        public TouchPhase phase { get; }
        
        /// <summary>
        /// The position of the touch in normalized coordinates.
        /// </summary>
        public Vector2 position { get; }
        
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="position">The position of the touch in normalized coordinates.</param>
        /// <param name="deltaPos">The delta position of the touch since the last frame.</param>
        /// <param name="phase">The phase of the gesture.</param>
        public PanGesture(Vector2 position, Vector2 deltaPos, TouchPhase phase)
        {
            this.position = position;
            this.deltaPos = deltaPos;
            this.phase = phase;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="other"> The object to compare with the current object.</param>
        /// <returns> True if objects are equal, false otherwise.</returns>
        public bool Equals(PanGesture other)
        {
            return deltaPos.Equals(other.deltaPos) && phase == other.phase && position.Equals(other.position);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj"> The object to compare with the current object.</param>
        /// <returns> True if objects are equal, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            return obj is PanGesture other && Equals(other);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns> A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(deltaPos, position, (int) phase);
        }
        
        /// <summary>
        /// Determines whether two specified PanGesture objects are equal.
        /// </summary>
        /// <param name="left"> The first PanGesture to compare.</param>
        /// <param name="right"> The second PanGesture to compare.</param>
        /// <returns> True if the first PanGesture is equal to the second PanGesture, false otherwise.</returns>
        public static bool operator ==(PanGesture left, PanGesture right)
        {
            return left.Equals(right);
        }
        
        /// <summary>
        /// Determines whether two specified PanGesture objects are not equal.
        /// </summary>
        /// <param name="left"> The first PanGesture to compare.</param>
        /// <param name="right"> The second PanGesture to compare.</param>
        /// <returns> True if the first PanGesture is not equal to the second PanGesture, false otherwise.</returns>
        public static bool operator !=(PanGesture left, PanGesture right)
        {
            return !left.Equals(right);
        }
    }
}
