using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;

public class LightmapSampler : EditorWindow
{
    enum Mode { Off, Sample, Paint }
    private Mode currentMode = Mode.Off;
    private Color sampledColor = Color.black;

    // Paint settings
    private Color paintColor = Color.red;
    private int paintRadius = 4;

    private Texture2D editableLightmapTexture; // keep reference to painted lightmap
    private LightmapData currentLightmapData;

    private Texture2D undoBackupTexture;
    Renderer[] allRenderers;
    private Vector3 lastHitPoint;

    [MenuItem("Tools/Lightmap Sampler")]
    static void Init()
    {
        LightmapSampler window = (LightmapSampler)GetWindow(typeof(LightmapSampler));
        window.titleContent = new GUIContent("Lightmap Sampler");
        window.Show();

        window.allRenderers = GameObject.FindObjectsOfType<Renderer>(true);

        SceneView.duringSceneGui += window.OnSceneGUI;
    }

    void OnDestroy()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    void OnGUI()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Toggle(currentMode == Mode.Off, "Off", "Button"))
            currentMode = Mode.Off;
        if (GUILayout.Toggle(currentMode == Mode.Sample, "Sample", "Button"))
            currentMode = Mode.Sample;
        if (GUILayout.Toggle(currentMode == Mode.Paint, "Paint", "Button"))
            currentMode = Mode.Paint;
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        if (currentMode == Mode.Sample)
        {
            GUILayout.Label("Sampled Lightmap Color:", EditorStyles.boldLabel);
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(50, 50), sampledColor);
            GUILayout.Label($"RGB: {sampledColor}");
        }
        else if (currentMode == Mode.Paint)
        {
            GUILayout.Label("Painting Mode Active", EditorStyles.boldLabel);
            GUILayout.Label($"Paint Color: {paintColor}");
            paintColor = EditorGUILayout.ColorField("Set Paint Color", paintColor);
            paintRadius = EditorGUILayout.IntSlider("Paint Radius", paintRadius, 1, 20);
        }
        else
        {
            GUILayout.Label("Tool Disabled");
        }
    }

    void OnSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;

        if (currentMode == Mode.Sample || currentMode == Mode.Paint)
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
            {
                if (currentMode == Mode.Sample)
                    SampleLightmapColor(e.mousePosition);
                else if (currentMode == Mode.Paint)
                    PaintLightmapAtMouse(e.mousePosition);

                e.Use();
            }
            else if (e.type == EventType.Repaint)
            {
                Handles.color = Color.red;
                Handles.SphereHandleCap(0, lastHitPoint, Quaternion.identity, 0.1f, EventType.Repaint);
            }
        }
    }

    void SampleLightmapColor(Vector2 mousePosition)
    {
        Color? color = TryGetLightmapColorAtMouse(mousePosition, out currentLightmapData, out editableLightmapTexture);
        if (color.HasValue)
        {
            sampledColor = color.Value;
            paintColor = sampledColor;
            Repaint();
        }
    }

    void PaintLightmapAtMouse(Vector2 mousePosition)
    {
        if (currentLightmapData == null)
        {
            // Need to get lightmap texture reference first by sampling once or manually
            TryGetLightmapColorAtMouse(mousePosition, out currentLightmapData, out editableLightmapTexture);
            if (editableLightmapTexture == null)
            {
                Debug.LogWarning("Cannot paint: no editable lightmap texture found.");
                return;
            }
        }

        // Backup before painting starts
        if (undoBackupTexture != null)
            DestroyImmediate(undoBackupTexture);

        undoBackupTexture = new Texture2D(editableLightmapTexture.width, editableLightmapTexture.height, TextureFormat.RGBA32, false);
        undoBackupTexture.SetPixels(editableLightmapTexture.GetPixels());
        undoBackupTexture.Apply();

        Undo.RegisterCompleteObjectUndo(this, "Lightmap Paint Undo");

        // Raycast and get UV again to paint at correct spot
        Ray editorRay = HandleUtility.GUIPointToWorldRay(mousePosition);

        if (Physics.Raycast(editorRay, out RaycastHit initialHit, float.MaxValue))
        {
            GameObject hitObject = initialHit.collider.gameObject;
            MeshFilter mf = hitObject.GetComponent<MeshFilter>();
            Renderer rend = hitObject.GetComponent<Renderer>();

            if (mf == null || mf.sharedMesh == null || rend == null) return;

            Mesh meshToSample = mf.sharedMesh;
            Matrix4x4 objectMatrix = hitObject.transform.localToWorldMatrix;

            if (!RXLookingGlass.IntersectRayMesh(editorRay, meshToSample, objectMatrix, out RaycastHit meshHit)) return;

            Vector2[] uv2 = meshToSample.uv2;
            int[] tris = meshToSample.triangles;
            int triIdx = meshHit.triangleIndex;
            int baseIdx = triIdx * 3;

            if (baseIdx < 0 || baseIdx + 2 >= tris.Length) return;

            int i0 = tris[baseIdx];
            int i1 = tris[baseIdx + 1];
            int i2 = tris[baseIdx + 2];

            Vector3 bary = meshHit.barycentricCoordinate;

            if (i0 >= uv2.Length || i1 >= uv2.Length || i2 >= uv2.Length) return;

            Vector2 uv0 = uv2[i0];
            Vector2 uv1 = uv2[i1];
            Vector2 uv2_ = uv2[i2];

            Vector2 interpolatedUV = uv0 * bary.x + uv1 * bary.y + uv2_ * bary.z;

            Vector4 lmST = rend.lightmapScaleOffset;
            Vector2 lightmapUV = new Vector2(
                interpolatedUV.x * lmST.x + lmST.z,
                interpolatedUV.y * lmST.y + lmST.w
            );

            int px = Mathf.Clamp((int)(lightmapUV.x * editableLightmapTexture.width), 0, editableLightmapTexture.width - 1);
            int py = Mathf.Clamp((int)(lightmapUV.y * editableLightmapTexture.height), 0, editableLightmapTexture.height - 1);

            PaintCircle(editableLightmapTexture, px, py, paintRadius, paintColor);

            editableLightmapTexture.Apply();

            // Assign the painted texture back to the lightmap data so SceneView updates
            currentLightmapData.lightmapColor = editableLightmapTexture;

            Repaint();

            //Debug.Log($"Painted at lightmap UV ({px},{py}) on texture '{editableLightmapTexture.name}'.");
        }
    }

    void PaintCircle(Texture2D tex, int cx, int cy, int radius, Color color)
    {
        int x0 = Mathf.Clamp(cx - radius, 0, tex.width - 1);
        int x1 = Mathf.Clamp(cx + radius, 0, tex.width - 1);
        int y0 = Mathf.Clamp(cy - radius, 0, tex.height - 1);
        int y1 = Mathf.Clamp(cy + radius, 0, tex.height - 1);

        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                int dx = x - cx;
                int dy = y - cy;
                if (dx * dx + dy * dy <= radius * radius)
                {
                    Color existing = tex.GetPixel(x, y);
                    Color blended = Color.Lerp(existing, color, color.a); // alpha blending if needed
                    tex.SetPixel(x, y, blended);
                }
            }
        }
    }

    // Helper: returns sampled color, also outputs lightmap data and editable texture for painting
    Color? TryGetLightmapColorAtMouse(Vector2 mousePosition, out LightmapData outLightmapData, out Texture2D outEditableLightmap)
    {
        outLightmapData = null;
        outEditableLightmap = null;

        Ray editorRay = HandleUtility.GUIPointToWorldRay(mousePosition);

        var (hit, hitRenderer, mesh, hitTriangleIndex, barycentricCoords, hitDistance) = RaycastMeshes(editorRay);

        if (!hit || hitRenderer == null || mesh == null || hitTriangleIndex < 0)
        {
            Debug.LogWarning("No valid mesh hit or mesh does not have UV2 coordinates for lightmapping.");
            return null;
        }

        Debug.Log("Hit object: " + hitRenderer.gameObject.name + ", Triangle Index: " + hitTriangleIndex);

        Vector2[] uv2 = mesh.uv2;
        if (uv2 == null || uv2.Length == 0)
        {
            Debug.LogWarning("Mesh does not have UV2 coordinates for lightmapping.");
            uv2 = mesh.uv;
        }
        int[] tris = mesh.triangles;
        int triIdx = hitTriangleIndex;
        int baseIdx = triIdx * 3;

        if (baseIdx < 0 || baseIdx + 2 >= tris.Length)
        {
            Debug.LogWarning("Invalid triangle index or UV mapping.");
            return null;
        }

        int i0 = tris[baseIdx];
        int i1 = tris[baseIdx + 1];
        int i2 = tris[baseIdx + 2];

        Vector3 bary = barycentricCoords;

        if (i0 >= uv2.Length || i1 >= uv2.Length || i2 >= uv2.Length)
        {
            Debug.LogWarning("UV indices out of bounds for the mesh UV2 array.");
            return null;
        }

        Vector2 uv0 = uv2[i0];
        Vector2 uv1 = uv2[i1];
        Vector2 uv2_ = uv2[i2];

        Vector2 interpolatedUV = uv0 * bary.x + uv1 * bary.y + uv2_ * bary.z;

        Vector4 lmST = hitRenderer.lightmapScaleOffset;
        Vector2 lightmapUV = new Vector2(
            interpolatedUV.x * lmST.x + lmST.z,
            interpolatedUV.y * lmST.y + lmST.w
        );

        int lmIndex = hitRenderer.lightmapIndex;
        if (lmIndex < 0 || LightmapSettings.lightmaps == null || lmIndex >= LightmapSettings.lightmaps.Length)
        {
            Debug.LogWarning("Invalid lightmap index or no lightmaps available.");
            return null;
        }

        LightmapData lmData = LightmapSettings.lightmaps[lmIndex];
        Texture lightmapTextureBase = lmData.lightmapColor;

        if (lightmapTextureBase == null)
        {
            Debug.LogWarning("No lightmap texture found for the hit object.");
            return null;
        }

        Texture2D lightmapTex2D = lightmapTextureBase as Texture2D;
        if (lightmapTex2D == null)
            return null;

        Texture2D readableLightmap = GetReadableCopy(lightmapTex2D);
        if (readableLightmap == null)
        {
            Debug.LogWarning("Failed to create a readable copy of the lightmap texture.");
            return null;
        }

        int x = Mathf.Clamp((int)(lightmapUV.x * readableLightmap.width), 0, readableLightmap.width - 1);
        int y = Mathf.Clamp((int)(lightmapUV.y * readableLightmap.height), 0, readableLightmap.height - 1);

        Color sampled = readableLightmap.GetPixel(x, y);

        // For painting keep reference to the copied texture and lightmap data
        outLightmapData = lmData;
        outEditableLightmap = readableLightmap;

        return sampled;
    }

    Texture2D GetReadableCopy(Texture2D source)
    {
        if (source == null) return null;

        RenderTexture rt = RenderTexture.GetTemporary(
            source.width,
            source.height,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Default);

        Graphics.Blit(source, rt);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D readableTex = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        readableTex.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
        readableTex.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        return readableTex;
    }

    (bool hit, Renderer hitRenderer, Mesh mesh, int hitTriangleIndex, Vector3 barycentricCoords, float hitDistance) RaycastMeshes(Ray ray)
    {
        Renderer closestRenderer = null;
        Mesh m = null;
        int closestTriangle = -1;
        Vector3 closestBarycentric = Vector3.zero;
        float closestDist = float.MaxValue;
        bool anyHit = false;

        Camera sceneCam = SceneView.lastActiveSceneView.camera;
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(sceneCam);

        foreach (var rend in allRenderers)
        {
            if (!GeometryUtility.TestPlanesAABB(frustumPlanes, rend.bounds))
                continue;

            if (rend is MeshRenderer)
            {
                var mf = rend.GetComponent<MeshFilter>();
                if (mf != null) m = mf.sharedMesh;
            }
            else if (rend is SkinnedMeshRenderer skinned)
            {
                m = skinned.sharedMesh;
            }

            if (m == null) continue;

            Vector3[] vertices = m.vertices;
            int[] triangles = m.triangles;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 v0 = rend.transform.TransformPoint(vertices[triangles[i]]);
                Vector3 v1 = rend.transform.TransformPoint(vertices[triangles[i + 1]]);
                Vector3 v2 = rend.transform.TransformPoint(vertices[triangles[i + 2]]);

                if (RayIntersectsTriangle(ray, v0, v1, v2, out float dist, out Vector3 bary))
                {
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestRenderer = rend;
                        closestTriangle = i / 3;
                        closestBarycentric = bary;
                        anyHit = true;

                        lastHitPoint = v0 * closestBarycentric.x + v1 * closestBarycentric.y + v2 * closestBarycentric.z;
                    }
                }
            }
        }

        return (anyHit, closestRenderer, m, closestTriangle, closestBarycentric, closestDist);
    }

    bool RayIntersectsTriangle(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, out float distance, out Vector3 barycentricCoords)
    {
        distance = 0f;
        barycentricCoords = Vector3.zero;

        Vector3 edge1 = v1 - v0;
        Vector3 edge2 = v2 - v0;
        Vector3 pvec = Vector3.Cross(ray.direction, edge2);
        float det = Vector3.Dot(edge1, pvec);

        if (Mathf.Abs(det) < 1e-8f)
            return false;

        float invDet = 1.0f / det;
        Vector3 tvec = ray.origin - v0;
        float u = Vector3.Dot(tvec, pvec) * invDet;
        if (u < 0 || u > 1)
            return false;

        Vector3 qvec = Vector3.Cross(tvec, edge1);
        float v = Vector3.Dot(ray.direction, qvec) * invDet;
        if (v < 0 || u + v > 1)
            return false;

        distance = Vector3.Dot(edge2, qvec) * invDet;
        if (distance < 0)
            return false;

        barycentricCoords = new Vector3(1 - u - v, u, v);
        return true;
    }

    void OnUndo()
    {
        if (undoBackupTexture != null && editableLightmapTexture != null)
        {
            editableLightmapTexture.SetPixels(undoBackupTexture.GetPixels());
            editableLightmapTexture.Apply();
            currentLightmapData.lightmapColor = editableLightmapTexture;
            Repaint();
        }
    }
}
