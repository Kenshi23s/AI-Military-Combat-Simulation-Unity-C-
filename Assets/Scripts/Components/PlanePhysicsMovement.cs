using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Todos los metodos de esta clase se deberian llamar dentro del FixedUpdate
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(DebugableObject))]
[DisallowMultipleComponent]
public class PlanePhysicsMovement : MonoBehaviour
{


    public Vector3 Velocity
    {
        get => _rb.velocity;
        set => _rb.velocity = value;
    }
    public Quaternion Rotation => _rb.rotation;
    public float CurrentSpeed => _rb.velocity.magnitude;

    [SerializeField, Min(0)] float _acceleration = 37.5f;

    public float Acceleration
    {
        get => _acceleration;
        set => _acceleration = Mathf.Max(0, value);
    }

    [SerializeField, Min(0)] float _maxSpeed = 8f;

    public float MaxSpeed
    {
        get => _maxSpeed;
        set => _maxSpeed = Mathf.Max(0, value);
    }

    [SerializeField, Min(0), Tooltip("La velocidad de rotacion en angulos por segundo")]
    float _rotationSpeed = 180f;

    public float RotationSpeed
    {
        get => _rotationSpeed;
        set
        {
            _rotationSpeed = value;
            _radiansRotSpeed = _rotationSpeed * Mathf.Deg2Rad;
        }
    }

    float _radiansRotSpeed;

    #region ShortCuts
    public Vector3 Right => _rb.rotation * Vector3.right;
    public Vector3 Up => _rb.rotation * Vector3.up;
    public Vector3 Forward => _rb.rotation * Vector3.forward;
    #endregion

    Rigidbody _rb;
    DebugableObject _debug;

    private void OnValidate()
    {
        _radiansRotSpeed = _rotationSpeed * Mathf.Deg2Rad;
    }


    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        _debug = GetComponent<DebugableObject>();
        _debug.AddGizmoAction(DrawSpeedArrow);
    }

    public void AccelerateTowards(Vector3 desiredDir)
    {
        if (desiredDir == Vector3.zero)
        {
            Debug.Log("[Custom Msg] desiredDir can't be Vector3.zero");
            return;
        }

        desiredDir.Normalize();

        // Rotar hacia la direccion
        float maxRotSpeed = _radiansRotSpeed * Time.fixedDeltaTime;
        Vector3 dir = Vector3.RotateTowards(transform.forward, desiredDir, maxRotSpeed, 0f);

        float maxSpeedChange = _acceleration * Time.fixedDeltaTime;
        float currentSpeed = Mathf.MoveTowards(CurrentSpeed, MaxSpeed, maxSpeedChange);
        _rb.velocity = dir * currentSpeed;

        _rb.rotation = Quaternion.LookRotation(dir);
    }

    public void ClearForces() => _rb.velocity = Vector3.zero;

    public void AccelerateTowardsTarget(Vector3 destination) => AccelerateTowards(destination - _rb.position);

    public void UseGravity(bool value) => _rb.useGravity = value;

    public void AddImpulse(Vector3 force) => _rb.velocity += force;

    void DrawSpeedArrow()
    {
        DrawArrow.ForGizmo(transform.position, Velocity, Color.green, 2f);
    }

}
