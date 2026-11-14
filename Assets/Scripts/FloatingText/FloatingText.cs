using TMPro;
using UnityEngine;
using System;
using Random = UnityEngine.Random;

/// <summary>
/// Pooled text that floats up with a random horizontal drift, faces the camera and fades out.
/// Returns to its pool when the alpha reaches 0.
/// </summary>
public class FloatingText : MonoBehaviour
{
    /// <summary>The text component.</summary>
    public TMP_Text _popUpText;
    [SerializeField]
    private FloatingTextParam _textParameters;

    private DebugableObject _debug;

    private Renderer _ren;

    private Vector3 _textForce, _initialPos;   

    private Action<FloatingText> _pool_ReturnMethod;

    private Color _color, _actualColor;

    /// <summary>Movement and fade settings of a FloatingText.</summary>
    [System.Serializable]
    public struct FloatingTextParam
    {
        [Range(0f, 1f)]
        public float x_Spread;       

        [Range(0.1f, 1), Tooltip("la velocidad con la que se mueve el texto")]
        public float speed;

        [Range(0.1f, 1f), Tooltip("la velocidad con la que se desvanece el texto")]
        public float fadeSpeed;
        private int _sortingOrder;

        /// <summary>Sorting order for the next text.</summary>
        public int sortingOrderRead => _sortingOrder;

        /// <summary>Adds 1 to the sorting order.</summary>
        public void IncreaseSortingOrder() => _sortingOrder++;
    }

    #region Initialize
    private void Awake()
    {
        _ren = _popUpText.GetComponent<MeshRenderer>();
    }

    /// <summary>Sets the action that returns the text to its pool and the DebugableObject for the gizmos.</summary>
    public void Configure(Action<FloatingText> pool_ReturnMethod,DebugableObject debug)
    {
        _pool_ReturnMethod = pool_ReturnMethod;

        _debug = debug;
        _debug.AddGizmoAction(TextGizmos);

       
        _color = _popUpText.color;
        
    }

    /// <summary>Shows TextValueDamage at pos in color with parametersPass and a random drift.</summary>
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

    /// <summary>Shows TextValueDamage at pos in the default color with parametersPass and a random drift.</summary>
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

    private void SetText(string value,int sortOrder)
    {
        // esto es para que el texto actual tenga prioridad de renderizado sobre los anteriores
        _ren.sortingOrder = sortOrder;

        //aplico el int pasado al texto
        _popUpText.text = value.ToString();
        // se pone el alpha al maximo por las dudas de que no lo estuviera
        _popUpText.color = _actualColor;
        _popUpText.gameObject.name = "Text Damage  " + value ;
    }

    private void SetRandomForce(float RandomX)
    {
        _textParameters.x_Spread = SetRandomValue(RandomX);
       
        _textForce = new Vector3(_textParameters.x_Spread, 1);
    }

    private float SetRandomValue(float Randomness) => Random.Range(-Mathf.Abs(Randomness), Mathf.Abs(Randomness));
    #endregion

    #region Update 
    private void Update()
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

    private void LookCamera(Vector3 cam, Vector3 posToChange) => transform.LookAt(new Vector3(posToChange.x, cam.y, cam.z));

    private void SubstractAlpha(float decreaseSpeed)
    {
        _actualColor.a = Mathf.Max(_actualColor.a - decreaseSpeed * Time.deltaTime, 0);
        _popUpText.color = _actualColor;

        if (_actualColor.a <= 0) GoToPool();
    }
    #endregion
   
    private void GoToPool()
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
