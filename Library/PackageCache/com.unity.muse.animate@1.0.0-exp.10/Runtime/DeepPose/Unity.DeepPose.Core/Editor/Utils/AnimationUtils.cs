using System;
using UnityEditor;
using UnityEngine;

namespace Unity.DeepPose.Core.Editor
{
    static class AnimationUtils
    {
        public static AnimationCurve GetAnimationCurve(this AnimationClip clip, string relativePath, Type type, string propertyName, out EditorCurveBinding binding)
        {
            binding = EditorCurveBinding.FloatCurve(relativePath, type, propertyName);
            var curve = AnimationUtility.GetEditorCurve(clip, binding) ?? new AnimationCurve();
            return curve;
        }

        public static void UpdateTransformPositionCurve(this AnimationClip clip, float time, Vector3 localPosition, string relativePath)
        {
            UpdatePositionCurve(clip, time, localPosition, relativePath, typeof(Transform), "m_LocalPosition");
        }

        public static void UpdateTransformRotationCurve(this AnimationClip clip, float time, Quaternion localRotation, string relativePath)
        {
            UpdateRotationCurve(clip, time, localRotation, relativePath, typeof(Transform), "m_LocalRotation");
        }

        public static void UpdatePositionCurve(this AnimationClip clip, float time, Vector3 localPosition, string relativePath, Type type, string propertyName)
        {
            clip.UpdateAnimationCurve(time, localPosition.x, relativePath, type, $"{propertyName}.x");
            clip.UpdateAnimationCurve(time, localPosition.y, relativePath, type, $"{propertyName}.y");
            clip.UpdateAnimationCurve(time, localPosition.z, relativePath, type, $"{propertyName}.z");
        }

        public static void UpdateRotationCurve(this AnimationClip clip, float time, Quaternion localRotation, string relativePath, Type type, string propertyName)
        {
            clip.UpdateAnimationCurve(time, localRotation.x, relativePath, type, $"{propertyName}.x");
            clip.UpdateAnimationCurve(time, localRotation.y, relativePath, type, $"{propertyName}.y");
            clip.UpdateAnimationCurve(time, localRotation.z, relativePath, type, $"{propertyName}.z");
            clip.UpdateAnimationCurve(time, localRotation.w, relativePath, type, $"{propertyName}.w");
        }

        public static void UpdateAnimationCurve(this AnimationClip clip, float time, float value, string relativePath, Type type, string propertyName)
        {
            var curve = clip.GetAnimationCurve(relativePath, type, propertyName, out var binding);

            for (var i = 0; i < curve.keys.Length; i++)
            {
                if (curve.keys[i].time == time)
                {
                    curve.RemoveKey(i);
                    break;
                }
            }

            curve.AddKey(time, value);
            clip.SetCurve(relativePath, type, propertyName, curve);

            AnimationUtility.onCurveWasModified?.Invoke(clip, binding, AnimationUtility.CurveModifiedType.CurveModified);
        }
    }
}
