using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Unity.Muse.Animate
{
    class TakeJsonConverter : JsonConverter<TakeModel>
    {
        static Dictionary<TakeModel.TakeType, Type> s_TakeTypeToType;

        static TakeJsonConverter()
        {
            // Get all the types that inherit from TakeModel with the TakesSerializationType attribute.
            var takeTypes = ReflectionUtils.GetAllTypesWithAttribute<SerializableTakeAttribute>(type =>
                typeof(TakeModel).IsAssignableFrom(type));

            // Create a mapping from the TakeType enum to the type.
            s_TakeTypeToType = takeTypes.ToDictionary(type => type.GetCustomAttribute<SerializableTakeAttribute>().Type);
        }

        public override void WriteJson(JsonWriter writer, TakeModel value, JsonSerializer serializer)
        {
            var takeType = value.Type;
            var serializedType = s_TakeTypeToType[takeType];
            
            // Serialize as the specified type
            serializer.Serialize(writer, value, serializedType);
        }

        public override TakeModel ReadJson(JsonReader reader,
            Type objectType,
            TakeModel existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            var jo = JObject.Load(reader);
            
            // Get the type of the take from the json.
            if (jo["m_Type"]?.ToObject<TakeModel.TakeType>() is not { } takeType)
            {
                throw new JsonSerializationException("Take type not found in json.");
            }
            
            // Create an instance of the correct take type.
            if (hasExistingValue)
            {
                serializer.Populate(jo.CreateReader(), existingValue);
                return existingValue;
            }

            return jo.ToObject(s_TakeTypeToType[takeType], serializer) as TakeModel;
        }
    }
}
