/*
 * Muse API
 *
 * No description provided (generated by Openapi Generator https://github.com/openapitools/openapi-generator)
 *
 * The version of the OpenAPI document: 1.0.11
 * Generated by: https://github.com/openapitools/openapi-generator.git
 */


using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using OpenAPIDateConverter = Unity.Muse.Chat.Client.OpenAPIDateConverter;

namespace Unity.Muse.Chat.Model
{
    /// <summary>
    /// Defines OptDecision
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    internal enum OptDecision
    {
        /// <summary>
        /// Enum In for value: in
        /// </summary>
        [EnumMember(Value = "in")]
        In = 1,

        /// <summary>
        /// Enum Out for value: out
        /// </summary>
        [EnumMember(Value = "out")]
        Out = 2
    }

}
