using System;
using UnityEngine;
/// <summary>Pool of FloatingText instances parented to a Transform.</summary>
[System.Serializable]
public class TextPool
{
    /// <summary>The underlying pool.</summary>
    public PoolObject<FloatingText> DamageTextPool = new PoolObject<FloatingText>();

    /// <summary>Instances created on Initialize.</summary>
    public int prewarm = 5;

    [SerializeField]
    private FloatingText sample;
    private Transform _transform;

    /// <summary>Creates the pool with clones of sample under _transform.</summary>
    public void Initialize(Transform _transform, FloatingText sample,DebugableObject _debug)
    {
        this._transform = _transform;
        this.sample= sample;    

        Action<FloatingText> turnOn = (x)  =>  x.gameObject.SetActive(true); 

        Action<FloatingText> turnOff = (x) => x.gameObject.SetActive(false); 

        Func<FloatingText> build = () =>
        {
            FloatingText dmgText = GameObject.Instantiate(sample);
            dmgText.Configure(ReturnHolder, _debug);
            dmgText.transform.SetParent(_transform);
            return dmgText;

        };

        DamageTextPool.Intialize(turnOn, turnOff, build, prewarm);
    }

    /// <summary>Returns t to the pool.</summary>
    public void ReturnHolder(FloatingText t) => DamageTextPool.Return(t);

    /// <summary>Returns an active FloatingText from the pool.</summary>
    public FloatingText GetHolder() => DamageTextPool.Get();
}
