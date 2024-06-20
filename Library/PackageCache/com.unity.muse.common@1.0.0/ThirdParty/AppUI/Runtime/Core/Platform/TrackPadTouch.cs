using UnityEngine;

namespace Unity.AppUI.Core
{
    /// <summary>
    /// A touch event received from a magic trackpad.
    /// </summary>
    /// <remarks>
    /// Theses Touch events can be received from a magic trackpad on macOS.
    /// </remarks>
    public struct TrackPadTouch
    {
        /// <summary>
        /// The unique identifier of the touch.
        /// </summary>
        public int fingerId { get; }

        /// <summary>
        /// The position of the touch in normalized coordinates.
        /// </summary>
        public Vector2 position { get; }

        /// <summary>
        /// The number of taps. This is always 1 for a trackpad.
        /// </summary>
        public int tapCount { get; }

        /// <summary>
        /// The delta position of the touch since the last frame.
        /// </summary>
        public Vector2 deltaPos { get; }

        /// <summary>
        /// The delta time since the last frame.
        /// </summary>
        public float deltaTime { get; }

        /// <summary>
        /// The phase of the touch.
        /// </summary>
        public TouchPhase phase { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="fingerId"> The unique identifier of the touch.</param>
        /// <param name="position"> The position of the touch in normalized coordinates.</param>
        /// <param name="tapCount"> The number of taps. This is always 1 for a trackpad.</param>
        /// <param name="deltaPos"> The delta position of the touch since the last frame.</param>
        /// <param name="deltaTime"> The delta time since the last frame.</param>
        /// <param name="phase"> The phase of the touch.</param>
        public TrackPadTouch(int fingerId, Vector2 position, int tapCount, Vector2 deltaPos, float deltaTime,
            TouchPhase phase)
        {
            this.fingerId = fingerId;
            this.position = position;
            this.tapCount = tapCount;
            this.deltaPos = deltaPos;
            this.deltaTime = deltaTime;
            this.phase = phase;
        }
    }
}
