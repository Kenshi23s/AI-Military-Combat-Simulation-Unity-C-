using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rigidbody movement with acceleration, maximum speed, ground snapping and alignment.
/// AccelerateTowards sets the input direction that FixedUpdate applies.
/// </summary>
/// <remarks>The movement methods are meant to be called from FixedUpdate.</remarks>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(DebugableObject))]
[DisallowMultipleComponent]
public class NewPhysicsMovement : MonoBehaviour
{
    /// <summary>Movement mode.</summary>
    public MovementType Movement = MovementType.Grounded;

    // Snap To Ground Settings
    public bool GroundSnapping = true;
    public LayerMask GroundMask = -1;

    public AlignmentType Alignment = AlignmentType.Velocity;
    public Transform AlignmentTarget;

    public bool FreezeXAlignment, FreezeYAlignment, FreezeZAlignment;
    [SerializeField, Min(0)] 
    private float _acceleration = 37.5f;
    [SerializeField, Min(0)]
    private float _maxSpeed = 8f;
    [SerializeField] private float _maxGroundAngle = 45f;

    [SerializeField, Min(0f)]
    private float _groundProbeDistance = 0.5f;
    [SerializeField, Range(0, 1)]
    private float _alignmentMix = 0.5f;
    [SerializeField, Min(0), Tooltip("El tiempo maximo que puede llegar a tardar en alinearse")]
    private float _alignTime = 0f;

    private int _groundContactCount;
    private Vector3 _groundContactNormal;
    private float _minGroundDotProduct;
    private int _stepsSinceLastGrounded;

    private Quaternion _customAlignment = Quaternion.identity;
    private bool _hasAlignTime = false;

    // Degrees per second that cover a 180 degree turn in _alignTime seconds.
    private float _alignDegreesPerSecond;
    private DebugableObject _debug;

    private Vector3 _velocity;
    private Quaternion _rotation;
    private Vector3 _inputMoveDirection;

    private Vector3 _desiredMoveDir;

    /// <summary>Velocity of the Rigidbody.</summary>
    public Vector3 Velocity {
        get => Rigidbody.velocity;
        set => Rigidbody.velocity = value;
    }

    /// <summary>Rotation of the Rigidbody.</summary>
    public Quaternion Rotation => Rigidbody.rotation;

    /// <summary>Magnitude of Velocity.</summary>
    public float CurrentSpeed => Rigidbody.velocity.magnitude;

    /// <summary>Velocity change per second. The minimum is 0.</summary>
    public float Acceleration
    {
        get => _acceleration;
        set => _acceleration = Mathf.Max(0, value);
    }

    /// <summary>Maximum speed. The minimum is 0.</summary>
    public float MaxSpeed
    {
        get => _maxSpeed;
        set => _maxSpeed = Mathf.Max(0, value);
    }
    /// <summary>Steepest slope, in degrees, that counts as ground. From 0 to 90.</summary>
    public float MaxGroundAngle
    {
        get => _maxGroundAngle;
        set 
        {
            _maxGroundAngle = Mathf.Clamp(value, 0, 90f);
            _minGroundDotProduct = Mathf.Cos(_maxGroundAngle * Mathf.Deg2Rad);
        }
    }

    /// <summary>true while the body touches ground.</summary>
    public bool OnGround => _groundContactCount > 0;

    /// <summary>Length of the downward raycast that snaps the body to the ground. The minimum is 0.</summary>
    public float GroundProbeDistance
    {
        get => _groundProbeDistance;
        set => _groundProbeDistance = Mathf.Max(0, value);
    }

    /// <summary>Weight of the desired direction against the velocity in AlignmentType.Mix. From 0 to 1.</summary>
    public float AlignmentMix
    {
        get => _alignmentMix;
        set => _alignmentMix = Mathf.Clamp01(value);
    }

    /// <summary>Rotation used by AlignmentType.Custom.</summary>
    public Quaternion CustomAlignment
    {
        get => _customAlignment;
        set => _customAlignment = value.normalized;
    }

    /// <summary>Seconds a 180 degree turn takes. 0 aligns in a single step.</summary>
    public float AlignTime
    {
        get => _alignTime;
        set
        {
            _alignTime = Mathf.Max(0, value);
            _hasAlignTime = !Mathf.Approximately(_alignTime, 0);

            if (_hasAlignTime)
                _alignDegreesPerSecond = 180f / _alignTime;
        }
    }

    /// <summary>Right axis of the Rigidbody.</summary>
    public Vector3 Right => Rigidbody.rotation * Vector3.right;

    /// <summary>Up axis of the Rigidbody.</summary>
    public Vector3 Up => Rigidbody.rotation * Vector3.up;

    /// <summary>Forward axis of the Rigidbody.</summary>
    public Vector3 Forward => Rigidbody.rotation * Vector3.forward;

    /// <summary>The Rigidbody that moves.</summary>
    public Rigidbody Rigidbody { get; private set; }

    /// <summary>Grounded moves along the ground plane. Free moves in any direction.</summary>
    public enum MovementType
    {
        Grounded,
        Free
    }

    /// <summary>Direction the body rotates toward.</summary>
    public enum AlignmentType
    {
        Velocity,
        DesiredMoveDirection,
        Mix,
        Target,
        Custom
    }

    private void Awake()
    {
        Rigidbody = GetComponent<Rigidbody>();

        _debug = GetComponent<DebugableObject>();
        _debug.AddGizmoAction(DrawSpeedArrow);

        // The setters compute _minGroundDotProduct and the align speed from the serialized values.
        MaxGroundAngle = _maxGroundAngle;
        AlignTime = _alignTime;
    }
    private void FixedUpdate()
    {
        UpdateState();

        GetMoveDirection();

        AdjustVelocity();

        AdjustRotation();

        Rigidbody.velocity = _velocity;
        Rigidbody.rotation = _rotation;

        ClearState();
    }

    private void AdjustVelocity() 
    {
        switch (Movement)
        {
            case MovementType.Grounded:
                GroundedAccelerate();
                break;
            case MovementType.Free:
                FreeAccelerate();
                break;
            default:
                break;
        }
    }

    private void AdjustRotation() 
    {
        Quaternion desiredRotation = default;

        // Conseguir targetRotation segun el metodo de alineacion.
        switch (Alignment)
        {
            case AlignmentType.Velocity:
                if (_velocity != Vector3.zero)
                    desiredRotation = Quaternion.LookRotation(_velocity);
                break;
            case AlignmentType.DesiredMoveDirection:
                if (_desiredMoveDir != Vector3.zero)
                    desiredRotation = Quaternion.LookRotation(_desiredMoveDir);
                break;
            case AlignmentType.Mix:
                Vector3 mixDir = Vector3.Lerp(_velocity.normalized, _desiredMoveDir, _alignmentMix);
                if (mixDir != Vector3.zero)
                    desiredRotation = Quaternion.LookRotation(mixDir);
                break;
            case AlignmentType.Target:
                Vector3 targetDir = AlignmentTarget.position - Rigidbody.position;
                if (targetDir != Vector3.zero)
                    desiredRotation = Quaternion.LookRotation(targetDir);
                break;
            case AlignmentType.Custom:
                desiredRotation = _customAlignment;
                break;
            default:
                desiredRotation = _rotation;
                break;
        }

        // Freezear ejes... tiene que haber una manera mas optima de hacer esto, sin tener que conseguir 
        // eulerAngles y hacer estas idas y vueltas de conversiones
        Vector3 desiredEulerAngles = desiredRotation.eulerAngles;
        desiredRotation.eulerAngles = new Vector3(
            FreezeXAlignment ? 0 : desiredEulerAngles.x,
            FreezeYAlignment ? 0 : desiredEulerAngles.y,
            FreezeZAlignment ? 0 : desiredEulerAngles.z
            );

        // Si tiene tiempo de alineacion, alinear de a poco
        if (_hasAlignTime)
            _rotation = Quaternion.RotateTowards(_rotation, desiredRotation, _alignDegreesPerSecond * Time.fixedDeltaTime);
        // Si no, alinear directo.
        else
            _rotation = desiredRotation;
    }

    /// <summary>Sets inputDir as the direction FixedUpdate accelerates toward.</summary>
    public void AccelerateTowards(Vector3 inputDir)
    {
        SendInput(inputDir);
    }

    /// <summary>Moves the velocity toward the desired direction at MaxSpeed, limited by Acceleration.</summary>
    public void FreeAccelerate()
    {
        float maxSpeedChange = _acceleration * Time.fixedDeltaTime;
        _velocity = Vector3.MoveTowards(_velocity, _desiredMoveDir * MaxSpeed, maxSpeedChange);
    }

    /// <summary>Sets targetDirection as the direction FixedUpdate accelerates toward.</summary>
    public void SendInput(Vector3 targetDirection)
    {
        _inputMoveDirection = targetDirection;
    }

    private void GroundedAccelerate()
    {
        // Aplanar la direccion
        Vector3 flattenedDesiredVelocity = _desiredMoveDir.ProjectDirectionOnPlane(Vector3.up) * MaxSpeed;

        Vector3 xAxis = transform.right.ProjectDirectionOnPlane(_groundContactNormal);
        Vector3 zAxis = transform.forward.ProjectDirectionOnPlane(_groundContactNormal);

        // Conseguir valores de movimiento hacia adelante y hacia el costado actuales
        Vector2 currentXZ = new Vector2(Vector3.Dot(_velocity, xAxis), Vector3.Dot(_velocity, zAxis));
        // Conseguir valores a los que apuntamos
        Vector2 targetXZ = new Vector2(Vector3.Dot(flattenedDesiredVelocity, transform.right), Vector3.Dot(flattenedDesiredVelocity, transform.forward));

        // Acelerar
        float maxSpeedChange = _acceleration * Time.fixedDeltaTime;
        Vector2 newXZ = Vector2.MoveTowards(currentXZ, targetXZ, maxSpeedChange);
        Vector2 deltaXZ = newXZ - currentXZ;

        _velocity += xAxis * deltaXZ.x + zAxis * deltaXZ.y;

        // Prevenir deslizamiento hacia abajo en pendientes cuando estamos quietos.
        // Practicamente cancelamos la gravedad
        if (OnGround && _velocity.sqrMagnitude < 0.01f)
            _velocity -= Physics.gravity * Time.fixedDeltaTime;
    }

    private void OnCollisionStay(Collision collision)
    {
        EvaluateCollision(collision);
    }

    private void GetMoveDirection() 
    {
        switch (Movement)
        {
            case MovementType.Grounded:
                _desiredMoveDir = _inputMoveDirection.ProjectDirectionOnPlane(_groundContactNormal);
                break;
            case MovementType.Free:
                _desiredMoveDir = _inputMoveDirection.normalized;
                break;
            default:
                break;
        }
    }

    private void EvaluateCollision(Collision collision) 
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            // minGroundDotProduct es un calculo que proviene de maxGroundAngle.
            // Aca se esta comparando si el ángulo entre el jugador y la superficie es mayor o menor a maxGroundAngle.
            Vector3 normal = collision.GetContact(i).normal;

            if (normal.y >= _minGroundDotProduct)
            {
                _groundContactCount++;

                // En el caso que de haya multiples contactos con el piso. ¿Qué dirección es la mejor? No hay una.
                // Tiene más sentido combinarlas a todas en una sola normal que represente un plano de tierra promedio.
                // Para hacer eso tenemos que acumular los vectores normales.
                _groundContactNormal += normal;
            }
        }
    }

    private void UpdateState()
    {

        _velocity = Rigidbody.velocity;
        _rotation = Rigidbody.rotation;

        if (Movement != MovementType.Grounded)
            return;

        _stepsSinceLastGrounded++;

        if (OnGround || SnapToGround())
        {
            _stepsSinceLastGrounded = 0;
            // Normalizar la acumulacion de normales de contacto para convertirla en un vector normal adecuado.
            if (_groundContactCount > 1)
            {
                _groundContactNormal.Normalize();
            }
        }
        else
        {
            _groundContactNormal = Vector3.up;
        }
    }

    /// <summary>Stops the Rigidbody and freezes it in place.</summary>
    public void DeactivateMovement()
    {
        _acceleration = 0;
        Rigidbody.Sleep();   
        Rigidbody.constraints = RigidbodyConstraints.FreezeAll;
        Rigidbody.velocity = Vector3.zero;
    }

    private bool SnapToGround() 
    {
        if (!GroundSnapping) 
            return false;

        if (_stepsSinceLastGrounded > 1) 
            return false;

        // Solo queremos snappear al piso cuando hay suelo debajo al que adherirse.
        if (!Physics.Raycast(Rigidbody.position, Vector3.down, out RaycastHit hit, _groundProbeDistance, GroundMask))
            return false;

        // Si el Raycast golpeó algo, entonces debemos verificar si cuenta como suelo.
        if (hit.normal.y < _minGroundDotProduct)
            return false;

        // Si no hemos abortado en este punto, simplemente perdimos el contacto con el suelo,
        // pero todavía estamos sobre el suelo, por lo que nos snappeamos a él.
        _groundContactCount = 1;
        _groundContactNormal = hit.normal;

        // Ahora nos consideramos grounded, aunque todavía estamos en el aire.
        // El siguiente paso es ajustar nuestra velocidad para alinearnos con el suelo.
        float speed = _velocity.magnitude;
        float dot = Vector3.Dot(_velocity, hit.normal);

        // En esta instancia todavía estamos flotando sobre el suelo, pero la gravedad se encargará de bajarnos hacia la superficie.
        // De hecho, la velocidad ya podría apuntar un poco hacia abajo, en cuyo caso, realinearla retrasaría la convergencia hacia el suelo.
        // Por lo tanto, solo debemos ajustar la velocidad cuando el producto escalar de esta y la superficie normal sea positivo (i.e. cuando hay algo de velocidad hacia abajo).
        if (dot > 0f)
            _velocity = (_velocity - hit.normal * dot).normalized * speed;

        return true;
    }

    private void ClearState()
    {
        if (Movement != MovementType.Grounded)
            return;

        _groundContactCount = 0;
        _groundContactNormal = Vector3.zero;
    }

    #region HelperMethods
    /// <summary>Accelerates toward destination.</summary>
    public void AccelerateTowardsTarget(Vector3 destination) => AccelerateTowards(destination - Rigidbody.position);

    /// <summary>Sets Velocity to zero.</summary>
    public void ClearForces() => Rigidbody.velocity = Vector3.zero;

    /// <summary>Turns the gravity of the Rigidbody on or off.</summary>
    public void UseGravity(bool value) => Rigidbody.useGravity = value;

    /// <summary>Adds force to Velocity.</summary>
    public void AddImpulse(Vector3 force) => Rigidbody.velocity += force;
    #endregion

    private void DrawSpeedArrow()
    {
        DrawArrow.ForGizmo(transform.position, Velocity, Color.green, 2f);
    }
}
