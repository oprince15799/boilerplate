using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Core.Utilities
{
    /// <summary>
    /// Dictionary extensions.
    /// </summary>
    public static class DictionaryExtensions
    {
        /// <summary>
        /// A Dictionary&lt;TKey,TValue&gt; extension method that attempts to
        /// remove a key from the dictionary.
        /// </summary>
        /// <typeparam name="TKey">Type of the key.</typeparam>
        /// <typeparam name="TValue">Type of the value.</typeparam>
        /// <param name="dictionary">The dictionary to act on.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">[out] The value.</param>
        /// <returns>true if it succeeds, false if it fails.</returns>
        /// <remarks>https://github.com/zzzprojects/Eval-SQL.NET/blob/master/src/Z.Expressions.SqlServer.Eval/Extensions/Dictionary%602/TryRemove.cs</remarks>
        public static bool TryRemove<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, [MaybeNullWhen(false)] out TValue value)
            where TKey : notnull
        {
            var isRemoved = dictionary.TryGetValue(key, out value);
            if (isRemoved)
            {
                dictionary.Remove(key);
            }

            return isRemoved;
        }

        /// <summary>
        ///     A Dictionary&lt;TKey,TValue&gt; extension method that adds or updates value for the
        ///     specified key.
        /// </summary>
        /// <typeparam name="TKey">Type of the key.</typeparam>
        /// <typeparam name="TValue">Type of the value.</typeparam>
        /// <param name="dictionary">The dictionary to act on.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="updateValueFactory">The update value factory.</param>
        /// <returns>A TValue.</returns>
        /// <remarks>https://github.com/zzzprojects/Eval-SQL.NET/blob/master/src/Z.Expressions.SqlServer.Eval/Extensions/Dictionary%602/AddOrUpdate.cs</remarks>
        public static TValue AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value, Func<TKey, TValue, TValue> updateValueFactory)
        {
            TValue? oldValue;

            if (dictionary.TryGetValue(key, out oldValue))
            {
                // TRY / CATCH should be done here, but this application does not require it
                value = updateValueFactory(key, oldValue);
                dictionary[key] = value;
            }
            else
            {
                dictionary.Add(key, value);
            }

            return value;
        }

        /// <summary>
        /// A Dictionary&lt;TKey,TValue&gt; extension method that adds or
        /// updates value for the specified key.
        /// </summary>
        /// <typeparam name="TKey">Type of the key.</typeparam>
        /// <typeparam name="TValue">Type of the value.</typeparam>
        /// <param name="dictionary">The dictionary to act on.</param>
        /// <param name="key">The key.</param>
        /// <param name="addValueFactory">The add value factory.</param>
        /// <param name="updateValueFactory">The update value factory.</param>
        /// <returns>A TValue.</returns>
        /// <remarks>https://github.com/zzzprojects/Eval-SQL.NET/blob/master/src/Z.Expressions.SqlServer.Eval/Extensions/Dictionary%602/AddOrUpdate.cs</remarks>
        public static TValue AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
        {
            TValue value;
            TValue? oldValue;
            if (dictionary.TryGetValue(key, out oldValue))
            {
                value = updateValueFactory(key, oldValue);
                dictionary[key] = value;
            }
            else
            {
                value = addValueFactory(key);
                dictionary.Add(key, value);
            }

            return value;
        }

        /// <summary>
        /// A Dictionary&lt;TKey,TValue&gt; extension method that attempts to
        /// add a value in the dictionary for the specified key.
        /// </summary>
        /// <typeparam name="TKey">Type of the key.</typeparam>
        /// <typeparam name="TValue">Type of the value.</typeparam>
        /// <param name="dictionary">The dictionary to act on.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>true if it succeeds, false if it fails.</returns>
        /// <remarks>https://github.com/zzzprojects/Eval-SQL.NET/blob/master/src/Z.Expressions.SqlServer.Eval/Extensions/Dictionary%602/TryAdd.cs</remarks>
        public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (!dictionary.ContainsKey(key))
            {
                dictionary.Add(key, value);
            }

            return true;
        }

        /// <summary>
        /// Gets the value, if available, or <paramref name="ifNotFound"/>.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="self">The dictionary to search.</param>
        /// <param name="key">The item key.</param>
        /// <param name="ifNotFound">The fallback value.</param>
        /// <returns>
        /// Returns the item in <paramref name="self"/> that matches <paramref name="key"/>,
        /// falling back to the value of <paramref name="ifNotFound"/> if the item is unavailable.
        /// </returns>
        public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> self, TKey key, TValue defaultValue = default)
        {
            TValue? val;
            return self.TryGetValue(key, out val) ? val : defaultValue;
        }

        /// <summary>
        /// Thread-safe way to gets or add the specified dictionary
        /// key and value pair.
        /// </summary>
        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> self, TKey key, Func<TKey, TValue> factory)
        {
            TValue? tValue;
            TValue? tValue1;

            lock (self)
            {
                if (!self.TryGetValue(key, out tValue))
                {
                    tValue = factory(key);
                    self[key] = tValue;
                }

                tValue1 = tValue;
            }

            return tValue1;
        }

        /// <summary>
        /// Tries to obtain the given key, otherwise returns the default value.
        /// </summary>
        /// <typeparam name="T">The struct type.</typeparam>
        /// <param name="values">The dictionary for the lookup.</param>
        /// <param name="key">The key to look for.</param>
        /// <returns>A nullable struct type.</returns>
        /// <remarks>https://github.com/AngleSharp/AngleSharp/blob/master/src/AngleSharp/Common/ObjectExtensions.cs#L106</remarks>
        public static T? TryGet<T>(this IDictionary<string, object> values, string key)
            where T : struct
        {
            if (values.TryGetValue(key, out var value) && value is T result)
            {
                return result;
            }

            return null;
        }

        /// <summary>
        /// Tries to obtain the given key, otherwise returns null.
        /// </summary>
        /// <param name="values">The dictionary for the lookup.</param>
        /// <param name="key">The key to look for.</param>
        /// <returns>An object instance or null.</returns>
        /// <remarks>https://github.com/AngleSharp/AngleSharp/blob/master/src/AngleSharp/Common/ObjectExtensions.cs#L123</remarks>
        public static object? TryGet(this IDictionary<string, object> values, string key)
        {
            values.TryGetValue(key, out var value);
            return value;
        }
    }
}
