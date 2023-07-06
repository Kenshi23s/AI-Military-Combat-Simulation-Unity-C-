using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FacundoColomboMethods;
using System;
using Random = UnityEngine.Random;

public class ShootComponent : MonoBehaviour
{
    [SerializeField] float _bulletspread;
    [SerializeField] int _bulletdamage;
    //estaria bueno sacar esto de un manager mejor, pero por ahora
    [SerializeField]LayerMask shootableLayers;

    public event Action<IDamagable> onHit;

    public void Shoot(Transform _shootPos)
    {
        Vector3 dir = _shootPos.transform.forward.RandomDirFrom(Random.Range(0, _bulletspread));
        if (Physics.Raycast(_shootPos.position,dir,out RaycastHit hit,Mathf.Infinity, shootableLayers))
        {
            if (hit.transform.TryGetComponent(out IDamagable victim))
            {
                victim.TakeDamage(_bulletdamage);
                onHit?.Invoke(victim);
            }
                


        }
    }



    
}
