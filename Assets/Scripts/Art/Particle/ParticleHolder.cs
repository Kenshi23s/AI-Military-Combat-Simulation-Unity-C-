using System;
using System.Collections;
using UnityEngine;
public class ParticleHolder : MonoBehaviour
{

   
    [SerializeField, Range(0, 10)] float _totalDuration;
    Action<ParticleHolder,int> _returnToPool;
    public event Action OnFinish;

    int key;
    private void Awake()
    {    
        //buscar la manera de decirle q no haga update pero si OnEnabled(consultar a algun profe)
       //enabled=false;
    }
    public void InitializeParticle(Action<ParticleHolder,int> _returnToPool,int key)
    {
        this._returnToPool = _returnToPool;
        this.key = key;       
    }      

    private void OnEnable() => StartCoroutine(CooldownDecrease());

    public IEnumerator CooldownDecrease()
    {
        yield return new WaitForSeconds(_totalDuration);
        OnFinish?.Invoke();
        OnFinish = null;
        _returnToPool(this,key);
    }

    public void ReturnNow()
    {
        StopCoroutine(CooldownDecrease());
        OnFinish?.Invoke();
        OnFinish = null;
        _returnToPool(this, key);
    }
}
