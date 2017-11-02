using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.EventSourcing
{
    public class JsonDataSerializer : IDataSerializer
    {
        private readonly static JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
        };

        public string Name { get; }

        public JsonDataSerializer(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public T Deserialize<T>(byte[] payload)
        {
            var serialized = Encoding.UTF8.GetString(payload);
            return JsonConvert.DeserializeObject<T>(serialized, _jsonSettings);
        }

        public byte[] Serialize(object value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var serialized = JsonConvert.SerializeObject(value, _jsonSettings);
            return Encoding.UTF8.GetBytes(serialized);
        }
    }
}
