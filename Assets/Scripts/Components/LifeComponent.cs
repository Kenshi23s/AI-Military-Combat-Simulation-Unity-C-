using System;
using System.Collections;
using UnityEngine;
/// <summary>
/// Health of an object: damage, healing, death and knockback events.
/// Shows a floating damage number on each hit while ShowDamageNumber is true.
/// </summary>
[RequireComponent(typeof(DebugableObject))]
public class LifeComponent : MonoBehaviour, IDamageable, IHealable,ILifeObject
{
    public int dmgResist = 1;

    [SerializeField] public bool CanTakeDamage = true;
    [SerializeField] public bool CanBeHealed = true;

    /// <summary>Shows the next damage numbers as critical.</summary>
    public bool IsCrit;

    [SerializeField] private int _life = 100;
    [SerializeField] private int _maxLife = 100;

    [SerializeField, Range(0.1f, 2)]
    private float _dmgMultiplier = 1f;
    private DebugableObject _debug;
    private bool _showDamageNumber = true;
    private Vector3 hitPos = Vector3.zero;

    /// <summary>true while Life is greater than 0.</summary>
    public bool IsAlive => Life > 0;

    /// <summary>Current health points.</summary>
    public int Life => _life;

    /// <summary>Maximum health points.</summary>
    public int MaxLife => _maxLife;

    public bool ShowDamageNumber
    {
        get
        {
            return _showDamageNumber;
        }

        set
        {
            if (_showDamageNumber == value) return;

            if (value)
                OnTakeDamage += DisplayDamageNumber;
            else
                OnTakeDamage -= DisplayDamageNumber;

            _showDamageNumber = value;

        }
    }

    public event Action<Vector3> OnKnockBack;
    public event Action<int, int> OnHealthChange;
    public event Action OnHeal;

    //pasar TODA LA INFORMACION AL TOMAR DAÑO (USAR EL STRUCT DE DAMAGE DATA)
    public event Action<int> OnTakeDamage;
    public event Action OnKilled;

    event Action ILifeObject.OnTakeDamage
    {
        add
        {
            OnTakeDamage += (_) => value.Invoke(); 
        }

        remove
        {
            OnTakeDamage -= (_) => value.Invoke();
        }
    }

    /// <summary>Sets MaxLife, the damage multiplier and dmgResist from stats.</summary>
    /// <param name="refill">true sets Life to MaxLife. false keeps Life, limited to MaxLife.</param>
    public void ApplyStats(HealthStats stats, bool refill)
    {
        _maxLife = Mathf.Max(1, stats.MaxLife);
        _dmgMultiplier = stats.DamageMultiplier;
        dmgResist = Mathf.Max(1, stats.DamageResist);
        _life = refill ? _maxLife : Mathf.Min(_life, _maxLife);
    }

    private void Awake()
    {
        
        _debug = GetComponent<DebugableObject>();
        // por si tenes hijos que pueden hacer de 
        //foreach (var item in GetComponentsInChildren<HitableObject>()) item.SetOwner(this);
                OnHeal += () => OnHealthChange?.Invoke(Life, MaxLife);
        OnTakeDamage += (x) => OnHealthChange?.Invoke(Life, MaxLife);
        OnTakeDamage += DisplayDamageNumber;
        OnHealthChange?.Invoke(Life, MaxLife);
                enabled = false;

    }

    /// <summary>Pops up x as floating text at the last hit position, or at the object position.</summary>
    /// <remarks>Runs only while the scene has a FloatingTextManager.</remarks>
    public void DisplayDamageNumber(int x)
    {
        if (FloatingTextManager.instance == null)
            return;

        FloatingTextManager.instance.PopUpText(x.ToString(), hitPos != Vector3.zero? hitPos : transform.position, IsCrit);
        hitPos = Vector3.zero;
    }

    /// <summary>Sets the position of the next damage number.</summary>
    public void SetHitPos(Vector3 x) => hitPos = x;

    /// <summary>Sets MaxLife to value. The minimum is 1.</summary>
    public void SetNewMaxLife(int value) => _maxLife = Mathf.Clamp(value, 1, int.MaxValue);

    /// <summary>Sets Life to MaxLife.</summary>
    public void Initialize()
    {
        _life = _maxLife;
    }

    #region DamageSide

    /// <summary>Applies dmgDealt with TakeDamage and shows the damage number at hitPos.</summary>
    public virtual DamageData TakeDamage(int dmgDealt,Vector3 hitPos)
    {
        this.hitPos = hitPos;
        return TakeDamage(dmgDealt);
    }

    /// <summary>
    /// Subtracts dmgDealt, scaled by the damage multiplier and divided by dmgResist, from Life.
    /// Raises OnTakeDamage, and OnKilled when Life reaches 0.
    /// </summary>
    /// <returns>The damage dealt and whether the hit killed the object.</returns>
    /// <remarks>OnKilled is cleared after it is raised.</remarks>
    public virtual DamageData TakeDamage(int dmgDealt)
    {
        DamageData data = new DamageData();
        if (!CanTakeDamage) return data;

         dmgDealt = (int)(Mathf.Abs(dmgDealt) * _dmgMultiplier) / dmgResist;
        _life -= dmgDealt; OnTakeDamage?.Invoke(dmgDealt);
        _debug.Log($" recibio {dmgDealt} de daño ");

        if (_life <= 0)
        {
            OnKilled?.Invoke();
            OnKilled = delegate { };
            data.wasKilled = true;
        }
        data.damageDealt = dmgDealt;
        return data;
    }

    #endregion

    #region HealingSide
    /// <summary>Adds HealAmount to Life up to MaxLife and raises OnHeal.</summary>
    /// <returns>HealAmount, or 0 while CanBeHealed is false.</returns>
    public virtual int Heal(int HealAmount)
    {
        if (!CanBeHealed) return 0;

        _debug.Log($" se curo {HealAmount} de vida ");
        _life += Mathf.Abs(HealAmount);

        OnHeal?.Invoke();
        if (_life > _maxLife) _life = _maxLife;

        return HealAmount;
    }
    #endregion

    
   

    
   
    private void OnValidate()
    {
        _maxLife = Mathf.Max(0, MaxLife);
        _life = MaxLife;
    }

    /// <summary>Raises OnKnockBack with force.</summary>
    public void AddKnockBack(Vector3 force)
    {
        OnKnockBack?.Invoke(force);
    }

    /// <summary>Returns the position of the object.</summary>
    public Vector3 Position()
    {
        return transform.position;
    }
}



