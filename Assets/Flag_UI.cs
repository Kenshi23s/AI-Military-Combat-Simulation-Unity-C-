using AYellowpaper.SerializedCollections;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class Flag_UI : MonoBehaviour
{
    [SerializeField] Gradient _gradientFlag;
    [SerializeField] Image _flagOwner;
    [SerializeField,SerializedDictionary("Team","TextMesh")] SerializedDictionary<Team, TextMeshProUGUI> TeamTexts;

    [Range(0,1)]public float SliderTest;
    CapturePoint _myCapturePoint;
    private void Awake()
    {
         _myCapturePoint = GetComponentInParent<CapturePoint>();

        if (_myCapturePoint == null) Destroy(gameObject);

        _myCapturePoint.onProgressChange.AddListener(SetImageValue);

        _myCapturePoint.onEntitiesAroundUpdate.AddListener(SetTexts);
        SetImageValue();
    }

    private void LateUpdate()
    {
        transform.forward = Camera.main.transform.position - transform.position;
    }

    void SetImageValue()
    {
        _flagOwner.color = _gradientFlag.Evaluate(_myCapturePoint.ZoneProgressNormalized);
    }


    void SetTexts(Dictionary<Team, Entity[]> col)
    {
        foreach (var key in TeamTexts.Keys.Where(x => col[x] != null ))
        {
            TeamTexts[key].text = col[key].Length.ToString();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnValidate()
    {
        if (_flagOwner == null) return;
        _flagOwner.color = _gradientFlag.Evaluate(SliderTest);
    }
}
