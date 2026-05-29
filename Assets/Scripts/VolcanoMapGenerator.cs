#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class VolcanoMapGenerator
{
    private const float MAP_SIZE = 100f;
    private const float PLAYER_START_ZONE = 25f;
    private const float SAFE_ZONE = 50f;

    // EmaceArt_LavaPlant 에셋 경로
    private const string TERRAIN_PATH = "Assets/EmaceArt_LavaPlant/Prefabs/EA_Terrain_Default.prefab";
    private const string ROCK_A_PATH = "Assets/EmaceArt_LavaPlant/Prefabs/Rocks_01a_Default.prefab";
    private const string ROCK_B_PATH = "Assets/EmaceArt_LavaPlant/Prefabs/Rocks_01b_Default.prefab";
    private const string ROCK_C_PATH = "Assets/EmaceArt_LavaPlant/Prefabs/Rocks_01c_Default.prefab";
    private const string ROCK_D_PATH = "Assets/EmaceArt_LavaPlant/Prefabs/Rocks_01d_Default.prefab";
    private const string PLANT_A_PATH = "Assets/EmaceArt_LavaPlant/Prefabs/EA_Plant_01a_Default.prefab";
    private const string PLANT_B_PATH = "Assets/EmaceArt_LavaPlant/Prefabs/EA_Plant_01b_Default.prefab";
    private const string PLANT_C_PATH = "Assets/EmaceArt_LavaPlant/Prefabs/EA_Plant_01c_Default.prefab";
    private const string PLANT_D_PATH = "Assets/EmaceArt_LavaPlant/Prefabs/EA_Plant_01d_Default.prefab";
    private const string PLANT_E_PATH = "Assets/EmaceArt_LavaPlant/Prefabs/EA_Plant_01e_Default.prefab";
    private const string FRUIT_A_PATH = "Assets/EmaceArt_LavaPlant/Prefabs/EA_Fruit_01a_Default.prefab";
    private const string FRUIT_B_PATH = "Assets/EmaceArt_LavaPlant/Prefabs/EA_Fruit_01b_Default.prefab";
    private const string FRUIT_C_PATH = "Assets/EmaceArt_LavaPlant/Prefabs/EA_Fruit_01c_Default.prefab";
    private const string FRUIT_D_PATH = "Assets/EmaceArt_LavaPlant/Prefabs/EA_Fruit_01d_Default.prefab";
    private const string FRUIT_PRE_PATH = "Assets/EmaceArt_LavaPlant/Prefabs/EA_Fruit_PRE.prefab";
    private const string PUDDLE_A_PATH = "Assets/EmaceArt_LavaPlant/Prefabs/EA_Puddle_01a_Default.prefab";
    private const string PUDDLE_B_PATH = "Assets/EmaceArt_LavaPlant/Prefabs/EA_Puddle_01b_Default.prefab";
    private const string PUDDLE_C_PATH = "Assets/EmaceArt_LavaPlant/Prefabs/EA_Puddle_01c_Default.prefab";

    private static float mapSize = 250f;
    private static int treeCount = 60;
    private static int rockCount = 120;
    private static int largeRockCount = 40;
    private static int plantCount = 100;
    private static int fruitCount = 50;
    private static int lavaPuddleCount = 40;
    private static Vector3 playerStartPosition = new Vector3(0, 0.5f, 0);
    private static int[] enemyCountPerLevel = { 20, 18, 15, 12, 10, 8, 6, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4 };

    private static GameObject terrainPrefab;
    private static GameObject rockAPrefab;
    private static GameObject rockBPrefab;
    private static GameObject rockCPrefab;
    private static GameObject rockDPrefab;
    private static GameObject plantAPrefab;
    private static GameObject plantBPrefab;
    private static GameObject plantCPrefab;
    private static GameObject plantDPrefab;
    private static GameObject plantEPrefab;
    private static GameObject fruitAPrefab;
    private static GameObject fruitBPrefab;
    private static GameObject fruitCPrefab;
    private static GameObject fruitDPrefab;
    private static GameObject fruitPREPrefab;
    private static GameObject puddleAPrefab;
    private static GameObject puddleBPrefab;
    private static GameObject puddleCPrefab;

    [MenuItem("DinoGrow/Generate Volcano Map")]
    public static void GenerateMap()
    {
        ClearExistingMap();
        CreateTerrain();
        CreateVolcanoElements();
        CreatePlayerStartPoint();
        CreateEnemySpawnPoints();
        Debug.Log("화산 맵 생성 완료!");
    }

    private static void ClearExistingMap()
    {
        GameObject[] existingObjects = GameObject.FindGameObjectsWithTag("EditorOnly");
        foreach (GameObject obj in existingObjects)
        {
            if (obj.name.Contains("Ground") || obj.name.Contains("Volcano") ||
                obj.name.Contains("Spawn") || obj.name.Contains("PlayerStart") ||
                obj.name.Contains("Rock") || obj.name.Contains("Lava") ||
                obj.name.Contains("Plant") || obj.name.Contains("Fruit") ||
                obj.name.Contains("Tree"))
            {
                Object.DestroyImmediate(obj);
            }
        }
    }

    private static void CreateTerrain()
    {
        LoadAssets();
        if (terrainPrefab != null)
        {
            GameObject groundParent = new GameObject("Ground_Volcano");
            groundParent.tag = "EditorOnly";
            float tileSize = 10f;
            int tileCount = Mathf.CeilToInt(mapSize / tileSize);
            float offset = (tileCount * tileSize) / 2f - tileSize / 2f;
            for (int x = 0; x < tileCount; x++)
            {
                for (int z = 0; z < tileCount; z++)
                {
                    GameObject tile = Object.Instantiate(terrainPrefab, groundParent.transform);
                    tile.name = $"VolcanoTerrain_{x}_{z}";
                    tile.transform.position = new Vector3(x * tileSize - offset, 0, z * tileSize - offset);
                    tile.transform.rotation = Quaternion.identity;
                    tile.transform.localScale = Vector3.one;
                }
            }
            Debug.Log($"지형 생성 완료: EmaceArt Lava Terrain ({tileCount}x{tileCount} 타일)");
        }
        else
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground_Volcano";
            ground.transform.localScale = new Vector3(mapSize / 10f, 1, mapSize / 10f);
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.18f, 0.10f, 0.06f);
            ground.GetComponent<Renderer>().material = mat;
            ground.tag = "EditorOnly";
        }
    }

    private static void CreateVolcanoElements()
    {
        LoadAssets();

        // 나무 (화산 식물)
        for (int i = 0; i < treeCount; i++)
        {
            Vector3 pos = GetRandomPosition(PLAYER_START_ZONE);
            CreateTree(pos);
        }

        // 작은 바위
        for (int i = 0; i < rockCount; i++)
        {
            Vector3 pos = GetRandomPosition(PLAYER_START_ZONE);
            CreateRock(pos, Random.Range(0.3f, 0.8f));
        }

        // 큰 바위
        for (int i = 0; i < largeRockCount; i++)
        {
            Vector3 pos = GetRandomPosition(PLAYER_START_ZONE + 10f);
            CreateRock(pos, Random.Range(0.8f, 1.8f));
        }

        // 화산 식물 (덤불/관목)
        for (int i = 0; i < plantCount; i++)
        {
            Vector3 pos = GetRandomPosition(PLAYER_START_ZONE);
            CreatePlant(pos);
        }

        // 화산 열매
        for (int i = 0; i < fruitCount; i++)
        {
            Vector3 pos = GetRandomPosition(PLAYER_START_ZONE);
            CreateFruit(pos);
        }

        // 용암 웅덩이
        for (int i = 0; i < lavaPuddleCount; i++)
        {
            Vector3 pos = GetRandomPosition(PLAYER_START_ZONE + 15f);
            CreateLavaPuddle(pos, Random.Range(0.6f, 1.5f));
        }

        Debug.Log($"화산 요소 생성 완료: 나무 {treeCount}, 작은바위 {rockCount}, 큰바위 {largeRockCount}, 식물 {plantCount}, 열매 {fruitCount}, 용암 {lavaPuddleCount}");
    }

    private static Vector3 GetRandomPosition(float minDistanceFromCenter)
    {
        Vector2 randomPos = Random.insideUnitCircle * (mapSize / 2 - 5f);
        if (randomPos.magnitude < minDistanceFromCenter)
        {
            randomPos = randomPos.normalized * (minDistanceFromCenter + Random.Range(5f, 15f));
        }
        return new Vector3(randomPos.x, 0f, randomPos.y);
    }

    private static void LoadAssets()
    {
#if UNITY_EDITOR
        if (terrainPrefab == null) terrainPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TERRAIN_PATH);
        if (rockAPrefab == null) rockAPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ROCK_A_PATH);
        if (rockBPrefab == null) rockBPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ROCK_B_PATH);
        if (rockCPrefab == null) rockCPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ROCK_C_PATH);
        if (rockDPrefab == null) rockDPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ROCK_D_PATH);
        if (plantAPrefab == null) plantAPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PLANT_A_PATH);
        if (plantBPrefab == null) plantBPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PLANT_B_PATH);
        if (plantCPrefab == null) plantCPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PLANT_C_PATH);
        if (plantDPrefab == null) plantDPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PLANT_D_PATH);
        if (plantEPrefab == null) plantEPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PLANT_E_PATH);
        if (fruitAPrefab == null) fruitAPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FRUIT_A_PATH);
        if (fruitBPrefab == null) fruitBPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FRUIT_B_PATH);
        if (fruitCPrefab == null) fruitCPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FRUIT_C_PATH);
        if (fruitDPrefab == null) fruitDPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FRUIT_D_PATH);
        if (fruitPREPrefab == null) fruitPREPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FRUIT_PRE_PATH);
        if (puddleAPrefab == null) puddleAPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PUDDLE_A_PATH);
        if (puddleBPrefab == null) puddleBPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PUDDLE_B_PATH);
        if (puddleCPrefab == null) puddleCPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PUDDLE_C_PATH);
#endif
    }

    private static void CreateTree(Vector3 position)
    {
        GameObject[] plantPrefabs = { plantAPrefab, plantBPrefab, plantCPrefab, plantDPrefab, plantEPrefab };
        GameObject selected = plantPrefabs[Random.Range(0, plantPrefabs.Length)];
        if (selected != null)
        {
            GameObject tree = Object.Instantiate(selected);
            tree.name = "VolcanoTree_" + Random.Range(1, 10000);
            tree.transform.position = position;
            tree.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            tree.transform.localScale = Vector3.one * Random.Range(0.8f, 1.5f);
            tree.tag = "EditorOnly";
        }
    }

    private static void CreateRock(Vector3 position, float scale)
    {
        GameObject[] rockPrefabs = { rockAPrefab, rockBPrefab, rockCPrefab, rockDPrefab };
        GameObject selected = rockPrefabs[Random.Range(0, rockPrefabs.Length)];
        if (selected != null)
        {
            GameObject rock = Object.Instantiate(selected);
            rock.name = "VolcanoRock_" + Random.Range(1, 10000);
            rock.transform.position = position;
            rock.transform.rotation = Quaternion.Euler(Random.Range(0, 15), Random.Range(0, 360), Random.Range(0, 15));
            rock.transform.localScale = Vector3.one * scale;
            rock.tag = "EditorOnly";
        }
    }

    private static void CreatePlant(Vector3 position)
    {
        GameObject[] plantPrefabs = { plantAPrefab, plantBPrefab, plantCPrefab, plantDPrefab, plantEPrefab };
        GameObject selected = plantPrefabs[Random.Range(0, plantPrefabs.Length)];
        if (selected != null)
        {
            GameObject plant = Object.Instantiate(selected);
            plant.name = "VolcanoPlant_" + Random.Range(1, 10000);
            plant.transform.position = position;
            plant.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            plant.transform.localScale = Vector3.one * Random.Range(0.4f, 0.8f);
            plant.tag = "EditorOnly";
        }
    }

    private static void CreateFruit(Vector3 position)
    {
        GameObject[] fruitPrefabs = { fruitAPrefab, fruitBPrefab, fruitCPrefab, fruitDPrefab, fruitPREPrefab };
        GameObject selected = fruitPrefabs[Random.Range(0, fruitPrefabs.Length)];
        if (selected != null)
        {
            GameObject fruit = Object.Instantiate(selected);
            fruit.name = "VolcanoFruit_" + Random.Range(1, 10000);
            fruit.transform.position = position;
            fruit.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            fruit.transform.localScale = Vector3.one * Random.Range(0.5f, 0.9f);
            fruit.tag = "EditorOnly";
        }
    }

    private static void CreateLavaPuddle(Vector3 position, float scale)
    {
        GameObject[] puddlePrefabs = { puddleAPrefab, puddleBPrefab, puddleCPrefab };
        GameObject selected = puddlePrefabs[Random.Range(0, puddlePrefabs.Length)];
        if (selected != null)
        {
            GameObject puddle = Object.Instantiate(selected);
            puddle.name = "LavaPuddle_" + Random.Range(1, 10000);
            puddle.transform.position = position;
            puddle.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            puddle.transform.localScale = Vector3.one * scale;
            puddle.tag = "EditorOnly";
        }
    }

    private static void CreatePlayerStartPoint()
    {
        GameObject startPoint = new GameObject("PlayerStartPoint");
        startPoint.transform.position = playerStartPosition;
        startPoint.tag = "EditorOnly";
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.transform.parent = startPoint.transform;
        marker.transform.position = Vector3.zero;
        marker.transform.localScale = Vector3.one * 2f;
        Material markerMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        markerMat.color = new Color(0f, 0.5f, 1f, 0.5f);
        marker.GetComponent<Renderer>().material = markerMat;
        marker.GetComponent<Collider>().enabled = false;
    }

    private static void CreateEnemySpawnPoints()
    {
        GameObject spawnContainer = new GameObject("EnemySpawnPoints");
        spawnContainer.tag = "EditorOnly";
        for (int level = 1; level <= 20; level++)
        {
            int count = enemyCountPerLevel[level - 1];
            float minDist = GetMinDistanceForLevel(level);
            float maxDist = GetMaxDistanceForLevel(level);
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = GetSpawnPositionForLevel(level, minDist, maxDist);
                CreateEnemySpawnPoint(spawnContainer.transform, level, pos);
            }
        }
    }

    private static float GetMinDistanceForLevel(int level)
    {
        if (level <= 3) return PLAYER_START_ZONE;
        if (level <= 6) return SAFE_ZONE;
        return 40f;
    }

    private static float GetMaxDistanceForLevel(int level)
    {
        return mapSize / 2 - 5f;
    }

    private static Vector3 GetSpawnPositionForLevel(int level, float minDist, float maxDist)
    {
        float distance = Random.Range(minDist, maxDist);
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(angle) * distance, 0.5f, Mathf.Sin(angle) * distance);
    }

    private static void CreateEnemySpawnPoint(Transform parent, int level, Vector3 position)
    {
        GameObject spawnPoint = new GameObject($"EnemySpawn_Lv{level}_{Random.Range(1, 1000)}");
        spawnPoint.transform.parent = parent;
        spawnPoint.transform.position = position;
        GameObject levelMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        levelMarker.transform.parent = spawnPoint.transform;
        levelMarker.transform.position = Vector3.up * 2f;
        levelMarker.transform.localScale = Vector3.one * 0.5f;
        Material markerMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        markerMat.color = GetLevelColor(level);
        levelMarker.GetComponent<Renderer>().material = markerMat;
        levelMarker.GetComponent<Collider>().enabled = false;
        spawnPoint.AddComponent<VolcanoEnemySpawnData>().level = level;
    }

    private static Color GetLevelColor(int level)
    {
        float t = (float)level / 20f;
        return Color.Lerp(new Color(0.8f, 0.3f, 0.0f), new Color(1.0f, 0.0f, 0.0f), t);
    }
}
#endif
