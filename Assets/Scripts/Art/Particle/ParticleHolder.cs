using System;
using System.Collections;
using UnityEngine;
[System.Serializable]
public struct ParticleHold
{
    public ParticleHolder particle;
    [NonSerialized] public int key;
}
public class ParticleHolder : MonoBehaviour
{  
    [SerializeField, Range(0, 10)] float _totalDuration;
    Action<ParticleHolder,int> _returnToPool;
    public event Action OnFinish;

    int _key;
    private void Awake()
    {    
        //buscar la manera de decirle q no haga update pero si OnEnabled(consultar a algun profe)
       //enabled=false;
    }
    public void InitializeParticle(Action<ParticleHolder, int> returnToPool, int key)
    {
        _returnToPool = returnToPool;
        _key = key;       
    }      

    private void OnEnable() => StartCoroutine(CooldownDecrease());

    public IEnumerator CooldownDecrease()
    {
        yield return new WaitForSeconds(_totalDuration);
        OnFinish?.Invoke();
        OnFinish = null;
        _returnToPool(this, _key);
    }

    public void ReturnNow()
    {
        StopCoroutine(CooldownDecrease());
        OnFinish?.Invoke();
        OnFinish = null;
        _returnToPool(this, _key);
    }
}
