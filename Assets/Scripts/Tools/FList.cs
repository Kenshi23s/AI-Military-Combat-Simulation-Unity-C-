using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Immutable sequence with + operators that return a new FList.
/// The elements are evaluated lazily.
/// </summary>
/// <typeparam name="T">Type of the elements.</typeparam>
public class FList<T> : IEnumerable<T>
{
    private readonly IEnumerable<T> collection;

    /// <summary>Creates an empty FList.</summary>
    public FList()
    {
        collection = new T[0];
    }

    /// <summary>Creates an FList with singleValue.</summary>
    public FList(T singleValue)
    {
        collection = new T[1] { singleValue };
    }

    /// <summary>Creates an FList over collection.</summary>
    public FList(IEnumerable<T> collection)
    {
        this.collection = collection;
    }

    /// <summary>Returns the elements of lhs followed by the elements of rhs.</summary>
    public static FList<T> operator +(FList<T> lhs, FList<T> rhs)
    {
        return FList.Cast(lhs.collection.Concat(rhs.collection));
    }

    /// <summary>Returns the elements of lhs followed by the elements of rhs.</summary>
    public static FList<T> operator +(FList<T> lhs, IEnumerable<T> rhs)
    {
        return FList.Cast(lhs.collection.Concat(rhs));
    }

    /// <summary>Returns the elements of lhs followed by the elements of rhs.</summary>
    public static FList<T> operator +(IEnumerable<T> lhs, FList<T> rhs)
    {
        return FList.Cast(FList.Cast(lhs).collection.Concat(rhs));
    }

    /// <summary>Returns the elements of lhs followed by rhs.</summary>
    public static FList<T> operator +(FList<T> lhs, T rhs)
    {
        return FList.Cast(lhs.Concat(Enumerable.Empty<T>().DefaultIfEmpty(rhs)));
    }

    /// <summary>Returns lhs followed by the elements of rhs.</summary>
    public static FList<T> operator +(T lhs, FList<T> rhs)
    {
        return FList.Cast(Enumerable.Empty<T>().DefaultIfEmpty(lhs).Concat(rhs));
    }

    /// <summary>Returns an enumerator over the elements.</summary>
    public IEnumerator<T> GetEnumerator()
    {
        foreach (var element in collection)
            yield return element;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

/// <summary>Factory methods for FList with type inference.</summary>
public static class FList
{
    /// <summary>Returns an FList over lhs.</summary>
    public static FList<T> ToFList<T>(this IEnumerable<T> lhs)
    {
        return Cast(lhs);
    }

    /// <summary>Returns an empty FList.</summary>
    public static FList<T> Create<T>()
    {
        return new FList<T>();
    }

    /// <summary>Returns an FList with singleValue.</summary>
    public static FList<T> Create<T>(T singleValue)
    {
        return new FList<T>(singleValue);
    }

    /// <summary>Returns an FList over collection.</summary>
    public static FList<T> Cast<T>(IEnumerable<T> collection)
    {
        return new FList<T>(collection);
    }
}
