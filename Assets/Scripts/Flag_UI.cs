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
   

    Material _mat;
    CapturePoint _myCapturePoint;

    [Header("OnTake")]
    [SerializeField] GameObject OnTakeGO;
    [SerializeField] TextMeshProUGUI UnitAmountText;

    [Header("OnDispute")]
    [SerializeField] GameObject OnDisputeGO;
    [SerializeField, SerializedDictionary("Team", "TextMesh")] SerializedDictionary<MilitaryTeam, TextMeshProUGUI> TeamTexts;

    private void Awake()
    {
         _myCapturePoint = GetComponentInParent<CapturePoint>();
        _mat = _flagOwner.material = new Material(_flagOwner.material);

        if (_myCapturePoint == null) 
            Destroy(gameObject);

        _myCapturePoint.OnProgressChange.AddListener(UpdateProgressUI);
        _myCapturePoint.OnPointOwnerChange.AddListener(SetLetterColor);

        _myCapturePoint.OnDisputeStart += ActivateDisputeUI;
        _myCapturePoint.OnDisputeEnd += DeActivateDisputeUI;


        _myCapturePoint.OnBeingCaptured += ActivateOnTake;
        _myCapturePoint.OnStopCapture   += DeActivateOnTake;

        UpdateProgressUI(_myCapturePoint.CaptureProgress); SetLetterColor(_myCapturePoint.CapturedBy);
    }

    private void LateUpdate()
    {
        transform.forward = Camera.main.transform.position - transform.position;
    }

    #region OnTake
    void ActivateOnTake(MilitaryTeam team)
    {
        OnTakeGO.SetActive(true);
        UnitAmountText.color = team == MilitaryTeam.Red ? Color.red : Color.blue;
        _myCapturePoint.OnTeamsInPointUpdate.AddListener(ChangeOnTakeText);
    }

    void ChangeOnTakeText(Dictionary<MilitaryTeam, IMilitary[]> col)
    {
        UnitAmountText.text = col.Select(x => x.Value).Maximum(x => x.Count()).Count().ToString();
    }

    void DeActivateOnTake()
    {
        OnTakeGO.SetActive(false);
        _myCapturePoint.OnTeamsInPointUpdate.RemoveListener(ChangeOnTakeText);
    }
    #endregion
    #region OnDispute

    #endregion
    void UpdateProgressUI(float progress)
    {
        _mat.SetFloat("_CaptureProgress", progress);

        Color color = progress >= 0 ? Color.red : Color.blue;

        _mat.SetColor("_ProgressFillColor", color);
    }

    void ActivateDisputeUI()
    {
        _myCapturePoint.OnTeamsInPointUpdate.AddListener(SetTexts);
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

    void DeActivateDisputeUI()
    {
        _myCapturePoint.OnTeamsInPointUpdate.RemoveListener(SetTexts);
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
