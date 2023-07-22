using FacundoColomboMethods;
using System;
using UnityEngine;

[RequireComponent(typeof(DebugableObject))]
public class FOVAgent : MonoBehaviour
{
   
  

    [field : SerializeField]public float ViewRadius { get; private set; }
    [SerializeField,Range(0,360)]float _viewAngle;
    [SerializeField] Transform _eyes;
    float _sqrViewRadius;


    public void SetFov(float newRadius)
    {
        ViewRadius = newRadius;
    }
    private void Awake()
    {
        GetComponent<DebugableObject>().AddGizmoAction(FovGizmos);
        if (_eyes == null)
        {
            _eyes = transform;
            Debug.LogWarning(name +": La variable EYES DE Fov no fue asignada, se recomienda asignar para evitar problemas a futuro, por ahora EYES es el transform del componente");
        }
    }
    public bool IN_FOV(Vector3 target)
    {
        Vector3 dir = target - _eyes.position;

        if (dir.magnitude <= ViewRadius)
        {
            if (Vector3.Angle(_eyes.forward, dir) <= _viewAngle / 2)                     
                return ColomboMethods.InLineOffSight(_eyes.position, target, AI_Manager.instance.WallMask);      
        }
        return false;
    }

    public bool IN_FOV(Vector3 target,LayerMask mask)
    {
        Vector3 dir = target - _eyes.position;

        if (dir.magnitude <= ViewRadius)
        {
            if (Vector3.Angle(_eyes.forward, dir) <= _viewAngle / 2)
                return ColomboMethods.InLineOffSight(_eyes.position, target, mask);
        }
        return false;
    }

    public bool IN_FOV(Vector3 target, float viewRadius,LayerMask mask)
    {
        Vector3 dir = target - _eyes.position;

        if (dir.magnitude <= ViewRadius)
        {
            if (Vector3.Angle(_eyes.forward, dir) <= _viewAngle / 2)
                return ColomboMethods.InLineOffSight(_eyes.position, target, mask);
        }
        return false;
    }

    public bool IN_FOV(Vector3 target, float viewRadius)
    {
        Vector3 dir = target - _eyes.position;

        if (dir.magnitude <= viewRadius)
        {
            if (Vector3.Angle(_eyes.forward, dir) <= _viewAngle / 2)
                return ColomboMethods.InLineOffSight(_eyes.position, target, AI_Manager.instance.WallMask);
        }

        return false;
    }

    public void FovGizmos()
    {        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(_eyes.position, ViewRadius);
        
        Gizmos.color = Color.white;
        
        Gizmos.DrawWireSphere(transform.position, ViewRadius);
        
        Vector3 lineA = GetVectorFromAngle(_viewAngle / 2 + transform.eulerAngles.y);
        Vector3 lineB = GetVectorFromAngle(-_viewAngle / 2 + transform.eulerAngles.y);
        
        Gizmos.DrawLine(_eyes.position, _eyes.position + lineA * ViewRadius);
        Gizmos.DrawLine(_eyes.position, _eyes.position + lineB * ViewRadius);    
    }

    
    //documentar esto, pq no entiendo la logica detras de la cuenta(lo vimos en IA 1)
    Vector3 GetVectorFromAngle(float angle) => new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), 0, Mathf.Cos(angle * Mathf.Deg2Rad));
   

}
