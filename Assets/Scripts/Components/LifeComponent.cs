using System;
using System.Collections;
using UnityEngine;
[RequireComponent(typeof(DebugableObject))]
public class LifeComponent : MonoBehaviour, IDamagable, IHealable
{
    DebugableObject _debug;

    [SerializeField] int _life = 100;
    [SerializeField] int _maxLife = 100;
    public bool IsAlive => Life > 0;
    public int Life => _life;
    public int MaxLife => _maxLife;

    [SerializeField, Range(0.1f, 2)]
    float _dmgMultiplier = 1f;

    public int dmgResist = 1;

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
    bool _showDamageNumber = true;

    
    [SerializeField] public bool CanTakeDamage = true;
    [SerializeField] public bool CanBeHealed = true;

    #region Events
    public event Action<Vector3> OnKnockBack;
    public event Action<int, int> OnHealthChange;
    public event Action OnHeal;

    //pasar TODA LA INFORMACION AL TOMAR DAÑO (USAR EL STRUCT DE DAMAGE DATA)
    public event Action<int> OnTakeDamage;
    public event Action OnKilled;
    #endregion


 
    private void Awake()
    {
        
        _debug = GetComponent<DebugableObject>();
        // por si tenes hijos que pueden hacer de 
        //foreach (var item in GetComponentsInChildren<HitableObject>()) item.SetOwner(this);
        #region SetEvents
        OnHeal += () => OnHealthChange?.Invoke(Life, MaxLife);
        OnTakeDamage += (x) => OnHealthChange?.Invoke(Life, MaxLife);
        OnTakeDamage += DisplayDamageNumber;
        OnHealthChange?.Invoke(Life, MaxLife);
        #endregion
        enabled = false;

    }

    public bool IsCrit;
    public void DisplayDamageNumber(int x)
    {
        if (FloatingTextManager.instance==null)
        {
            Debug.Log("es Null");
        }
        
        FloatingTextManager.instance.PopUpText(x.ToString(), hitPos != Vector3.zero? hitPos : transform.position, IsCrit);
        hitPos = Vector3.zero;
    }
    Vector3 hitPos = Vector3.zero;

    public void SetHitPos(Vector3 x) => hitPos = x;

    public void SetNewMaxLife(int value) => _maxLife = Mathf.Clamp(value, 1, int.MaxValue);


    public void Initialize()
    {
        _life = _maxLife;       
    }

    #region DamageSide

    public virtual DamageData TakeDamage(int dmgDealt,Vector3 hitPos)
    {
        this.hitPos = hitPos;
        return TakeDamage(dmgDealt);
    }

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
            data.wasKilled = true;
        }
        data.damageDealt = dmgDealt;
        return data;
    }

    #endregion

    #region HealingSide
    /// <summary>
    /// añade vida, no supera la vida maxima
    /// </summary>
    /// <param name="HealAmount"></param>
    /// <returns></returns>
    public virtual int Heal(int HealAmount)
    {
        if (!CanBeHealed) return 0;

        _debug.Log($" se curo {HealAmount} de vida ");
        _life += Mathf.Abs(HealAmount);

        OnHeal?.Invoke();
        if (_life > _maxLife) _life = _maxLife;

        return HealAmount;
    }
    /// <summary>
    /// Añade x cantidad de vida al objetivo a lo largo de y segundos(no supera la vida maxima)
    /// </summary>
    /// <param name="totalHeal"></param>
    /// <param name="timeAmount"></param>
 
    #endregion

    
   

    
   
    private void OnValidate()
    {
        _maxLife = Mathf.Max(0, MaxLife);
        _life = MaxLife;
    }

    public void AddKnockBack(Vector3 force)
    {
        OnKnockBack?.Invoke(force);
    }

    public Vector3 Position()
    {
        return transform.position;
    }
}



