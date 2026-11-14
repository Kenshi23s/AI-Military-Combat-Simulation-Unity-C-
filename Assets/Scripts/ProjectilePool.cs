using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Pool of Misile instances, prewarmed on Awake.</summary>
public class ProjectilePool : MonoSingleton<ProjectilePool>
{
    [SerializeField] private Misile _prefab;
    //devuelve la key a la pool
    [SerializeField] private int _prewarmMisiles = 20;
    private PoolObject<Misile> pool = new PoolObject<Misile>();

    protected override void SingletonAwake()
    {

        Action<Misile> turnOn = (x) => x.gameObject.SetActive(true);

        Action<Misile> turnOff = (x) => x.gameObject.SetActive(false);

        Func<Misile> build = () =>
        {
            Misile misileClone = GameObject.Instantiate(_prefab);

            Action<Misile> ReturnToPool = (x) => pool.Return(x);

            misileClone.PoolObjectInitialize(ReturnToPool);

            return misileClone;
        };

        pool.Intialize(turnOn, turnOff, build, _prewarmMisiles);
    }

    /// <summary>Returns an active missile from the pool.</summary>
    public Misile GetMisile() => pool.Get();
}
