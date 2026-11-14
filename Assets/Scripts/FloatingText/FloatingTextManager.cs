using UnityEngine;
using static FloatingText;
/// <summary>Shows pooled FloatingText damage numbers.</summary>
[RequireComponent(typeof(DebugableObject))]
public class FloatingTextManager : MonoSingleton<FloatingTextManager>
{
    [SerializeField]
    private FloatingTextParam _parameters;
    [SerializeField]
    private FloatingText sampleFloatingText;

    private DebugableObject _debug;
    private TextPool pool = new TextPool();

    protected override void SingletonAwake()
    {
        _debug = GetComponent<DebugableObject>();   
        pool.Initialize(transform, sampleFloatingText, _debug);
    }

    /// <summary>Shows text at pos. A critical hit shows it in yellow.</summary>
    public void PopUpText(string text,Vector3 pos, bool isCrit = false)
    {
     
        FloatingText t = pool.GetHolder();

        if (t == null) return;
                                
            _parameters.IncreaseSortingOrder();
            if (isCrit)
                t.InitializeText(text, pos, _parameters, Color.yellow);
            else
                t.InitializeText(text, pos, _parameters);    
    }
}
