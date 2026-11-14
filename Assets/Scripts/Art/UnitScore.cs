using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitScore : MonoBehaviour
{
    [SerializeField] 
    private Image icon;
    [SerializeField] private TextMeshProUGUI UnitName;
    [SerializeField] private TextMeshProUGUI damageDealtTXT;

    public void SetOwner(IMilitary military)
    {
        icon.color = military.Team == MilitaryTeam.Red ? Color.red : Color.blue;
        UnitName.text = (military as MonoBehaviour).gameObject.name;
        damageDealtTXT.text = military.TotalDamageDealt.ToString();
    }

  
   
}
