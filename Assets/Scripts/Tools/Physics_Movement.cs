using System;
using System.Collections;
using UnityEngine;

#region Components
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(DebugableObject))]
#endregion
[DisallowMultipleComponent]
//se usa para hacer movimiento "Realista"(lo usan las IA del juego, pero no esta limitado a ellas)
public class Physics_Movement : MonoBehaviour
{
    public Rigidbody _rb { get; private set; }
    DebugableObject _debug;

   public bool isFalling => _rb.velocity.y <= 0; 

   
    public Vector3 _velocity { get; private set; }

    [SerializeField,Range(1,100)]
    float _maxForce;
    public float maxForce
    {
        get => _maxForce;
        set
        {
            float aux = _maxForce;
            _maxForce =  Mathf.Clamp(value, 0, maxSpeed);
            TryDebug($"MAX FORCE cambio de {aux} a {_maxForce}");
        }
    } 

    [SerializeField,Range(1,100)]
    float _maxSpeed;
    public float maxSpeed 
    {
        get => _maxSpeed;

        set
        {
            _maxSpeed = Mathf.Max(0, value);
            TryDebug($"MAXSPEED cambio de {this._maxSpeed} a {_maxSpeed}");
            maxForce = maxForce;
        }
    }

    [SerializeField, Range(0f, 200)]
    float _steeringForce;
    public float steeringForce
    {
        get => _steeringForce;
        set 
        {
            float aux = _steeringForce;
            _steeringForce = MathF.Max(0, value);
            TryDebug($"STEERING FORCE cambio de {aux} a {_steeringForce}");
        }
    }

    private void Awake()
    {
       
        _debug = GetComponent<DebugableObject>(); _debug.AddGizmoAction(MovementGizmos);
        _rb = GetComponent<Rigidbody>();
    }

    public void RemoveForces()
    {
        _rb.velocity = Vector3.zero;
        _debug.Log("Se removieron todas las fuerzas");
    }

    public void AddForce(Vector3 force)
    {
        force = CalculateSteering(force);//estara bien asi?
        _velocity = Vector3.ClampMagnitude(_rb.velocity + force * Time.fixedDeltaTime, _maxSpeed);
        _rb.velocity = _velocity;
    }

    public void LookTowardsVelocity() 
    {

        _rb.rotation = Quaternion.LookRotation(Vector3.RotateTowards(transform.forward, new Vector3(_velocity.x, 0, _velocity.z), 180f * Mathf.Deg2Rad * Time.deltaTime, 0), Vector3.up);
    }

    public void LookAt(Vector3 position) 
    {
        Vector3 forward = position - _rb.position;
        forward.y = 0;

        _rb.rotation = Quaternion.LookRotation(Vector3.RotateTowards(transform.forward, forward, 180f * Mathf.Deg2Rad * Time.deltaTime, 0), Vector3.up);
    }

    public void AddImpulse(Vector3 force) => _rb.AddForce(force, ForceMode.Impulse);
   
     
        
    void TryDebug(string msg)
    {
        if (_debug != null) { _debug.Log(msg); }
        _debug.Log($"MAXSPEED cambio de {this._maxSpeed} a {_maxSpeed}");
    }

 

   

  
    void MovementGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(_rb.position, _rb.position + _velocity);
    }

    public Vector3 Seek(Vector3 targetSeek)
    {
        Vector3 desired = targetSeek - transform.position;
        desired.Normalize();
        return desired * maxForce;
    }


    public Vector3 Arrive(Vector3 actualTarget, float arriveRadius)
    {
        Vector3 desired = actualTarget - transform.position;
        float dist = desired.magnitude;
        desired.Normalize();
        if (dist <= arriveRadius)
            desired *= maxSpeed * (dist / arriveRadius);
        else
            desired *= maxSpeed;
        return desired;
    }
    private void OnValidate()
    {
        _maxForce = Mathf.Min(_maxForce, _maxSpeed);
    }

    public Vector3 CalculateSteering(Vector3 desired) => Vector3.ClampMagnitude(desired - _velocity * steeringForce, maxSpeed);
}
