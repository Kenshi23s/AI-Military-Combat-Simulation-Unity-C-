using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Generic object pool backed by a stack.
/// Get turns an object on and Return turns it off.
/// </summary>
/// <typeparam name="T">Type of the pooled objects.</typeparam>
public class PoolObject<T>
{
    /// <summary>The objects available in the pool.</summary>
    [SerializeField] public Stack<T> pool = new Stack<T>();

    private Action<T> turnOn;
    private Action<T> turnOff;
    private Func<T> build;

    private int prewarm = 0;

    /// <summary>Creates an empty pool. Intialize prepares it.</summary>
    public PoolObject() { }

    /// <summary>Sets the turn on, turn off and build actions and creates prewarm objects.</summary>
    public void Intialize(Action<T> _turnOn, Action<T> _turnOff, Func<T> _build, int prewarm = 5)
    {
        this.turnOn = _turnOn;
        this.turnOff = _turnOff;
        this.build = _build;
        this.prewarm = prewarm;

        AddMore();
    }

    /// <summary>Takes an object from the pool and turns it on. An empty pool creates prewarm more objects first.</summary>
    public T Get()
    {
        if (pool.Count <= 0) AddMore();
        var obj = pool.Pop();
        turnOn(obj);
        return obj;
    }

    /// <summary>Puts obj back in the pool and turns it off.</summary>
    public void Return(T obj)
    {
        pool.Push(obj);
        turnOff(obj);
    }

    // Creates prewarm objects, turned off, and pushes them to the pool.
    private void AddMore()
    {
        // At least one object, so Get always finds one after AddMore.
        int amount = Mathf.Max(prewarm, 1);
        for (int i = 0; i < amount; i++)
        {
            var obj = build.Invoke();
            pool.Push(obj);
            turnOff(obj);
        }
    }
}
