using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using Unity.Mathematics;

public class SaveChildPositionsToJson : EditorWindow
{
    private bool saveRotations = false;

    [MenuItem("Tools/Save Child Positions to JSON")]
    public static void ShowWindow()
    {
        GetWindow<SaveChildPositionsToJson>("Save Child Positions to JSON");
    }

    private void OnGUI()
    {
        saveRotations = EditorGUILayout.Toggle("Save Rotations", saveRotations);

        if (GUILayout.Button("Save Child Positions"))
        {
            SavePositions();
        }
    }

    private void SavePositions()
    {
        GameObject selectedObject = Selection.activeGameObject;

        if (selectedObject == null)
        {
            Debug.LogWarning("No object selected. Please select a GameObject in the scene.");
            return;
        }

        List<Vector3> childPositions = new List<Vector3>();
        List<Vector3> childRotations = new List<Vector3>();

        foreach (Transform child in selectedObject.transform)
        {
            childPositions.Add(child.position);
            if (saveRotations) childRotations.Add(child.rotation.eulerAngles);
        }

        string json = JsonUtility.ToJson(new PositionList(childPositions, childRotations), true);

        string path = EditorUtility.SaveFilePanel("Save JSON File", "", "ChildPositions.json", "json");

        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, json);
            Debug.Log("Child positions saved to " + path);
        }
    }

    [System.Serializable]
    private class PositionList
    {
        public List<Vector3> positions;
        public List<Vector3> rotations;

        public PositionList(List<Vector3> positions, List<Vector3> rotations)
        {
            this.positions = positions;
            this.rotations = rotations;
        }
    }
}
