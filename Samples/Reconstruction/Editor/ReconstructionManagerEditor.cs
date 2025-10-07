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

        switch (deviceType)
        {
            case DeviceType.AR:
                EditorGUILayout.LabelField("AR Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("arCameraManager"));
                break;
            case DeviceType.AR_EVAL:
                EditorGUILayout.LabelField("AR Eval Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("arCameraManager"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("captureCamera"));
                break;
            case DeviceType.VR:
                EditorGUILayout.LabelField("VR Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("webcamTextureManager"));
                break;
            case DeviceType.VR_EVAL:
                EditorGUILayout.LabelField("VR Eval Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("captureCamera"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("webcamTextureManager"));
                break;
        }

        // Draw the rest normally
        EditorGUILayout.LabelField("UI Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("deviceInfo"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("captureToggle"));

        EditorGUILayout.LabelField("Debug Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("drawDeviceInfo"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("drawCameraFrustrum"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("useReplay"));

        serializedObject.ApplyModifiedProperties();
    }
}
