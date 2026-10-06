using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Menyusun scene Praktikum 5 (Enemy FSM) secara otomatis lewat menu "Praktikum 5".
/// Arena 40 x 40 m: dinding batu + batu + pohon (layer Obstacle), 4 waypoint patroli, 2 SafePoint,
/// Player (Supersoldier) dan Enemy (Skeleton Synty), lalu NavMesh di-bake.
/// </summary>
public static class Praktikum5SceneBuilder
{
    private const string SceneFolder = "Assets/Scenes/Praktikum5";
    private const string MaterialFolder = SceneFolder + "/Materials";
    private const string ScenePath = SceneFolder + "/Praktikum05_FSM.unity";
    private const string EnemyControllerPath = SceneFolder + "/EnemyAnimation.controller";

    private const string SkeletonPrefab = "Assets/Synty/PolygonGeneric/Prefabs/Characters/SM_Gen_Chr_Skeleton_01.prefab";
    private const string SoldierModel = "Assets/Materials/sci-fi-supersoldier-game-ready-animated/source/Idle.fbx";
    private const string RunningClipModel = "Assets/Materials/sci-fi-supersoldier-game-ready-animated/animations/Running.fbx";
    private const string PlayerController = "Assets/Animations/PlayerAnimation.controller";

    private const string PolyRocks = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Rocks/";
    private const string SyntyStarter = "Assets/Synty/PolygonStarter/Prefabs/";

    private const float ArenaHalf = 20f;
    private const float ChaseSpeed = 4.5f;

    [MenuItem("Praktikum 5/Build Scene - Enemy FSM")]
    private static void BuildScene()
    {
        int obstacleLayer = LayerMask.NameToLayer("Obstacle");
        if (obstacleLayer < 0)
        {
            EditorUtility.DisplayDialog("Praktikum 5", "Layer 'Obstacle' belum ada. Tambahkan di Project Settings > Tags and Layers.", "OK");
            return;
        }
        if (!PrepareNewScene())
            return;

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        Material groundMat = GetMaterial("P5_Ground", new Color(0.42f, 0.6f, 0.32f));
        Material wallMat = GetMaterial("P5_StoneWall", new Color(0.52f, 0.5f, 0.47f));
        Material borderMat = GetMaterial("P5_Border", new Color(0.4f, 0.32f, 0.24f));
        Material waypointMat = GetMaterial("P5_Waypoint", new Color(0.2f, 0.85f, 1f), unlit: true);
        Material safeMat = GetMaterial("P5_SafePoint", new Color(0.2f, 1f, 0.45f), unlit: true);
        Material indicatorMat = GetMaterial("P5_StateIndicator", Color.white, unlit: true);

        // ---------------------------------------------------------------- Environment
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(ArenaHalf * 0.2f + 0.4f, 1f, ArenaHalf * 0.2f + 0.4f);
        ground.GetComponent<Renderer>().sharedMaterial = groundMat;

        var env = new GameObject("Environment").transform;

        var border = new GameObject("Border").transform;
        border.SetParent(env, false);
        float b = ArenaHalf + 0.5f;
        CreateBox(border, "Border_N", new Vector3(0f, 0.75f, b), new Vector3(2 * b + 1f, 1.5f, 1f), obstacleLayer, borderMat);
        CreateBox(border, "Border_S", new Vector3(0f, 0.75f, -b), new Vector3(2 * b + 1f, 1.5f, 1f), obstacleLayer, borderMat);
        CreateBox(border, "Border_E", new Vector3(b, 0.75f, 0f), new Vector3(1f, 1.5f, 2 * b + 1f), obstacleLayer, borderMat);
        CreateBox(border, "Border_W", new Vector3(-b, 0.75f, 0f), new Vector3(1f, 1.5f, 2 * b + 1f), obstacleLayer, borderMat);

        // Dinding tinggi untuk menguji Line of Sight: Player bersembunyi di baliknya.
        var walls = new GameObject("Walls (Obstacle - LoS)").transform;
        walls.SetParent(env, false);
        CreateBox(walls, "Wall_West", new Vector3(-6f, 1.5f, 4f), new Vector3(8f, 3f, 1f), obstacleLayer, wallMat);
        CreateBox(walls, "Wall_East", new Vector3(6f, 1.5f, 2f), new Vector3(1f, 3f, 7f), obstacleLayer, wallMat);
        CreateBox(walls, "Wall_South", new Vector3(-1f, 1.5f, -9f), new Vector3(7f, 3f, 1f), obstacleLayer, wallMat);
        CreateBox(walls, "Wall_SafeA", new Vector3(13.5f, 1.5f, 13.5f), new Vector3(5f, 3f, 1f), obstacleLayer, wallMat);
        CreateBox(walls, "Wall_SafeB", new Vector3(-13.5f, 1.5f, -13.5f), new Vector3(1f, 3f, 5f), obstacleLayer, wallMat);

        var props = new GameObject("Props (Obstacle)").transform;
        props.SetParent(env, false);
        PlaceProp(props, PolyRocks + "PT_Generic_Rock_01.prefab", new Vector3(-2f, 0f, 12f), 2.2f, 20f, obstacleLayer, trunk: false);
        PlaceProp(props, PolyRocks + "PT_Menhir_Rock_02.prefab", new Vector3(14f, 0f, 2f), 3.5f, 0f, obstacleLayer, trunk: false);
        PlaceProp(props, PolyRocks + "PT_Generic_Rock_01.prefab", new Vector3(-15f, 0f, 3f), 1.8f, 140f, obstacleLayer, trunk: false);
        PlaceProp(props, PolyRocks + "PT_Ore_Rock_01.prefab", new Vector3(9f, 0f, -14f), 2f, 60f, obstacleLayer, trunk: false);
        PlaceProp(props, SyntyStarter + "SM_Generic_Tree_03.prefab", new Vector3(-17f, 0f, 17f), 7f, 0f, obstacleLayer, trunk: true);
        PlaceProp(props, SyntyStarter + "SM_Generic_Tree_04.prefab", new Vector3(3f, 0f, 17f), 6.5f, 45f, obstacleLayer, trunk: true);
        PlaceProp(props, SyntyStarter + "SM_Generic_Tree_01.prefab", new Vector3(17f, 0f, -4f), 5.5f, 90f, obstacleLayer, trunk: true);
        PlaceProp(props, SyntyStarter + "SM_Generic_Tree_02.prefab", new Vector3(-9f, 0f, -16f), 5.5f, 200f, obstacleLayer, trunk: true);
        PlaceProp(props, SyntyStarter + "SM_Generic_Tree_02.prefab", new Vector3(17f, 0f, -17f), 6f, 0f, obstacleLayer, trunk: true);
        PlaceProp(props, SyntyStarter + "SM_Generic_Tree_01.prefab", new Vector3(-3f, 0f, -3f), 5f, 30f, obstacleLayer, trunk: true);

        // ---------------------------------------------------------------- Waypoints & SafePoints
        var wpRoot = new GameObject("Waypoints").transform;
        Vector3[] wpPositions =
        {
            new Vector3(-12f, 0f, 12f),
            new Vector3(10f, 0f, 9f),
            new Vector3(11f, 0f, -6f),
            new Vector3(-11f, 0f, -5f),
        };
        var waypoints = new Transform[wpPositions.Length];
        for (int i = 0; i < wpPositions.Length; i++)
            waypoints[i] = CreateMarker(wpRoot, $"WP_{i + 1}", wpPositions[i], 0.5f, waypointMat).transform;

        var safeRoot = new GameObject("SafePoints").transform;
        SafePoint safeA = CreateSafePoint(safeRoot, "SafePoint_A (NE)", new Vector3(15f, 0f, 17f), safeMat);
        SafePoint safeB = CreateSafePoint(safeRoot, "SafePoint_B (SW)", new Vector3(-17f, 0f, -15f), safeMat);

        // ---------------------------------------------------------------- Player
        Camera cam = Object.FindAnyObjectByType<Camera>();
        GameObject player = CreatePlayer(new Vector3(0f, 0f, -15f), cam);
        Health playerHealth = player.GetComponent<Health>();

        // ---------------------------------------------------------------- Enemy
        GameObject enemy = CreateEnemy(new Vector3(-11f, 0f, 9f), player.transform, obstacleLayer,
            waypoints, new[] { safeA, safeB }, indicatorMat);

        // ---------------------------------------------------------------- Kamera, HUD, NavMesh
        cam.transform.SetPositionAndRotation(new Vector3(0f, 5f, -24f), Quaternion.Euler(15f, 0f, 0f));
        var follow = cam.gameObject.AddComponent<CameraFollow>();
        Set(follow, "target", player.transform);
        Set(follow, "distance", 9f);
        Set(follow, "height", 2.5f);

        var hud = new GameObject("HUD (FSM Debug)").AddComponent<FSMDebugHUD>();
        Set(hud, "enemy", enemy.GetComponent<EnemyFSM>());
        Set(hud, "enemyHealth", enemy.GetComponent<Health>());
        Set(hud, "playerHealth", playerHealth);
        SetArray(hud, "disableOnPlayerDeath", new Object[]
        {
            player.GetComponent<global::PlayerController>(),
            player.GetComponent<PlayerMeleeAttack>(),
        });

        var surfaceObject = new GameObject("NavMeshSurface");
        var surface = surfaceObject.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings(ScenePath);
        Selection.activeGameObject = enemy;

        BakeAndSave(surface, scene);
    }

    // ------------------------------------------------------------------ Player

    private static GameObject CreatePlayer(Vector3 position, Camera cam)
    {
        var player = new GameObject("Player");
        player.transform.position = position;

        var cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.35f;
        cc.center = new Vector3(0f, 0.9f, 0f);

        var controller = player.AddComponent<global::PlayerController>();
        Set(controller, "cameraTransform", cam.transform);

        var health = player.AddComponent<Health>();
        Set(health, "maxHealth", 150f);
        Set(health, "current", 150f);

        var model = InstantiateAsset(SoldierModel, player.transform);
        if (model != null)
        {
            model.name = "Supersoldier";
            // FBX ini ikut membawa Light dan Camera dari file sumbernya. Light-nya membuat area sekitar
            // Player silau, jadi dimatikan (sama seperti di scene NPCDetector).
            foreach (Light l in model.GetComponentsInChildren<Light>(true))
                l.gameObject.SetActive(false);
            foreach (Camera c in model.GetComponentsInChildren<Camera>(true))
                c.gameObject.SetActive(false);
            Animator animator = GetOrAdd<Animator>(model);
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PlayerController);
            animator.applyRootMotion = false;
        }

        // Driver dipasang setelah model ada supaya Animator langsung ditemukan.
        var driver = player.AddComponent<PlayerAnimatorDriver>();
        if (model != null)
            Set(driver, "animator", model.GetComponent<Animator>());

        var melee = player.AddComponent<PlayerMeleeAttack>();
        if (model != null)
            Set(melee, "model", model.transform);

        IgnoreFromBake(player);
        return player;
    }

    // ------------------------------------------------------------------ Enemy

    private static GameObject CreateEnemy(Vector3 position, Transform player, int obstacleLayer,
        Transform[] waypoints, SafePoint[] safePoints, Material indicatorMat)
    {
        var enemy = new GameObject("Enemy_Skeleton (FSM)");
        enemy.transform.position = position;
        enemy.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

        var collider = enemy.AddComponent<CapsuleCollider>();
        collider.height = 1.8f;
        collider.radius = 0.35f;
        collider.center = new Vector3(0f, 0.9f, 0f);

        var agent = enemy.AddComponent<NavMeshAgent>();
        agent.baseOffset = 0f;
        agent.radius = 0.4f;
        agent.height = 1.8f;
        agent.speed = 2f;
        agent.angularSpeed = 540f;
        agent.acceleration = 16f;
        agent.autoBraking = true;

        enemy.AddComponent<Health>();

        var perception = enemy.AddComponent<EnemyPerception>();
        Set(perception, "target", player);
        SetMask(perception, "obstacleMask", 1 << obstacleLayer);

        var fsm = enemy.AddComponent<EnemyFSM>();
        SetArray(fsm, "waypoints", waypoints);
        SetArray(fsm, "safePoints", safePoints);
        Set(fsm, "chaseSpeed", ChaseSpeed);

        // Model Skeleton (Humanoid) memakai clip Idle/Running Supersoldier lewat retarget Humanoid.
        GameObject model = InstantiateAsset(SkeletonPrefab, enemy.transform);
        Animator animator = null;
        if (model != null)
        {
            model.name = "Skeleton_Model";
            animator = GetOrAdd<Animator>(model);
            animator.runtimeAnimatorController = CreateEnemyAnimator();
            animator.applyRootMotion = false;
            foreach (Collider c in model.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(c);
        }

        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        indicator.name = "StateIndicator";
        Object.DestroyImmediate(indicator.GetComponent<Collider>());
        indicator.transform.SetParent(enemy.transform, false);
        indicator.transform.localPosition = new Vector3(0f, 2.25f, 0f);
        indicator.transform.localScale = new Vector3(0.25f, 0.35f, 0.25f);
        indicator.GetComponent<Renderer>().sharedMaterial = indicatorMat;

        var visual = enemy.AddComponent<EnemyVisualFeedback>();
        Set(visual, "fsm", fsm);
        Set(visual, "health", enemy.GetComponent<Health>());
        Set(visual, "agent", agent);
        if (animator != null)
            Set(visual, "animator", animator);
        if (model != null)
            Set(visual, "model", model.transform);
        Set(visual, "stateIndicator", indicator.GetComponent<Renderer>());

        IgnoreFromBake(enemy);
        return enemy;
    }

    /// <summary>Animator Enemy: satu blend tree Idle (Speed 0) -> Running (Speed = chase speed).</summary>
    private static RuntimeAnimatorController CreateEnemyAnimator()
    {
        AnimationClip idle = FirstClip(SoldierModel);
        AnimationClip run = FirstClip(RunningClipModel);
        if (idle == null || run == null)
        {
            Debug.LogWarning("[Praktikum 5] Clip Idle/Running tidak ditemukan, Enemy tanpa animasi.");
            return null;
        }

        AssetDatabase.DeleteAsset(EnemyControllerPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(EnemyControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);

        AnimatorState state = controller.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
        tree.blendType = BlendTreeType.Simple1D;
        tree.blendParameter = "Speed";
        tree.useAutomaticThresholds = false;
        tree.AddChild(idle, 0f);
        tree.AddChild(run, ChaseSpeed);
        controller.layers[0].stateMachine.defaultState = state;
        AssetDatabase.SaveAssets();
        return controller;
    }

    private static AnimationClip FirstClip(string modelPath)
    {
        return AssetDatabase.LoadAllAssetsAtPath(modelPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
    }

    // ------------------------------------------------------------------ Waypoint / SafePoint / Prop

    private static GameObject CreateMarker(Transform parent, string name, Vector3 position, float radius, Material mat)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = name;
        Object.DestroyImmediate(marker.GetComponent<Collider>());
        marker.transform.SetParent(parent, false);
        marker.transform.position = position + Vector3.up * 0.02f;
        marker.transform.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);
        marker.GetComponent<Renderer>().sharedMaterial = mat;
        return marker;
    }

    private static SafePoint CreateSafePoint(Transform parent, string name, Vector3 position, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        var sp = go.AddComponent<SafePoint>();
        Set(sp, "radius", 1.5f);

        GameObject pad = CreateMarker(go.transform, "Pad", position, 1.5f, mat);
        pad.transform.localScale = new Vector3(3f, 0.02f, 3f);
        return sp;
    }

    /// <summary>
    /// Pasang prefab dekorasi sebagai obstacle: diskalakan ke tinggi tertentu, diberi layer Obstacle,
    /// dan diberi collider (MeshCollider untuk batu, CapsuleCollider di batang untuk pohon) agar
    /// ikut di-bake ke NavMesh dan menghalangi Raycast Line of Sight.
    /// </summary>
    private static void PlaceProp(Transform parent, string path, Vector3 position, float targetHeight,
        float rotY, int layer, bool trunk)
    {
        GameObject prop = InstantiateAsset(path, parent);
        if (prop == null)
            return;

        prop.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, rotY, 0f));
        Bounds bounds = RenderBounds(prop);
        if (bounds.size.y > 0.01f)
            prop.transform.localScale *= targetHeight / bounds.size.y;

        foreach (Transform t in prop.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = layer;

        foreach (Collider c in prop.GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(c);

        if (trunk)
        {
            var capsule = prop.AddComponent<CapsuleCollider>();
            float s = prop.transform.lossyScale.y;
            capsule.radius = 0.45f / s;
            capsule.height = 3f / s;
            capsule.center = new Vector3(0f, 1.5f / s, 0f);
        }
        else
        {
            foreach (MeshFilter mf in prop.GetComponentsInChildren<MeshFilter>())
                mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
        }
        GameObjectUtility.SetStaticEditorFlags(prop, StaticEditorFlags.NavigationStatic);
    }

    private static Bounds RenderBounds(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(go.transform.position, Vector3.zero);
        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers)
            b.Encapsulate(r.bounds);
        return b;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    private static GameObject InstantiateAsset(string path, Transform parent)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null)
        {
            Debug.LogWarning($"[Praktikum 5] Aset tidak ditemukan: {path}");
            return null;
        }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        return go;
    }

    // ------------------------------------------------------------------ NavMesh & scene

    private static void IgnoreFromBake(GameObject go)
    {
        var modifier = go.AddComponent<NavMeshModifier>();
        modifier.ignoreFromBuild = true;
    }

    private static void BakeAndSave(NavMeshSurface surface, Scene scene)
    {
        surface.BuildNavMesh();

        string dataFolder = ScenePath.Substring(0, ScenePath.Length - ".unity".Length);
        EnsureFolder(dataFolder);
        string assetPath = $"{dataFolder}/NavMesh-{surface.name}.asset";
        AssetDatabase.DeleteAsset(assetPath);
        AssetDatabase.CreateAsset(surface.navMeshData, assetPath);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[Praktikum 5] Scene tersimpan di {ScenePath}, NavMesh sudah di-bake ({assetPath}).");
    }

    /// <summary>Entry point command line: Unity -batchmode -executeMethod Praktikum5SceneBuilder.BuildSceneBatch</summary>
    public static void BuildSceneBatch()
    {
        BuildScene();
    }

    private static bool PrepareNewScene()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return false;

        if (!Application.isBatchMode && AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
            !EditorUtility.DisplayDialog("Praktikum 5", $"{ScenePath} sudah ada. Timpa dengan scene baru?", "Timpa", "Batal"))
            return false;

        EnsureFolder(SceneFolder);
        EnsureFolder(MaterialFolder);
        return true;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }

    private static Material GetMaterial(string name, Color color, bool unlit = false)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null)
            return mat;

        Shader shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        mat = new Material(shader) { color = color };
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static GameObject CreateBox(Transform parent, string name, Vector3 center, Vector3 size, int layer, Material mat)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.layer = layer;
        box.transform.SetParent(parent, false);
        box.transform.position = center;
        box.transform.localScale = size;
        box.GetComponent<Renderer>().sharedMaterial = mat;
        GameObjectUtility.SetStaticEditorFlags(box, StaticEditorFlags.NavigationStatic);
        return box;
    }

    private static void AddToBuildSettings(string scenePath)
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == scenePath))
            return;
        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // Field script bersifat private [SerializeField], jadi diisi lewat SerializedObject.
    private static void Set(Component target, string property, object value)
    {
        var so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(property);
        if (prop == null)
        {
            Debug.LogError($"[Praktikum 5] Field '{property}' tidak ditemukan di {target.GetType().Name}.");
            return;
        }

        switch (value)
        {
            case Object obj: prop.objectReferenceValue = obj; break;
            case float f: prop.floatValue = f; break;
            case int i: prop.intValue = i; break;
            case bool bo: prop.boolValue = bo; break;
            default:
                Debug.LogError($"[Praktikum 5] Tipe {value?.GetType().Name} belum didukung untuk '{property}'.");
                return;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetArray(Component target, string property, Object[] values)
    {
        var so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(property);
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetMask(Component target, string property, int mask)
    {
        var so = new SerializedObject(target);
        so.FindProperty(property).intValue = mask;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
