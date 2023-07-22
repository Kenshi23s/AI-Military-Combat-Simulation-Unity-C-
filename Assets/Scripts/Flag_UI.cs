using AYellowpaper.SerializedCollections;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Flag_UI : MonoBehaviour
{
    [SerializeField] Image _flagOwner;
    [SerializeField, SerializedDictionary("Team","TextMesh")] SerializedDictionary<MilitaryTeam, TextMeshProUGUI> TeamTexts;

    Material _mat;
    CapturePoint _myCapturePoint;
    private void Awake()
    {
         _myCapturePoint = GetComponentInParent<CapturePoint>();
        _mat = _flagOwner.material = new Material(_flagOwner.material);

        if (_myCapturePoint == null) 
            Destroy(gameObject);

        _myCapturePoint.OnProgressChange.AddListener(UpdateProgressUI);
        _myCapturePoint.OnTeamsInPointUpdate.AddListener(SetTexts);
        _myCapturePoint.OnPointOwnerChange.AddListener(SetLetterColor);

        UpdateProgressUI(_myCapturePoint.CaptureProgress);
        SetLetterColor(_myCapturePoint.CapturedBy);
    }

    private void LateUpdate()
    {
        transform.forward = Camera.main.transform.position - transform.position;
    }

    void UpdateProgressUI(float progress)
    {
        _mat.SetFloat("_CaptureProgress", progress);

        Color color = progress >= 0 ? Color.red : Color.blue;

        _mat.SetColor("_ProgressFillColor", color);
    }

    void SetTexts(Dictionary<MilitaryTeam, IMilitary[]> col)
    {
        foreach (var key in TeamTexts.Keys)
        {
            if (col.ContainsKey(key))
                TeamTexts[key].text = col[key].Length.ToString();
            else
                TeamTexts[key].text = 0.ToString();
        }
    }

    void SetLetterColor(MilitaryTeam team) 
    {
        Color color;
        switch (team)
        {
            case MilitaryTeam.Blue:
                color = Color.blue;
                break;
            case MilitaryTeam.Red:
                color = Color.red;
                break;
            default:
                color = Color.white;
                break;
        }

        _mat.SetColor("_LetterColor", color);
    } 
  
    private void OnValidate()
    {
      
    }
}
