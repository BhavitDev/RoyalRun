using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class IndianEnvironmentBuilder
{
    private const string RootFolder = "Assets/Environments/IndianMarket";
    private const string PrefabFolder = RootFolder + "/Prefabs";
    private const string MaterialFolder = RootFolder + "/Materials";
    private const string MeshFolder = RootFolder + "/Meshes";
    private const string PreviewScenePath = "Assets/Scenes/IndianMarketPreview.unity";
    private const string SourceScenePath = "Assets/Scenes/SampleScene.unity";
    private const string CheckpointSourcePath = "Assets/Prefabs/Platform/CheckPoint.prefab";
    private const string MarketCheckpointPath = PrefabFolder + "/IndianMarketCheckpoint.prefab";
    private const string FallingObjectFolder = "Assets/Prefabs/FallingObjects/IndianHomeAppliances";
    private const string TelevisionMaterialPath = FallingObjectFolder + "/Television_Material.mat";

    [MenuItem("Tools/Indian Environment/Build Separate Preview")]
    public static void BuildSeparatePreview()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Exit Play mode before building the Indian market preview.");
            return;
        }

        EnsureFolder("Assets/Environments");
        EnsureFolder(RootFolder);
        EnsureFolder(PrefabFolder);
        EnsureFolder(MaterialFolder);
        EnsureFolder(MeshFolder);
        EnsureFolder("Assets/Scenes");

        var prefabs = RebuildEnvironmentPrefabs();
        var checkpointPrefab = BuildIndianCheckpoint(CreateMaterials());
        var marketVolumeProfile = CreateIndianMarketVolumeProfile();
        var fallingObstaclePrefabs = BuildFallingObstaclePrefabs();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!File.Exists(SourceScenePath))
        {
            Debug.LogError($"Could not find the source game scene at {SourceScenePath}.");
            return;
        }

        var sourceScene = SceneManager.GetSceneByPath(SourceScenePath);
        var openedSourceScene = !sourceScene.IsValid() || !sourceScene.isLoaded;
        if (openedSourceScene)
            sourceScene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Additive);

        var loader = sourceScene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<PlatformChunkLoader>(true))
            .FirstOrDefault();
        if (loader == null)
        {
            if (openedSourceScene)
                EditorSceneManager.CloseScene(sourceScene, true);
            Debug.LogError("The source game scene has no PlatformChunkLoader to attach the market segments to.");
            return;
        }

        var serializedLoader = new SerializedObject(loader);
        var environmentArray = serializedLoader.FindProperty("environmentSegmentPrefabs");
        var checkpointProperty = serializedLoader.FindProperty("CheckPointChunk");
        var originalArraySize = environmentArray.arraySize;
        var originalCheckpoint = checkpointProperty.objectReferenceValue;
        var originalPrefabs = new Object[originalArraySize];
        for (var i = 0; i < originalArraySize; i++)
            originalPrefabs[i] = environmentArray.GetArrayElementAtIndex(i).objectReferenceValue;

        environmentArray.arraySize = prefabs.Length;
        for (var i = 0; i < prefabs.Length; i++)
            environmentArray.GetArrayElementAtIndex(i).objectReferenceValue = prefabs[i];
        checkpointProperty.objectReferenceValue = checkpointPrefab;
        serializedLoader.ApplyModifiedPropertiesWithoutUndo();

        var previewPath = AssetDatabase.GenerateUniqueAssetPath(PreviewScenePath);
        var copyCreated = EditorSceneManager.SaveScene(sourceScene, previewPath, true);

        var restoreLoader = new SerializedObject(loader);
        environmentArray = restoreLoader.FindProperty("environmentSegmentPrefabs");
        checkpointProperty = restoreLoader.FindProperty("CheckPointChunk");
        environmentArray.arraySize = originalArraySize;
        for (var i = 0; i < originalArraySize; i++)
            environmentArray.GetArrayElementAtIndex(i).objectReferenceValue = originalPrefabs[i];
        checkpointProperty.objectReferenceValue = originalCheckpoint;
        restoreLoader.ApplyModifiedPropertiesWithoutUndo();

        if (openedSourceScene)
        {
            EditorSceneManager.CloseScene(sourceScene, true);
        }

        if (!copyCreated)
        {
            Debug.LogError("Could not create the separate Indian market preview scene.");
            return;
        }
        AssignCheckpointToMarketPreview(checkpointPrefab, marketVolumeProfile, previewPath);
        AssignFallingObstaclesToScene(previewPath, fallingObstaclePrefabs);
        Selection.activeObject = prefabs[0];
        Debug.Log($"Built three reusable Indian market segment prefabs and a separate preview scene: {previewPath}. The original scene file and the currently open editor scene were left unchanged.");
    }

    [MenuItem("Tools/Indian Environment/Rebuild Market Prefabs")]
    public static void RebuildMarketPrefabsMenu()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Exit Play mode before rebuilding the Indian market prefabs.");
            return;
        }

        RebuildEnvironmentPrefabs();
        var checkpointPrefab = BuildIndianCheckpoint(CreateMaterials());
        var marketVolumeProfile = CreateIndianMarketVolumeProfile();
        var fallingObstaclePrefabs = BuildFallingObstaclePrefabs();
        BuildJalebiPickup();
        AssignCheckpointToMarketPreview(checkpointPrefab, marketVolumeProfile, PreviewScenePath);
        AssignFallingObstaclesToScene(SourceScenePath, fallingObstaclePrefabs);
        AssignFallingObstaclesToScene(PreviewScenePath, fallingObstaclePrefabs);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Rebuilt the Indian market, checkpoint and three falling appliance prefabs. Both game scenes now spawn the appliances at track-matched spacing and speed.");
    }

    private static GameObject[] BuildFallingObstaclePrefabs()
    {
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Prefabs/FallingObjects");
        EnsureFolder(FallingObjectFolder);

        return new[]
        {
            BuildFallingObstacle("Falling_Television", "Assets/Imported Assets/jethalal/old-television/source/sdfsd.obj", new Vector3(1.5f, 1.15f, 1.1f), 8f, Quaternion.identity),
            BuildFallingObstacle("Falling_TVBox", "Assets/Imported Assets/jethalal/simple_tv_packagingcardboard_box.glb", new Vector3(1.4f, 1.5f, 1.2f), 6f, Quaternion.identity),
            BuildFallingObstacle("Falling_Refrigerator", "Assets/Imported Assets/jethalal/house_props__fridge.glb", new Vector3(1.1f, 2.2f, 1.0f), 24f, Quaternion.Euler(90f, 0f, 0f))
        };
    }

    private static void BuildJalebiPickup()
    {
        const string prefabPath = "Assets/Prefabs/ApplePrefab.prefab";
        const string matPath = "Assets/Prefabs/FallingObjects/IndianHomeAppliances/Jalebi_Glaze.mat";
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        var model = root.transform.Find("Model");
        if (model == null) { PrefabUtility.UnloadPrefabContents(root); return; }
        for (var i = model.childCount - 1; i >= 0; i--) Object.DestroyImmediate(model.GetChild(i).gameObject);

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, matPath); }
        mat.color = new Color(1f, 0.43f, 0.025f);
        var go = new GameObject("Jalebi_Coil"); go.transform.SetParent(model, false);
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        go.transform.localScale = Vector3.one * 1.5f;
        var filter = go.AddComponent<MeshFilter>();
        var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = mat;
        filter.sharedMesh = CreateJalebiMesh();
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static Mesh CreateJalebiMesh()
    {
        const int pathSegments = 240, tubeSegments = 10;
        const float turns = 2.6f, majorRadius = 0.34f, tubeRadius = 0.075f;
        var vertices = new Vector3[pathSegments * tubeSegments];
        var triangles = new int[(pathSegments - 1) * tubeSegments * 6];
        for (var i = 0; i < pathSegments; i++)
        {
            var t = i / (float)(pathSegments - 1); var a = t * turns * Mathf.PI * 2f;
            var r = majorRadius * (0.18f + 0.82f * t);
            var center = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            for (var j = 0; j < tubeSegments; j++)
            {
                var b = j * Mathf.PI * 2f / tubeSegments;
                vertices[i * tubeSegments + j] = center + new Vector3(Mathf.Cos(a) * Mathf.Cos(b), Mathf.Sin(b), Mathf.Sin(a) * Mathf.Cos(b)) * tubeRadius;
                if (i == pathSegments - 1) continue;
                var nextI = i + 1; var nextJ = (j + 1) % tubeSegments; var k = (i * tubeSegments + j) * 6;
                var v = i * tubeSegments + j; var v1 = i * tubeSegments + nextJ; var v2 = nextI * tubeSegments + j; var v3 = nextI * tubeSegments + nextJ;
                triangles[k] = v; triangles[k + 1] = v1; triangles[k + 2] = v2; triangles[k + 3] = v1; triangles[k + 4] = v3; triangles[k + 5] = v2;
            }
        }
        var mesh = new Mesh { name = "Jalebi_Coil_Mesh", vertices = vertices, triangles = triangles };
        mesh.RecalculateNormals();
        const string meshPath = "Assets/Prefabs/FallingObjects/IndianHomeAppliances/Jalebi_Coil_Mesh.asset";
        AssetDatabase.CreateAsset(mesh, meshPath);
        return mesh;
    }

    private static GameObject BuildFallingObstacle(string objectName, string sourcePath, Vector3 targetSize, float mass, Quaternion visualRotation)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
        if (source == null)
        {
            Debug.LogError($"Could not load the falling obstacle model at {sourcePath}.");
            return null;
        }

        var root = new GameObject(objectName);
        var visual = PrefabUtility.InstantiatePrefab(source) as GameObject;
        if (visual == null)
            visual = Object.Instantiate(source);
        visual.name = objectName + "_Visual";
        visual.transform.SetParent(root.transform, false);
        visual.transform.localRotation = visualRotation;
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = Vector3.one;

        var bounds = GetRendererBounds(root.transform);
        if (bounds.size.sqrMagnitude < 0.0001f)
        {
            Object.DestroyImmediate(root);
            Debug.LogError($"The model at {sourcePath} has no visible renderer bounds.");
            return null;
        }

        var factor = Mathf.Min(targetSize.x / bounds.size.x, targetSize.y / bounds.size.y, targetSize.z / bounds.size.z);
        visual.transform.localScale = Vector3.one * factor;
        bounds = GetRendererBounds(root.transform);
        visual.transform.localPosition = -bounds.center;

        if (objectName == "Falling_Television")
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Imported Assets/jethalal/old-television/textures/TV_Low_initialShadingGroup_BaseColor.png");
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (texture != null && shader != null)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(TelevisionMaterialPath);
                if (material == null)
                {
                    material = new Material(shader);
                    AssetDatabase.CreateAsset(material, TelevisionMaterialPath);
                }
                material.shader = shader;
                material.SetTexture("_BaseMap", texture);
                material.SetColor("_BaseColor", Color.white);
                foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
                    renderer.sharedMaterial = material;
            }
        }

        var body = root.AddComponent<Rigidbody>();
        body.mass = mass;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.constraints = RigidbodyConstraints.None;

        var collider = root.AddComponent<BoxCollider>();
        collider.size = targetSize;
        root.AddComponent<ObstaclePace>();

        var prefabPath = $"{FallingObjectFolder}/{objectName}.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static Bounds GetRendererBounds(Transform root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(root.position, Vector3.zero);

        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static void AssignFallingObstaclesToScene(string scenePath, GameObject[] prefabs)
    {
        if (prefabs == null || prefabs.Length == 0 || !File.Exists(scenePath))
            return;

        var scene = SceneManager.GetSceneByPath(scenePath);
        var openedScene = !scene.IsValid() || !scene.isLoaded;
        if (openedScene)
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

        var spawner = scene.GetRootGameObjects()
            .SelectMany(obj => obj.GetComponentsInChildren<ObstacleSpawnerScript>(true))
            .FirstOrDefault();
        if (spawner == null)
        {
            Debug.LogError($"The scene {scenePath} has no ObstacleSpawnerScript.");
        }
        else
        {
            var serializedSpawner = new SerializedObject(spawner);
            var obstacleArray = serializedSpawner.FindProperty("obstaclePrefabs");
            obstacleArray.arraySize = prefabs.Length;
            for (var i = 0; i < prefabs.Length; i++)
                obstacleArray.GetArrayElementAtIndex(i).objectReferenceValue = prefabs[i];
            serializedSpawner.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        if (openedScene)
            EditorSceneManager.CloseScene(scene, true);
    }

    private static GameObject BuildIndianCheckpoint(Material[] mats)
    {
        if (!File.Exists(CheckpointSourcePath))
        {
            Debug.LogError($"Could not find the original checkpoint prefab at {CheckpointSourcePath}.");
            return null;
        }

        var root = PrefabUtility.LoadPrefabContents(CheckpointSourcePath);
        if (root == null)
        {
            Debug.LogError("Could not load the original checkpoint prefab.");
            return null;
        }

        var gate = new GameObject("IndianCheckpointGate");
        gate.transform.SetParent(root.transform, false);

        // Keep the checkpoint's existing trigger, barrier and score text intact;
        // these are non-colliding decorations added only to the market variant.
        for (var side = -1; side <= 1; side += 2)
        {
            Cube(gate.transform, $"GatePillar_{side}", new Vector3(side * 8.5f, 3.15f, 0), new Vector3(0.42f, 6.3f, 0.42f), side < 0 ? mats[3] : mats[4]);
            Cube(gate.transform, $"GatePillarBase_{side}", new Vector3(side * 8.5f, 0.45f, 0), new Vector3(0.76f, 0.42f, 0.76f), mats[10]);
            Cube(gate.transform, $"GatePillarCap_{side}", new Vector3(side * 8.5f, 6.35f, 0), new Vector3(0.76f, 0.32f, 0.76f), mats[5]);
        }

        Cube(gate.transform, "GateTopBeam", new Vector3(0, 6.42f, 0), new Vector3(18f, 0.8f, 0.55f), mats[4]);

        for (var stripe = -4; stripe <= 4; stripe++)
            Cube(gate.transform, $"CheckpointTricolor_{stripe}", new Vector3(stripe * 0.92f, 6.42f, -0.31f), new Vector3(0.9f, 0.22f, 0.07f), stripe % 3 == 0 ? mats[4] : stripe % 3 == 1 ? mats[10] : mats[6]);

        for (var bead = -11; bead <= 11; bead++)
        {
            var x = bead * 0.36f;
            var y = 6.28f - 0.20f * (1f - Mathf.Abs(bead) / 11f);
            Sphere(gate.transform, $"CheckpointMarigold_{bead}", new Vector3(x, y, -0.34f), Vector3.one * 0.13f, bead % 2 == 0 ? mats[5] : mats[4]);
        }

        var originalLabel = root.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault();
        if (originalLabel != null)
        {
            originalLabel.text = "CHECKPOINT +5 SEC";
            originalLabel.enableAutoSizing = true;
            originalLabel.fontSizeMin = 10;
            originalLabel.fontSizeMax = 14;
            originalLabel.alignment = TextAlignmentOptions.Center;
        }

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, MarketCheckpointPath);
        PrefabUtility.UnloadPrefabContents(root);
        return prefab;
    }

    private static VolumeProfile CreateIndianMarketVolumeProfile()
    {
        const string profilePath = RootFolder + "/IndianMarketVolumeProfile.asset";
        const string sourceProfilePath = "Assets/Settings/SampleSceneProfile.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
        if (profile == null)
        {
            var source = AssetDatabase.LoadAssetAtPath<VolumeProfile>(sourceProfilePath);
            if (source == null)
            {
                Debug.LogError($"Could not find the source volume profile at {sourceProfilePath}.");
                return null;
            }

            profile = Object.Instantiate(source);
            profile.name = "IndianMarketVolumeProfile";
            AssetDatabase.CreateAsset(profile, profilePath);
        }

        if (!profile.TryGet<Bloom>(out var bloom))
            bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(1.1f);
        bloom.intensity.Override(0.25f);

        var depthOfField = profile.components.OfType<DepthOfField>().FirstOrDefault();
        if (depthOfField != null)
            depthOfField.active = false;

        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static void AssignCheckpointToMarketPreview(GameObject checkpointPrefab, VolumeProfile marketVolumeProfile, string previewPath)
    {
        if (!File.Exists(previewPath))
            return;

        var preview = SceneManager.GetSceneByPath(previewPath);
        var openedPreview = !preview.IsValid() || !preview.isLoaded;
        if (openedPreview)
            preview = EditorSceneManager.OpenScene(previewPath, OpenSceneMode.Additive);

        var loader = preview.GetRootGameObjects()
            .SelectMany(obj => obj.GetComponentsInChildren<PlatformChunkLoader>(true))
            .FirstOrDefault();
        var changed = false;
        if (loader != null && checkpointPrefab != null)
        {
            var serializedLoader = new SerializedObject(loader);
            var checkpointProperty = serializedLoader.FindProperty("CheckPointChunk");
            checkpointProperty.objectReferenceValue = checkpointPrefab;
            serializedLoader.ApplyModifiedPropertiesWithoutUndo();
            changed = true;
        }
        else if (loader == null)
        {
            Debug.LogError("The Indian market preview scene has no PlatformChunkLoader for its checkpoint prefab.");
        }

        var volume = preview.GetRootGameObjects()
            .SelectMany(obj => obj.GetComponentsInChildren<Volume>(true))
            .FirstOrDefault();
        if (volume != null && marketVolumeProfile != null)
        {
            volume.sharedProfile = marketVolumeProfile;
            EditorUtility.SetDirty(volume);
            changed = true;
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(preview);
            EditorSceneManager.SaveScene(preview);
        }

        if (openedPreview)
            EditorSceneManager.CloseScene(preview, true);
    }

    private static GameObject[] RebuildEnvironmentPrefabs()
    {
        EnsureFolder("Assets/Environments");
        EnsureFolder(RootFolder);
        EnsureFolder(PrefabFolder);
        EnsureFolder(MaterialFolder);
        EnsureFolder(MeshFolder);

        var materials = CreateMaterials();
        var flagMesh = CreateFlagMesh();
        var prefabs = new GameObject[3];
        for (var i = 0; i < prefabs.Length; i++)
            prefabs[i] = CreateSegmentPrefab(i, materials, flagMesh);
        return prefabs;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        var parent = Path.GetDirectoryName(path).Replace('\\', '/');
        var leaf = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    private static Material[] CreateMaterials()
    {
        var names = new[]
        {
            "Warm Plaster", "Pale Sand", "Terracotta", "Turquoise", "Saffron",
            "Marigold", "Leaf Green", "Deep Indigo", "Wood", "Shadow",
            "Ivory", "Brick Red", "Mint", "Peacock", "Night Blue",
            "Market Road", "Paver Pale", "Paver Rose", "Paver Ochre", "Paver Teal"
        };
        var colors = new[]
        {
            "D9AD78", "F2DFC0", "BD6648", "178887", "E88B27",
            "F4B82E", "3E8A58", "3D4F78", "704A32", "332D30",
            "FFF3D9", "A84839", "A9CDB9", "147276", "253754",
            "B8926B", "DCC8A6", "C9876C", "C8A477", "197875"
        };

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var materials = new Material[names.Length];
        for (var i = 0; i < names.Length; i++)
        {
            var path = $"{MaterialFolder}/{names[i].Replace(' ', '_')}.mat";
            materials[i] = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (materials[i] == null)
            {
                materials[i] = new Material(shader) { name = names[i] };
                AssetDatabase.CreateAsset(materials[i], path);
            }

            ColorUtility.TryParseHtmlString("#" + colors[i], out var color);
            materials[i].color = color;
            if (materials[i].HasProperty("_BaseColor"))
                materials[i].SetColor("_BaseColor", color);
            if (materials[i].HasProperty("_Smoothness"))
                materials[i].SetFloat("_Smoothness", i == 7 || i == 13 ? 0.32f : 0.12f);
            EditorUtility.SetDirty(materials[i]);
        }
        return materials;
    }

    private static Mesh CreateFlagMesh()
    {
        const string path = MeshFolder + "/TriangleBuntingFlag.asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null)
            mesh = new Mesh { name = "Triangle Bunting Flag" };
        mesh.vertices = new[]
        {
            new Vector3(-0.23f, 0.02f, 0),
            new Vector3(0.23f, 0.02f, 0),
            new Vector3(0, -0.48f, 0),
            new Vector3(-0.23f, 0.02f, 0),
            new Vector3(0.23f, 0.02f, 0),
            new Vector3(0, -0.48f, 0)
        };
        // Separate vertices keep the front and back normals independent, so
        // the flags stay correctly lit on both sides.
        mesh.triangles = new[] { 0, 2, 1, 3, 4, 5 };
        mesh.normals = new[]
        {
            Vector3.forward, Vector3.forward, Vector3.forward,
            Vector3.back, Vector3.back, Vector3.back
        };
        mesh.RecalculateBounds();
        if (AssetDatabase.LoadAssetAtPath<Mesh>(path) == null)
            AssetDatabase.CreateAsset(mesh, path);
        else
            EditorUtility.SetDirty(mesh);
        return mesh;
    }

    private static GameObject CreateSegmentPrefab(int variant, Material[] mats, Mesh flagMesh)
    {
        var suffix = (char)('A' + variant);
        var root = new GameObject($"IndianMarketSegment_{suffix}");
        root.transform.position = Vector3.zero;

        var wall = new[] { mats[0], mats[3], mats[12] }[variant];
        var upper = new[] { mats[1], mats[0], mats[1] }[variant];
        var accent = new[] { mats[2], mats[4], mats[11] }[variant];
        var awning = new[] { mats[3], mats[7], mats[13] }[variant];

        for (var side = -1; side <= 1; side += 2)
        {
            BuildShopFront(root.transform, side, variant, wall, upper, accent, awning, mats);
            BuildSidewalkRangoli(root.transform, side, variant, mats);
        }

        BuildMarketFloor(root.transform, variant, mats);
        if (variant == 2)
            BuildTricolorBunting(root.transform, flagMesh, mats);
        BuildStreetLamps(root.transform, variant, mats);

        var prefabPath = $"{PrefabFolder}/IndianMarketSegment_{suffix}.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void BuildMarketFloor(Transform root, int variant, Material[] mats)
    {
        // These meshes sit just above the existing road surface and have no colliders.
        Cube(root, "WarmStoneRoadBase", new Vector3(0, 0.024f, 0), new Vector3(7.92f, 0.045f, 9.82f), mats[15]);

        var paverPalette = new[] { mats[16], mats[17], mats[18], mats[1] };
        for (var row = 0; row < 6; row++)
        {
            var z = -4.03f + row * 1.61f;
            for (var column = 0; column < 5; column++)
            {
                var x = -3.16f + column * 1.58f;
                var paletteIndex = (row + column + variant) % paverPalette.Length;
                Cube(root, $"RoadPaver_{row}_{column}", new Vector3(x, 0.060f, z), new Vector3(1.52f, 0.026f, 1.55f), paverPalette[paletteIndex]);
            }
        }

        for (var side = -1; side <= 1; side += 2)
        {
            Cube(root, $"RoadBorder_{side}", new Vector3(side * 3.88f, 0.078f, 0), new Vector3(0.09f, 0.018f, 9.74f), mats[19]);
            for (var tile = -4; tile <= 4; tile++)
            {
                var material = tile % 2 == 0 ? mats[4] : mats[10];
                Cube(root, $"BorderInlay_{side}_{tile}", new Vector3(side * 3.68f, 0.078f, tile * 1.08f), new Vector3(0.16f, 0.018f, 0.68f), material);
            }
        }

        Cylinder(root, "RangoliRoadBase", new Vector3(0, 0.081f, 0), new Vector3(0.82f, 0.018f, 0.82f), mats[10]);
        Cylinder(root, "RangoliRoadCenter", new Vector3(0, 0.097f, 0), new Vector3(0.22f, 0.016f, 0.22f), variant == 1 ? mats[19] : mats[4]);
        for (var petal = 0; petal < 8; petal++)
        {
            var angle = petal * Mathf.PI / 4f;
            var offset = new Vector3(Mathf.Cos(angle) * 0.49f, 0.10f, Mathf.Sin(angle) * 0.49f);
            Sphere(root, $"RangoliRoadPetal_{petal}", new Vector3(0, 0.10f, 0) + offset, new Vector3(0.20f, 0.025f, 0.34f), petal % 2 == 0 ? mats[5] : mats[3]);
        }
    }

    private static void BuildShopFront(Transform root, int side, int variant, Material wall, Material upper, Material accent, Material awning, Material[] mats)
    {
        // Each module has a distinct skyline: low market stalls, a tall electronics
        // block, and a mixed-height row of neighbourhood shops.
        var facadeHeight = variant == 0 ? 4.65f : variant == 2 ? 6.25f : 8.0f;
        var facade = Cube(root, $"Facade_{side}", new Vector3(side * 8.15f, facadeHeight * 0.5f, 0), new Vector3(5.5f, facadeHeight, 9.9f), wall);
        if (variant == 2)
        {
            Cube(root, $"TallShopTower_{side}", new Vector3(side * 8.15f, 7.7f, -3.15f), new Vector3(5.5f, 2.9f, 3.55f), upper);
            Cube(root, $"TowerWindow_{side}", new Vector3(side * 5.28f, 7.5f, -3.15f), new Vector3(0.10f, 1.35f, 2.25f), mats[14]);
            Cube(root, $"TowerWindowMullion_{side}", new Vector3(side * 5.20f, 7.5f, -3.15f), new Vector3(0.08f, 1.35f, 0.10f), accent);
        }
        Cube(root, $"Plinth_{side}", new Vector3(side * 5.30f, 0.38f, 0), new Vector3(0.25f, 0.76f, 9.9f), mats[8]);
        Cube(root, $"RoofBand_{side}", new Vector3(side * 5.27f, facadeHeight - 0.2f, 0), new Vector3(0.42f, 0.32f, 10.0f), accent);
        if (variant == 2)
            Cube(root, $"TowerRoofBand_{side}", new Vector3(side * 5.27f, 9.1f, -3.15f), new Vector3(0.42f, 0.32f, 3.7f), accent);

        var faceX = side * 5.32f;
        for (var zIndex = -1; zIndex <= 1; zIndex++)
        {
            var z = zIndex * 2.8f;
            var door = zIndex == 0;
            var baseY = door ? 1.35f : 1.65f;
            var width = door ? 1.65f : 1.45f;
            Cube(root, $"ShopOpening_{side}_{zIndex}", new Vector3(faceX, baseY, z), new Vector3(0.10f, door ? 2.7f : 2.15f, width), mats[9]);
            Cube(root, $"ShopJamb_{side}_{zIndex}", new Vector3(side * 5.22f, baseY + (door ? 1.36f : 1.1f), z), new Vector3(0.22f, 0.18f, width + 0.32f), mats[10]);
            if (!door)
            {
                Cube(root, $"Shutter_{side}_{zIndex}", new Vector3(side * 5.24f, 1.42f, z), new Vector3(0.12f, 1.95f, width - 0.12f), variant == 1 ? mats[13] : mats[8]);
                for (var slat = 0; slat < 5; slat++)
                    Cube(root, $"ShutterSlat_{side}_{zIndex}_{slat}", new Vector3(side * 5.15f, 0.72f + slat * 0.29f, z), new Vector3(0.045f, 0.035f, width - 0.16f), mats[1]);
            }
            else
            {
                Cube(root, $"Door_{side}", new Vector3(side * 5.24f, 1.25f, z), new Vector3(0.12f, 2.25f, 1.34f), accent);
                Cube(root, $"DoorPanel_{side}", new Vector3(side * 5.16f, 1.28f, z), new Vector3(0.04f, 1.75f, 0.98f), mats[8]);
                Sphere(root, $"DoorKnob_{side}", new Vector3(side * 5.12f, 1.25f, z + 0.39f), new Vector3(0.10f, 0.10f, 0.10f), mats[5]);
            }
        }

        var awningY = variant == 0 ? 2.75f : 3.02f;
        Cube(root, $"ShopAwning_{side}", new Vector3(side * 4.94f, awningY, 0), new Vector3(1.05f, 0.16f, 9.6f), mats[1]);
        for (var stripe = -4; stripe <= 4; stripe++)
        {
            var stripeMat = stripe % 2 == 0 ? awning : accent;
            Cube(root, $"AwningStripe_{side}_{stripe}", new Vector3(side * 4.92f, awningY - 0.09f, stripe * 1.05f), new Vector3(1.0f, 0.08f, 0.54f), stripeMat);
            Cube(root, $"AwningDrop_{side}_{stripe}", new Vector3(side * 4.45f, awningY - 0.30f, stripe * 1.05f), new Vector3(0.07f, 0.42f, 0.53f), stripeMat);
        }

        Cube(root, $"Signboard_{side}", new Vector3(side * 5.19f, 3.62f, 0), new Vector3(0.24f, 0.76f, 3.2f), accent);
        Cube(root, $"SignInset_{side}", new Vector3(side * 5.04f, 3.62f, 0), new Vector3(0.06f, 0.52f, 2.92f), mats[10]);
        var sign = new GameObject($"ShopSign_{side}");
        sign.transform.SetParent(root, false);
        sign.transform.localPosition = new Vector3(side * 4.98f, 3.62f, 0);
        sign.transform.localRotation = Quaternion.Euler(0, side > 0 ? -90 : 90, 0);
        var text = sign.AddComponent<TextMesh>();
        text.text = variant == 1 ? "MOBILES" : variant == 2 ? "BAZAAR" : "CHAI";
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.fontSize = 42;
        text.characterSize = 0.105f;
        text.color = mats[14].color;

        for (var bead = -7; bead <= 7; bead++)
        {
            var z = bead * 0.22f;
            var y = 4.12f - 0.28f * (1f - Mathf.Abs(bead) / 7f);
            Sphere(root, $"MarigoldToran_{side}_{bead}", new Vector3(side * 5.13f, y, z), Vector3.one * 0.17f, bead % 2 == 0 ? mats[5] : mats[4]);
        }

        if (variant != 0)
        {
            var upperY = variant == 2 ? 5.05f : 6.15f;
            Cube(root, $"UpperFloorBand_{side}", new Vector3(side * 5.22f, variant == 2 ? 4.25f : 4.45f, 0), new Vector3(0.25f, 0.20f, 9.8f), mats[10]);
            for (var zIndex = -1; zIndex <= 1; zIndex++)
            {
                var z = zIndex * 2.8f;
                var windowY = variant == 2 ? upperY : upperY;
                Cube(root, $"UpperWindowFrame_{side}_{zIndex}", new Vector3(side * 5.30f, windowY, z), new Vector3(0.18f, 1.35f, 1.52f), mats[10]);
                Cube(root, $"UpperWindowPane_{side}_{zIndex}", new Vector3(side * 5.19f, windowY + 0.03f, z), new Vector3(0.05f, 1.05f, 1.18f), mats[14]);
                Cube(root, $"UpperWindowMullionV_{side}_{zIndex}", new Vector3(side * 5.12f, windowY + 0.03f, z), new Vector3(0.08f, 1.1f, 0.08f), upper);
                Cube(root, $"UpperWindowMullionH_{side}_{zIndex}", new Vector3(side * 5.12f, windowY + 0.03f, z), new Vector3(0.08f, 0.08f, 1.2f), upper);
            }

            var balconyY = variant == 2 ? 4.15f : 5.14f;
            var railTopY = variant == 2 ? 4.8f : 5.88f;
            Cube(root, $"BalconyFloor_{side}", new Vector3(side * 5.05f, balconyY, 0), new Vector3(0.72f, 0.18f, 9.8f), mats[10]);
            Cube(root, $"BalconyRailTop_{side}", new Vector3(side * 4.67f, railTopY, 0), new Vector3(0.12f, 0.12f, 9.7f), accent);
            Cube(root, $"BalconyRailBottom_{side}", new Vector3(side * 4.67f, balconyY + 0.22f, 0), new Vector3(0.10f, 0.10f, 9.7f), accent);
            for (var post = -4; post <= 4; post++)
                Cube(root, $"BalconyPost_{side}_{post}", new Vector3(side * 4.67f, (railTopY + balconyY + 0.22f) * 0.5f, post * 1.1f), new Vector3(0.10f, railTopY - balconyY - 0.1f, 0.10f), accent);

            for (var panel = 0; panel < 5; panel++)
            {
                var z = (panel - 2) * 1.72f;
                Cube(root, $"BalconyFabric_{side}_{panel}", new Vector3(side * 4.91f, balconyY - 0.35f, z), new Vector3(0.12f, 0.48f, 0.9f), panel % 2 == 0 ? mats[4] : mats[10]);
            }
        }
        else
        {
            // Shallow stall canopy roofs give the low shops their own stepped outline.
            for (var stall = -1; stall <= 1; stall++)
                Cube(root, $"StallCanopy_{side}_{stall}", new Vector3(side * 7.95f, 4.95f, stall * 3.1f), new Vector3(5.7f, 0.22f, 2.65f), stall % 2 == 0 ? accent : upper);
        }

        for (var lampZ = -1; lampZ <= 1; lampZ += 2)
        {
            Cube(root, $"LanternBracket_{side}_{lampZ}", new Vector3(side * 5.17f, 4.30f, lampZ * 2.05f), new Vector3(0.48f, 0.07f, 0.07f), mats[8]);
            Cube(root, $"Lantern_{side}_{lampZ}", new Vector3(side * 4.95f, 4.05f, lampZ * 2.05f), new Vector3(0.20f, 0.32f, 0.20f), mats[5]);
        }

        if (variant != 0)
        {
            var roofY = variant == 2 ? 6.35f : 8.15f;
            Cube(root, $"RoofParapet_{side}", new Vector3(side * 8.15f, roofY, 0), new Vector3(5.55f, 0.32f, 10.0f), upper);
            for (var parapet = -4; parapet <= 4; parapet++)
                Cube(root, $"ParapetBlock_{side}_{parapet}", new Vector3(side * 5.78f, roofY + 0.27f, parapet * 1.08f), new Vector3(0.26f, 0.24f, 0.42f), accent);

            var domeZ = variant == 2 ? 0.4f : -0.4f;
            var domeY = variant == 2 ? 7.0f : 8.75f;
            var dome = Cylinder(root, $"RoofDome_{side}", new Vector3(side * 8.15f, domeY, domeZ), new Vector3(1.2f, 0.55f, 1.2f), accent);
            dome.transform.localScale = new Vector3(1.35f, 0.72f, 1.35f);
            Cylinder(root, $"DomeFinial_{side}", new Vector3(side * 8.15f, domeY + 0.6f, domeZ), new Vector3(0.18f, 0.28f, 0.18f), mats[5]);
        }

        if (variant == 1)
        {
            // Bright display panels and phone silhouettes make this clearly read as an electronics shop.
            for (var display = -1; display <= 1; display++)
            {
                var z = display * 2.8f;
                Cube(root, $"ElectronicsDisplay_{side}_{display}", new Vector3(side * 5.12f, 2.55f, z), new Vector3(0.08f, 0.68f, 0.82f), display == 0 ? mats[19] : mats[16]);
                Cube(root, $"PhoneDisplay_{side}_{display}", new Vector3(side * 5.05f, 2.55f, z), new Vector3(0.06f, 0.42f, 0.20f), mats[14]);
            }
            Cube(root, $"ElectronicsAntenna_{side}", new Vector3(side * 8.15f, 9.05f, 3.7f), new Vector3(0.10f, 1.7f, 0.10f), mats[8]);
            Cube(root, $"AntennaCrossbar_{side}", new Vector3(side * 8.15f, 9.4f, 3.7f), new Vector3(0.10f, 0.10f, 0.75f), mats[8]);
        }

        for (var plant = -1; plant <= 1; plant += 2)
        {
            var z = plant * 4.0f;
            Cylinder(root, $"Planter_{side}_{plant}", new Vector3(side * 5.45f, 0.46f, z), new Vector3(0.44f, 0.46f, 0.44f), mats[2]);
            Sphere(root, $"PlantLeavesA_{side}_{plant}", new Vector3(side * 5.45f, 1.23f, z), new Vector3(0.24f, 0.96f, 0.24f), mats[6]);
            Sphere(root, $"PlantLeavesB_{side}_{plant}", new Vector3(side * 5.20f, 1.08f, z + 0.16f), new Vector3(0.22f, 0.72f, 0.30f), mats[6]);
            Sphere(root, $"PlantLeavesC_{side}_{plant}", new Vector3(side * 5.70f, 1.08f, z - 0.16f), new Vector3(0.22f, 0.72f, 0.30f), mats[6]);
        }
    }

    private static void BuildSidewalkRangoli(Transform root, int side, int variant, Material[] mats)
    {
        Cube(root, $"Sidewalk_{side}", new Vector3(side * 4.60f, -0.08f, 0), new Vector3(0.92f, 0.16f, 9.9f), mats[1]);
        for (var tile = -4; tile <= 4; tile++)
        {
            Cube(root, $"SidewalkTile_{side}_{tile}", new Vector3(side * 4.59f, 0.015f, tile * 1.08f), new Vector3(0.88f, 0.025f, 1.02f), tile % 2 == 0 ? mats[0] : mats[1]);
        }

        var centerZ = variant == 1 ? -2.7f : 2.7f;
        Cylinder(root, $"RangoliBase_{side}", new Vector3(side * 4.58f, 0.055f, centerZ), new Vector3(0.74f, 0.04f, 0.74f), mats[10]);
        Cylinder(root, $"RangoliCenter_{side}", new Vector3(side * 4.58f, 0.084f, centerZ), new Vector3(0.23f, 0.025f, 0.23f), variant == 2 ? mats[3] : mats[4]);
        for (var petal = 0; petal < 8; petal++)
        {
            var angle = petal * Mathf.PI / 4f;
            var offset = new Vector3(Mathf.Cos(angle) * 0.46f, 0.10f, Mathf.Sin(angle) * 0.46f);
            Sphere(root, $"RangoliPetal_{side}_{petal}", new Vector3(side * 4.58f, 0.10f, centerZ) + offset, new Vector3(0.20f, 0.045f, 0.32f), petal % 2 == 0 ? mats[5] : mats[4]);
        }
    }

    private static void BuildTricolorBunting(Transform root, Mesh flagMesh, Material[] mats)
    {
        var left = new Vector3(-4.5f, 6.1f, 0);
        var right = new Vector3(4.5f, 6.1f, 0);
        CylinderBetween(root, "BuntingCord", left, right, 0.035f, mats[8]);
        var flags = new[] { mats[4], mats[10], mats[6] };
        for (var i = 0; i < 7; i++)
        {
            var t = i / 6f;
            var p = Vector3.Lerp(left, right, t);
            p.y -= 0.30f * (1f - Mathf.Abs(2f * t - 1f));
            p.y -= 0.05f;
            var flag = new GameObject($"TricolorFlag_{i:00}");
            flag.transform.SetParent(root, false);
            flag.transform.localPosition = p;
            flag.AddComponent<MeshFilter>().sharedMesh = flagMesh;
            flag.AddComponent<MeshRenderer>().sharedMaterial = flags[i % flags.Length];
        }
    }

    private static void BuildStreetLamps(Transform root, int variant, Material[] mats)
    {
        for (var side = -1; side <= 1; side += 2)
        {
            var z = variant == 2 ? -3.2f : 3.2f;
            Cylinder(root, $"StreetLampPost_{side}", new Vector3(side * 4.78f, 2.05f, z), new Vector3(0.10f, 2.05f, 0.10f), mats[8]);
            Cube(root, $"StreetLampArm_{side}", new Vector3(side * 4.98f, 3.96f, z), new Vector3(0.56f, 0.09f, 0.09f), mats[8]);
            Sphere(root, $"StreetLampGlow_{side}", new Vector3(side * 5.22f, 3.75f, z), new Vector3(0.23f, 0.35f, 0.23f), mats[5]);
        }
    }

    private static GameObject Cube(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        return Primitive(parent, PrimitiveType.Cube, name, position, scale, material);
    }

    private static GameObject Sphere(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        return Primitive(parent, PrimitiveType.Sphere, name, position, scale, material);
    }

    private static GameObject Cylinder(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        return Primitive(parent, PrimitiveType.Cylinder, name, position, scale, material);
    }

    private static GameObject Primitive(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material)
    {
        var obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = material;
        var collider = obj.GetComponent<Collider>();
        if (collider != null)
            Object.DestroyImmediate(collider);
        return obj;
    }

    private static void CylinderBetween(Transform root, string name, Vector3 start, Vector3 end, float radius, Material material)
    {
        var direction = end - start;
        var obj = Cylinder(root, name, (start + end) * 0.5f, new Vector3(radius, direction.magnitude * 0.5f, radius), material);
        obj.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
    }
}
