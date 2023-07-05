using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public static class LinqExtension
{
    public static T Minimum<T>(this IEnumerable<T> col,Func<T,float> GetValue)
    {
        float minimum = float.MaxValue;
        T returnItem = default;
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

    public static T Maximum<T>(this IEnumerable<T> col, Func<T,float> GetValue)
    {
        float maximum = float.MinValue;
        T returnItem = default;
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

    public static IEnumerable<T> NotOfType<T,K>(this IEnumerable<T> col)
    {
        foreach (var item in col)
        {
            if (!(item.GetType()==typeof(K)))           
                yield return item;            
        }
    }

    //public static IEnumerable<T> GetRandomAmount<T>(this IEnumerable<T> col,int quantity = 1)
    //{
    //    HashSet<T> list = new HashSet<T>();
    //    while (quantity>0)
    //    {
    //        yield return col.Skip(Random.Range(0, col.Count())).Take(1);
    //        quantity--;
    //    }
    //}


}
