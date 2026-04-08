using System;
using System.Collections.Generic;
using System.Reflection;

namespace FakeXrmEasy.Extensions
{
    /// <summary>
    /// Provides extension methods for deep cloning objects using reflection.
    /// Adapted from: https://github.com/Burtsev-Alexey/net-object-deep-copy/blob/master/ObjectExtensions.cs
    /// </summary>
    public static class ObjectExtensions
    {
        private static readonly MethodInfo CloneMethod = typeof(Object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// Determines whether a type is considered primitive for cloning purposes.
        /// Strings are treated as primitive (immutable), along with value types that are also primitives.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns><c>true</c> if the type is primitive or string; otherwise <c>false</c>.</returns>
        public static bool IsPrimitive(this Type type)
        {
            if (type == typeof(String)) return true;
            return (type.IsValueType & type.IsPrimitive);
        }

        private static FieldInfo GetFieldInfo(Type type, string fieldName)
        {
            FieldInfo fieldInfo;
            do
            {
                fieldInfo = type.GetField(fieldName,
                       BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                type = type.BaseType;
            }
            while (fieldInfo == null && type != null);
            return fieldInfo;
        }

        private static PropertyInfo GetPropertyInfo(Type type, string propertyName)
        {
            PropertyInfo propertyInfo;
            do
            {
                propertyInfo = type.GetProperty(propertyName,
                       BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                type = type.BaseType;
            }
            while (propertyInfo == null && type != null);
            return propertyInfo;
        }

        /// <summary>
        /// Gets the value of a field from an object using reflection, traversing the type hierarchy if necessary.
        /// </summary>
        /// <param name="obj">The object from which to retrieve the field value.</param>
        /// <param name="fieldName">The name of the field to retrieve.</param>
        /// <returns>The value of the field.</returns>
        /// <exception cref="ArgumentNullException">Thrown when obj is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the field is not found in the type hierarchy.</exception>
        public static object GetFieldValue(this object obj, string fieldName)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            Type objType = obj.GetType();
            FieldInfo fieldInfo = GetFieldInfo(objType, fieldName);
            if (fieldInfo == null)
                throw new ArgumentOutOfRangeException("fieldName",
                  string.Format("Couldn't find field {0} in type {1}", fieldName, objType.FullName));
            return fieldInfo.GetValue(obj);
        }

        /// <summary>
        /// Sets the value of a field or property on an object using reflection.
        /// Attempts to find a field first, then a property, then a backing field pattern (_fieldName).
        /// </summary>
        /// <param name="obj">The object on which to set the field value.</param>
        /// <param name="fieldName">The name of the field or property to set.</param>
        /// <param name="val">The value to assign.</param>
        /// <exception cref="ArgumentNullException">Thrown when obj is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when neither field nor property is found.</exception>
        public static void SetFieldValue(this object obj, string fieldName, object val)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            Type objType = obj.GetType();

            // First try to find as a field
            FieldInfo fieldInfo = GetFieldInfo(objType, fieldName);
            if (fieldInfo != null)
            {
                fieldInfo.SetValue(obj, val);
                return;
            }

            // If not found as a field, try as a property (for SDK compatibility)
            PropertyInfo propertyInfo = GetPropertyInfo(objType, fieldName);
            if (propertyInfo != null && propertyInfo.CanWrite)
            {
                propertyInfo.SetValue(obj, val);
                return;
            }

            // If still not found, try the backing field pattern for properties (e.g., _keys for Keys)
            if (char.IsUpper(fieldName[0]))
            {
                string backingFieldName = "_" + char.ToLower(fieldName[0]) + fieldName.Substring(1);
                FieldInfo backingFieldInfo = GetFieldInfo(objType, backingFieldName);
                if (backingFieldInfo != null)
                {
                    backingFieldInfo.SetValue(obj, val);
                    return;
                }
            }

            throw new ArgumentOutOfRangeException("fieldName",
              string.Format("Couldn't find field or property {0} in type {1}", fieldName, objType.FullName));
        }


        /// <summary>
        /// Creates a deep copy of an object, cloning all fields recursively while handling circular references.
        /// </summary>
        /// <param name="originalObject">The object to clone.</param>
        /// <returns>A deep copy of the original object, or null if the original is null.</returns>
        public static Object Copy(this Object originalObject)
        {
            return InternalCopy(originalObject, new Dictionary<Object, Object>(new ReferenceEqualityComparer()));
        }

        private static Object InternalCopy(Object originalObject, IDictionary<Object, Object> visited)
        {
            if (originalObject == null) return null;
            var typeToReflect = originalObject.GetType();
            if (IsPrimitive(typeToReflect)) return originalObject;
            if (visited.ContainsKey(originalObject)) return visited[originalObject];
            if (typeof(Delegate).IsAssignableFrom(typeToReflect)) return null;
            var cloneObject = CloneMethod.Invoke(originalObject, null);
            if (typeToReflect.IsArray)
            {
                var arrayType = typeToReflect.GetElementType();
                if (IsPrimitive(arrayType) == false)
                {
                    Array clonedArray = (Array)cloneObject;
                    clonedArray.ForEach((array, indices) => array.SetValue(InternalCopy(clonedArray.GetValue(indices), visited), indices));
                }
            }
            visited.Add(originalObject, cloneObject);
            CopyFields(originalObject, visited, cloneObject, typeToReflect);
            RecursiveCopyBaseTypePrivateFields(originalObject, visited, cloneObject, typeToReflect);
            return cloneObject;
        }

        private static void RecursiveCopyBaseTypePrivateFields(object originalObject, IDictionary<object, object> visited, object cloneObject, Type typeToReflect)
        {
            if (typeToReflect.BaseType != null)
            {
                RecursiveCopyBaseTypePrivateFields(originalObject, visited, cloneObject, typeToReflect.BaseType);
                CopyFields(originalObject, visited, cloneObject, typeToReflect.BaseType, BindingFlags.Instance | BindingFlags.NonPublic, info => info.IsPrivate);
            }
        }

        private static void CopyFields(object originalObject, IDictionary<object, object> visited, object cloneObject, Type typeToReflect, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy, Func<FieldInfo, bool> filter = null)
        {
            foreach (FieldInfo fieldInfo in typeToReflect.GetFields(bindingFlags))
            {
                if (filter != null && filter(fieldInfo) == false) continue;
                if (IsPrimitive(fieldInfo.FieldType)) continue;
                var originalFieldValue = fieldInfo.GetValue(originalObject);
                var clonedFieldValue = InternalCopy(originalFieldValue, visited);
                fieldInfo.SetValue(cloneObject, clonedFieldValue);
            }
        }

        /// <summary>
        /// Creates a deep copy of an object with strongly-typed return value.
        /// </summary>
        /// <typeparam name="T">The type of the object to clone.</typeparam>
        /// <param name="original">The object to clone.</param>
        /// <returns>A deep copy of the original object cast to type T.</returns>
        public static T Copy<T>(this T original)
        {
            return (T)Copy((Object)original);
        }
    }

    /// <summary>
    /// An equality comparer that compares objects by reference identity rather than value equality.
    /// Used during deep cloning to detect and handle circular references.
    /// </summary>
    public class ReferenceEqualityComparer : EqualityComparer<Object>
    {
        /// <summary>
        /// Determines whether two objects are the same instance.
        /// </summary>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The second object to compare.</param>
        /// <returns><c>true</c> if both references point to the same object; otherwise <c>false</c>.</returns>
        public override bool Equals(object x, object y)
        {
            return ReferenceEquals(x, y);
        }

        /// <summary>
        /// Returns the hash code for an object.
        /// </summary>
        /// <param name="obj">The object for which to get the hash code.</param>
        /// <returns>The hash code of the object, or 0 if the object is null.</returns>
        public override int GetHashCode(object obj)
        {
            if (obj == null) return 0;
            return obj.GetHashCode();
        }
    }

    /// <summary>
    /// Provides extension methods for working with arrays, particularly multi-dimensional arrays.
    /// </summary>
    public static class ArrayExtensions
    {
        /// <summary>
        /// Iterates over all elements in a multi-dimensional array, invoking an action for each element.
        /// </summary>
        /// <param name="array">The array to iterate over.</param>
        /// <param name="action">The action to invoke for each element, receiving the array and the current indices.</param>
        public static void ForEach(this Array array, Action<Array, int[]> action)
        {
            if (array.LongLength == 0) return;
            ArrayTraverse walker = new ArrayTraverse(array);
            do action(array, walker.Position);
            while (walker.Step());
        }
    }

    internal class ArrayTraverse
    {
        public int[] Position;
        private int[] maxLengths;

        public ArrayTraverse(Array array)
        {
            maxLengths = new int[array.Rank];
            for (int i = 0; i < array.Rank; ++i)
            {
                maxLengths[i] = array.GetLength(i) - 1;
            }
            Position = new int[array.Rank];
        }

        public bool Step()
        {
            for (int i = 0; i < Position.Length; ++i)
            {
                if (Position[i] < maxLengths[i])
                {
                    Position[i]++;
                    for (int j = 0; j < i; j++)
                    {
                        Position[j] = 0;
                    }
                    return true;
                }
            }
            return false;
        }
    }
}