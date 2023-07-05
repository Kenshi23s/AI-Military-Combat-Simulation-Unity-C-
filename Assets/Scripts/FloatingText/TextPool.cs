using System;
using UnityEngine;
[System.Serializable]
public class TextPool 
{
    #region PoolRequirements
    public PoolObject<FloatingText> DamageTextPool = new PoolObject<FloatingText>();

    [SerializeField]
    FloatingText sample;
    //Action<FloatingText> ReturnMethod;
    public int prewarm = 5;
    Transform _transform;
    #endregion

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

   

    public void ReturnHolder(FloatingText t) => DamageTextPool.Return(t);

    public FloatingText GetHolder() => DamageTextPool.Get();


}
