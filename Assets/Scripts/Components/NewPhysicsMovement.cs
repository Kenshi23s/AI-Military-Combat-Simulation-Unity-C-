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
        set => _acceleration = Mathf.Max(0, value);
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

    public bool SnapToGround = true;

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

        UpdateState();

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
        rotation = Quaternion.LookRotation(lookDir);
        // 


        Vector3 xAxis = transform.right.ProjectDirectionOnPlane(groundContactNormal);
        Vector3 zAxis = transform.forward.ProjectDirectionOnPlane(groundContactNormal);

        Vector3 desiredVelocity = lookDir * MaxSpeed;
        Vector2 targetXZ = new Vector2(Vector3.Dot(desiredVelocity, transform.right), Vector3.Dot(desiredVelocity, transform.forward));
        Vector2 currentXZ = new Vector2(Vector3.Dot(velocity, xAxis), Vector3.Dot(velocity, zAxis));

        float maxSpeedChange = _acceleration * Time.fixedDeltaTime;

        Vector2 newXZ = Vector2.MoveTowards(currentXZ, targetXZ, maxSpeedChange);
        Vector2 deltaXZ = newXZ - currentXZ;

        velocity += xAxis * deltaXZ.x + zAxis * deltaXZ.y;

        // Prevenir deslizamiento hacia abajo en pendientes.
        if (OnGround && velocity.sqrMagnitude < 0.01f)
        {
            velocity += Physics.gravity * Time.deltaTime;
        }
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

    int groundContactCount;
    Vector3 groundContactNormal;
    float maxGroundAngle = 45f;

    private void OnCollisionStay(Collision collision)
    {
        EvaluateCollision(collision);
    }

    void EvaluateCollision(Collision collision) 
    {
        float minDot = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
        for (int i = 0; i < collision.contactCount; i++)
        {
            // minGroundDotProduct es un calculo que proviene de maxGroundAngle.
            // Aca se esta comparando si el ángulo entre el jugador y la superficie es mayor o menor a maxGroundAngle.
            Vector3 normal = collision.GetContact(i).normal;

            if (normal.y >= minDot)
            {
                groundContactCount++;

                // En el caso que de haya multiples contactos con el piso. ¿Qué dirección es la mejor? No hay una.
                // Tiene más sentido combinarlas a todas en una sola normal que represente un plano de tierra promedio.
                // Para hacer eso tenemos que acumular los vectores normales.
                groundContactNormal += normal;
            }
        }
    }


    bool OnGround => groundContactCount > 0;

    void UpdateState()
    {
        velocity = rb.velocity;
        rotation = rb.rotation;

        if (OnGround)
        {
            // Normalizar la acumulacion de normales de contacto para convertirla en un vector normal adecuado.
            if (groundContactCount > 1)
            {
                groundContactNormal.Normalize();
            }
        }
        else
        {
            groundContactNormal = Vector3.up;
        }
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
