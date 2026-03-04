using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class LightProbePlacer : EditorWindow
{
    private List<GameObject> objectsToProcess = new List<GameObject>();
    private float minDistance = 0.5f;
    private float normalOffset = 0.1f;
    private float extraProbeSpacing = 0.5f;
    
    [MenuItem("Tools/Auto Light Probe Placer")]
    public static void ShowWindow()
    {
        GetWindow<LightProbePlacer>("Light Probe Placer");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Add Objects to Process", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Add Selected Objects"))
        {
            foreach (GameObject obj in Selection.gameObjects)
            {
                if (!objectsToProcess.Contains(obj))
                    objectsToProcess.Add(obj);
            }
        }

        for (int i = 0; i < objectsToProcess.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            objectsToProcess[i] = (GameObject)EditorGUILayout.ObjectField(objectsToProcess[i], typeof(GameObject), true);
            if (GUILayout.Button("X", GUILayout.Width(20)))
                objectsToProcess.RemoveAt(i);
            EditorGUILayout.EndHorizontal();
        }

        minDistance = EditorGUILayout.FloatField("Min Distance Between Probes", minDistance);
        normalOffset = EditorGUILayout.FloatField("Normal Offset", normalOffset);
        extraProbeSpacing = EditorGUILayout.FloatField("Extra Probe Spacing", extraProbeSpacing);
        
        if (GUILayout.Button("Place Light Probes"))
        {
            PlaceLightProbes();
        }
    }

    private void PlaceLightProbes()
    {
        LightProbeGroup probeGroup = FindObjectOfType<LightProbeGroup>();
        if (probeGroup == null)
        {
            GameObject probeHolder = new GameObject("LightProbeGroup");
            probeGroup = probeHolder.AddComponent<LightProbeGroup>();
        }

        HashSet<Vector3> probePositions = new HashSet<Vector3>();
        foreach (GameObject obj in objectsToProcess)
        {
            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null) continue;
            
            Mesh mesh = meshFilter.sharedMesh;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 worldPos = obj.transform.TransformPoint(vertices[i]);
                Vector3 worldNormal = obj.transform.TransformDirection(normals[i]);
                
                Vector3 offsetPos = worldPos + worldNormal * normalOffset;
                if (!probePositions.Any(p => Vector3.Distance(p, offsetPos) < minDistance))
                {
                    probePositions.Add(offsetPos);
                }
            }
        }

        // Add extra probes in spaces where normals indicate empty areas
        List<Vector3> extraProbes = new List<Vector3>();
        foreach (Vector3 probe in probePositions)
        {
            Vector3 avgNormal = Vector3.zero;
            int count = 0;
            foreach (GameObject obj in objectsToProcess)
            {
                MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
                if (meshFilter == null || meshFilter.sharedMesh == null) continue;
                
                Mesh mesh = meshFilter.sharedMesh;
                Vector3[] vertices = mesh.vertices;
                Vector3[] normals = mesh.normals;

                for (int i = 0; i < vertices.Length; i++)
                {
                    Vector3 worldPos = obj.transform.TransformPoint(vertices[i]);
                    if (Vector3.Distance(worldPos, probe) < minDistance * 2)
                    {
                        avgNormal += obj.transform.TransformDirection(normals[i]);
                        count++;
                    }
                }
            }
            if (count > 0)
            {
                avgNormal.Normalize();
                Vector3 extraPos = probe + avgNormal * extraProbeSpacing;
                if (!probePositions.Any(p => Vector3.Distance(p, extraPos) < minDistance))
                {
                    extraProbes.Add(extraPos);
                }
            }
        }
        
        probePositions.UnionWith(extraProbes);
        probeGroup.probePositions = probePositions.ToArray();
        Debug.Log($"Placed {probePositions.Count} Light Probes.");
    }
}