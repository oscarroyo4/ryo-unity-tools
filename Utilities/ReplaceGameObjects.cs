using UnityEditor;
using UnityEngine;

public class ReplaceGameObjects : EditorWindow
{
    private GameObject Prefab;

    [MenuItem("Tools/Replace GameObjects")]
    public static void ShowWindow()
    {
        GetWindow<ReplaceGameObjects>("Replace Tool");
    }

    private void OnGUI()
    {
        Prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", Prefab, typeof(GameObject), false);

        if (Selection.gameObjects.Length == 0)
        {
            GUILayout.Label("Select some GameObjects in the hierarchy to replace.");
            return;
        }

        if (Prefab != null)
        {
            if (GUILayout.Button($"Replace Selection With Prefab {Prefab.name}"))
            {
                Replace();
            }
        }
        else
        {
            GUI.enabled = false;
            GUILayout.Button("Replace Selection With Prefab {none}");
            GUI.enabled = true;

            var oldColor = GUI.color;
            GUI.color = Color.yellow;
            GUILayout.Label("Select a Prefab above first to replace any selection.");
            GUI.color = oldColor;
        }
    }

    private void Replace()
    {
        // get all GameObjects of current selection in Editor
        GameObject[] selectedObjects = Selection.gameObjects;

        // replace each GameObject of selectedObjects with the prefab "Prefab" instantiated at the same position and rotation
        foreach (GameObject selectedObject in selectedObjects)
        {
            GameObject newObject = PrefabUtility.InstantiatePrefab(Prefab) as GameObject;
            newObject.transform.position = selectedObject.transform.position;
            newObject.transform.rotation = selectedObject.transform.rotation;
            newObject.transform.parent = selectedObject.transform.parent;
            DestroyImmediate(selectedObject);
        }
    }
}