using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class FList<T> : IEnumerable<T>
{
	readonly IEnumerable<T> collection;

    /// <summary>
    /// Use FList.Create instead of this constructor directly
    /// </summary>
    public FList() {
		collection = new T[0];
	}

    /// <summary>
    /// Use FList.Create instead of this constructor directly
    /// </summary>
    /// <param name="singleValue"></param>
    public FList(T singleValue) {
		collection = new T[1] { singleValue };
	}

    /// <summary>
    /// Use FList.Cast instead of this constructor directly
    /// </summary>
    /// <param name="collection"></param>
    public FList(IEnumerable<T> collection) {
		this.collection = collection;
	}


	public static FList<T> operator+(FList<T> lhs, FList<T> rhs) {
		return FList.Cast(lhs.collection.Concat(rhs.collection));
	}

	public static FList<T> operator+(FList<T> lhs, IEnumerable<T> rhs) {
		return FList.Cast(lhs.collection.Concat(rhs));
	}

	public static FList<T> operator+(IEnumerable<T> lhs, FList<T> rhs) {
		return FList.Cast(FList.Cast(lhs).collection.Concat(rhs));
	}

	public static FList<T> operator+(FList<T> lhs, T rhs) {
		return FList.Cast(lhs.Concat(Enumerable.Empty<T>().DefaultIfEmpty(rhs)));
	}

	public static FList<T> operator+(T lhs, FList<T> rhs) {
		return FList.Cast(Enumerable.Empty<T>().DefaultIfEmpty(lhs).Concat(rhs));
	}
		
	public IEnumerator<T> GetEnumerator() {
		foreach(var element in collection)
			yield return element;
	}
		
	IEnumerator IEnumerable.GetEnumerator() {
		return GetEnumerator();
	}
}

public static class FList {
	public static FList<T> ToFList<T>(this IEnumerable<T> lhs) {
		return Cast(lhs);
	}

	public static FList<T> Create<T>() {
		return new FList<T>();
	}

	public static FList<T> Create<T>(T singleValue) {
		return new FList<T>(singleValue);
	}
		
	public static FList<T> Cast<T>(IEnumerable<T> collection) {
		return new FList<T>(collection);
	}
}
