using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Muse.Animate
{
    class ActorDefinitionComponent : MonoBehaviour
    {
        public ArmatureMappingComponent ReferenceViewArmature => m_ViewArmature;
        public ArmatureMappingComponent ReferencePosingArmature => m_PosingArmature;
        public ArmatureMappingComponent ReferencePhysicsArmature => m_PhysicsArmature;
        public ArmatureMappingComponent ReferenceMotionArmature => m_MotionArmature;
        public ArmatureMappingComponent ReferenceTextToMotionArmature => m_TextToMotionArmature;
        
        public ArmatureToArmatureMapping PosingToCharacterArmatureMapping => m_PosingToCharacterArmatureMapping;
        public JointMask EvaluationJointMask => m_EvaluationJointMask;
        
        public int NumJoints => PosingToCharacterArmatureMapping.TargetArmature.NumJoints;

        [SerializeField]
        ArmatureMappingComponent m_ViewArmature;

        [SerializeField]
        ArmatureMappingComponent m_PosingArmature;

        [SerializeField]
        ArmatureMappingComponent m_PhysicsArmature;

        [SerializeField]
        ArmatureMappingComponent m_MotionArmature;

        [SerializeField]
        ArmatureMappingComponent m_TextToMotionArmature;

        [SerializeField]
        ArmatureToArmatureMapping m_PosingToCharacterArmatureMapping;

        [SerializeField, Tooltip("Joints to use for evaluating errors in recovered motion.")]
        JointMask m_EvaluationJointMask;

        void Awake()
        {
            Assert.IsNotNull(m_ViewArmature, "No view armature specified");
            Assert.IsNotNull(m_PosingArmature, "No posing armature specified");
            Assert.IsNotNull(m_PhysicsArmature, "No physics armature specified");
            Assert.IsNotNull(m_MotionArmature, "No motion armature specified");
            Assert.IsNotNull(m_PosingToCharacterArmatureMapping, "No ArmatureToArmatureMapping specified for Posing");

            Assert.IsTrue(m_ViewArmature.IsValid, "Invalid view armature");
            Assert.IsTrue(m_PosingArmature.IsValid, "Invalid posing armature");
            Assert.IsTrue(m_PhysicsArmature.IsValid, "Invalid physics armature");
            Assert.IsTrue(m_MotionArmature.IsValid, "Invalid motion armature");

            Assert.IsFalse(m_ViewArmature.gameObject.activeSelf, "View armature gameObject should be disabled in prefab");
            Assert.IsFalse(m_PosingArmature.gameObject.activeSelf, "Posing armature gameObject should be disabled in prefab");
            Assert.IsFalse(m_PhysicsArmature.gameObject.activeSelf, "Physics armature gameObject should be disabled in prefab");
            Assert.IsFalse(m_MotionArmature.gameObject.activeSelf, "Motion armature gameObject should be disabled in prefab");

            Assert.IsTrue(m_ViewArmature.ArmatureDefinition.HaveSameJointNames(m_PosingArmature.ArmatureDefinition), "Inconsistent armatures");
            Assert.IsTrue(m_ViewArmature.ArmatureDefinition.HaveSameJointNames(m_PhysicsArmature.ArmatureDefinition), "Inconsistent armatures");
            Assert.IsTrue(m_ViewArmature.ArmatureDefinition.HaveSameJointNames(m_MotionArmature.ArmatureDefinition), "Inconsistent armatures");

            // Hell yeah
            m_PhysicsArmature.gameObject.AddComponent<PhysicsJointFixer>();
        }
    }
}
