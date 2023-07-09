using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Todos los metodos de esta clase se deberian llamar dentro del FixedUpdate
[RequireComponent(typeof(Rigidbody))]
[DisallowMultipleComponent]
public class NewPhysicsMovement : MonoBehaviour
{
    public Vector3 Velocity => rb.velocity;
    public Quaternion Rotation => rb.rotation;
    public float CurrentSpeed => rb.velocity.magnitude;

    [SerializeField, Min(0)] float _acceleration = 5f;
    [SerializeField, Min(0)] float _maxSpeed = 5f;
    float _currentSpeed;

    public bool FreezeYRotation = false;

    [SerializeField, Min(0), Tooltip("La velocidad de rotacion en angulos por segundo")] 
    float _rotationSpeed = 180f;

    public float RotationSpeed {
        get 
        { 
            return _rotationSpeed; 
        }
        set 
        { 
            _rotationSpeed = value;
            _radiansRotSpeed = _rotationSpeed * Mathf.Deg2Rad;
        }
    }

    float _radiansRotSpeed;
    public Vector3 Forward => Rotation * Vector3.forward;
    public Vector3 Right => Rotation * Vector3.right;
    public Vector3 Up => Rotation * Vector3.up;
    private void OnValidate()
    {
        _radiansRotSpeed = _rotationSpeed * Mathf.Deg2Rad;
    }

    Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void AccelerateTowards(Vector3 dir) 
    {
        // Acelerar hacia maxSpeed
        _currentSpeed = Mathf.MoveTowards(rb.velocity.magnitude, _maxSpeed, _acceleration * Time.fixedDeltaTime);

        // Rotar hacia la direccion
        dir = Vector3.RotateTowards(Velocity != Vector3.zero ? Forward : transform.forward, dir, _radiansRotSpeed * Time.fixedDeltaTime, 1000f);

        rb.velocity = dir.normalized * _currentSpeed;

        if (FreezeYRotation)
        {
            rb.rotation = Quaternion.LookRotation(new Vector3(rb.velocity.x, 0f, rb.velocity.z));
            return;
        }

        rb.rotation = Quaternion.LookRotation(rb.velocity);
    }

    public void ClearForces() => rb.velocity = Vector3.zero;

    public void AccelerateTowardsTarget(Vector3 destination) => AccelerateTowards(destination - rb.position);

    public void UseGravity(bool value) => rb.useGravity = value;

}
