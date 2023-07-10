using System;
using System.Collections.Generic;
using UnityEngine;

public class ParticlePool:MonoSingleton<ParticlePool>
{
   
    Dictionary<int,PoolObject<ParticleHolder>> _vfxPools = new Dictionary<int, PoolObject<ParticleHolder>>();
   //devuelve la key a la pool
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

    public ParticleHolder GetVFX(int key) => _vfxPools.ContainsKey(key) ? _vfxPools[key].Get() : null;

    protected override void SingletonAwake()
    {
     
    }
}
