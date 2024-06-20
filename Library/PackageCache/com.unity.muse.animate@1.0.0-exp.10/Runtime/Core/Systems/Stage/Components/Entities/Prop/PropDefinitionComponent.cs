using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Muse.Animate
{
    class PropDefinitionComponent : MonoBehaviour
    {
        public ArmatureMappingComponent ReferenceViewArmature => m_ViewArmature;
        public ArmatureMappingComponent ReferencePosingArmature => m_PosingArmature;
        public ArmatureMappingComponent ReferencePhysicsArmature => m_PhysicsArmature;
        public int NumJoints => m_PosingArmature.NumJoints;

        [SerializeField]
        ArmatureMappingComponent m_ViewArmature;

        [SerializeField]
        ArmatureMappingComponent m_PosingArmature;

        [SerializeField]
        ArmatureMappingComponent m_PhysicsArmature;

        void Awake()
        {
            Assert.IsNotNull(m_ViewArmature, "No view armature specified");
            Assert.IsNotNull(m_PosingArmature, "No posing armature specified");
            Assert.IsNotNull(m_PhysicsArmature, "No physics armature specified");

            Assert.IsTrue(m_ViewArmature.IsValid, "Invalid view armature");
            Assert.IsTrue(m_PosingArmature.IsValid, "Invalid posing armature");
            Assert.IsTrue(m_PhysicsArmature.IsValid, "Invalid physics armature");

            Assert.IsFalse(m_ViewArmature.gameObject.activeSelf, "View armature gameObject should be disabled in prefab");
            Assert.IsFalse(m_PosingArmature.gameObject.activeSelf, "Posing armature gameObject should be disabled in prefab");
            Assert.IsFalse(m_PhysicsArmature.gameObject.activeSelf, "Physics armature gameObject should be disabled in prefab");

            Assert.IsTrue(m_ViewArmature.ArmatureDefinition.HaveSameJointNames(m_PosingArmature.ArmatureDefinition), "Inconsistent armatures");
            Assert.IsTrue(m_ViewArmature.ArmatureDefinition.HaveSameJointNames(m_PhysicsArmature.ArmatureDefinition), "Inconsistent armatures");
        }
    }
}
