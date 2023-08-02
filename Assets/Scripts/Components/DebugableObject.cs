using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class DebugableObject : MonoBehaviour
{
    [SerializeField]public bool canDebug = true;
    public UnityEvent gizmoDraw;

    Action _lateUpdate;
    private void Awake()
    {

#if UNITY_EDITOR
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


    public void AddGizmoAction(Action x) => gizmoDraw.AddListener(new UnityAction(x));

    void OnDrawGizmos()
    {
        if (!canDebug) return;
        gizmoDraw?.Invoke();
    }

    public void Log(string message)
    {
        if (!canDebug) return;
        Debug.Log(gameObject.name+": " +message);
    }

    public void WarningLog(string message)
    {
        if (!canDebug) return;
        Debug.LogWarning(gameObject.name + ": " + message);
    }

    public void ErrorLog(string message)
    {
        if (!canDebug) return;
        Debug.LogError(gameObject.name + ": " + message);
    }

    private void OnDrawGizmosSelected()
    {
        gizmoDraw?.Invoke();
    }
}

