using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tether.Plugins;

namespace Tether
{
    class MetricJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null || value.GetType() != typeof(Metric))
            {
                return;
            }

            var item = (Metric) value;

            var array = new JArray
            {
                item.Name, 
                item.Timestamp.GetUnixTimestamp(),
                item.Value,
                new JObject()
                {
                    {"hostname", item.Hostname},
                    {"type", item.Type.ToString().ToLowerInvariant()},
                    {"tags", JToken.FromObject(item.Tags.Select(f=>$"{f.Key}:{f.Value}"))}
                }
            };

            array.WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) => null;

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Metric);
        }
    }
}