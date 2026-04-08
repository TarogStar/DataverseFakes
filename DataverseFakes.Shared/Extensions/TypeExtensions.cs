using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace FakeXrmEasy.Extensions
{
    /// <summary>
    /// Utility extensions that make it easier to reason about CRM-specific CLR types.
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// Determines whether the provided type represents an option set value or enum (including nullable variants).
        /// </summary>
        /// <param name="t">The type to inspect.</param>
        /// <returns><c>true</c> if the type is an option set or enum; otherwise <c>false</c>.</returns>
        public static bool IsOptionSet(this Type t)
        {
            var nullableType = Nullable.GetUnderlyingType(t);
            return t == typeof(OptionSetValue)
                   || t.IsEnum
                   || nullableType != null && nullableType.IsEnum;
        }

#if FAKE_XRM_EASY_9
        /// <summary>
        /// Indicates whether the type is an <see cref="OptionSetValueCollection"/>, allowing multi-select handling.
        /// </summary>
        /// <param name="t">The type to inspect.</param>
        /// <returns><c>true</c> when the type is <see cref="OptionSetValueCollection"/>.</returns>
        public static bool IsOptionSetValueCollection(this Type t)
        {
            var nullableType = Nullable.GetUnderlyingType(t);
            return t == typeof(OptionSetValueCollection);
        }
#endif

        /// <summary>
        /// Determines whether the provided type is a <see cref="DateTime"/> or nullable <see cref="DateTime"/>.
        /// </summary>
        /// <param name="t">The type to inspect.</param>
        /// <returns><c>true</c> when the type represents a date.</returns>
        public static bool IsDateTime(this Type t)
        {
            var nullableType = Nullable.GetUnderlyingType(t);
            return t == typeof(DateTime)
                   || nullableType != null && nullableType == typeof(DateTime);
        }

        /// <summary>
        /// Determines whether the provided type is a nullable enum (i.e. <c>Nullable&lt;TEnum&gt;</c>).
        /// </summary>
        /// <param name="t">The type to inspect.</param>
        /// <returns><c>true</c> if the type is a nullable enum; otherwise <c>false</c>.</returns>
        public static bool IsNullableEnum(this Type t)
        {
            return
                t.IsGenericType
                && t.GetGenericTypeDefinition() == typeof(Nullable<>)
                && t.GetGenericArguments()[0].IsEnum;
        }
    }
}