using System;
using System.Collections.Generic;
using UnityEngine;


/// <summary>One PoolObject of ParticleHolder per particle prefab, indexed by key.</summary>
public class ParticlePool:MonoSingleton<ParticlePool>
{
    private Dictionary<int,PoolObject<ParticleHolder>> _vfxPools = new Dictionary<int, PoolObject<ParticleHolder>>();

    /// <summary>Creates the pool of particle and returns its key.</summary>
    /// <returns>The key of the existing pool when particle already has one.</returns>
    public int CreateVFXPool(ParticleHolder particle)
    {
        int key = particle.GetHashCode();
        if (_vfxPools.ContainsKey(key)) return key;      

        PoolObject<ParticleHolder> pool = new PoolObject<ParticleHolder>();

        Action<ParticleHolder> turnOn  = (x) =>  x.gameObject.SetActive(true);

        Action<ParticleHolder> turnOff = (x) =>  x.gameObject.SetActive(false); 

        Func<ParticleHolder>  build = () => 
        {         
            ParticleHolder particleClone = GameObject.Instantiate(particle);

            Action<ParticleHolder,int> ReturnToPool = (x,y) =>  _vfxPools[y].Return(x);

            particleClone.InitializeParticle(ReturnToPool,key);

            return particleClone;
        };

        pool.Intialize(turnOn,turnOff,build); _vfxPools.Add(key, pool);

        return key;
           
    }

    /// <summary>Returns a particle from the pool of key, or null for an unknown key.</summary>
    public ParticleHolder GetVFX(int key) => _vfxPools.TryGetValue(key, out var pool) ? pool.Get() : null;

    protected override void SingletonAwake()
    {
     
    }
}
