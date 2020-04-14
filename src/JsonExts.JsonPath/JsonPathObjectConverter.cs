using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JsonExts.JsonPath.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace JsonExts.JsonPath
{
    /// <summary>
    /// A <see cref="JsonConverter"/> for deserializing objects containing properties
    /// with the <see cref="JsonPathAttribute"/> using JSONPath expressions.
    /// </summary>
    public class JsonPathObjectConverter : JsonConverter
    {
        /// <summary>
        /// Cache of calls to <see cref="CanConvert"/> method to avoid unnecessary reflection.
        /// </summary>
        protected ConcurrentDictionary<Type, bool> CanConvertCache { get; set; } = new ConcurrentDictionary<Type, bool>();

        /// <inheritdoc />
        public override bool CanConvert(Type objectType)
        {
            if (CanConvertCache.TryGetValue(objectType, out var cacheResult))
                return cacheResult;

            var result = objectType.GetProperties().Any(x => x.GetCustomAttribute<JsonPathAttribute>(inherit: true) != null);
            CanConvertCache.TryAdd(objectType, result);
            return result;
        }

        /// <inheritdoc />
        public override bool CanWrite => true;

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var contract = serializer.ContractResolver.ResolveContract(value.GetType()) as JsonObjectContract;

            if (contract == null)
            {
                throw new JsonSerializationException(
                    $"Unexpected contract type: {serializer.ContractResolver.ResolveContract(value.GetType()).GetType().Name}. " +
                    $"{nameof(JsonPathObjectConverter)} can only be used for object types.");
            }

            writer.WriteStartObject();

            foreach (var property in contract.Properties)
            {
                if (!property.Readable)
                    continue;

                if (property.Ignored && !property.HasAttribute<JsonPathAttribute>())
                    continue;

                if (property.ShouldSerialize?.Invoke(value) == false)
                    continue;

                writer.WritePropertyName(property.PropertyName);

                var propValue = property.ValueProvider.GetValue(value);

                if (propValue == null)
                {
                    writer.WriteNull();
                }
                else if (property.Converter?.CanWrite == true)
                {
                    property.Converter.WriteJson(writer, propValue, serializer);
                }
                else if (property.ItemConverter?.CanWrite == true && serializer.ContractResolver.ResolveContract(property.PropertyType) is JsonArrayContract)
                {
                    writer.WriteStartArray();
                    foreach (var item in (IEnumerable) propValue)
                    {
                        property.ItemConverter.WriteJson(writer, item, serializer);
                    }
                    writer.WriteEndArray();
                }
                else
                {
                    serializer.Serialize(writer, propValue);
                }
            }

            writer.WriteEndObject();
        }

        /// <inheritdoc />
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartObject)
                throw new JsonSerializationException($"Unexpected token: {reader.TokenType}. {nameof(JsonPathObjectConverter)} only supports objects.");

            var contract = serializer.ContractResolver.ResolveContract(objectType) as JsonObjectContract;
            if (contract == null)
            {
                throw new JsonSerializationException(
                    $"Unexpected contract type: {serializer.ContractResolver.ResolveContract(objectType).GetType().Name}. " +
                    $"{nameof(JsonPathObjectConverter)} can only be used for object types.");
            }

            var jobject = JObject.Load(reader);
            var value = existingValue ?? contract.DefaultCreator();

            foreach (var prop in GetJsonPathProperties(contract.Properties))
            {
                // Set property to ignore so it is ignored by default deserialization
                prop.JsonProperty.Ignored = true;
                PopulateProperty(value, prop, jobject, serializer);
            }

            using (var subReader = jobject.CreateReader())
            {
                serializer.Populate(subReader, value);
            }

            return value;
        }

        /// <summary>
        /// Creates a list of <see cref="JsonPathProperty"/> from the <paramref name="properties"/>
        /// that has been attributed with the <see cref="JsonPathAttribute"/>.
        /// </summary>
        /// <param name="properties"></param>
        protected virtual List<JsonPathProperty> GetJsonPathProperties(JsonPropertyCollection properties)
        {
            var jsonPathProperties = from jsonProperty in properties
                                     let attribute = jsonProperty.GetAttribute<JsonPathAttribute>()
                                     where attribute != null
                                     select new JsonPathProperty
                                     {
                                         JsonProperty = jsonProperty,
                                         Expression = attribute.Expression
                                     };

            return jsonPathProperties.ToList();
        }

        /// <summary>
        /// Populates the property on the <paramref name="target"/> object using the JSONPath expression from the <paramref name="pathProperty"/>.
        /// </summary>
        /// <param name="target">The target object being mapped.</param>
        /// <param name="pathProperty">The <see cref="JsonPathProperty"/> containing info about the property to be populated.</param>
        /// <param name="rootObject">The <see cref="JObject"/> that is being deserialized.</param>
        /// <param name="serializer">The calling serializer.</param>
        protected virtual void PopulateProperty(object target, JsonPathProperty pathProperty, JObject rootObject, JsonSerializer serializer)
        {
            var jsonProperty = pathProperty.JsonProperty;

            var propertyContract = serializer.ContractResolver.ResolveContract(jsonProperty.PropertyType);
            var token = GetTokenFromExpression(pathProperty.Expression, rootObject, propertyContract);

            if (token == null)
            {
                SetPropertyValue(target, null, jsonProperty, serializer);
                return;
            }

            if (jsonProperty.Converter?.CanRead == true)
            {
                SetPropertyUsingConverter(target, jsonProperty, token, serializer);
            }
            else if (propertyContract is JsonArrayContract && jsonProperty.ItemConverter?.CanRead == true)
            {
                SetPropertyUsingItemConverter(target, jsonProperty, (JArray) token, propertyContract, serializer);
            }
            else
            {
                SetPropertyWithoutConverter(target, jsonProperty, token, serializer);
            }
        }

        /// <summary>
        /// Gets a <see cref="JToken"/> from the <paramref name="rootJObject"/> by evaluating the <paramref name="expression"/>.
        /// </summary>
        /// <param name="expression">The JSONPath expression to evaluate.</param>
        /// <param name="rootJObject">The <see cref="JObject"/> that is being deserialized.</param>
        /// <param name="propertyContract">The contract for the property.</param>
        /// <returns></returns>
        protected virtual JToken GetTokenFromExpression(string expression, JObject rootJObject, JsonContract propertyContract)
        {
            var tokens = rootJObject.SelectTokens(expression).ToList();

            if (tokens.Count == 0)
                return null;

            return tokens.Count == 1 ? tokens.First() : JArray.FromObject(tokens);
        }

        /// <summary>
        /// Sets the property on the <paramref name="target"/> to the specified <paramref name="value"/>,
        /// taking <see cref="DefaultValueHandling"/> and <see cref="NullValueHandling"/> into consideration.
        /// </summary>
        /// <param name="target">The target object to set the property on.</param>
        /// <param name="value">The property value to set.</param>
        /// <param name="property"></param>
        /// <param name="serializer"></param>
        protected virtual void SetPropertyValue(object target, object value, JsonProperty property, JsonSerializer serializer)
        {
            if (value == null)
            {
                if (GetDefaultValueHandling(property, serializer).HasFlag(DefaultValueHandling.Populate))
                {
                    // Value is null and default value should be used, so use default value
                    value = property.DefaultValue;
                }

                if (value == null)
                {
                    if (GetNullValueHandling(property, serializer).HasFlag(NullValueHandling.Ignore))
                    {
                        // Value is null and null values should be ignored, skip
                        return;
                    }

                    // Type is value type and not nullable, create default value instead
                    var isValueType = property.PropertyType.GetTypeInfo().IsValueType;
                    if (isValueType && Nullable.GetUnderlyingType(property.PropertyType) == null)
                        value = Activator.CreateInstance(property.PropertyType);
                }
            }

            property.ValueProvider.SetValue(target, value);
        }

        /// <summary>
        /// Gets the <see cref="DefaultValueHandling"/> from either the property or the serializer.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="serializer"></param>
        protected virtual DefaultValueHandling GetDefaultValueHandling(JsonProperty property, JsonSerializer serializer)
        {
            return property.DefaultValueHandling ?? serializer.DefaultValueHandling;
        }

        /// <summary>
        /// Gets the <see cref="NullValueHandling"/> from either the property or the serializer.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="serializer"></param>
        protected virtual NullValueHandling GetNullValueHandling(JsonProperty property, JsonSerializer serializer)
        {
            return property.NullValueHandling ?? serializer.NullValueHandling;
        }

        /// <summary>
        /// Sets the property on the <paramref name="target"/> object using the calling serializer.
        /// </summary>
        /// <param name="target">The target object being mapped.</param>
        /// <param name="jsonProperty">The property to populate.</param>
        /// <param name="token">The token containing the value to convert.</param>
        /// <param name="serializer">The calling serializer.</param>
        protected virtual void SetPropertyWithoutConverter(object target, JsonProperty jsonProperty, JToken token, JsonSerializer serializer)
        {
            using (var reader = token.CreateReader())
            {
                var value = serializer.Deserialize(reader, jsonProperty.PropertyType);
                SetPropertyValue(target, value, jsonProperty, serializer);
            }
        }

        /// <summary>
        /// Sets the property on the <paramref name="target"/> object using the <see cref="JsonConverter"/> specified on the <paramref name="jsonProperty"/>.
        /// </summary>
        /// <param name="target">The target object being mapped.</param>
        /// <param name="jsonProperty">The property to populate.</param>
        /// <param name="token">The token containing the value to convert.</param>
        /// <param name="serializer">The calling serializer.</param>
        protected virtual void SetPropertyUsingConverter(object target, JsonProperty jsonProperty, JToken token, JsonSerializer serializer)
        {
            using (var reader = token.CreateReader())
            {
                // Advance reader so it is in the usual state for the converter
                reader.Read();
                var existingValue = jsonProperty.Readable ? jsonProperty.ValueProvider.GetValue(target) : null;
                var value = jsonProperty.Converter.ReadJson(reader, jsonProperty.PropertyType, existingValue, serializer);
                SetPropertyValue(target, value, jsonProperty, serializer);
            }
        }

        /// <summary>
        /// Sets the list-type property on the <paramref name="target"/> object using the <see cref="JsonConverter"/>
        /// for collection items specified on the <paramref name="jsonProperty"/> (<see cref="JsonProperty.ItemConverter"/>).
        /// </summary>
        /// <param name="target">The target object being mapped.</param>
        /// <param name="jsonProperty">The property to populate.</param>
        /// <param name="token">The array containing the values to convert.</param>
        /// <param name="propertyContract"></param>
        /// <param name="serializer"></param>
        protected virtual void SetPropertyUsingItemConverter(object target, JsonProperty jsonProperty, JArray token, JsonContract propertyContract, JsonSerializer serializer)
        {
            var list = CreateNewList(propertyContract);

            if (list == null)
            {
                throw new JsonSerializationException(
                    $"Unsupported property type {propertyContract.CreatedType.Name} when using ItemConverter with {nameof(JsonPathAttribute)}.");
            }

            using (var reader = token.CreateReader())
            {
                // Advance reader to start of array
                reader.Read();

                while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                {
                    list.Add(jsonProperty.ItemConverter.ReadJson(reader, jsonProperty.PropertyType, null, serializer));
                }

                if (propertyContract.CreatedType.IsArray)
                {
                    var array = (Array) Activator.CreateInstance(propertyContract.CreatedType, list.Count);
                    list.CopyTo(array, 0);
                    list = array;
                }

                SetPropertyValue(target, list, jsonProperty, serializer);
            }
        }

        /// <summary>
        /// Creates a new list according to the <paramref name="contract"/>.
        /// </summary>
        /// <param name="contract"></param>
        protected virtual IList CreateNewList(JsonContract contract)
        {
            if (contract.DefaultCreator?.Invoke() is IList list)
                return list;

            var genericType = GetGenericTypeForList(contract.CreatedType);
            var listType = typeof(List<>).MakeGenericType(genericType);
            return Activator.CreateInstance(listType) as IList;
        }

        /// <summary>
        /// Gets the generic type argument of the <paramref name="createdType"/> to be used with <see cref="List{T}"/>.
        /// </summary>
        /// <param name="createdType"></param>
        protected virtual Type GetGenericTypeForList(Type createdType)
        {
            if (createdType.IsArray)
                return createdType.GetElementType();

            return createdType.GenericTypeArguments.FirstOrDefault() ??
                   typeof(object);
        }
    }
}
