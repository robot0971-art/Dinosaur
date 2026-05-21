using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public static class DesertMapGenerator
{
    // Îß??¨Í∏∞ ?§Ï†ï
    private const float MAP_SIZE = 100f;
    private const float PLAYER_START_ZONE = 25f;
    private const float SAFE_ZONE = 50f;

    // Runemark Studio - Polygon Desert Pack + Tiny Teacup Studio Low Poly Desert Environment ?êÏÖã Í≤ΩÎ°ú
    private const string GROUND_PATH = "Assets/Tiny Teacup Studio/Low Poly Desert Environment/Prefabs/Ground_01.prefab";
    private const string TREE_PATH = "Assets/Tiny Teacup Studio/Low Poly Desert Environment/Prefabs/Tree_01.prefab";
    private const string CACTUS_PATH = "Assets/Runemark Studio/Freebies/Polygon Desert Pack/Prefabs/Cactus1.prefab";
    private const string CACTUS2_PATH = "Assets/Runemark Studio/Freebies/Polygon Desert Pack/Prefabs/Cactus2.prefab";
    private const string CACTUS3_PATH = "Assets/Runemark Studio/Freebies/Polygon Desert Pack/Prefabs/Cactus3.prefab";
    private const string ALOE_PATH = "Assets/Runemark Studio/Freebies/Polygon Desert Pack/Prefabs/Aloe.prefab";
    private const string ROCK_PATH = "Assets/Runemark Studio/Freebies/Polygon Desert Pack/Prefabs/rock01.prefab";
    private const string ROCK2_PATH = "Assets/Runemark Studio/Freebies/Polygon Desert Pack/Prefabs/rock02.prefab";
    private const string ROCK3_PATH = "Assets/Runemark Studio/Freebies/Polygon Desert Pack/Prefabs/rock03.prefab";
    private const string ROCK4_PATH = "Assets/Runemark Studio/Freebies/Polygon Desert Pack/Prefabs/rock04.prefab";
    private const string ROCK5_PATH = "Assets/Runemark Studio/Freebies/Polygon Desert Pack/Prefabs/rock05.prefab";
    private const string ROCK6_PATH = "Assets/Runemark Studio/Freebies/Polygon Desert Pack/Prefabs/rock06.prefab";
    private const string ROCK7_PATH = "Assets/Runemark Studio/Freebies/Polygon Desert Pack/Prefabs/rock07.prefab";
    private const string ROCK8_PATH = "Assets/Runemark Studio/Freebies/Polygon Desert Pack/Prefabs/rock08.prefab";
    private const string TT_ROCK_PATH = "Assets/Tiny Teacup Studio/Low Poly Desert Environment/Prefabs/Rock_01.prefab";
    private const string TT_ROCK2_PATH = "Assets/Tiny Teacup Studio/Low Poly Desert Environment/Prefabs/Rock_02.prefab";
    private const string TT_ROCK3_PATH = "Assets/Tiny Teacup Studio/Low Poly Desert Environment/Prefabs/Rock_03.prefab";
    private const string TT_ROCK4_PATH = "Assets/Tiny Teacup Studio/Low Poly Desert Environment/Prefabs/Rock_04.prefab";
    private const string TT_ROCK5_PATH = "Assets/Tiny Teacup Studio/Low Poly Desert Environment/Prefabs/Rock_05.prefab";

    // Îß??§Ï†ï
    private static float mapSize = 250f;
    private static int treeCount = 60;
    private static int rockCount = 120;
    private static int cactusCount = 100;
    private static int plantCount = 50;
    private static Vector3 playerStartPosition = new Vector3(0, 0.5f, 0);
    private static int[] enemyCountPerLevel = { 20, 18, 15, 12, 10, 8, 6, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4 };

    // ?êÏÖã Ï∫êÏãú
    private static GameObject groundPrefab;
    private static GameObject treePrefab;
    private static GameObject cactusPrefab;
    private static GameObject cactus2Prefab;
    private static GameObject cactus3Prefab;
    private static GameObject aloePrefab;
    private static GameObject rockPrefab;
    private static GameObject rock2Prefab;
    private static GameObject rock3Prefab;
    private static GameObject rock4Prefab;
    private static GameObject rock5Prefab;
    private static GameObject rock6Prefab;
    private static GameObject rock7Prefab;
    private static GameObject rock8Prefab;
    private static GameObject ttRockPrefab;
    private static GameObject ttRock2Prefab;
    private static GameObject ttRock3Prefab;
    private static GameObject ttRock4Prefab;
    private static GameObject ttRock5Prefab;

    /// <summary>
    /// ?êÎîî??Î©îÎâ¥?êÏÑú ?∏Ï∂ú?òÎäî Îß??ùÏÑ± ?®Ïàò
    /// </summary>
    [MenuItem("DinoGrow/Generate Desert Map")]
    public static void GenerateMap()
    {
        // Í∏∞Ï°¥ Îß??ïÎ¶¨
        ClearExistingMap();

        // ??Îß??ùÏÑ±
        CreateGround();
        CreateDesertElements();
        CreatePlayerStartPoint();
        CreateEnemySpawnPoints();

        Debug.Log("?¨Îßâ Îß??ùÏÑ± ?ÑÎ£å!");
    }

    /// <summary>
    /// Í∏∞Ï°¥ Îß??§Î∏å?ùÌä∏ ??†ú
    /// </summary>
    private static void ClearExistingMap()
    {
        GameObject[] existingObjects = GameObject.FindGameObjectsWithTag("EditorOnly");
        foreach (GameObject obj in existingObjects)
        {
            if (obj.name.Contains("Ground") || obj.name.Contains("Desert") ||
                obj.name.Contains("Spawn") || obj.name.Contains("PlayerStart") ||
                obj.name.Contains("Tree") || obj.name.Contains("Rock") ||
                obj.name.Contains("Cactus") || obj.name.Contains("TTRock"))
            {
                Object.DestroyImmediate(obj);
            }
        }
    }

    /// <summary>
    /// ÏßÄ???ùÏÑ± (?¨Îßâ) - Desert Pack Ground ?êÏÖã
    /// </summary>
    private static void CreateGround()
    {
        LoadAssets();

        if (groundPrefab != null)
        {
            GameObject groundParent = new GameObject("Ground_Desert");
            groundParent.tag = "EditorOnly";

            float tileSize = 5f;
            int tileCount = Mathf.CeilToInt(mapSize / tileSize);
            float offset = (tileCount * tileSize) / 2f - tileSize / 2f;

            for (int x = 0; x < tileCount; x++)
            {
                for (int z = 0; z < tileCount; z++)
                {
                    GameObject tile = Object.Instantiate(groundPrefab, groundParent.transform);
                    tile.name = $"DesertGround_{x}_{z}";
                    tile.transform.position = new Vector3(x * tileSize - offset, 0, z * tileSize - offset);
                    tile.transform.rotation = Quaternion.identity;
                    tile.transform.localScale = Vector3.one;
                }
            }

            Debug.Log($"ÏßÄ???ùÏÑ± ?ÑÎ£å: Desert Ground ({tileCount}x{tileCount} ?Ä??");
        }
        else
        {
            // ?¥Î∞±: Í∏∞Î≥∏ Plane (?¨Îßâ ?âÏÉÅ)
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground_Desert";
            ground.transform.localScale = new Vector3(mapSize / 10f, 1, mapSize / 10f);

            Material desertMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            desertMaterial.color = new Color(0.85f, 0.72f, 0.45f);
            ground.GetComponent<Renderer>().material = desertMaterial;

            ground.tag = "EditorOnly";
            Debug.Log("ÏßÄ???ùÏÑ± ?ÑÎ£å: Í∏∞Î≥∏ Plane (?¨Îßâ ?âÏÉÅ ?¥Î∞±)");
        }
    }

    /// <summary>
    /// ?¨Îßâ ?îÏÜå ?ùÏÑ± (?ºÏûê?? Î∞îÏúÑ, ?†Ïù∏?? ?àÎ≤Ω)
    /// </summary>
    private static void CreateDesertElements()
    {
        LoadAssets();

        // ?òÎ¨¥ (?ºÏûê?? ?ùÏÑ±
        for (int i = 0; i < treeCount; i++)
        {
            Vector3 pos = GetRandomPosition(PLAYER_START_ZONE);
            CreateTree(pos);
        }

        // Î∞îÏúÑ ?ùÏÑ± (?ëÏ? ?êÍ∞à ~ Ï§ëÍ∞Ñ ?¨Í∏∞ Î∞îÏúÑ)
        for (int i = 0; i < rockCount; i++)
        {
            Vector3 pos = GetRandomPosition(PLAYER_START_ZONE);
            CreateRock(pos, Random.Range(0.3f, 0.8f));
        }

        // ??Î∞îÏúÑ ?ùÏÑ±
        for (int i = 0; i < 40; i++)
        {
            Vector3 pos = GetRandomPosition(PLAYER_START_ZONE + 10f);
            CreateRock(pos, Random.Range(0.8f, 1.8f));
        }

        // ?†Ïù∏???ùÏÑ±
        for (int i = 0; i < cactusCount; i++)
        {
            Vector3 pos = GetRandomPosition(PLAYER_START_ZONE);
            CreateCactus(pos);
        }

        // ?¨Îßâ ?ùÎ¨º ?ùÏÑ±
        for (int i = 0; i < plantCount; i++)
        {
            Vector3 pos = GetRandomPosition(PLAYER_START_ZONE);
            CreatePlant(pos);
        }

        // ?àÎ≤Ω ?Ä??Tiny Teacup Studio Î∞îÏúÑÎ°?ÏßÄ???îÌÖå??Ï∂îÍ?
        for (int i = 0; i < 60; i++)
        {
            Vector3 pos = GetRandomPosition(PLAYER_START_ZONE + 15f);
            CreateTTRock(pos, Random.Range(0.8f, 2.0f));
        }

        Debug.Log($"?¨Îßâ ?îÏÜå ?ùÏÑ± ?ÑÎ£å: ?ºÏûê??{treeCount}, ?ëÏ?Î∞îÏúÑ {rockCount}, ?∞Î∞î??40, ?†Ïù∏??{cactusCount}, ?ùÎ¨º {plantCount}, TTÎ∞îÏúÑ 60");
    }

    /// <summary>
    /// ?úÎç§ ?ÑÏπò Î∞òÌôò
    /// </summary>
    private static Vector3 GetRandomPosition(float minDistanceFromCenter)
    {
        Vector2 randomPos = Random.insideUnitCircle * (mapSize / 2 - 5f);

        if (randomPos.magnitude < minDistanceFromCenter)
        {
            randomPos = randomPos.normalized * (minDistanceFromCenter + Random.Range(5f, 15f));
        }

        return new Vector3(randomPos.x, 0f, randomPos.y);
    }

    /// <summary>
    /// ?êÏÖã Î°úÎìú
    /// </summary>
    private static void LoadAssets()
    {
#if UNITY_EDITOR
        // Ground
        if (groundPrefab == null)
            groundPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GROUND_PATH);

        // Tree
        if (treePrefab == null)
            treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TREE_PATH);

        // Cacti
        if (cactusPrefab == null)
            cactusPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CACTUS_PATH);
        if (cactus2Prefab == null)
            cactus2Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CACTUS2_PATH);
        if (cactus3Prefab == null)
            cactus3Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CACTUS3_PATH);
        if (aloePrefab == null)
            aloePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ALOE_PATH);

        // Rocks (Runemark Studio - 8Ï¢?
        if (rockPrefab == null)
            rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ROCK_PATH);
        if (rock2Prefab == null)
            rock2Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ROCK2_PATH);
        if (rock3Prefab == null)
            rock3Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ROCK3_PATH);
        if (rock4Prefab == null)
            rock4Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ROCK4_PATH);
        if (rock5Prefab == null)
            rock5Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ROCK5_PATH);
        if (rock6Prefab == null)
            rock6Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ROCK6_PATH);
        if (rock7Prefab == null)
            rock7Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ROCK7_PATH);
        if (rock8Prefab == null)
            rock8Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ROCK8_PATH);

        // Tiny Teacup Studio Rocks (5Ï¢?
        if (ttRockPrefab == null)
            ttRockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TT_ROCK_PATH);
        if (ttRock2Prefab == null)
            ttRock2Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TT_ROCK2_PATH);
        if (ttRock3Prefab == null)
            ttRock3Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TT_ROCK3_PATH);
        if (ttRock4Prefab == null)
            ttRock4Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TT_ROCK4_PATH);
        if (ttRock5Prefab == null)
            ttRock5Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TT_ROCK5_PATH);

        Debug.Log($"Runemark Studio + Tiny Teacup Desert ?êÏÖã Î°úÎìú ?ÑÎ£å: Ground={groundPrefab != null}, Rocks={rockPrefab != null}(8Ï¢?, TTRocks={ttRockPrefab != null}(5Ï¢?, Cacti={cactusPrefab != null}(3Ï¢?Aloe)");
#endif
    }

    /// <summary>
    /// ?òÎ¨¥ (?ºÏûê?? ?ùÏÑ±
    /// </summary>
    private static void CreateTree(Vector3 position)
    {
        GameObject tree;

        if (treePrefab != null)
        {
            tree = Object.Instantiate(treePrefab);
            tree.name = "DesertTree_" + Random.Range(1, 1000);
            tree.transform.localScale = Vector3.one * Random.Range(0.8f, 1.5f);
        }
        else
        {
            tree = new GameObject("DesertTree_" + Random.Range(1, 1000));
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.transform.parent = tree.transform;
            trunk.transform.localPosition = new Vector3(0, 2f, 0);
            trunk.transform.localScale = new Vector3(0.25f, 2f, 0.25f);
            Material trunkMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            trunkMat.color = new Color(0.55f, 0.40f, 0.25f);
            trunk.GetComponent<Renderer>().material = trunkMat;
            
            GameObject leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaves.transform.parent = tree.transform;
            leaves.transform.localPosition = new Vector3(0, 4f, 0);
            leaves.transform.localScale = new Vector3(2.5f, 1f, 2.5f);
            Material leavesMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            leavesMat.color = new Color(0.35f, 0.55f, 0.25f);
            leaves.GetComponent<Renderer>().material = leavesMat;
        }

        tree.transform.position = position;
        tree.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        tree.tag = "EditorOnly";
    }

    /// <summary>
    /// Î∞îÏúÑ ?ùÏÑ±
    /// </summary>
    private static void CreateRock(Vector3 position, float scale)
    {
        GameObject rock;

        GameObject[] rockPrefabs = { rockPrefab, rock2Prefab, rock3Prefab, rock4Prefab, rock5Prefab, rock6Prefab, rock7Prefab, rock8Prefab };
        GameObject selectedRock = rockPrefabs[Random.Range(0, rockPrefabs.Length)];

        if (selectedRock != null)
        {
            rock = Object.Instantiate(selectedRock);
            rock.name = "DesertRock_" + Random.Range(1, 1000);
            rock.transform.localScale = Vector3.one * scale;
        }
        else
        {
            rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rock.name = "DesertRock_" + Random.Range(1, 1000);
            rock.transform.localScale = new Vector3(scale * 2f, scale * 0.8f, scale * 2f);
            Material rockMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            rockMat.color = new Color(0.75f, 0.65f, 0.50f);
            rock.GetComponent<Renderer>().material = rockMat;
        }

        rock.transform.position = position;
        rock.transform.rotation = Quaternion.Euler(Random.Range(0, 15), Random.Range(0, 360), Random.Range(0, 15));
        rock.tag = "EditorOnly";
    }

    /// <summary>
    /// ?†Ïù∏???ùÏÑ±
    /// </summary>
    private static void CreateCactus(Vector3 position)
    {
        GameObject cactus;

        GameObject[] cactusPrefabs = { cactusPrefab, cactus2Prefab, cactus3Prefab };
        GameObject selectedCactus = cactusPrefabs[Random.Range(0, cactusPrefabs.Length)];

        if (selectedCactus != null)
        {
            cactus = Object.Instantiate(selectedCactus);
            cactus.name = "Cactus_" + Random.Range(1, 1000);
            cactus.transform.localScale = Vector3.one * Random.Range(0.7f, 1.4f);
        }
        else
        {
            cactus = new GameObject("Cactus_" + Random.Range(1, 1000));
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.transform.parent = cactus.transform;
            body.transform.localPosition = new Vector3(0, 0.8f, 0);
            body.transform.localScale = new Vector3(0.4f, 0.8f, 0.4f);
            Material cactusMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            cactusMat.color = new Color(0.30f, 0.50f, 0.25f);
            body.GetComponent<Renderer>().material = cactusMat;
        }

        cactus.transform.position = position;
        cactus.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        cactus.tag = "EditorOnly";
    }

    private static void CreatePlant(Vector3 position)
    {
        GameObject[] plantPrefabs = { aloePrefab, cactus2Prefab, cactus3Prefab };
        GameObject selectedPlant = plantPrefabs[Random.Range(0, plantPrefabs.Length)];

        if (selectedPlant != null)
        {
            GameObject plant = Object.Instantiate(selectedPlant);
            plant.name = "DesertPlant_" + Random.Range(1, 1000);
            plant.transform.position = position;
            plant.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            plant.transform.localScale = Vector3.one * Random.Range(0.4f, 0.8f);
            plant.tag = "EditorOnly";
        }
    }

    private static void CreateTTRock(Vector3 position, float scale)
    {
        GameObject[] ttRockPrefabs = { ttRockPrefab, ttRock2Prefab, ttRock3Prefab, ttRock4Prefab, ttRock5Prefab };
        GameObject selectedRock = ttRockPrefabs[Random.Range(0, ttRockPrefabs.Length)];

        if (selectedRock != null)
        {
            GameObject rock = Object.Instantiate(selectedRock);
            rock.name = "TTRock_" + Random.Range(1, 1000);
            rock.transform.position = position;
            rock.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            rock.transform.localScale = Vector3.one * scale;
            rock.tag = "EditorOnly";
        }
    }

    /// <summary>
    /// ?åÎ†à?¥Ïñ¥ ?úÏûë ÏßÄ???ùÏÑ±
    /// </summary>
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

        Debug.Log($"?åÎ†à?¥Ïñ¥ ?úÏûë ÏßÄ???ùÏÑ±: {playerStartPosition}");
    }

    /// <summary>
    /// ??Í≥µÎ£° ?§Ìè∞ ?ÑÏπò ?ùÏÑ±
    /// </summary>
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

        Debug.Log("??Í≥µÎ£° ?§Ìè∞ ?ÑÏπò ?ùÏÑ± ?ÑÎ£å (?àÎ≤® 1-20)");
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

        return new Vector3(
            Mathf.Cos(angle) * distance,
            0.5f,
            Mathf.Sin(angle) * distance
        );
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

        spawnPoint.AddComponent<DesertEnemySpawnData>().SetLevel(level);
    }

    private static Color GetLevelColor(int level)
    {
        float t = (float)level / 20f;
        return Color.Lerp(new Color(0.85f, 0.72f, 0.45f), Color.red, t);
    }
}
