using System;
using UnityEngine;

namespace Unity.AppUI.Core
{
    /// <summary>
    /// A magnification gesture received from a magic trackpad.
    /// </summary>
    public struct MagnificationGesture : IEquatable<MagnificationGesture>
    {
        /// <summary>
        /// The magnification delta of the gesture since the last frame.
        /// </summary>
        public float deltaMagnification { get; }

        /// <summary>
        /// The scroll delta of the gesture since the last frame.
        /// </summary>
        /// <remarks>
        /// This is a convenience property to convert the magnification delta to a scroll delta.
        /// </remarks>
        public Vector2 scrollDelta => new Vector2(0, -deltaMagnification * 50f);

        /// <summary>
        /// The phase of the gesture.
        /// </summary>
        public TouchPhase phase { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="deltaMagnification">The magnification delta of the gesture since the last frame.</param>
        /// <param name="phase">The phase of the gesture.</param>
        public MagnificationGesture(float deltaMagnification, TouchPhase phase)
        {
            this.phase = phase;
            this.deltaMagnification = deltaMagnification;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="other"> The object to compare with the current object.</param>
        /// <returns> True if objects are equal, false otherwise.</returns>
        public bool Equals(MagnificationGesture other)
        {
            return Mathf.Approximately(deltaMagnification, other.deltaMagnification) && phase == other.phase;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj"> The object to compare with the current object.</param>
        /// <returns> True if the first MagnificationGesture is equal to the second MagnificationGesture, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            return obj is MagnificationGesture other && Equals(other);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns> A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(deltaMagnification, (int) phase);
        }
        
        /// <summary>
        /// Determines whether two specified MagnificationGesture objects are equal.
        /// </summary>
        /// <param name="left"> The first MagnificationGesture to compare.</param>
        /// <param name="right"> The second MagnificationGesture to compare.</param>
        /// <returns> True if the first MagnificationGesture is equal to the second MagnificationGesture, false otherwise.</returns>
        public static bool operator ==(MagnificationGesture left, MagnificationGesture right)
        {
            return left.Equals(right);
        }
        
        /// <summary>
        /// Determines whether two specified MagnificationGesture objects are not equal.
        /// </summary>
        /// <param name="left"> The first MagnificationGesture to compare.</param>
        /// <param name="right"> The second MagnificationGesture to compare.</param>
        /// <returns> True if the first MagnificationGesture is not equal to the second MagnificationGesture, false otherwise.</returns>
        public static bool operator !=(MagnificationGesture left, MagnificationGesture right)
        {
            return !left.Equals(right);
        }
    }
}
