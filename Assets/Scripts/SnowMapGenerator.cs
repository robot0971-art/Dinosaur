#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class SnowMapGenerator
{
    private const float MAP_SIZE = 100f;
    private const float PLAYER_START_ZONE = 25f;
    private const float SAFE_ZONE = 50f;

    private const string TERRAIN_PATH = "Assets/Tiny Teacup Studio/Low Poly Desert Environment/Prefabs/Ground_01.prefab";
    private const string SNOW_MAT_PATH = "Assets/Materials Collection/4 Snow Materials/Materials/";

    private static float mapSize = 250f;
    private static int pineTreeCount = 80;
    private static int roundTreeCount = 40;
    private static int shrubCount = 100;
    private static int grassCount = 200;
    private static int rockCount = 60;
    private static int deadTreeCount = 20;
    private static int stumpCount = 15;
    private static int fallenLogCount = 15;
    private static int cliffCount = 20;
    private static Vector3 playerStartPosition = new Vector3(0, 0.5f, 0);
    private static int[] enemyCountPerLevel = { 20, 18, 15, 12, 10, 8, 6, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4 };

    private static GameObject terrainPrefab;
    private static GameObject[] pineTreePrefabs = new GameObject[4];
    private static GameObject[] roundTreePrefabs = new GameObject[3];
    private static GameObject[] shrubPrefabs = new GameObject[4];
    private static GameObject[] grassPrefabs = new GameObject[2];
    private static GameObject[] groundPlantPrefabs = new GameObject[2];
    private static GameObject[] rockPrefabs = new GameObject[3];
    private static GameObject[] cliffPrefabs = new GameObject[3];
    private static GameObject[] lowCliffPrefabs = new GameObject[2];
    private static GameObject deadTree1Prefab;
    private static GameObject deadTree2Prefab;
    private static GameObject stumpPrefab;
    private static GameObject fallenLogPrefab;
    private static Material[] snowMaterials = new Material[4];
 
    [MenuItem("DinoGrow/Generate Snow Map")]
    public static void GenerateMap()
    {
        ClearExistingMap();
        CreateTerrain();
        CreateSnowElements();
        CreatePlayerStartPoint();
        CreateEnemySpawnPoints();
        Debug.Log("눈 맵 생성 완료!");
    }

    private static void ClearExistingMap()
    {
        GameObject[] existingObjects = GameObject.FindGameObjectsWithTag("EditorOnly");
        foreach (GameObject obj in existingObjects)
        {
            if (obj.name.Contains("Ground") || obj.name.Contains("Snow") ||
                obj.name.Contains("Spawn") || obj.name.Contains("PlayerStart") ||
                obj.name.Contains("Pine") || obj.name.Contains("Round") ||
                obj.name.Contains("Shrub") || obj.name.Contains("Grass") ||
                obj.name.Contains("Rock") || obj.name.Contains("Cliff") ||
                obj.name.Contains("Dead") || obj.name.Contains("Stump") ||
                obj.name.Contains("Log") || obj.name.Contains("Plant"))
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
            GameObject groundParent = new GameObject("Ground_Snow");
            groundParent.tag = "EditorOnly";
            float tileSize = 5f;
            int tileCount = Mathf.CeilToInt(mapSize / tileSize);
            float offset = (tileCount * tileSize) / 2f - tileSize / 2f;
            for (int x = 0; x < tileCount; x++)
            {
                for (int z = 0; z < tileCount; z++)
                {
                    GameObject tile = Object.Instantiate(terrainPrefab, groundParent.transform);
                    tile.name = $"SnowTerrain_{x}_{z}";
                    tile.transform.position = new Vector3(x * tileSize - offset, 0, z * tileSize - offset);
                    tile.transform.rotation = Quaternion.identity;
                    tile.transform.localScale = Vector3.one;
                    Material snowMat = snowMaterials[0];
                    foreach (var renderer in tile.GetComponentsInChildren<Renderer>())
                    {
                        var mats = renderer.sharedMaterials;
                        for (int i = 0; i < mats.Length; i++)
                            mats[i] = snowMat;
                        renderer.sharedMaterials = mats;
                    }
                }
            }
            Debug.Log($"지형 생성 완료 ({tileCount}x{tileCount} 타일)");
        }
    }

    private static void CreateSnowElements()
    {
        LoadAssets();

        for (int i = 0; i < pineTreeCount; i++)
        {
            Vector3 pos = GetRandomPosition(PLAYER_START_ZONE);
            CreatePineTree(pos);
        }

        for (int i = 0; i < roundTreeCount; i++)
        {
            Vector3 pos = GetRandomPosition(PLAYER_START_ZONE);
            CreateRoundTree(pos);
        }

        for (int i = 0; i < shrubCount; i++)
        {
            Vector3 pos = GetRandomPosition(PLAYER_START_ZONE);
            CreateShrub(pos);
        }

        for (int i = 0; i < grassCount; i++)
        {
            Vector3 pos = GetRandomPosition(PLAYER_START_ZONE);
            CreateGrassOrPlant(pos);
        }

        for (int i = 0; i < rockCount; i++)
        {
            Vector3 pos = GetRandomPosition(PLAYER_START_ZONE);
            CreateRock(pos);
        }

        for (int i = 0; i < deadTreeCount; i++)
        {
            Vector3 pos = GetRandomPosition(PLAYER_START_ZONE + 10f);
            CreateDeadTree(pos);
        }

        for (int i = 0; i < stumpCount; i++)
        {
            Vector3 pos = GetRandomPosition(PLAYER_START_ZONE + 5f);
            CreateStump(pos);
        }

        for (int i = 0; i < fallenLogCount; i++)
        {
            Vector3 pos = GetRandomPosition(PLAYER_START_ZONE + 5f);
            CreateFallenLog(pos);
        }

        for (int i = 0; i < cliffCount; i++)
        {
            Vector3 pos = GetRandomPosition(PLAYER_START_ZONE + 15f);
            CreateCliff(pos);
        }

        Debug.Log($"눈 요소 생성 완료: 소나무 {pineTreeCount}, 둥근나무 {roundTreeCount}, 덤불 {shrubCount}, 풀 {grassCount}, 바위 {rockCount}, 죽은나무 {deadTreeCount}, 그루터기 {stumpCount}, 통나무 {fallenLogCount}, 절벽 {cliffCount}");
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
        string basePath = "Assets/Lowpoly Forest Pack Winter/Prefabs/";
        if (terrainPrefab == null) terrainPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TERRAIN_PATH);
        if (snowMaterials[0] == null)
        {
            for (int i = 0; i < 4; i++)
                snowMaterials[i] = AssetDatabase.LoadAssetAtPath<Material>(SNOW_MAT_PATH + $"snow_{i + 1}_tga.mat");
        }
        if (pineTreePrefabs[0] == null)
        {
            for (int i = 0; i < 4; i++)
                pineTreePrefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(basePath + $"Winter Pine Tree {i + 1}.prefab");
            for (int i = 0; i < 3; i++)
                roundTreePrefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(basePath + $"Winter Round Tree {i + 1}.prefab");
            for (int i = 0; i < 4; i++)
                shrubPrefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(basePath + $"Winter Shrub {i + 1}.prefab");
            for (int i = 0; i < 2; i++)
                grassPrefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(basePath + $"Winter Grass {i + 1}.prefab");
            for (int i = 0; i < 2; i++)
                groundPlantPrefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(basePath + $"Winter Ground Plant {i + 1}.prefab");
            for (int i = 0; i < 3; i++)
                rockPrefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(basePath + $"Winter Rock {i + 1}.prefab");
            for (int i = 0; i < 3; i++)
                cliffPrefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(basePath + $"Winter Cliff {i + 1}.prefab");
            for (int i = 0; i < 2; i++)
                lowCliffPrefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(basePath + $"Winter Low Cliff {i + 1}.prefab");
            deadTree1Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePath + "Winter Dead Standing Tree 1.prefab");
            deadTree2Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePath + "Winter Dead Standing Tree 2.prefab");
            stumpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePath + "Winter Tree Stump 1.prefab");
            fallenLogPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePath + "Winter Fallen Log.prefab");
        }
#endif
    }

    private static void CreatePineTree(Vector3 position)
    {
        GameObject selected = pineTreePrefabs[Random.Range(0, pineTreePrefabs.Length)];
        if (selected != null)
        {
            GameObject tree = Object.Instantiate(selected);
            tree.name = "SnowPine_" + Random.Range(1, 10000);
            tree.transform.position = position;
            tree.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            tree.transform.localScale = tree.transform.localScale * Random.Range(0.8f, 1.5f);
            ConvertToURP(tree);
            tree.tag = "EditorOnly";
        }
    }

    private static void CreateRoundTree(Vector3 position)
    {
        GameObject selected = roundTreePrefabs[Random.Range(0, roundTreePrefabs.Length)];
        if (selected != null)
        {
            GameObject tree = Object.Instantiate(selected);
            tree.name = "SnowRoundTree_" + Random.Range(1, 10000);
            tree.transform.position = position;
            tree.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            tree.transform.localScale = tree.transform.localScale * Random.Range(0.8f, 1.4f);
            ConvertToURP(tree);
            tree.tag = "EditorOnly";
        }
    }

    private static void CreateShrub(Vector3 position)
    {
        GameObject selected = shrubPrefabs[Random.Range(0, shrubPrefabs.Length)];
        if (selected != null)
        {
            GameObject shrub = Object.Instantiate(selected);
            shrub.name = "SnowShrub_" + Random.Range(1, 10000);
            shrub.transform.position = position;
            shrub.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            shrub.transform.localScale = shrub.transform.localScale * Random.Range(0.6f, 1.2f);
            ConvertToURP(shrub);
            shrub.tag = "EditorOnly";
        }
    }

    private static void CreateGrassOrPlant(Vector3 position)
    {
        GameObject[] all = new GameObject[grassPrefabs.Length + groundPlantPrefabs.Length];
        grassPrefabs.CopyTo(all, 0);
        groundPlantPrefabs.CopyTo(all, grassPrefabs.Length);
        GameObject selected = all[Random.Range(0, all.Length)];
        if (selected != null)
        {
            GameObject obj = Object.Instantiate(selected);
            obj.name = "SnowGrass_" + Random.Range(1, 10000);
            obj.transform.position = position;
            obj.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            obj.transform.localScale = obj.transform.localScale * Random.Range(0.7f, 1.3f);
            ConvertToURP(obj);
            obj.tag = "EditorOnly";
        }
    }

    private static void CreateRock(Vector3 position)
    {
        GameObject selected = rockPrefabs[Random.Range(0, rockPrefabs.Length)];
        if (selected != null)
        {
            GameObject rock = Object.Instantiate(selected);
            rock.name = "SnowRock_" + Random.Range(1, 10000);
            rock.transform.position = position;
            rock.transform.rotation = Quaternion.Euler(Random.Range(0, 20), Random.Range(0, 360), Random.Range(0, 20));
            rock.transform.localScale = rock.transform.localScale * Random.Range(0.5f, 1.5f);
            ConvertToURP(rock);
            rock.tag = "EditorOnly";
        }
    }

    private static void CreateDeadTree(Vector3 position)
    {
        GameObject[] deadTrees = { deadTree1Prefab, deadTree2Prefab };
        GameObject selected = deadTrees[Random.Range(0, deadTrees.Length)];
        if (selected != null)
        {
            GameObject tree = Object.Instantiate(selected);
            tree.name = "SnowDeadTree_" + Random.Range(1, 10000);
            tree.transform.position = position;
            tree.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            tree.transform.localScale = tree.transform.localScale * Random.Range(0.8f, 1.3f);
            ConvertToURP(tree);
            tree.tag = "EditorOnly";
        }
    }

    private static void CreateStump(Vector3 position)
    {
        if (stumpPrefab != null)
        {
            GameObject stump = Object.Instantiate(stumpPrefab);
            stump.name = "SnowStump_" + Random.Range(1, 10000);
            stump.transform.position = position;
            stump.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            stump.transform.localScale = stump.transform.localScale * Random.Range(0.6f, 1.2f);
            ConvertToURP(stump);
            stump.tag = "EditorOnly";
        }
    }

    private static void CreateFallenLog(Vector3 position)
    {
        if (fallenLogPrefab != null)
        {
            GameObject log = Object.Instantiate(fallenLogPrefab);
            log.name = "SnowFallenLog_" + Random.Range(1, 10000);
            log.transform.position = position;
            log.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), Random.Range(-10, 10));
            log.transform.localScale = log.transform.localScale * Random.Range(0.7f, 1.3f);
            ConvertToURP(log);
            log.tag = "EditorOnly";
        }
    }

    private static void CreateCliff(Vector3 position)
    {
        GameObject[] cliffs = new GameObject[cliffPrefabs.Length + lowCliffPrefabs.Length];
        cliffPrefabs.CopyTo(cliffs, 0);
        lowCliffPrefabs.CopyTo(cliffs, cliffPrefabs.Length);
        GameObject selected = cliffs[Random.Range(0, cliffs.Length)];
        if (selected != null)
        {
            GameObject cliff = Object.Instantiate(selected);
            cliff.name = "SnowCliff_" + Random.Range(1, 10000);
            cliff.transform.position = position;
            cliff.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            cliff.transform.localScale = cliff.transform.localScale * Random.Range(0.8f, 1.5f);
            ConvertToURP(cliff);
            cliff.tag = "EditorOnly";
        }
    }

    private static void ConvertToURP(GameObject obj)
    {
        var renderers = obj.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            if (renderer == null || renderer.sharedMaterials == null) continue;
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                var mat = materials[i];
                if (mat == null) continue;
                if (mat.shader != null && mat.shader.name != null &&
                    mat.shader.name.Contains("Universal Render Pipeline")) continue;
                Color col = Color.white;
                Texture tex = null;
                if (mat.HasProperty("_Color")) col = mat.color;
                else if (mat.HasProperty("_BaseColor")) col = mat.GetColor("_BaseColor");
                if (mat.HasProperty("_MainTex") && mat.mainTexture != null) tex = mat.mainTexture;
                else if (mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") != null) tex = mat.GetTexture("_BaseMap");
                var newMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                newMat.color = col;
                if (tex != null) newMat.mainTexture = tex;
                materials[i] = newMat;
            }
            renderer.sharedMaterials = materials;
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
        spawnPoint.AddComponent<SnowEnemySpawnData>().level = level;
    }

    private static Color GetLevelColor(int level)
    {
        float t = (float)level / 20f;
        return Color.Lerp(new Color(0.5f, 0.7f, 1.0f), new Color(1.0f, 0.0f, 0.0f), t);
    }
}
#endif
