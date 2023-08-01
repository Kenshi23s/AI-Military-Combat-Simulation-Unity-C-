using UnityEngine;
using System;

public interface IDamageable 
{
    public bool IsAlive { get; }

    DamageData TakeDamage(int dmgToDeal);
    DamageData TakeDamage(int dmgToDeal, Vector3 hitPoint);
    void AddKnockBack(Vector3 force);
    Vector3 Position();

}

public struct DamageData
{
    public int damageDealt;
    public bool wasKilled;
    public bool wasCrit;
    public IDamageable victim;

   
}

public interface IHealable
{
    int Heal(int HealAmount);
}

public interface ILifeObject : IDamageable, IHealable
{
    public float NormalizedLife => (float)Life / MaxLife;
    public int MaxLife { get; }
    public int Life { get; }
    public event Action OnTakeDamage;
}




