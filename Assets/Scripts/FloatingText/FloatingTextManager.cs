using UnityEngine;
using static FloatingText;
[RequireComponent(typeof(DebugableObject))]
public class FloatingTextManager : MonoSingleton<FloatingTextManager>
{

    DebugableObject _debug;
    TextPool pool = new TextPool();

    [SerializeField]
    FloatingTextParam _parameters;
    [SerializeField]
    FloatingText sampleFloatingText;

 
    protected override void SingletonAwake()
    {
        _debug = GetComponent<DebugableObject>();   
        pool.Initialize(transform, sampleFloatingText, _debug);
    }
    //podriamos tener en este metodo variaciones por ej
    // si es critico o baja que salga de otro color el texto, con otra fuente, o otra proporcion
    //revisar para 6to cuatrimestre o cuando no haya tareas heavy


    //esto deberia subscribirse a el gun manager.actualgunhit<HitData>, por ahora lo dejo asi
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
