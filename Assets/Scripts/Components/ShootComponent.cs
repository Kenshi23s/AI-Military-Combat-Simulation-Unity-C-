using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FacundoColomboMethods;
using System;
using Random = UnityEngine.Random;
using static UnityEngine.ParticleSystem;

public class ShootComponent : MonoBehaviour
{
    public event Action<IDamagable> onHit;

    [SerializeField] float _bulletspread;
    [SerializeField] int _bulletdamage;
    //estaria bueno sacar esto de un manager mejor, pero por ahora
    [SerializeField]LayerMask shootableLayers;
    [SerializeField] TrailRenderer trailSample;

    [Header("Shooting Parameters")]
    [SerializeField] float _bulletsPerBurst;
    [SerializeField] float _burstCD = 3;
    [SerializeField] float _bulletCD = 0.3f;
    [SerializeField] Transform _shootPos;

    const float maxTravelDistance = 100;

    public void Shoot(Transform shootPos)
    {
        Vector3 dir = shootPos.transform.forward.RandomDirFrom(Random.Range(0, _bulletspread));
        Vector3 finalTrailPos = Vector3.zero;
 
        if (Physics.Raycast(shootPos.position,dir,out RaycastHit hit,Mathf.Infinity, shootableLayers))
        {
            if (hit.transform.TryGetComponent(out IDamagable victim))
            {
                victim.TakeDamage(_bulletdamage);
                Debug.Log($"Hit! le hice daño a {victim}");
                onHit?.Invoke(victim);
            }
            finalTrailPos = hit.point;
        }

        if (finalTrailPos == Vector3.zero) finalTrailPos = dir.normalized * maxTravelDistance;

        var trail = Instantiate(trailSample, shootPos.position, Quaternion.identity);
        StartCoroutine(SpawnTrail(trail, finalTrailPos));
    }

    public void Shoot(Transform shootPos,Vector3 dir)
    {
        Vector3 finalTrailPos = Vector3.zero;
        var randomDir = dir.RandomDirFrom(Random.Range(0,_bulletspread)); 

        if (Physics.Raycast(shootPos.position, randomDir, out RaycastHit hit, Mathf.Infinity, shootableLayers))
        {
            if (hit.transform.TryGetComponent(out IDamagable victim))
            {
                victim.TakeDamage(_bulletdamage);
                Debug.Log($"Hit! le hice daño a {victim}");
                onHit?.Invoke(victim);
            }
            finalTrailPos = hit.point;
        }

        if (finalTrailPos == Vector3.zero) finalTrailPos = randomDir.normalized * maxTravelDistance;

        var trail = Instantiate(trailSample, shootPos.position, Quaternion.identity);
        StartCoroutine(SpawnTrail(trail, finalTrailPos));
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

    public IEnumerator ShootBullets()
    {
        while (true)
        {
            for (int i = 0; i < _bulletsPerBurst; i++)
            {
                Shoot(_shootPos);
                yield return new WaitForSeconds(_bulletCD);
            }
            yield return new WaitForSeconds(_burstCD);
        }
    }

}
