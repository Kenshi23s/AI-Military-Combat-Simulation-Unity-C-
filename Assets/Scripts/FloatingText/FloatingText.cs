using TMPro;
using UnityEngine;
using System;
using Random = UnityEngine.Random;

public class FloatingText : MonoBehaviour
{
    [System.Serializable]
    public struct FloatingTextParam
    {
        [Range(0f, 1f)]
        public float x_Spread;       

        [Range(0.1f, 1), Tooltip("la velocidad con la que se mueve el texto")]
        public float speed;

        [Range(0.1f, 1f), Tooltip("la velocidad con la que se desvanece el texto")]
        public float fadeSpeed;

        public int sortingOrderRead => _sortingOrder;
        int _sortingOrder;

        public void IncreaseSortingOrder() => _sortingOrder++;
       

    }
   

    DebugableObject _debug;
    [SerializeField]
    FloatingTextParam _textParameters;

    Renderer _ren;

    public TMP_Text _popUpText;

    Vector3 _textForce, _initialPos;   
   
    Action<FloatingText> _pool_ReturnMethod;

    Color _color, _actualColor;


    #region Initialize
    private void Awake()
    {
        _ren = _popUpText.GetComponent<MeshRenderer>();
    }
    //Al crearse                                                     // MI metodo Return es igual al metodo Return que me pasaron por parametro
    public void Configure(Action<FloatingText> pool_ReturnMethod,DebugableObject debug) 
    {
        _pool_ReturnMethod = pool_ReturnMethod;

        _debug = debug;
        _debug.AddGizmoAction(TextGizmos);

       
        _color = _popUpText.color;
        
    }

    /// <summary>
    /// este metodo es el encargado de preparar a el texto para que haga todos 
    /// los comportamientos necesarios para su correcta funcion
    /// </summary>
    public void InitializeText(string TextValueDamage, Vector3 pos, FloatingTextParam parametersPass, Color color)
    {
        //inital pos lo uso para fijarme(en el gizmos) en que direccion va el texto 
        //transform.position = pos;

        _actualColor = color;
        _initialPos = transform.position = pos;
        _textParameters = parametersPass;
        SetText(TextValueDamage, parametersPass.sortingOrderRead);
        SetRandomForce(parametersPass.x_Spread);

    }

    /// <summary>
    /// este metodo es el encargado de preparar a el texto para que haga todos 
    /// los comportamientos necesarios para su correcta funcion
    /// </summary>
    public void InitializeText(string TextValueDamage, Vector3 pos, FloatingTextParam parametersPass)
    {
        //inital pos lo uso para fijarme(en el gizmos) en que direccion va el texto 
        //transform.position = pos;

       _actualColor = _color;
       _initialPos = transform.position =  pos;
       _textParameters = parametersPass;
       SetText(TextValueDamage,parametersPass.sortingOrderRead);   
       SetRandomForce(parametersPass.x_Spread);

    }
 
    void SetText(string value,int sortOrder)
    {
        // esto es para que el texto actual tenga prioridad de renderizado sobre los anteriores
        _ren.sortingOrder = sortOrder;

        //aplico el int pasado al texto
        _popUpText.text = value.ToString();
        // se pone el alpha al maximo por las dudas de que no lo estuviera
        _popUpText.color = _actualColor;
        _popUpText.gameObject.name = "Text Damage  " + value ;
    }

    void SetRandomForce(float RandomX)
    {
        _textParameters.x_Spread = SetRandomValue(RandomX);
       
        _textForce = new Vector3(_textParameters.x_Spread, 1);
    }

    float SetRandomValue(float Randomness) => Random.Range(-Mathf.Abs(Randomness), Mathf.Abs(Randomness));
    #endregion

    #region Update 
    void Update()
    {
        //se le suma al transform una fuerza para que se mueva a lo largo del tiempo hasta que
        //el "Alpha" del texto llegue a 0 (va de 0 a 1, no de 0 a 255)
        transform.position += _textForce.normalized * Time.deltaTime * _textParameters.speed;
        SubstractAlpha(_textParameters.fadeSpeed);
    }

    private void LateUpdate()
    {
        transform.forward = Camera.main.transform.position - transform.position;
    } 

    void LookCamera(Vector3 cam, Vector3 posToChange) => transform.LookAt(new Vector3(posToChange.x, cam.y, cam.z));

    void SubstractAlpha(float decreaseSpeed)
    {
        _actualColor.a = Mathf.Max(_actualColor.a - decreaseSpeed * Time.deltaTime, 0);
        _popUpText.color = _actualColor;

        if (_actualColor.a <= 0) GoToPool();
    }
    #endregion
   
    void GoToPool()
    {     
        _initialPos = _textForce = Vector3.zero;
        _pool_ReturnMethod(this);
    }

    private void TextGizmos()
    {          
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(_initialPos, _initialPos + _textForce * 10);
        Gizmos.DrawWireSphere(_initialPos + _textForce*10,1f);
    }
}
