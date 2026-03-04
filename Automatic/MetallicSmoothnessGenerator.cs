using UnityEngine;
using UnityEditor;
using System.IO;

public class MetallicSmoothnessGenerator : EditorWindow
{
    private Texture2D roughnessMap;
    private Texture2D metallicMap;

    [MenuItem("Tools/Metallic Smoothness Generator")]
    public static void ShowWindow()
    {
        GetWindow<MetallicSmoothnessGenerator>("Metallic Smoothness Generator");
    }

    void OnGUI()
    {
        GUILayout.Label("Input Textures", EditorStyles.boldLabel);

        roughnessMap = (Texture2D)EditorGUILayout.ObjectField("Roughness Map", roughnessMap, typeof(Texture2D), false);
        metallicMap = (Texture2D)EditorGUILayout.ObjectField("Metallic Map", metallicMap, typeof(Texture2D), false);

        if (GUILayout.Button("Generate Metallic-Smoothness Texture"))
        {
            if (roughnessMap == null || metallicMap == null)
            {
                EditorUtility.DisplayDialog("Missing Texture", "Please assign both Roughness and Metallic maps.", "OK");
                return;
            }

            CreateMetallicSmoothnessTexture(roughnessMap, metallicMap);
        }
    }

    private void CreateMetallicSmoothnessTexture(Texture2D rough, Texture2D metal)
    {
        int width = metal.width;
        int height = metal.height;

        if (rough.width != width || rough.height != height)
        {
            EditorUtility.DisplayDialog("Error", "Roughness and Metallic maps must have the same resolution.", "OK");
            return;
        }

        Texture2D output = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] roughPixels = rough.GetPixels();
        Color[] metalPixels = metal.GetPixels();
        Color[] resultPixels = new Color[roughPixels.Length];

        for (int i = 0; i < resultPixels.Length; i++)
        {
            float metallicValue = metalPixels[i].r; // Assuming grayscale
            float smoothnessValue = 1f - roughPixels[i].r; // Invert roughness to get smoothness

            resultPixels[i] = new Color(metallicValue, metallicValue, metallicValue, smoothnessValue);
        }

        output.SetPixels(resultPixels);
        output.Apply();

        string path = EditorUtility.SaveFilePanelInProject("Save Metallic-Smoothness Texture", "MetallicSmoothness", "png", "Save the new texture");
        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllBytes(path, output.EncodeToPNG());
            AssetDatabase.Refresh();
        }
    }
}
