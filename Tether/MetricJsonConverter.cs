using System;
using System.Collections.Generic;
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

            var content = new JObject()
            {
                {"hostname", item.Hostname},
                {"type", item.Type.ToString().ToLowerInvariant()},
            };


            if (item.Tags == null || !item.Tags.Any())
            {
                item.Tags = new Dictionary<string, string>{{"device_name", Environment.MachineName}};
            }
            
            content.Add("tags", JToken.FromObject(item.Tags.Select(f => $"{f.Key}:{f.Value}")));

            var array = new JArray
            {
                item.Name, 
                item.Timestamp.GetUnixTimestamp(),
                item.Value,
                content
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