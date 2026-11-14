using System;
using System.Collections.Generic;

//IA2-LINQ
/// <summary>LINQ-style extensions: Minimum, Maximum, NotOfType and EmptyIfNull.</summary>
public static class LinqExtension
{
    /// <summary>Returns the element with the lowest GetValue, or default for an empty col. O(n).</summary>
    public static T Minimum<T>(this IEnumerable<T> col, Func<T, float> GetValue)
    {
        float minimum = float.MaxValue; T returnItem = default;

        foreach (var item in col)
        {
            float newValue = GetValue(item);

            if (minimum > newValue)
            {
                minimum = newValue;
                returnItem = item;
            }
        }
        return returnItem;
    }

    /// <summary>Returns the element with the highest GetValue, or default for an empty col. O(n).</summary>
    public static T Maximum<T>(this IEnumerable<T> col, Func<T, float> GetValue)
    {
        float maximum = float.MinValue; T returnItem = default;

        foreach (var item in col)
        {
            float newValue = GetValue(item);

            if (newValue > maximum)
            {
                maximum = newValue;
                returnItem = item;
            }
        }
        return returnItem;
    }

    /// <summary>Returns the elements whose exact type is different from K.</summary>
    public static IEnumerable<T> NotOfType<T, K>(this IEnumerable<T> col)
    {
        foreach (var item in col)
        {
            if (item.GetType() != typeof(K))
                yield return item;
        }
    }

    /// <summary>Adds an empty array for each value of the enum K that dic is missing and returns dic.</summary>
    /// <exception cref="Exception">K is a type other than an enum.</exception>
    public static Dictionary<K, V[]> EmptyIfNull<K, V>(this Dictionary<K, V[]> dic)
    {
        if (!typeof(K).IsEnum)
            throw new Exception("Se paso un tipo que no era Enum");

        foreach (K key in Enum.GetValues(typeof(K)))
        {
            if (dic.ContainsKey(key)) continue;

            dic.Add(key, new V[0]);
        }
        return dic;
    }
}
