using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace BiosConfiguration.JsonConverters
{
    public class InputConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            throw new NotImplementedException();
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;
            var obj = JObject.Load(reader);

            // Remove the Result property for manual deserialization
            var typeString = obj.GetValue(nameof(BiosInput.Interface), StringComparison.OrdinalIgnoreCase);

            // Process the Result property
            if (typeString is null) throw new ArgumentException("configuration input doesn't contain a type");

            var returnType = BiosInput.GetTypeForType(typeString.Value<string>());

            var contract = (JsonObjectContract)serializer.ContractResolver.ResolveContract(returnType);

            var output = existingValue ?? contract.DefaultCreator?.Invoke();

            // Populate the remaining properties.
            if (output is not null)
            {
                using var subReader = obj.CreateReader();
                serializer.Populate(subReader, output);
            }

            return output;
        }

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}