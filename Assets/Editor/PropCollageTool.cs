using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Unity Editor tool that scans a Level scene for all unique prop prefabs,
/// arranges them into a grid layout in the Screenshots scene,
/// and captures a clean white-background collage screenshot.
///
/// Usage: Tools > Prop Collage Tool
/// </summary>
public class PropCollageTool : EditorWindow
{
    // ── Config ────────────────────────────────────────────────────────────
    private int selectedLevel = 0;
    private int gridColumns = 10;
    private float cellSpacing = 3f;
    private int imageWidth = 1920;
    private int imageHeight = 1080;
    private float isometricAngle = 35f; // degrees from horizontal

    // ── State ────────────────────────────────────────────────────────────
    private List<GameObject> colliderProps = new List<GameObject>();
    private List<GameObject> decorProps = new List<GameObject>();
    private bool hasScanned = false;
    private Vector2 scrollPosCollider;
    private Vector2 scrollPosDecor;
    private string statusMessage = "Ready.";
    private MessageType statusType = MessageType.Info;

    private const string SCREENSHOTS_SCENE_PATH = "Assets/Scenes/Screenshots.unity";
    private const string PARENT_NAME = "__PropCollage__";

    // ── Window ───────────────────────────────────────────────────────────

    [MenuItem("Tools/Prop Collage Tool")]
    public static void ShowWindow()
    {
        var w = GetWindow<PropCollageTool>("Prop Collage");
        w.minSize = new Vector2(420, 580);
    }

    // ── GUI ──────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        DrawHeader();
        DrawLevelSelector();
        DrawScanSection();
        DrawPropList();
        DrawSettings();
        DrawGenerateSection();
        DrawStatus();
    }

    private void DrawHeader()
    {
        GUILayout.Space(8);
        EditorGUILayout.LabelField("Prop Collage Screenshot Tool", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Scans a level scene, extracts unique props, and generates a collage image.",
            EditorStyles.miniLabel);
        EditorGUILayout.Space(6);
    }

    private void DrawLevelSelector()
    {
        selectedLevel = EditorGUILayout.IntSlider("Level", selectedLevel, 0, 6);
        if (hasScanned)
        {
            EditorGUILayout.LabelField("Found", $"{colliderProps.Count} collider objects, {decorProps.Count} decorative");
        }
        EditorGUILayout.Space(4);
    }

    private void DrawScanSection()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(" Scan Level Scene", GUILayout.Height(28)))
            {
                ScanLevelScene();
            }

            if (GUILayout.Button("Clear Results", GUILayout.Height(28)))
            {
                hasScanned = false;
                colliderProps.Clear();
                decorProps.Clear();
                statusMessage = "Cleared.";
                statusType = MessageType.Info;
            }
        }
        EditorGUILayout.Space(6);
    }

    private void DrawPropList()
    {
        if (!hasScanned || (colliderProps.Count == 0 && decorProps.Count == 0)) return;

        DrawCategoryList("Collider Objects", colliderProps, ref scrollPosCollider);
        DrawCategoryList("Decorative Objects", decorProps, ref scrollPosDecor);
    }

    private void DrawCategoryList(string title, List<GameObject> props, ref Vector2 scroll)
    {
        if (props.Count == 0) return;
        EditorGUILayout.LabelField($"{title} ({props.Count}):", EditorStyles.boldLabel);
        float listHeight = Mathf.Min(props.Count * 20f + 4f, 120f);
        scroll = EditorGUILayout.BeginScrollView(scroll, GUI.skin.box, GUILayout.Height(listHeight));
        foreach (var p in props)
        {
            string name = p != null ? p.name : "null";
            EditorGUILayout.LabelField("  " + name, EditorStyles.miniLabel);
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.Space(4);
    }

    private void DrawSettings()
    {
        EditorGUILayout.LabelField("Collage Settings", EditorStyles.boldLabel);
        gridColumns = EditorGUILayout.IntField("Grid Columns", gridColumns);
        if (gridColumns < 1) gridColumns = 1;

        cellSpacing = EditorGUILayout.FloatField("Cell Spacing", cellSpacing);
        if (cellSpacing < 0.5f) cellSpacing = 0.5f;

        isometricAngle = EditorGUILayout.Slider("Isometric Angle (°)", isometricAngle, 10f, 80f);

        EditorGUILayout.LabelField("Output Resolution", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        imageWidth = EditorGUILayout.IntField("Width", imageWidth);
        imageHeight = EditorGUILayout.IntField("Height", imageHeight);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(4);
    }

    private void DrawGenerateSection()
    {
        int total = colliderProps.Count + decorProps.Count;
        GUI.enabled = hasScanned && total > 0;
        if (GUILayout.Button($" Generate Collages for Level {selectedLevel}", GUILayout.Height(36)))
        {
            GenerateCollage();
        }
        GUI.enabled = true;

        // Quick-generate all levels
        EditorGUILayout.Space(4);
        if (GUILayout.Button("Generate ALL Levels (0-6)", GUILayout.Height(28)))
        {
            GenerateAllLevels();
        }
        EditorGUILayout.Space(4);
    }

    private void DrawStatus()
    {
        if (!string.IsNullOrEmpty(statusMessage))
        {
            EditorGUILayout.HelpBox(statusMessage, statusType);
        }
    }

    // ── Scanning ─────────────────────────────────────────────────────────

    /// <summary>
    /// Opens the selected level scene additively, finds all unique prop prefabs
    /// (objects starting with "SM_Prop_"), closes the level, and stores the list.
    /// </summary>
    private void ScanLevelScene()
    {
        statusMessage = "Scanning level scene...";
        statusType = MessageType.Info;

        string levelPath = $"Assets/Scenes/Level{selectedLevel}.unity";

        if (!File.Exists(levelPath))
        {
            statusMessage = $"Scene not found: {levelPath}";
            statusType = MessageType.Error;
            hasScanned = false;
            colliderProps.Clear();
            decorProps.Clear();
            return;
        }

        // Open the level scene additively
        var levelScene = EditorSceneManager.OpenScene(levelPath, OpenSceneMode.Additive);
        if (!levelScene.IsValid())
        {
            statusMessage = $"Failed to open {levelPath}";
            statusType = MessageType.Error;
            return;
        }

        // Gather unique props, categorized by collider presence
        HashSet<string> uniqueAssetPaths = new HashSet<string>();
        List<GameObject> colliderList = new List<GameObject>();
        List<GameObject> decorList = new List<GameObject>();

        var roots = levelScene.GetRootGameObjects();
        foreach (var root in roots)
        {
            ScanTransformRecursive(root.transform, uniqueAssetPaths, colliderList, decorList);
        }

        // Close the level scene WITHOUT saving
        EditorSceneManager.CloseScene(levelScene, true);

        colliderProps = colliderList;
        decorProps = decorList;
        int total = colliderProps.Count + decorProps.Count;
        hasScanned = total > 0;

        if (hasScanned)
        {
            statusMessage = $"Level {selectedLevel}: {colliderProps.Count} collider objects, {decorProps.Count} decorative";
            statusType = MessageType.Info;
        }
        else
        {
            statusMessage = "No SM_Prop_ objects found. Check that the level contains props.";
            statusType = MessageType.Warning;
        }
    }

    private void ScanTransformRecursive(Transform t, HashSet<string> assetPaths,
        List<GameObject> colliderList, List<GameObject> decorList)
    {
        string name = t.name;
        string cleanName = RemoveCloneSuffix(name);

        if (cleanName.StartsWith("SM_Prop_"))
        {
            GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
            if (source != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(source);
                if (!string.IsNullOrEmpty(assetPath) && assetPaths.Add(assetPath))
                {
                    GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    if (prefabAsset != null)
                    {
                        if (prefabAsset.GetComponentInChildren<Collider>())
                            colliderList.Add(prefabAsset);
                        else
                            decorList.Add(prefabAsset);
                    }
                }
            }
            return;
        }

        foreach (Transform child in t)
        {
            ScanTransformRecursive(child, assetPaths, colliderList, decorList);
        }
    }

    private static string RemoveCloneSuffix(string name)
    {
        // Remove patterns like " (1)", " (2)", " (Clone)", " (24)" etc.
        int idx = name.LastIndexOf(" (");
        if (idx > 0 && idx < name.Length - 2)
        {
            string after = name.Substring(idx + 2);
            if (after.EndsWith(")") && (after.Length == 1 || char.IsDigit(after[0]) || after == "Clone)"))
            {
                return name.Substring(0, idx);
            }
        }
        return name;
    }

    // ── Collage Generation ───────────────────────────────────────────────

    /// <summary>
    /// Spawns all unique props in a grid layout within the Screenshots scene,
    /// adjusts the camera for an isometric view, and captures the screenshot.
    /// Larger props are placed at the back rows, smaller props at the front.
    /// </summary>
    private void GenerateCollage()
    {
        // Ensure we are in the Screenshots scene
        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (activeScene.path != SCREENSHOTS_SCENE_PATH)
        {
            if (!File.Exists(SCREENSHOTS_SCENE_PATH))
            {
                statusMessage = $"Screenshots scene not found at {SCREENSHOTS_SCENE_PATH}";
                statusType = MessageType.Error;
                return;
            }
            EditorSceneManager.OpenScene(SCREENSHOTS_SCENE_PATH, OpenSceneMode.Single);
        }

        GenerateGrid(colliderProps, "ColliderObjects");
        GenerateGrid(decorProps, "DecorativeObjects");

        statusMessage = $"Level {selectedLevel}: saved ColliderObjects.png + DecorativeObjects.png";
        statusType = MessageType.Info;
        AssetDatabase.Refresh();
    }

    private void GenerateGrid(List<GameObject> props, string category)
    {
        if (props.Count == 0)
        {
            Debug.Log($"[PropCollage] {category}: no props, skipping.");
            return;
        }

        CleanupPreviousCollage();

        int totalProps = props.Count;
        GameObject parent = new GameObject(PARENT_NAME);
        parent.transform.position = Vector3.zero;

        List<PropWithBounds> propsWithBounds = new List<PropWithBounds>();

        for (int i = 0; i < totalProps; i++)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(props[i]);
            if (instance == null) continue;
            instance.transform.SetParent(parent.transform);

            Bounds propBounds = CalculatePropBounds(instance);
            if (propBounds.size.sqrMagnitude < 0.0001f)
            {
                DestroyImmediate(instance);
                continue;
            }

            float sizeScore = Mathf.Max(propBounds.size.x, propBounds.size.y, propBounds.size.z);

            propsWithBounds.Add(new PropWithBounds
            {
                instance = instance,
                bounds = propBounds,
                sizeScore = sizeScore
            });
        }

        if (propsWithBounds.Count == 0)
        {
            Debug.LogWarning($"[PropCollage] {category}: no renderable props.");
            return;
        }

        propsWithBounds.Sort((a, b) => a.sizeScore.CompareTo(b.sizeScore));

        Bounds totalBounds = new Bounds();
        bool hasTotalBounds = false;

        for (int i = 0; i < propsWithBounds.Count; i++)
        {
            var pb = propsWithBounds[i];
            int col = i % gridColumns;
            int row = i / gridColumns;

            Vector3 cellCenter = new Vector3(
                col * cellSpacing,
                0f,
                row * cellSpacing
            );

            Vector3 offset = pb.bounds.center - pb.instance.transform.position;
            offset.y = -pb.bounds.min.y + 0.001f;
            pb.instance.transform.position = cellCenter - offset + new Vector3(0, 0.001f, 0);
            pb.instance.transform.rotation = Quaternion.Euler(0, 180f, 0);

            Bounds movedBounds = CalculatePropBounds(pb.instance);
            if (!hasTotalBounds)
            {
                totalBounds = movedBounds;
                hasTotalBounds = true;
            }
            else
            {
                totalBounds.Encapsulate(movedBounds);
            }
        }

        // ── Camera Setup ────────────────────────────────────────────────
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("[PropCollage] No Main Camera found in the Screenshots scene.");
            return;
        }

        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.white;
        cam.orthographic = true;

        Vector3 gridCenter = totalBounds.center;

        float angleRad = isometricAngle * Mathf.Deg2Rad;
        float distFromCenter = Mathf.Max(totalBounds.size.x, totalBounds.size.z) * 1.0f;
        float camHeight = distFromCenter * Mathf.Sin(angleRad);
        float camDist = distFromCenter * Mathf.Cos(angleRad);

        Vector3 camPos = gridCenter + new Vector3(-camDist * 0.3f, camHeight, -camDist);
        cam.transform.position = camPos;
        cam.transform.LookAt(gridCenter);

        cam.orthographicSize = CalculateOrthographicSize(cam, totalBounds, 0.88f);

        // ── Capture Screenshot ──────────────────────────────────────────
        string filename = $"Level{selectedLevel}_{category}.png";
        string outputPath = Path.Combine(Application.dataPath, "..", filename);
        outputPath = Path.GetFullPath(outputPath);

        RenderTexture rt = new RenderTexture(imageWidth, imageHeight, 24, RenderTextureFormat.ARGB32);
        RenderTexture prevTarget = cam.targetTexture;

        cam.targetTexture = rt;

        RenderTexture prevActive = RenderTexture.active;
        RenderTexture.active = rt;

        cam.Render();

        Texture2D tex = new Texture2D(imageWidth, imageHeight, TextureFormat.ARGB32, false);
        tex.ReadPixels(new Rect(0, 0, imageWidth, imageHeight), 0, 0);
        tex.Apply();

        cam.targetTexture = prevTarget;
        RenderTexture.active = prevActive;
        rt.Release();
        DestroyImmediate(rt);

        byte[] pngBytes = tex.EncodeToPNG();
        File.WriteAllBytes(outputPath, pngBytes);
        DestroyImmediate(tex);

        CleanupPreviousCollage();

        Debug.Log($"[PropCollage] Saved: {outputPath}");
    }

    /// <summary>
    /// Generates collages for all 7 levels sequentially.
    /// </summary>
    private void GenerateAllLevels()
    {
        for (int lvl = 0; lvl <= 6; lvl++)
        {
            selectedLevel = lvl;
            ScanLevelScene();
            int total = colliderProps.Count + decorProps.Count;
            if (hasScanned && total > 0)
            {
                GenerateCollage();
                EditorUtility.DisplayProgressBar("Prop Collage", $"Generating Level {lvl}...", (lvl + 1) / 7f);
            }
            else
            {
                Debug.LogWarning($"[PropCollage] Level {lvl}: no props found or scan failed. Skipping.");
            }
        }
        EditorUtility.ClearProgressBar();
        statusMessage = "All levels generated! Check project root for LevelX_ColliderObjects.png and LevelX_DecorativeObjects.png files.";
        statusType = MessageType.Info;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private void CleanupPreviousCollage()
    {
        GameObject existing = GameObject.Find(PARENT_NAME);
        while (existing != null)
        {
            DestroyImmediate(existing);
            existing = GameObject.Find(PARENT_NAME);
        }
    }

    private Bounds CalculatePropBounds(GameObject obj)
    {
        Bounds bounds = new Bounds();
        bool hasBounds = false;

        var renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            if (r is MeshRenderer || r is SkinnedMeshRenderer)
            {
                if (!hasBounds)
                {
                    bounds = r.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }
        }

        return bounds;
    }

    private float CalculateOrthographicSize(Camera cam, Bounds bounds, float fillFactor)
    {
        // Get the 8 corners of the bounding box
        Vector3[] corners = new Vector3[8];
        Vector3 c = bounds.center;
        Vector3 e = bounds.extents;

        corners[0] = c + new Vector3(-e.x, -e.y, -e.z);
        corners[1] = c + new Vector3(e.x, -e.y, -e.z);
        corners[2] = c + new Vector3(-e.x, e.y, -e.z);
        corners[3] = c + new Vector3(e.x, e.y, -e.z);
        corners[4] = c + new Vector3(-e.x, -e.y, e.z);
        corners[5] = c + new Vector3(e.x, -e.y, e.z);
        corners[6] = c + new Vector3(-e.x, e.y, e.z);
        corners[7] = c + new Vector3(e.x, e.y, e.z);

        // Project to viewport space and find the extent needed
        float minX = 1f, maxX = 0f, minY = 1f, maxY = 0f;

        foreach (var corner in corners)
        {
            Vector3 vp = cam.WorldToViewportPoint(corner);
            if (vp.z < 0) continue; // behind camera, skip
            minX = Mathf.Min(minX, vp.x);
            maxX = Mathf.Max(maxX, vp.x);
            minY = Mathf.Min(minY, vp.y);
            maxY = Mathf.Max(maxY, vp.y);
        }

        float widthSpan = maxX - minX;
        float heightSpan = maxY - minY;

        if (widthSpan <= 0 || heightSpan <= 0)
        {
            // Fallback
            return 10f;
        }

        // The camera's current orthographic size * 2.0 = visible height in world units at the image plane
        // widthSpan/heightSpan = fraction of viewport occupied
        // We want to scale so that max(widthSpan, heightSpan) == fillFactor

        float scaleX = widthSpan / fillFactor;
        float scaleY = heightSpan / fillFactor;
        float scale = Mathf.Max(scaleX, scaleY);

        float currentSize = cam.orthographicSize;
        return currentSize * scale;
    }

    /// <summary>
    /// Stores a prop instance together with its calculated bounds and a size score for sorting.
    /// </summary>
    private struct PropWithBounds
    {
        public GameObject instance;
        public Bounds bounds;
        public float sizeScore; // largest dimension (max of x, y, z)
    }
}
