using UnityEditor;
using UnityEngine;

public class CirclePlacerEditorWindow : EditorWindow
{
    private float radius = 5f;
    private bool lookAtCenter = false;
    private Vector3 rotationOffset = Vector3.zero;

    [MenuItem("Tools/Circle Placer")]
    public static void ShowWindow()
    {
        GetWindow<CirclePlacerEditorWindow>("Circle Placer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Circle Placer Settings", EditorStyles.boldLabel);

        // Radius input
        radius = EditorGUILayout.FloatField("Radius", radius);

        // Look at center toggle
        lookAtCenter = EditorGUILayout.Toggle("Look At Center", lookAtCenter);

        // Rotation offset input
        rotationOffset = EditorGUILayout.Vector3Field("Rotation Offset", rotationOffset);

        // Button to arrange objects in a circle
        if (GUILayout.Button("Place Objects In Circle"))
        {
            PlaceObjectsInCircle();
        }
    }

    private void PlaceObjectsInCircle()
    {
        // Get selected objects in the editor
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("No objects selected!");
            return;
        }

        // Calculate the angle between each object
        float angleStep = 360f / selectedObjects.Length;

        // Calculate the center point of the objects
        Vector3 center = Vector3.zero;
        foreach (GameObject obj in selectedObjects)
        {
            center += obj.transform.position;
        }
        center /= selectedObjects.Length;

        // Place each object in a circular arrangement
        for (int i = 0; i < selectedObjects.Length; i++)
        {
            GameObject obj = selectedObjects[i];

            // Calculate the angle and position for this object
            float angle = i * angleStep;
            float radianAngle = angle * Mathf.Deg2Rad;

            Vector3 newPosition = new Vector3(
                Mathf.Cos(radianAngle) * radius + center.x,
                obj.transform.position.y,  // Keep original Y position
                Mathf.Sin(radianAngle) * radius + center.z
            );

            // Set the new position
            obj.transform.position = newPosition;

            // Rotate the object to face the center if the option is enabled
            if (lookAtCenter)
            {
                obj.transform.LookAt(center);
            }

            // Apply additional rotation offset
            obj.transform.rotation *= Quaternion.Euler(rotationOffset);
        }
    }
}
