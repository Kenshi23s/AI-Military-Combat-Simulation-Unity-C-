using UnityEngine;
public interface IDamagable 
{
    public bool IsAlive { get; }

    DamageData TakeDamage(int dmgToDeal);
    DamageData TakeDamage(int dmgToDeal,Vector3 hitPoint);
    void AddKnockBack(Vector3 force);
    Vector3 Position();

}
public struct DamageData
{
    public int damageDealt;
    public bool wasKilled;
    public bool wasCrit;
    public IDamagable victim;

   
}

public interface IHealable
{
    int Heal(int HealAmount);
   



}


