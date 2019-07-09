using Newtonsoft.Json.Serialization;

namespace JsonExts.JsonPath
{
    /// <summary>
    /// Class containing info about the <see cref="JsonProperty"/> and associated
    /// JSONPath expression coming from the <see cref="JsonPathAttribute"/>.
    /// </summary>
    public class JsonPathProperty
    {
        /// <summary>
        /// The <see cref="JsonProperty"/> associated with the same property as the <see cref="JsonPathAttribute"/>.
        /// </summary>
        public JsonProperty JsonProperty { get; set; }

        /// <summary>
        /// The JSONPath expression to be evaluated for the property.
        /// </summary>
        public string Expression { get; set; }
    }
}
