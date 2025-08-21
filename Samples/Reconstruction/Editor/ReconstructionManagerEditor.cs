using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ReconstructionManager))]
public class ReconstructionManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw the deviceType field first
        var deviceTypeProp = serializedObject.FindProperty("deviceType");
        EditorGUILayout.PropertyField(deviceTypeProp);

        // Draw relevant settings depending on enum
        var deviceType = (DeviceType)deviceTypeProp.enumValueIndex;

        if (deviceType == DeviceType.AR)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("arCameraManager"));
        }
        else if (deviceType == DeviceType.VR)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("webcamTextureManager"));
        }

        // Draw the rest normally
        EditorGUILayout.PropertyField(serializedObject.FindProperty("deviceInfo"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("captureToggle"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("drawDeviceInfo"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("drawCameraFrustrum"));

        serializedObject.ApplyModifiedProperties();
    }
}
