using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Icon and name above a unit, colored by its team, that faces the camera.
/// The name color follows the life of the owner and the indicator destroys itself when the owner dies.
/// </summary>
public class TeamIndicator : MonoBehaviour
{
    [SerializeField] private Image Image;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Gradient _lifeGradient;

    /// <summary>The unit of the indicator.</summary>
    public IMilitary Owner { get; private set; }

    /// <summary>Sets the owner, the icon and the team color.</summary>
    public void AssignOwner(IMilitary NewOwner, Sprite icon)
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

        EvaluateLife();
        x.OnTakeDamage += EvaluateLife;
        x.OnHeal += () => EvaluateLife();
    }

    /// <summary>Sets the name text.</summary>
    public void SetName(string x)
    {
        nameText.text = x;
    }

    private void EvaluateLife()
    {
        var x = Owner as ILifeObject;

        nameText.color = _lifeGradient.Evaluate(x.NormalizedLife);
    }

    private void LookCamera()
    {
        transform.forward = (Camera.main.transform.position - transform.position).normalized;
    }

    private void LateUpdate()
    {
        LookCamera();
    }
}
