using System;
using System.Collections;
using UnityEngine;

/// <summary>A ParticleHolder prefab and the key of its pool in ParticlePool.</summary>
[System.Serializable]
public struct ParticleHold
{
    public ParticleHolder particle;
    [NonSerialized] public int key;
}

/// <summary>
/// Pooled particle effect that returns to its pool after _totalDuration seconds.
/// Raises OnFinish before returning.
/// </summary>
public class ParticleHolder : MonoBehaviour
{
    [SerializeField, Range(0, 10)] private float _totalDuration;
    private Action<ParticleHolder,int> _returnToPool;

    private int _key;

    // Countdown started in OnEnable.
    private Coroutine _cooldown;

    /// <summary>Raised once when the particle returns to the pool. Cleared after it is raised.</summary>
    public event Action OnFinish;

    /// <summary>Sets the action that returns the particle to the pool with key.</summary>
    public void InitializeParticle(Action<ParticleHolder, int> returnToPool, int key)
    {
        _returnToPool = returnToPool;
        _key = key;
    }

    private void OnEnable() => _cooldown = StartCoroutine(CooldownDecrease());

    /// <summary>Waits _totalDuration seconds and returns the particle to the pool.</summary>
    public IEnumerator CooldownDecrease()
    {
        yield return new WaitForSeconds(_totalDuration);
        _cooldown = null;
        Finish();
    }

    /// <summary>Returns the particle to the pool before the countdown ends.</summary>
    public void ReturnNow()
    {
        if (_cooldown != null)
        {
            StopCoroutine(_cooldown);
            _cooldown = null;
        }

        Finish();
    }

    private void Finish()
    {
        OnFinish?.Invoke();
        OnFinish = null;
        _returnToPool(this, _key);
    }
}
