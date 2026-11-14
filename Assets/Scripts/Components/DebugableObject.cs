using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Events;
using Debug = UnityEngine.Debug;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Per-object debug switch for logs and gizmos.
/// Log, WarningLog and ErrorLog print only while canDebug is true.
/// </summary>
/// <remarks>
/// The log methods carry [Conditional("UNITY_EDITOR")].
/// Builds remove the calls together with the string arguments.
/// </remarks>
[DisallowMultipleComponent]
public class DebugableObject : MonoBehaviour
{
    /// <summary>Enables the logs and the gizmos of this object.</summary>
    [SerializeField] public bool canDebug = true;

    /// <summary>Gizmo actions drawn while canDebug is true or the object is selected.</summary>
    public UnityEvent gizmoDraw;

    private Action _lateUpdate;

    private void Awake()
    {
#if UNITY_EDITOR
        // Un objeto con canDebug apagado se debuggea mientras esta seleccionado.
        if (!canDebug)
        {
            _lateUpdate += () => canDebug = Selection.activeObject == gameObject;
            enabled = true;
        }
#else
        canDebug = false;
        enabled = false;
#endif
    }

    private void LateUpdate()
    {
#if UNITY_EDITOR
        _lateUpdate?.Invoke();
#endif
    }

    /// <summary>Adds an action to gizmoDraw.</summary>
    public void AddGizmoAction(Action x) => gizmoDraw.AddListener(new UnityAction(x));

    private void OnDrawGizmos()
    {
        if (!canDebug) return;
        gizmoDraw?.Invoke();
    }

    private void OnDrawGizmosSelected()
    {
        gizmoDraw?.Invoke();
    }

    /// <summary>Logs message with the name of the GameObject.</summary>
    [Conditional("UNITY_EDITOR")]
    public void Log(string message)
    {
        if (!canDebug) return;
        Debug.Log(gameObject.name + ": " + message);
    }

    /// <summary>Logs message as a warning with the name of the GameObject.</summary>
    [Conditional("UNITY_EDITOR")]
    public void WarningLog(string message)
    {
        if (!canDebug) return;
        Debug.LogWarning(gameObject.name + ": " + message);
    }

    /// <summary>Logs message as an error with the name of the GameObject.</summary>
    [Conditional("UNITY_EDITOR")]
    public void ErrorLog(string message)
    {
        if (!canDebug) return;
        Debug.LogError(gameObject.name + ": " + message);
    }
}
