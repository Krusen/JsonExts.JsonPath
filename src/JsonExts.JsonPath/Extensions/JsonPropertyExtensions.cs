using System.Linq;
using Newtonsoft.Json.Serialization;

namespace JsonExts.JsonPath.Extensions
{
    internal static class JsonPropertyExtensions
    {
        public static T GetAttribute<T>(this JsonProperty property) where T : class
        {
            return property.AttributeProvider.GetAttributes(typeof(T), true).FirstOrDefault() as T;
        }

        public static bool HasAttribute<T>(this JsonProperty property) where T : class
        {
            return property.AttributeProvider.GetAttributes(typeof(T), true).Any();
        }
    }
}
