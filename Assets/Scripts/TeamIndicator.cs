
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TeamIndicator : MonoBehaviour
{
    public IMilitary Owner { get; private set; }


    [SerializeField] Image Image;
    [SerializeField] TMP_Text nameText;
    [SerializeField] Gradient _lifeGradient;

    public void AssignOwner(IMilitary NewOwner,Sprite icon)
    {
        Owner = NewOwner;
     
        Color color = Color.white;
        switch (NewOwner.Team)
        {
            case MilitaryTeam.Blue:
                color = Color.blue;
                break;
            case MilitaryTeam.Red:
                color = Color.red;
                break;     
        }
        Image.sprite = icon;
        Image.color = color;


        Action destroy = delegate { };

        destroy += () =>
        {
            Destroy(this.gameObject);
            NewOwner.OnDeathInCombat -= destroy;
        };
        NewOwner.OnDeathInCombat += destroy;

        var x = NewOwner as ILifeObject;
        if (x == null) return;

        nameText.color = _lifeGradient.Evaluate(x.NormalizedLife);
        x.OnTakeDamage += () => nameText.color = _lifeGradient.Evaluate(x.NormalizedLife);
    }

    public void SetName(string x)
    {
        nameText.text = x;
    }
    




    void LookCamera()
    {
        transform.forward = (Camera.main.transform.position - transform.position).normalized;
    }
    private void LateUpdate()
    {
        LookCamera();
    }

    //private void OnBecameVisible()
    //{
    //    if (alreadyIn) return;

    //    TeamsManager.instance.OnLateUpdate += LookCamera;
    //    alreadyIn = true;
    //}

    //private void OnBecameInvisible()
    //{
    //    TeamsManager.instance.OnLateUpdate -= LookCamera;
    //    alreadyIn = false;
    //}






}
