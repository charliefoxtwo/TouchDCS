using System;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Configuration.JsonConverters
{
    // heavily inspired by https://stackoverflow.com/a/24473416
    public class IpAddressConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IPAddress);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            return IPAddress.Parse(JToken.Load(reader).ToString());
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            JToken.FromObject(value?.ToString() ?? string.Empty).WriteTo(writer);
        }
    }
}