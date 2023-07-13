using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using InternalEditorUtility = UnityEditorInternal.InternalEditorUtility;
using static NewPhysicsMovement;

[CustomEditor(typeof(NewPhysicsMovement))]
public class NewPhysicsMovementEditor : Editor
{
    NewPhysicsMovement myTarget;

    public override void OnInspectorGUI()
    {
        myTarget = (NewPhysicsMovement)target;

        myTarget.Acceleration = EditorGUILayout.FloatField("Acceleration", myTarget.Acceleration);
        myTarget.MaxSpeed = EditorGUILayout.FloatField("Max Speed", myTarget.MaxSpeed);

        myTarget.Movement = (MovementType)EditorGUILayout.EnumPopup("Movement Type", myTarget.Movement);

        EditorGUI.indentLevel++;
        switch (myTarget.Movement)
        {
            case MovementType.Grounded:
                ShowGroundedMovementGUI();
                break;
            case MovementType.Free:
                ShowFreeMovementGUI();
                break;
            default:
                break;
        }
        EditorGUI.indentLevel--;

        myTarget.Alignment = (AlignmentType)EditorGUILayout.EnumPopup("Alignment Type", myTarget.Alignment);

        EditorGUI.indentLevel++;
        switch (myTarget.Alignment)
        {
            case AlignmentType.Velocity:
                break;
            case AlignmentType.DesiredMoveDirection:
                break;
            case AlignmentType.Mix:
                myTarget.AlignmentMix = EditorGUILayout.Slider(myTarget.AlignmentMix, 0, 1);
                break;
            case AlignmentType.Target:
                myTarget.AlignmentTarget = EditorGUILayout.ObjectField("Target", myTarget.AlignmentTarget, typeof(Transform), true) as Transform;
                break;
            case AlignmentType.Custom:
                break;
            default:
                break;
        }
        EditorGUI.indentLevel--;

        EditorGUILayout.LabelField("Freeze Alignment");
        EditorGUI.indentLevel++;
        myTarget.FreezeXAlignment = EditorGUILayout.Toggle("X", myTarget.FreezeXAlignment);
        myTarget.FreezeYAlignment = EditorGUILayout.Toggle("Y", myTarget.FreezeYAlignment);
        myTarget.FreezeZAlignment = EditorGUILayout.Toggle("Z", myTarget.FreezeZAlignment);
        EditorGUI.indentLevel--;

        if (GUI.changed) 
            EditorUtility.SetDirty(myTarget);
    }

    void ShowGroundedMovementGUI() 
    {
        myTarget.MaxGroundAngle = EditorGUILayout.Slider("Max Ground Angle", myTarget.MaxGroundAngle, 0, 90f);
        myTarget.GroundSnapping = EditorGUILayout.Toggle("Snap To Ground", myTarget.GroundSnapping);

        if (myTarget.GroundSnapping)
        {
            EditorGUI.indentLevel++;
            myTarget.GroundProbeDistance = EditorGUILayout.FloatField("Ground Distance", myTarget.GroundProbeDistance);
            myTarget.GroundMask = EditorGUILayout.MaskField("Ground Mask", myTarget.GroundMask, InternalEditorUtility.layers);
            EditorGUI.indentLevel--;
        }
    }
    void ShowFreeMovementGUI() { }
}
