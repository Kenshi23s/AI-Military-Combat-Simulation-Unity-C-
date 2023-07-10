using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Todos los metodos de esta clase se deberian llamar dentro del FixedUpdate
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(DebugableObject))]
[DisallowMultipleComponent]
public class NewPhysicsMovement : MonoBehaviour
{


    public Vector3 Velocity { 
        get => rb.velocity;
        set => rb.velocity = value;
    }

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

    [SerializeField] bool _groundedMovement = true;
    public bool GroundedMovement {
        get => _groundedMovement;
        set {
            _groundedMovement = value;
            OnGroundedMovementChanged(value);
        }
    }

    [SerializeField, Min(0), Tooltip("La velocidad de rotacion en angulos por segundo")] 
    float _rotationSpeed = 180f;

    public float RotationSpeed {
        get => _rotationSpeed;
        set 
        { 
            _rotationSpeed = value;
            _radiansRotSpeed = _rotationSpeed * Mathf.Deg2Rad;
        }
    }

    float _radiansRotSpeed;
    #region ShortCuts
    public Vector3 Right => rb.rotation * Vector3.right;
    public Vector3 Up => rb.rotation * Vector3.up;
    public Vector3 Forward => rb.rotation * Vector3.forward;
    #endregion

    Rigidbody rb;
    DebugableObject _debug;

    private void OnValidate()
    {
        rb = GetComponent<Rigidbody>();
        _radiansRotSpeed = _rotationSpeed * Mathf.Deg2Rad;
        OnGroundedMovementChanged(_groundedMovement);
    }


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        OnGroundedMovementChanged(_groundedMovement);

        _debug = GetComponent<DebugableObject>();
        _debug.AddGizmoAction(DrawSpeedArrow);
    }

    Vector3 velocity;
    Quaternion rotation;

    public void AccelerateTowards(Vector3 desiredDir) 
    {
        if (desiredDir == Vector3.zero)
        {
            Debug.Log("[Custom Msg] desiredDir can't be Vector3.zero");
            return;
        }

        desiredDir.Normalize();

        velocity = rb.velocity;
        rotation = rb.rotation;

        if (GroundedMovement)
            GroundedAccelerate(desiredDir);
        else
            PlaneAccelerate(desiredDir);

        rb.velocity = velocity;
        rb.rotation = rotation;
    }

    void GroundedAccelerate(Vector3 desiredDir) 
    {
        // Aplanar la direccion
        Vector3 desiredLookDir = new Vector3(desiredDir.x, 0, desiredDir.z).normalized;

        // Rotar hacia la direccion
        float maxRotSpeed = _radiansRotSpeed * Time.fixedDeltaTime;        
        Vector3 lookDir = Vector3.RotateTowards(transform.forward, desiredLookDir, maxRotSpeed, 0f);

        // Aca falta proyectar la direccion hacia la normal de contacto
        Vector3 moveDir = lookDir;
        Vector3 desiredVelocity = moveDir * MaxSpeed;
        desiredVelocity.y = velocity.y;

        float maxSpeedChange = _acceleration * Time.fixedDeltaTime;

        velocity = Vector3.MoveTowards(velocity, desiredVelocity, maxSpeedChange);

        rotation = Quaternion.LookRotation(lookDir);
    }

    void PlaneAccelerate(Vector3 desiredDir) 
    {
        // Rotar hacia la direccion
        float maxRotSpeed = _radiansRotSpeed * Time.fixedDeltaTime;
        Vector3 dir = Vector3.RotateTowards(transform.forward, desiredDir, maxRotSpeed, 0f);

        float maxSpeedChange = _acceleration * Time.fixedDeltaTime;
        float currentSpeed = Mathf.MoveTowards(velocity.magnitude, MaxSpeed, maxSpeedChange); 
        velocity = dir * currentSpeed;

        rotation = Quaternion.LookRotation(dir);
    }


    public void SetVelocity() 
    {
        rb.velocity = velocity;
    }
    public void ClearForces() => rb.velocity = Vector3.zero;

    public void AccelerateTowardsTarget(Vector3 destination) => AccelerateTowards(destination - rb.position);

    public void UseGravity(bool value) => rb.useGravity = value;

    public void AddImpulse(Vector3 force) 
    {
        rb.velocity += force;
    }

    void OnGroundedMovementChanged(bool freeze) 
    {
        _groundedMovement = freeze;
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
