using UnityEngine;
using UnityEditor;

public class RectanglePlacerEditorWindow : EditorWindow
{
    // Variables for prefab, grid size, and spacing
    public GameObject prefab;
    public int rows = 5;
    public int columns = 5;
    public float xSpacing = 1.0f;
    public float zSpacing = 1.0f;
    public Vector2 startPosition = Vector2.zero;

    // Adding a menu item to open the window
    [MenuItem("Tools/Rectangle Prefab Placer")]
    public static void ShowWindow()
    {
        GetWindow<RectanglePlacerEditorWindow>("Rectangle Prefab Placer");
    }

    void OnGUI()
    {
        GUILayout.Label("Prefab Placer Settings", EditorStyles.boldLabel);

        // Fields for prefab, grid size, and spacing
        prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), false);
        rows = EditorGUILayout.IntField("Rows", rows);
        columns = EditorGUILayout.IntField("Columns", columns);
        xSpacing = EditorGUILayout.FloatField("X Spacing", xSpacing);
        zSpacing = EditorGUILayout.FloatField("Z Spacing", zSpacing);
        startPosition = EditorGUILayout.Vector2Field("Start Position (X,Z)", startPosition);

        // Place prefabs when the button is clicked
        if (GUILayout.Button("Place Prefabs"))
        {
            if (prefab != null)
            {
                PlacePrefabs();
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Please assign a prefab!", "OK");
            }
        }
    }

    // Method to place prefabs in a grid
    private void PlacePrefabs()
    {
        // Disable automatic scene refresh to reduce the performance hit
        bool previousAutoRefresh = EditorPrefs.GetBool("SceneView.autoRepaintOnSceneChange");
        EditorPrefs.SetBool("SceneView.autoRepaintOnSceneChange", false);
        try
        {
            // Create a parent object to hold all the prefabs, to group them in the hierarchy
            GameObject parentObject = new GameObject("Prefab Grid");

            // Use an array to collect created objects for batching the Undo
            GameObject[] createdObjects = new GameObject[rows * columns];
            int index = 0;

            // Batch the creation of prefabs
            for (int x = 0; x < rows; x++)
            {
                for (int z = 0; z < columns; z++)
                {
                    Vector3 position = new Vector3(startPosition.x + x * xSpacing, 0, startPosition.y + z * zSpacing);

                    // Instantiate the prefab at the calculated position
                    GameObject newPrefab = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parentObject.transform);
                    newPrefab.transform.position = position;

                    // Add to the created objects list for undo batching
                    createdObjects[index] = newPrefab;
                    index++;
                }
            }

            // Batch Undo registration after all objects are placed
            Undo.RegisterCreatedObjectUndo(parentObject, "Placed Prefabs");

            // Optionally select the created parent in the hierarchy
            Selection.activeGameObject = parentObject;
        }
        finally
        {
            // Re-enable automatic scene refresh and force a refresh
            EditorPrefs.SetBool("SceneView.autoRepaintOnSceneChange", previousAutoRefresh);
            SceneView.RepaintAll();
        }
    }
}
