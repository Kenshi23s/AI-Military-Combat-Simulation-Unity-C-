
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TeamIndicator : MonoBehaviour
{
    public IMilitary Owner { get; private set; }

    [SerializeField]
    Image Image;
    bool alreadyIn = false;

    public void AssignOwner(IMilitary NewOwner,SpriteRenderer icon)
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
        icon.color = color;
        Image.sprite = icon.sprite;
        enabled = false;
    }



    void LookCamera()
    {
        transform.forward = Camera.main.transform.position - transform.position;
    }

    private void OnBecameVisible()
    {
        if (alreadyIn) { return; }

        TeamsManager.instance.OnLateUpdate += LookCamera;
        alreadyIn = true;
    }

    private void OnBecameInvisible()
    {
        TeamsManager.instance.OnLateUpdate -= LookCamera;
    }

  

   


}
