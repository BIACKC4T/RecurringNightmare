﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace Unity.Muse.Animate
{
    class UnityContractResolver : DefaultContractResolver
    {
        protected override List<MemberInfo> GetSerializableMembers(Type objectType)
        {
            var members = base.GetSerializableMembers(objectType);

            for (var i = members.Count - 1; i >= 0; i--)
            {
                if (FilterField(members[i]))
                    members.RemoveAt(i);
            }

            members.AddRange(GetMissingMembers(objectType, members));

            return members;
        }

        static bool FilterField(MemberInfo info)
        {
            return (info.MemberType & MemberTypes.Property) != 0;
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var jsonProperty = base.CreateProperty(member, memberSerialization);

            if (member.GetCustomAttribute<SerializeField>() != null)
            {
                jsonProperty.Ignored = false;
                jsonProperty.Writable = CanWriteMemberWithSerializeField(member);
                jsonProperty.Readable = CanReadMemberWithSerializeField(member);
                jsonProperty.HasMemberAttribute = true;
            }

            return jsonProperty;
        }

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var lists = base.CreateProperties(type, memberSerialization);
            return lists;
        }

        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {
            var jsonObjectContract = base.CreateObjectContract(objectType);

            if (typeof(ScriptableObject).IsAssignableFrom(objectType))
            {
                jsonObjectContract.DefaultCreator = () => ScriptableObject.CreateInstance(objectType);
            }

            return jsonObjectContract;
        }

        /// <summary>
        /// Get the members with the <see cref="SerializeField"/> attribute that are not already in the list.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="alreadyAdded"></param>
        /// <returns></returns>
        static IEnumerable<MemberInfo> GetMissingMembers(Type type, List<MemberInfo> alreadyAdded)
        {
            return type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                .Cast<MemberInfo>()
                .Concat(type.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
                .Where(o => o.GetCustomAttribute<SerializeField>() != null
                    && !alreadyAdded.Contains(o))
                .Concat(GetParentPrivateFields(type, alreadyAdded));
        }

        /// <summary>
        /// Get the private fields from the parent hierarchy with the <see cref="SerializeField"/> attribute that are
        /// not already in the list.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="alreadyAdded"></param>
        /// <returns></returns>
        static IEnumerable<MemberInfo> GetParentPrivateFields(Type type, ICollection<MemberInfo> alreadyAdded)
        {
            var targetType = type;
            const BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;
            // Get all the private fields from the parent classes by walking up the hierarchy.
            // This something that Type.GetFields() isn't able to do on its own, unfortunately. 
            while ((targetType = targetType.BaseType) != null)
            {
                var fields = targetType.GetFields(bindingFlags);
                foreach (var field in fields)
                {
                    if (!field.IsPrivate ||
                        field.GetCustomAttribute<SerializeField>() == null ||
                        alreadyAdded.Contains(field))
                        continue;

                    yield return field;
                }
            }
        }

        static bool CanReadMemberWithSerializeField(MemberInfo member)
        {
            if (member is PropertyInfo property)
                return property.CanRead;

            return true;
        }

        static bool CanWriteMemberWithSerializeField(MemberInfo member)
        {
            if (member is PropertyInfo property)
                return property.CanWrite;

            return true;
        }
    }
}
