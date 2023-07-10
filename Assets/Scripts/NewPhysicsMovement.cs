using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Todos los metodos de esta clase se deberian llamar dentro del FixedUpdate
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(DebugableObject))]
[DisallowMultipleComponent]
public class NewPhysicsMovement : MonoBehaviour
{


    public Vector3 Velocity => rb.velocity;
    public Quaternion Rotation => rb.rotation;
    public float CurrentSpeed => rb.velocity.magnitude;

    [SerializeField, Min(0)] float _acceleration = 5f;

    public float Acceleration 
    {
        get => _acceleration;
        set => _acceleration = Mathf.Max(0,value);
    }
    [SerializeField, Min(0)] float _maxSpeed = 5f;

    public float MaxSpeed
    {
        get => _maxSpeed;
        set => _maxSpeed = Mathf.Max(0, value);
    }

    float _currentSpeed;

    [SerializeField] bool _freezeXZRotation = true;
    public bool FreezeXZRotation { get
        {
            return _freezeXZRotation;
        }
        set 
        {
            _freezeXZRotation = value;
            FreezeXZRotationChanged(value);
        }
    }

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
    #region ShortCuts
    public Vector3 Forward => Rotation * Vector3.forward;
    public Vector3 Right => Rotation * Vector3.right;
    public Vector3 Up => Rotation * Vector3.up;
    #endregion

    Rigidbody rb;
    DebugableObject _debug;


    private void OnValidate()
    {
        rb = GetComponent<Rigidbody>();
        _radiansRotSpeed = _rotationSpeed * Mathf.Deg2Rad;
        FreezeXZRotationChanged(_freezeXZRotation);
    }


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        FreezeXZRotationChanged(_freezeXZRotation);

        _debug = GetComponent<DebugableObject>();
        _debug.AddGizmoAction(DrawSpeedArrow);
    }

    public void AccelerateTowards(Vector3 dir) 
    {
        // Acelerar hacia maxSpeed
        _currentSpeed = Mathf.MoveTowards(rb.velocity.magnitude, _maxSpeed, _acceleration * Time.fixedDeltaTime);

        // Rotar hacia la direccion
        dir = Vector3.RotateTowards(Velocity != Vector3.zero ? Forward : transform.forward, dir, _radiansRotSpeed * Time.fixedDeltaTime, 1000f);

        rb.velocity = dir.normalized * _currentSpeed;

        if (_freezeXZRotation)
        {
            rb.rotation = Quaternion.LookRotation(new Vector3(rb.velocity.x, 0f, rb.velocity.z));
            return;
        }

        rb.rotation = Quaternion.LookRotation(rb.velocity);
    }

    public void ClearForces() => rb.velocity = Vector3.zero;

    public void AccelerateTowardsTarget(Vector3 destination) => AccelerateTowards(destination - rb.position);

    public void UseGravity(bool value) => rb.useGravity = value;

    void FreezeXZRotationChanged(bool freeze) 
    {
        _freezeXZRotation = freeze;
        if (freeze)
            rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        else
            rb.constraints &= ~(RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ);
    }

    void DrawSpeedArrow()
    {
        DrawArrow.ForGizmo(transform.position, Velocity , Color.green , 2f);
    }

}
