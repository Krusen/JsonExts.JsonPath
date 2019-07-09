using System;
using System.Collections.Generic;
using System.Reflection;

#if NETSTANDARD1_3

namespace JsonExts.JsonPath.Extensions
{
    internal static class ReflectionExtensions
    {
        public static IEnumerable<PropertyInfo> GetProperties(this Type type)
        {
            return type.GetTypeInfo().DeclaredProperties;
        }
    }
}

#endif