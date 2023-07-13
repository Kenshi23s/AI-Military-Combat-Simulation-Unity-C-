using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Todos los metodos de esta clase se deberian llamar dentro del FixedUpdate
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(DebugableObject))]
[DisallowMultipleComponent]
public class NewPhysicsMovement : MonoBehaviour
{
    public enum MovementType
    {
        Grounded,
        Free
    }

    public enum AlignmentType
    {
        Velocity,
        DesiredMoveDirection,
        Mix,
        Target,
        Custom
    }

    public Vector3 Velocity { 
        get => Rigidbody.velocity;
        set => Rigidbody.velocity = value;
    }
    public Quaternion Rotation => Rigidbody.rotation;
    public float CurrentSpeed => Rigidbody.velocity.magnitude;

    public MovementType Movement = MovementType.Grounded;

    #region Speed
    public float Acceleration
    {
        get => _acceleration;
        set => _acceleration = Mathf.Max(0, value);
    }
    [SerializeField, Min(0)] 
    float _acceleration = 37.5f;

    public float MaxSpeed
    {
        get => _maxSpeed;
        set => _maxSpeed = Mathf.Max(0, value);
    }
    [SerializeField, Min(0)]
    float _maxSpeed = 8f;
    #endregion

    #region Grounding
    int _groundContactCount;
    Vector3 _groundContactNormal;
    [SerializeField] float _maxGroundAngle = 45f;
    public float MaxGroundAngle 
    {
        get => _maxGroundAngle;
        set 
        {
            _maxGroundAngle = Mathf.Clamp(value, 0, 90f);
            _minGroundDotProduct = Mathf.Cos(_maxGroundAngle * Mathf.Deg2Rad);
        }
    }
    float _minGroundDotProduct;

    public bool OnGround => _groundContactCount > 0;
    int _stepsSinceLastGrounded;

    // Snap To Ground Settings
    public bool GroundSnapping = true;

    [SerializeField, Min(0f)]
    float _groundProbeDistance = 0.5f;
    public float GroundProbeDistance 
    {
        get => _groundProbeDistance;
        set => _groundProbeDistance = Mathf.Max(0, value);
    }
    public LayerMask GroundMask = -1;
    #endregion

    #region Alignment
    public AlignmentType Alignment = AlignmentType.Velocity;



    public float AlignmentMix 
    {
        get => _alignmentMix;
        set => _alignmentMix = Mathf.Clamp01(value);
    }
    [SerializeField, Range(0, 1)]
    float _alignmentMix = 0.5f;
    public Transform AlignmentTarget;

    public Quaternion CustomAlignment
    {
        get => _customAlignment;
        set => _customAlignment = value.normalized;
    }

    Quaternion _customAlignment = Quaternion.identity;

    public float AlignTime
    {
        get => _alignTime;
        set
        {
            _alignTime = value;
            _hasAlignTime = !Mathf.Approximately(_alignTime, 0);

            if (_hasAlignTime)
                _radiansAlignSpeed = (180f / _alignTime) * Mathf.Deg2Rad;
        }
    }
    [SerializeField, Min(0), Tooltip("El tiempo maximo que puede llegar a tardar en alinearse")]
    float _alignTime = 0f;
    bool _hasAlignTime = false;

    float _radiansAlignSpeed;

    public bool FreezeXAlignment, FreezeYAlignment, FreezeZAlignment;

    #endregion

    #region Shortcuts
    public Vector3 Right => Rigidbody.rotation * Vector3.right;
    public Vector3 Up => Rigidbody.rotation * Vector3.up;
    public Vector3 Forward => Rigidbody.rotation * Vector3.forward;
    #endregion

    public Rigidbody Rigidbody { get; private set; }
    DebugableObject _debug;

    Vector3 _velocity;
    Quaternion _rotation;
    Vector3 _inputMoveDirection;

    private void OnValidate()
    {
        Rigidbody = GetComponent<Rigidbody>();
        MaxGroundAngle = _maxGroundAngle;
        AlignTime = _alignTime;
    }


    private void Awake()
    {
        OnValidate();
        Rigidbody = GetComponent<Rigidbody>();

        _debug = GetComponent<DebugableObject>();
        _debug.AddGizmoAction(DrawSpeedArrow);
    }


    Vector3 _desiredMoveDir;
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

    void AdjustVelocity() 
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

    void AdjustRotation() 
    {
        Quaternion targetRotation;

        // Conseguir targetRotation segun el metodo de alineacion.
        switch (Alignment)
        {
            case AlignmentType.Velocity:
                targetRotation = Quaternion.LookRotation(_velocity);
                break;
            case AlignmentType.DesiredMoveDirection:
                targetRotation = Quaternion.LookRotation(_desiredMoveDir);
                break;
            case AlignmentType.Mix:
                targetRotation = Quaternion.LookRotation(Vector3.Lerp(_velocity, _desiredMoveDir, _alignmentMix));
                break;
            case AlignmentType.Target:
                targetRotation = Quaternion.LookRotation(AlignmentTarget.position - Rigidbody.position);
                break;
            case AlignmentType.Custom:
                targetRotation = _customAlignment;
                break;
            default:
                targetRotation = _rotation;
                break;
        }

        // Freezear ejes... tiene que haber una manera mas optima de hacer esto, sin tener que conseguir 
        // eulerAngles y hacer estas idas y vueltas de conversiones
        Vector3 currentEulerAngles = _rotation.eulerAngles;
        Vector3 targetEulerAngles = targetRotation.eulerAngles;
        targetRotation.eulerAngles = new Vector3(
            FreezeXAlignment ? currentEulerAngles.x : targetEulerAngles.x,
            FreezeYAlignment ? currentEulerAngles.y : targetEulerAngles.y,
            FreezeZAlignment ? currentEulerAngles.z : targetEulerAngles.z
            );

        // Si tiene tiempo de alineacion, alinear de a poco
        if (_hasAlignTime)
            _rotation = Quaternion.RotateTowards(_rotation, targetRotation, _radiansAlignSpeed);
        // Si no, alinear directo.
        else
            _rotation = targetRotation;
    }

    public void AccelerateTowards(Vector3 inputDir) 
    {
        SendInput(inputDir);
    }

    public void FreeAccelerate()
    {
        float maxSpeedChange = _acceleration * Time.fixedDeltaTime;        
        _velocity = Vector3.MoveTowards(_velocity, _desiredMoveDir * MaxSpeed, maxSpeedChange);
    }

    public void SendInput(Vector3 targetDirection) 
    {
        _inputMoveDirection = targetDirection;
    }

    public void SendInput(Vector3 targetDirection, float targetSpeed) 
    {
        throw new System.Exception("El metodo 'SendInput(Vector3, float)' no esta implementado");
    }

    void GroundedAccelerate()
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

    void GetMoveDirection() 
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

    void EvaluateCollision(Collision collision) 
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

    void UpdateState()
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

    bool SnapToGround() 
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

    void ClearState()
    {
        if (Movement != MovementType.Grounded)
            return;

        _groundContactCount = 0;
        _groundContactNormal = Vector3.zero;
    }

    #region HelperMethods
    public void AccelerateTowardsTarget(Vector3 destination) => AccelerateTowards(destination - Rigidbody.position);

    public void ClearForces() => Rigidbody.velocity = Vector3.zero;

    public void UseGravity(bool value) => Rigidbody.useGravity = value;

    public void AddImpulse(Vector3 force) => Rigidbody.velocity += force;
    #endregion

    void DrawSpeedArrow()
    {
        DrawArrow.ForGizmo(transform.position, Velocity, Color.green, 2f);
    }

}
