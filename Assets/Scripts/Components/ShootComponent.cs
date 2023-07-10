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
    [SerializeField] TrailRenderer trailSample;
    public event Action<IDamagable> onHit;

    public void Shoot(Transform _shootPos)
    {
        Vector3 dir = _shootPos.transform.forward.RandomDirFrom(Random.Range(0, _bulletspread));
        Vector3 finalTrailPos = Vector3.zero;
 
        if (Physics.Raycast(_shootPos.position,dir,out RaycastHit hit,Mathf.Infinity, shootableLayers))
        {
            if (hit.transform.TryGetComponent(out IDamagable victim))
            {
                victim.TakeDamage(_bulletdamage);
                Debug.Log($"Hit! le hice daño a {victim}");
                onHit?.Invoke(victim);
            }
            finalTrailPos = hit.point;
        }

        if (finalTrailPos==Vector3.zero) finalTrailPos = dir.normalized * 100;

        StartCoroutine(SpawnTrail(Instantiate(trailSample,_shootPos.position,Quaternion.identity), finalTrailPos));
    }

    IEnumerator SpawnTrail(TrailRenderer trail, Vector3 impactPos)
    {
        Vector3 startPos = trail.transform.position;
        float dist = Vector3.Distance(startPos, impactPos);
        float time = 0f;

        while (time < 1)
        {

            trail.transform.position = Vector3.Lerp(startPos, impactPos, time);


            time += (Time.deltaTime / trail.time) * (dist / Vector3.Distance(trail.transform.position, impactPos));
            yield return null;
        }
        trail.transform.position = impactPos;
        Destroy(trail.gameObject);
    }



}
