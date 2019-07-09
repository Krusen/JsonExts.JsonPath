using System;

namespace JsonExts.JsonPath
{
    /// <summary>
    /// Instructs the <see cref="JsonPathObjectConverter"/> to deserialize the property using the specified JSONPath expression.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class JsonPathAttribute : Attribute
    {
        /// <summary>
        /// The JSONPath expression to evaluate for the marked property.
        /// </summary>
        public string Expression { get; }

        /// <param name="expression">The JSONPath expression to evaluate for this property.</param>
        public JsonPathAttribute(string expression)
        {
            Expression = expression;
        }
    }
}
