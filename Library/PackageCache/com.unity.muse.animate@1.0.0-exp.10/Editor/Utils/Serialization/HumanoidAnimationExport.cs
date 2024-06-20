using System.Linq;
using Unity.DeepPose.Core;
using Unity.Muse.Animate.Usd;
using UnityEngine;
using UnityEngine.Assertions;
using static Unity.DeepPose.Core.Editor.AnimationUtils;

namespace Unity.Muse.Animate.Editor
{
    static class HumanoidAnimationExport
    {
        public static AnimationClip Export(ExportData exportData)
        {
            var actor = exportData.ActorsData.FirstOrDefault();
            Assert.IsNotNull(actor);
            
            if (!actor.PosingArmature.TryGetComponent<Animator>(out var animator))
                return null;
            
            var clip = new AnimationClip();
            using var humanPoseHandler = new HumanPoseHandler(animator.avatar, animator.transform);
            var humanPose = new HumanPose();
            for (var i = 0; i < exportData.BackedTimeline.FramesCount; i++)
            {
                var time = i / ApplicationConstants.FramesPerSecond;
                var frameModel = exportData.BackedTimeline.GetFrame(i);
                if (!frameModel.TryGetPose(actor.ActorModel.EntityID, out var pose))
                    continue;
                
                pose.ApplyTo(actor.PosingArmature.ArmatureMappingData);
                humanPoseHandler.GetHumanPose(ref humanPose);
                
                clip.UpdatePositionCurve(time, humanPose.bodyPosition, "", typeof(Animator), "RootT");
                clip.UpdateRotationCurve(time, humanPose.bodyRotation, "", typeof(Animator), "RootQ");

                for (var j = 0; j < humanPose.muscles.Length; j++)
                {
                    var muscleName = HumanoidUtils.GetMuscleBindingName(j);
                    
                    clip.UpdateAnimationCurve(time, humanPose.muscles[j], "", typeof(Animator), muscleName);
                }
            }

            return clip;
        }
    }
}
