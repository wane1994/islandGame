using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif

[ExecuteAlways]
public sealed class IslandTerrainGenerator : MonoBehaviour
{
    [Header("Terrain")]
    [SerializeField] private int heightmapResolution = 257;
    [SerializeField] private int alphamapResolution = 256;
    [SerializeField] private Vector3 terrainSize = new Vector3(240f, 56f, 240f);
    [SerializeField] private float waterLevel = 3.4f;
    [SerializeField] private int seed = 7241;

    [Header("Details")]
    [SerializeField] private int rockCount = 34;
    [SerializeField] private int palmCount = 26;
    [SerializeField] private int crystalCount = 12;
    [SerializeField] private int enemyCount = 5;
    [SerializeField] private int healItemCount = 3;
    [SerializeField] private int megaEnemyCount = 1;

    private const string GeneratedRootName = "Generated Island";
    private const string PlayerName = "Player";
#if UNITY_EDITOR
    private bool generationQueued;
#endif

    private void OnEnable()
    {
        QueueGenerate();
    }

    private void OnValidate()
    {
        heightmapResolution = Mathf.ClosestPowerOfTwo(Mathf.Max(33, heightmapResolution - 1)) + 1;
        alphamapResolution = Mathf.ClosestPowerOfTwo(Mathf.Max(16, alphamapResolution));
        rockCount = Mathf.Max(0, rockCount);
        palmCount = Mathf.Max(0, palmCount);
        crystalCount = Mathf.Max(1, crystalCount);
        enemyCount = Mathf.Max(0, enemyCount);

        if (isActiveAndEnabled)
        {
            QueueGenerate();
        }
    }

    private void QueueGenerate()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (generationQueued)
            {
                return;
            }

            generationQueued = true;
            EditorApplication.delayCall += () =>
            {
                generationQueued = false;
                if (this != null && isActiveAndEnabled)
                {
                    Generate();
                }
            };
            return;
        }
#endif

        Generate();
    }

    [ContextMenu("Regenerate Island")]
    public void Generate()
    {
        ClearGeneratedChildren();

        var root = new GameObject(GeneratedRootName);
        root.transform.SetParent(transform, false);

        var terrainData = new TerrainData
        {
            heightmapResolution = heightmapResolution,
            alphamapResolution = alphamapResolution,
            size = terrainSize
        };

        terrainData.terrainLayers = new[]
        {
            CreateTerrainLayer("Sand", new Color(0.76f, 0.66f, 0.42f)),
            CreateTerrainLayer("Grass", new Color(0.22f, 0.48f, 0.25f)),
            CreateTerrainLayer("Rock", new Color(0.34f, 0.34f, 0.32f))
        };

        BuildHeights(terrainData);
        PaintTerrain(terrainData);

        GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);
        terrainObject.name = "Island Terrain";
        terrainObject.transform.SetParent(root.transform, false);
        terrainObject.transform.localPosition = new Vector3(-terrainSize.x * 0.5f, 0f, -terrainSize.z * 0.5f);

        var terrain = terrainObject.GetComponent<Terrain>();
        terrain.drawInstanced = false;
        terrain.materialTemplate = LoadRuntimeMaterial("IslandTerrain");

        CreateWater(root.transform);
        CreateRocks(root.transform);
        CreatePalms(root.transform);
        CreateGameManager(root.transform);
        CreateCrystals(root.transform);
        CreateBeacon(root.transform);
        CreatePlayer(root.transform);
        CreateEnemies(root.transform);
        CreateHealItems(root.transform);
        CreateMegaEnemies(root.transform);
    }
    private void CreateMegaEnemies(Transform parent)
    {
        Random.InitState(seed + 123);
        for (int i = 0; i < megaEnemyCount; i++)
        {
            if (!TryCreateMegaEnemy(parent))
            {
                i--;
            }
        }
    }

    public void SpawnHealItem(Vector3 position)
    {
        var heal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        heal.name = "HealItem";
        var parent = FindGeneratedRoot();
        heal.transform.SetParent(parent, false);
        var healMaterial = CreateMaterial("Heal Material", new Color(1f, 0.4f, 0.8f), true);
        heal.transform.localPosition = position + Vector3.up * 1.2f;
        heal.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
        heal.GetComponent<Renderer>().sharedMaterial = healMaterial;
        var collider = heal.GetComponent<Collider>();
        collider.isTrigger = true;
        heal.AddComponent<IslandHealItem>();
        heal.AddComponent<IslandMinimapIcon>().Configure(IslandMinimapIconType.Heal);
    }

    public void CreateHealItems(Transform parent)
    {
        Debug.Log("Creating heal items...");
        Random.InitState(seed + 99);
        var healMaterial = CreateMaterial("Heal Material", new Color(1f, 0.4f, 0.8f), true);
        for (int i = 0; i < healItemCount; i++)
        {
            Vector3 position = RandomIslandPoint(24f, 100f);
            if (position.y < waterLevel + 2f)
            {
                i--;
                continue;
            }
            SpawnHealItem(position);
        }
    }

    private void BuildHeights(TerrainData terrainData)
    {
        int resolution = terrainData.heightmapResolution;
        var heights = new float[resolution, resolution];

        for (int y = 0; y < resolution; y++)
        {
            float v = y / (float)(resolution - 1);

            for (int x = 0; x < resolution; x++)
            {
                float u = x / (float)(resolution - 1);
                float dx = (u - 0.5f) * 2f;
                float dz = (v - 0.5f) * 2f;
                float radius = Mathf.Sqrt(dx * dx + dz * dz);

                float islandShape = Mathf.Clamp01(1f - Mathf.Pow(radius, 2.25f));
                float mountain = Mathf.Exp(-radius * radius * 4.7f) * 0.58f;
                float ridge = Mathf.PerlinNoise(u * 5.2f + seed, v * 5.2f + seed) * 0.18f;
                float fineNoise = Mathf.PerlinNoise(u * 18.5f + seed * 0.37f, v * 18.5f + seed * 0.17f) * 0.045f;
                float lagoonDip = Mathf.Exp(-((dx + 0.26f) * (dx + 0.26f) * 18f + (dz - 0.12f) * (dz - 0.12f) * 24f)) * 0.12f;

                heights[y, x] = Mathf.Clamp01((mountain + ridge + fineNoise - lagoonDip) * islandShape);
            }
        }

        terrainData.SetHeights(0, 0, heights);
    }

    private void PaintTerrain(TerrainData terrainData)
    {
        int resolution = terrainData.alphamapResolution;
        var maps = new float[resolution, resolution, 3];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float u = x / (float)(resolution - 1);
                float v = y / (float)(resolution - 1);
                float height = terrainData.GetInterpolatedHeight(u, v);
                float steepness = terrainData.GetSteepness(u, v);

                float sand = Mathf.InverseLerp(waterLevel + 4.5f, waterLevel - 0.3f, height);
                float rock = Mathf.InverseLerp(28f, 46f, height) + Mathf.InverseLerp(28f, 42f, steepness);
                float grass = Mathf.Clamp01(1f - sand - rock * 0.75f);

                sand = Mathf.Clamp01(sand);
                rock = Mathf.Clamp01(rock);
                float total = sand + grass + rock;

                maps[y, x, 0] = sand / total;
                maps[y, x, 1] = grass / total;
                maps[y, x, 2] = rock / total;
            }
        }

        terrainData.SetAlphamaps(0, 0, maps);
    }

    private void CreateWater(Transform parent)
    {
        var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
        water.name = "Water";
        water.transform.SetParent(parent, false);
        water.transform.localPosition = new Vector3(0f, waterLevel - 0.15f, 0f);
        water.transform.localScale = new Vector3(26f, 1f, 26f);

        var renderer = water.GetComponent<Renderer>();
        renderer.sharedMaterial = CreateMaterial("Water Material", new Color(0.05f, 0.42f, 0.78f, 0.32f), true);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private void ConfigureCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        Vector3 cameraPosition = new Vector3(0f, SampleApproximateHeight(0f, -36f) + 3.1f, -36f);
        camera.transform.position = cameraPosition;
        camera.transform.rotation = Quaternion.LookRotation(new Vector3(0f, SampleApproximateHeight(0f, 5f) + 1.6f, 5f) - cameraPosition, Vector3.up);
        camera.fieldOfView = 62f;
        camera.farClipPlane = 1000f;
    }

    private void CreatePlayer(Transform parent)
    {
        Vector3 spawnPosition = new Vector3(0f, SampleApproximateHeight(0f, -36f) + 0.15f, -36f);

        var player = new GameObject(PlayerName);
        player.name = PlayerName;
        player.tag = "Player";
        player.transform.SetParent(parent, false);
        player.transform.localPosition = spawnPosition;
        player.transform.localRotation = Quaternion.identity;
        player.transform.localScale = Vector3.one;

        CreateHeroVisual(player.transform);

        var characterController = player.AddComponent<CharacterController>();
        characterController.height = 2f;
        characterController.radius = 0.38f;
        characterController.center = new Vector3(0f, 1f, 0f);
        characterController.stepOffset = 0.35f;
        characterController.slopeLimit = 50f;

        player.AddComponent<IslandPlayerHealth>();
        player.AddComponent<IslandMinimapIcon>().Configure(IslandMinimapIconType.Player);
        AddHumanoidAnimator(player);
        player.AddComponent<IslandPlayerController>();
        player.AddComponent<IslandPlayerWeapon>();
        ConfigureCamera();
    }

    private void CreateHeroVisual(Transform parent)
    {
        CreateHumanoidVisual(
            parent,
            CreateMaterial("Hero Skin Material", new Color(0.86f, 0.62f, 0.42f)),
            CreateMaterial("Hero Shirt Material", new Color(0.12f, 0.42f, 0.95f)),
            CreateMaterial("Hero Pants Material", new Color(0.10f, 0.16f, 0.25f)),
            CreateMaterial("Hero Hair Material", new Color(0.13f, 0.08f, 0.04f)),
            true);
    }

    private void CreateEnemyVisual(Transform parent)
    {
        CreateHumanoidVisual(
            parent,
            CreateMaterial("Enemy Skin Material", new Color(0.65f, 0.20f, 0.18f)),
            CreateMaterial("Enemy Armor Material", new Color(0.36f, 0.06f, 0.07f)),
            CreateMaterial("Enemy Legs Material", new Color(0.12f, 0.08f, 0.07f)),
            CreateMaterial("Enemy Horn Material", new Color(0.95f, 0.86f, 0.56f)),
            false);
    }

    private void CreateHumanoidVisual(Transform parent, Material skinMaterial, Material shirtMaterial, Material pantsMaterial, Material hairMaterial, bool isHero)
    {
        var body = CreateBodyPart("Body", PrimitiveType.Capsule, parent, new Vector3(0f, 1.15f, 0f), new Vector3(0.68f, 0.62f, 0.42f), shirtMaterial);
        body.localRotation = Quaternion.identity;

        CreateBodyPart("Head", PrimitiveType.Sphere, parent, new Vector3(0f, 1.95f, 0f), new Vector3(0.46f, 0.46f, 0.46f), skinMaterial);
        CreateBodyPart(isHero ? "Hair" : "Horns", PrimitiveType.Sphere, parent, new Vector3(0f, 2.14f, -0.03f), isHero ? new Vector3(0.48f, 0.22f, 0.46f) : new Vector3(0.62f, 0.20f, 0.28f), hairMaterial);
        CreateBodyPart("Nose", PrimitiveType.Sphere, parent, new Vector3(0f, 1.95f, 0.24f), new Vector3(0.10f, 0.08f, 0.12f), skinMaterial);
        CreateBodyPart("Left Eye", PrimitiveType.Sphere, parent, new Vector3(-0.13f, 2.02f, 0.38f), new Vector3(0.055f, 0.055f, 0.055f), CreateMaterial("Eye Material", Color.black));
        CreateBodyPart("Right Eye", PrimitiveType.Sphere, parent, new Vector3(0.13f, 2.02f, 0.38f), new Vector3(0.055f, 0.055f, 0.055f), CreateMaterial("Eye Material", Color.black));

        CreateLimb("Left Arm", parent, new Vector3(-0.55f, 1.32f, 0f), new Vector3(0.18f, 0.58f, 0.18f), skinMaterial);
        CreateLimb("Right Arm", parent, new Vector3(0.55f, 1.32f, 0f), new Vector3(0.18f, 0.58f, 0.18f), skinMaterial);
        CreateLimb("Left Leg", parent, new Vector3(-0.22f, 0.44f, 0f), new Vector3(0.20f, 0.52f, 0.20f), pantsMaterial);
        CreateLimb("Right Leg", parent, new Vector3(0.22f, 0.44f, 0f), new Vector3(0.20f, 0.52f, 0.20f), pantsMaterial);

        if (isHero)
        {
            CreateBodyPart("Backpack", PrimitiveType.Cube, parent, new Vector3(0f, 1.18f, -0.38f), new Vector3(0.42f, 0.52f, 0.18f), CreateMaterial("Backpack Material", new Color(0.36f, 0.18f, 0.08f)));
        }
    }

    private Transform CreateLimb(string limbName, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        var pivot = new GameObject(limbName + " Pivot");
        pivot.transform.SetParent(parent, false);
        pivot.transform.localPosition = position + Vector3.up * scale.y;

        Transform limb = CreateBodyPart(limbName, PrimitiveType.Capsule, pivot.transform, Vector3.down * scale.y, scale, material);
        limb.localRotation = Quaternion.identity;
        return pivot.transform;
    }

    private Transform CreateBodyPart(string partName, PrimitiveType primitiveType, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        var part = GameObject.CreatePrimitive(primitiveType);
        part.name = partName;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().sharedMaterial = material;

        var collider = part.GetComponent<Collider>();
        if (collider != null)
        {
            if (Application.isPlaying)
            {
                Destroy(collider);
            }
            else
            {
                DestroyImmediate(collider);
            }
        }

        return part.transform;
    }

    private void AddHumanoidAnimator(GameObject target)
    {
        var animator = target.AddComponent<Animator>();
#if UNITY_EDITOR
        animator.runtimeAnimatorController = EnsureHumanoidAnimatorController();
#endif
        animator.speed = 0f;
    }

#if UNITY_EDITOR
    private RuntimeAnimatorController EnsureHumanoidAnimatorController()
    {
        const string folderPath = "Assets/Generated";
        const string clipPath = folderPath + "/HumanoidWalk.anim";
        const string controllerPath = folderPath + "/Humanoid.controller";

        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "Generated");
        }

        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            clip = new AnimationClip
            {
                name = "HumanoidWalk",
                frameRate = 30f
            };

            clip.SetCurve("Left Arm Pivot", typeof(Transform), "localEulerAnglesRaw.x", AnimationCurve.EaseInOut(0f, 28f, 0.5f, -28f));
            clip.SetCurve("Right Arm Pivot", typeof(Transform), "localEulerAnglesRaw.x", AnimationCurve.EaseInOut(0f, -28f, 0.5f, 28f));
            clip.SetCurve("Left Leg Pivot", typeof(Transform), "localEulerAnglesRaw.x", AnimationCurve.EaseInOut(0f, -28f, 0.5f, 28f));
            clip.SetCurve("Right Leg Pivot", typeof(Transform), "localEulerAnglesRaw.x", AnimationCurve.EaseInOut(0f, 28f, 0.5f, -28f));

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, clipPath);
        }

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        }

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState walkState = stateMachine.defaultState;
        if (walkState == null)
        {
            walkState = stateMachine.AddState("Walk");
            stateMachine.defaultState = walkState;
        }

        walkState.motion = clip;
        AssetDatabase.SaveAssets();

        return controller;
    }
#endif

    private void CreateGameManager(Transform parent)
    {
        var gameManager = new GameObject("Game Manager");
        gameManager.transform.SetParent(parent, false);
        gameManager.AddComponent<IslandGameManager>();
    }

    private void CreateCrystals(Transform parent)
    {
        Random.InitState(seed + 41);
        var crystalMaterial = CreateMaterial("Crystal Material", new Color(0.16f, 0.86f, 1f, 0.92f), true);

        for (int i = 0; i < crystalCount; i++)
        {
            Vector3 position = RandomIslandPoint(20f, 92f);
            if (position.y < waterLevel + 2f)
            {
                i--;
                continue;
            }

            var crystal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crystal.name = "Crystal";
            crystal.transform.SetParent(parent, false);
            crystal.transform.localPosition = position + Vector3.up * 1.2f;
            crystal.transform.localRotation = Quaternion.Euler(90f, 0f, 45f);
            crystal.transform.localScale = new Vector3(0.55f, 0.22f, 0.55f);
            crystal.GetComponent<Renderer>().sharedMaterial = crystalMaterial;

            var collider = crystal.GetComponent<Collider>();
            collider.isTrigger = true;

            var light = crystal.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.18f, 0.9f, 1f);
            light.range = 7f;
            light.intensity = 1.3f;

            crystal.AddComponent<IslandCrystal>();
            crystal.AddComponent<IslandMinimapIcon>().Configure(IslandMinimapIconType.Crystal);
        }
    }

    private void CreateBeacon(Transform parent)
    {
        Vector3 position = new Vector3(0f, SampleApproximateHeight(0f, 0f) + 0.6f, 0f);
        var beacon = new GameObject("Golden Beacon");
        beacon.transform.SetParent(parent, false);
        beacon.transform.localPosition = position;

        var baseObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseObject.name = "Beacon Base";
        baseObject.transform.SetParent(beacon.transform, false);
        baseObject.transform.localPosition = new Vector3(0f, 1.4f, 0f);
        baseObject.transform.localScale = new Vector3(1.3f, 1.4f, 1.3f);
        baseObject.GetComponent<Renderer>().sharedMaterial = CreateMaterial("Beacon Material", new Color(1f, 0.76f, 0.12f));

        var glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glow.name = "Beacon Glow";
        glow.transform.SetParent(beacon.transform, false);
        glow.transform.localPosition = new Vector3(0f, 3.35f, 0f);
        glow.transform.localScale = new Vector3(1.9f, 1.9f, 1.9f);
        glow.GetComponent<Renderer>().sharedMaterial = CreateMaterial("Beacon Glow Material", new Color(1f, 0.88f, 0.2f, 0.55f), true);

        var trigger = beacon.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 3.3f;
        trigger.center = new Vector3(0f, 1.7f, 0f);

        var light = beacon.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.74f, 0.18f);
        light.range = 22f;
        light.intensity = 1.1f;

        beacon.AddComponent<IslandBeacon>().Configure(light);
        beacon.AddComponent<IslandMinimapIcon>().Configure(IslandMinimapIconType.Beacon);
    }

    private void CreateEnemies(Transform parent)
    {
        Random.InitState(seed + 71);

        for (int i = 0; i < enemyCount; i++)
        {
            if (!TryCreateEnemy(parent))
            {
                i--;
            }
        }
    }

    public void SpawnEnemy()
    {
        Transform parent = FindGeneratedRoot();
        if (parent == null)
        {
            parent = transform;
        }

        for (int i = 0; i < 12; i++)
        {
            if (TryCreateEnemy(parent))
            {
                return;
            }
        }
    }

    public void SpawnMegaEnemy()
    {
        Transform parent = FindGeneratedRoot();
        if (parent == null)
        {
            parent = transform;
        }

        for (int i = 0; i < 12; i++)
        {
            if (TryCreateMegaEnemy(parent))
            {
                return;
            }
        }
    }

    private bool TryCreateEnemy(Transform parent)
    {
        Vector3 position = RandomIslandPoint(38f, 96f);
        if (position.y < waterLevel + 2.5f)
        {
            return false;
        }

        CreateEnemy(parent, position);
        return true;
    }

    private bool TryCreateMegaEnemy(Transform parent)
    {
        Vector3 position = RandomIslandPoint(60f, 110f);
        if (position.y < waterLevel + 2.5f)
        {
            return false;
        }

        CreateMegaEnemy(parent, position);
        return true;
    }

    private void CreateEnemy(Transform parent, Vector3 position)
    {
        var enemy = new GameObject("Island Sentry");
        enemy.transform.SetParent(parent, false);
        enemy.transform.localPosition = position + Vector3.up * 0.15f;
        enemy.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        CreateEnemyVisual(enemy.transform);

        var controller = enemy.AddComponent<CharacterController>();
        controller.height = 2f;
        controller.radius = 0.4f;
        controller.center = new Vector3(0f, 1f, 0f);
        controller.stepOffset = 0.35f;
        controller.slopeLimit = 48f;

        AddHumanoidAnimator(enemy);
        enemy.AddComponent<IslandEnemy>();
        enemy.AddComponent<IslandMinimapIcon>().Configure(IslandMinimapIconType.Enemy);
    }

    private void CreateMegaEnemy(Transform parent, Vector3 position)
    {
        var mega = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        mega.name = "MegaEnemy";
        mega.transform.SetParent(parent, false);
        mega.transform.localPosition = position + Vector3.up * 1.0f;
        mega.transform.localScale = new Vector3(2.2f, 4.5f, 2.2f);
        mega.GetComponent<Renderer>().sharedMaterial = CreateMaterial("MegaEnemy Material", new Color(0.5f, 0.1f, 0.7f));

        var collider = mega.GetComponent<Collider>();
        collider.isTrigger = false;

        var controller = mega.AddComponent<CharacterController>();
        controller.height = 4.5f;
        controller.radius = 1.1f;
        controller.center = new Vector3(0f, 2.25f, 0f);

        var enemy = mega.AddComponent<IslandEnemy>();
        enemy.maxHealth = 40000;
        enemy.patrolSpeed = 10f;
        enemy.chaseSpeed = 1000f;
        enemy.damage = 30;

        mega.AddComponent<IslandMegaEnemy>();
        mega.AddComponent<IslandMinimapIcon>().Configure(IslandMinimapIconType.Enemy);
    }

    private Transform FindGeneratedRoot()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.name == GeneratedRootName)
            {
                return child;
            }
        }

        return null;
    }

    private void CreateRocks(Transform parent)
    {
        Random.InitState(seed + 11);
        var rockMaterial = CreateMaterial("Rock Material", new Color(0.28f, 0.27f, 0.25f));

        for (int i = 0; i < rockCount; i++)
        {
            Vector3 position = RandomIslandPoint(34f, 95f);
            if (position.y < waterLevel + 3f)
            {
                continue;
            }

            var rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.name = "Rock";
            rock.transform.SetParent(parent, false);
            rock.transform.localPosition = position;
            rock.transform.localRotation = Random.rotation;
            rock.transform.localScale = new Vector3(Random.Range(2.2f, 6.5f), Random.Range(1.2f, 3.4f), Random.Range(2.0f, 5.8f));
            rock.GetComponent<Renderer>().sharedMaterial = rockMaterial;
        }
    }

    private void CreatePalms(Transform parent)
    {
        Random.InitState(seed + 23);
        var trunkMaterial = CreateMaterial("Palm Trunk Material", new Color(0.45f, 0.27f, 0.12f));
        var leafMaterial = CreateMaterial("Palm Leaf Material", new Color(0.10f, 0.44f, 0.18f));

        for (int i = 0; i < palmCount; i++)
        {
            Vector3 position = RandomIslandPoint(42f, 88f);
            if (position.y < waterLevel + 1.4f || position.y > waterLevel + 11f)
            {
                continue;
            }

            var palm = new GameObject("Palm");
            palm.transform.SetParent(parent, false);
            palm.transform.localPosition = position;
            palm.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), Random.Range(-5f, 5f));

            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(palm.transform, false);
            trunk.transform.localPosition = new Vector3(0f, 3.0f, 0f);
            trunk.transform.localScale = new Vector3(0.55f, 3.0f, 0.55f);
            trunk.GetComponent<Renderer>().sharedMaterial = trunkMaterial;

            for (int leafIndex = 0; leafIndex < 6; leafIndex++)
            {
                var leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leaf.name = "Leaf";
                leaf.transform.SetParent(palm.transform, false);
                leaf.transform.localPosition = Quaternion.Euler(0f, leafIndex * 60f, 0f) * new Vector3(1.55f, 6.15f, 0f);
                leaf.transform.localRotation = Quaternion.Euler(18f, leafIndex * 60f, 8f);
                leaf.transform.localScale = new Vector3(3.0f, 0.12f, 0.65f);
                leaf.GetComponent<Renderer>().sharedMaterial = leafMaterial;
            }

            // Lisää minimap-ikoni palmuun
            palm.AddComponent<IslandMinimapIcon>().Configure(IslandMinimapIconType.Palm);
        }
    }

    private Vector3 RandomIslandPoint(float minRadius, float maxRadius)
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float radius = Random.Range(minRadius, maxRadius);
        float x = Mathf.Cos(angle) * radius;
        float z = Mathf.Sin(angle) * radius;
        float y = SampleApproximateHeight(x, z);
        return new Vector3(x, y, z);
    }

    private float SampleApproximateHeight(float worldX, float worldZ)
    {
        float u = (worldX / terrainSize.x) + 0.5f;
        float v = (worldZ / terrainSize.z) + 0.5f;
        float dx = (u - 0.5f) * 2f;
        float dz = (v - 0.5f) * 2f;
        float radius = Mathf.Sqrt(dx * dx + dz * dz);
        float islandShape = Mathf.Clamp01(1f - Mathf.Pow(radius, 2.25f));
        float mountain = Mathf.Exp(-radius * radius * 4.7f) * 0.58f;
        float ridge = Mathf.PerlinNoise(u * 5.2f + seed, v * 5.2f + seed) * 0.18f;
        float fineNoise = Mathf.PerlinNoise(u * 18.5f + seed * 0.37f, v * 18.5f + seed * 0.17f) * 0.045f;
        float lagoonDip = Mathf.Exp(-((dx + 0.26f) * (dx + 0.26f) * 18f + (dz - 0.12f) * (dz - 0.12f) * 24f)) * 0.12f;
        return Mathf.Clamp01((mountain + ridge + fineNoise - lagoonDip) * islandShape) * terrainSize.y;
    }

    private TerrainLayer CreateTerrainLayer(string layerName, Color color)
    {
        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            name = layerName + " Texture"
        };
        texture.SetPixel(0, 0, color);
        texture.Apply();

        return new TerrainLayer
        {
            name = layerName,
            diffuseTexture = texture,
            tileSize = new Vector2(18f, 18f)
        };
    }

    private Material CreateMaterial(string materialName, Color color, bool transparent = false)
    {
        Material sourceMaterial = LoadRuntimeMaterial(transparent ? "IslandTransparent" : "IslandOpaque");
        if (sourceMaterial != null)
        {
            Material clonedMaterial = new Material(sourceMaterial)
            {
                name = materialName
            };
            ApplyMaterialColor(clonedMaterial, color);
            return clonedMaterial;
        }

        Shader shader = transparent ? Shader.Find("Legacy Shaders/Transparent/Diffuse") : Shader.Find("Legacy Shaders/Diffuse");
        if (shader == null)
        {
            shader = Shader.Find("Legacy Shaders/VertexLit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Lit");
        }

        if (shader == null)
        {
            Debug.LogWarning($"Could not create material '{materialName}' because no runtime shader was available.");
            return null;
        }

        Material material = new Material(shader)
        {
            name = materialName
        };
        ApplyMaterialColor(material, color);

        if (transparent)
        {
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 0f);
            }

            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
        }

        return material;
    }

    private Material LoadRuntimeMaterial(string materialName)
    {
        return Resources.Load<Material>("RuntimeMaterials/" + materialName);
    }

    private void ApplyMaterialColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
    }

    private void ClearGeneratedChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name != GeneratedRootName)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }
}

