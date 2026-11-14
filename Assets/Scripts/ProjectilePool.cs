using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectilePool : MonoSingleton<ProjectilePool>
{
    //estaria bueno tener una pool con timeSlicing(?)
    private PoolObject<Misile> pool = new PoolObject<Misile>();

    [SerializeField] private Misile _prefab;
    //devuelve la key a la pool
    [SerializeField] private int _prewarmMisiles = 20;

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

    public Misile GetMisile() => pool.Get();
}
