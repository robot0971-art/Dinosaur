using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public static class GrasslandMapGenerator
{
    // 맵 크기 설정
    private const float MAP_SIZE = 100f;
    private const float PLAYER_START_ZONE = 25f;
    private const float SAFE_ZONE = 50f;

    // Pure Poly - Free Low Poly Nature Forest 에셋 경로
    private const string GROUND_PATH = "Assets/Pure Poly/Free Low Poly Nature Pack/Prefabs/PP_Meadow_07.prefab";
    private const string GROUND2_PATH = "Assets/Pure Poly/Free Low Poly Nature Pack/Prefabs/PP_Meadow_08.prefab";
    private const string TREE_PATH = "Assets/Pure Poly/Free Low Poly Nature Pack/Prefabs/PP_Birch_Tree_05.prefab";
    private const string TREE2_PATH = "Assets/Pure Poly/Free Low Poly Nature Pack/Prefabs/PP_Birch_Tree_06.prefab";
    private const string TREE3_PATH = "Assets/Pure Poly/Free Low Poly Nature Pack/Prefabs/PP_Tree_02.prefab";
    private const string TREE4_PATH = "Assets/Pure Poly/Free Low Poly Nature Pack/Prefabs/PP_Tree_10.prefab";
    private const string ROCK_PATH = "Assets/Pure Poly/Free Low Poly Nature Pack/Prefabs/PP_Rock_Moss_Grown_09.prefab";
    private const string ROCK2_PATH = "Assets/Pure Poly/Free Low Poly Nature Pack/Prefabs/PP_Rock_Moss_Grown_11.prefab";
    private const string ROCK3_PATH = "Assets/Pure Poly/Free Low Poly Nature Pack/Prefabs/PP_Rock_Pile_Forest_Moss_05.prefab";
    private const string ROCK4_PATH = "Assets/Pure Poly/Free Low Poly Nature Pack/Prefabs/PP_Rock_Pile_Forest_Moss_10.prefab";
    private const string GRASS_PATH = "Assets/Pure Poly/Free Low Poly Nature Pack/Prefabs/PP_Grass_11.prefab";
    private const string GRASS2_PATH = "Assets/Pure Poly/Free Low Poly Nature Pack/Prefabs/PP_Grass_15.prefab";
    private const string BUSH_PATH = "Assets/Pure Poly/Free Low Poly Nature Pack/Prefabs/PP_Bush_02.prefab";
    private const string FLOWERS_PATH = "Assets/Pure Poly/Free Low Poly Nature Pack/Prefabs/PP_Sunflower_04.prefab";
    private const string FLOWERS2_PATH = "Assets/Pure Poly/Free Low Poly Nature Pack/Prefabs/PP_Daffodil_03.prefab";
    private const string FLOWERS3_PATH = "Assets/Pure Poly/Free Low Poly Nature Pack/Prefabs/PP_Hyacinth_04.prefab";
    private const string STUMP_PATH = "Assets/Pure Poly/Free Low Poly Nature Pack/Prefabs/PP_Mushroom_Fantasy_Orange_09.prefab";
    private const string STUMP2_PATH = "Assets/Pure Poly/Free Low Poly Nature Pack/Prefabs/PP_Mushroom_Fantasy_Purple_05.prefab";

    // 맵 설정
    private static float mapSize = 250f;
    private static int treeCount = 100;
    private static int rockCount = 60;
    private static int grassClusterCount = 400;
    private static int bushCount = 60;
    private static int flowerCount = 100;
    private static int mushroomCount = 50;
    private static int scatteredGrassCount = 800;
    private static Vector3 playerStartPosition = new Vector3(0, 0.5f, 0);
    private static int[] enemyCountPerLevel = { 20, 18, 15, 12, 10, 8, 6, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4 };

    // 에셋 캐시
    private static GameObject groundPrefab;
    private static GameObject ground2Prefab;
    private static GameObject treePrefab;
    private static GameObject tree2Prefab;
    private static GameObject tree3Prefab;
    private static GameObject tree4Prefab;
    private static GameObject rockPrefab;
    private static GameObject rock2Prefab;
    private static GameObject rock3Prefab;
    private static GameObject rock4Prefab;
    private static GameObject grassPrefab;
    private static GameObject grass2Prefab;
    private static GameObject bushPrefab;
    private static GameObject flowersPrefab;
    private static GameObject flowers2Prefab;
    private static GameObject flowers3Prefab;
    private static GameObject stumpPrefab;
    private static GameObject stump2Prefab;

    /// <summary>
    /// 에디터 메뉴에서 호출하는 맵 생성 함수
    /// </summary>
    [MenuItem("DinoGrow/Generate Grassland Map")]
    public static void GenerateMap()
    {
        // 기존 맵 정리
        ClearExistingMap();

        // 새 맵 생성
        CreateGround();
        CreateNatureElements();
        CreatePlayerStartPoint();
        CreateEnemySpawnPoints();

        Debug.Log("초원 맵 생성 완료!");
    }

    /// <summary>
    /// 기존 맵 오브젝트 삭제
    /// </summary>
    private static void ClearExistingMap()
    {
        // 기존 생성된 오브젝트 찾기 및 삭제
        GameObject[] existingObjects = GameObject.FindGameObjectsWithTag("EditorOnly");
        foreach (GameObject obj in existingObjects)
        {
            if (obj.name.Contains("Ground") || obj.name.Contains("Nature") ||
                obj.name.Contains("Spawn") || obj.name.Contains("PlayerStart") ||
                obj.name.Contains("Tree") || obj.name.Contains("Rock") ||
                obj.name.Contains("Grass"))
            {
                Object.DestroyImmediate(obj);
            }
        }
    }

/// <summary>
    /// 지형 생성 (초원) - Pure Poly Meadow 에셋
    /// </summary>
    private static void CreateGround()
    {
        // 에셋 로드
        LoadAssets();

        // Meadow 에셋 사용 (초원 스타일)
        GameObject selectedGround = (Random.value > 0.5f && ground2Prefab != null) ? ground2Prefab : groundPrefab;

        if (selectedGround != null)
        {
            // 부모 오브젝트 생성
            GameObject groundParent = new GameObject("Ground_Grassland");
            groundParent.tag = "EditorOnly";

            // Meadow 타일 크기는 10x10 정도, 맵 크기에 맞게 배치
            float tileSize = 10f;
            int tileCount = Mathf.CeilToInt(mapSize / tileSize);
            float offset = (tileCount * tileSize) / 2f - tileSize / 2f;

            for (int x = 0; x < tileCount; x++)
            {
                for (int z = 0; z < tileCount; z++)
                {
                    GameObject tile = Object.Instantiate(selectedGround, groundParent.transform);
                    tile.name = $"Meadow_{x}_{z}";
                    tile.transform.position = new Vector3(x * tileSize - offset, 0, z * tileSize - offset);
                    tile.transform.rotation = Quaternion.identity;
                    tile.transform.localScale = Vector3.one;
                }
            }

            Debug.Log($"지형 생성 완료: Pure Poly Meadow ({tileCount}x{tileCount} 타일)");
        }
        else
        {
            // 폴백: 기본 Plane
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground_Grassland";
            ground.transform.localScale = new Vector3(mapSize / 10f, 1, mapSize / 10f);

            Material grassMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            grassMaterial.color = new Color(0.35f, 0.65f, 0.25f);
            ground.GetComponent<Renderer>().material = grassMaterial;

            ground.tag = "EditorOnly";
            Debug.Log("지형 생성 완료: 기본 Plane (폴백)");
        }
    }

    /// <summary>
    /// 자연 요소 생성 (나무, 바위, 풀)
    /// </summary>
    private static void CreateNatureElements()
    {
        // 에셋 로드
        LoadAssets();

        // 나무 생성
        for (int i = 0; i < treeCount; i++)
        {
            Vector3 pos = GetRandomPosition(PLAYER_START_ZONE);
            CreateTree(pos);
        }

        // 바위 생성
        for (int i = 0; i < rockCount; i++)
        {
            Vector3 pos = GetRandomPosition(PLAYER_START_ZONE);
            CreateRock(pos);
        }

        // 풀 클러스터 생성
        for (int i = 0; i < grassClusterCount; i++)
        {
            Vector3 centerPos = GetRandomPosition(PLAYER_START_ZONE);
            CreateGrassCluster(centerPos);
        }

        // 덩굴 생성
        for (int i = 0; i < bushCount; i++)
        {
            Vector3 pos = GetRandomPosition(PLAYER_START_ZONE);
            CreateBush(pos);
        }

        // 꽃 생성
        for (int i = 0; i < flowerCount; i++)
        {
            Vector3 pos = GetRandomPosition(PLAYER_START_ZONE);
            CreateFlowers(pos);
        }

        // 버섯 생성
        for (int i = 0; i < mushroomCount; i++)
        {
            Vector3 pos = GetRandomPosition(PLAYER_START_ZONE);
            CreateMushroom(pos);
        }

        // 추가：무직초 풀 배치 (맵 전체에 퍼뜨리기)
        for (int i = 0; i < scatteredGrassCount; i++)
        {
            Vector3 pos = GetRandomPosition(PLAYER_START_ZONE);
            CreateScatteredGrass(pos);
        }

        Debug.Log($"자연 요소 생성 완료: 나무 {treeCount}, 바위 {rockCount}, 풀 클러스터 {grassClusterCount}, 덩굴 {bushCount}, 꽃 {flowerCount}, 버섯 {mushroomCount}, 개별 풀 {scatteredGrassCount}");
    }

    /// <summary>
    /// 랜덤 위치 반환
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
    /// 에셋 로드
    /// </summary>
    private static void LoadAssets()
    {
#if UNITY_EDITOR
        // Ground
        if (groundPrefab == null)
            groundPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GROUND_PATH);
        if (ground2Prefab == null)
            ground2Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GROUND2_PATH);

        // Trees
        if (treePrefab == null)
            treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TREE_PATH);
        if (tree2Prefab == null)
            tree2Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TREE2_PATH);
        if (tree3Prefab == null)
            tree3Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TREE3_PATH);
        if (tree4Prefab == null)
            tree4Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TREE4_PATH);

        // Rocks
        if (rockPrefab == null)
            rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ROCK_PATH);
        if (rock2Prefab == null)
            rock2Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ROCK2_PATH);
        if (rock3Prefab == null)
            rock3Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ROCK3_PATH);
        if (rock4Prefab == null)
            rock4Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ROCK4_PATH);

        // Grass
        if (grassPrefab == null)
            grassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GRASS_PATH);
        if (grass2Prefab == null)
            grass2Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GRASS2_PATH);

        // Bush
        if (bushPrefab == null)
            bushPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BUSH_PATH);

        // Flowers
        if (flowersPrefab == null)
            flowersPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FLOWERS_PATH);
        if (flowers2Prefab == null)
            flowers2Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FLOWERS2_PATH);
        if (flowers3Prefab == null)
            flowers3Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FLOWERS3_PATH);

        // Mushrooms
        if (stumpPrefab == null)
            stumpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(STUMP_PATH);
        if (stump2Prefab == null)
            stump2Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(STUMP2_PATH);

        Debug.Log($"Pure Poly 에셋 로드 완료: Ground={groundPrefab != null}, Trees={treePrefab != null}, Rocks={rockPrefab != null}");
#endif
    }

    /// <summary>
    /// URP 재질로 변환 (색상 유지)
    /// </summary>
    private static void ConvertToURP(GameObject obj)
    {
        var renderers = obj.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            if (renderer != null && renderer.sharedMaterials != null)
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var mat = materials[i];
                    if (mat != null)
                    {
                        // 원본 색상 저장
                        Color originalColor = Color.white;
                        bool hasColor = false;

                        // 다양한 색상 프로퍼티 확인
                        if (mat.HasProperty("_BaseColor"))
                        {
                            originalColor = mat.GetColor("_BaseColor");
                            hasColor = true;
                        }
                        else if (mat.HasProperty("_Color"))
                        {
                            originalColor = mat.GetColor("_Color");
                            hasColor = true;
                        }
                        else if (mat.HasProperty("_MainTex_Color"))
                        {
                            originalColor = mat.GetColor("_MainTex_Color");
                            hasColor = true;
                        }

                        // URP Lit 재질 생성
                        var newMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        newMat.name = mat.name + "_URP";

                        // 색상 복사 (하얀색이 아니면)
                        if (hasColor && originalColor != Color.white && originalColor != new Color(1, 1, 1, 0))
                        {
                            newMat.color = originalColor;
                        }

                        materials[i] = newMat;
                    }
                }
                renderer.sharedMaterials = materials;
            }
        }
    }

    /// <summary>
    /// 나무 생성
    /// </summary>
    private static void CreateTree(Vector3 position)
    {
        GameObject tree;

        // 랜덤으로 나무 종류 선택 (4가지 변형)
        GameObject[] treePrefabs = { treePrefab, tree2Prefab, tree3Prefab, tree4Prefab };
        GameObject selectedTree = treePrefabs[Random.Range(0, treePrefabs.Length)];

        if (selectedTree != null)
        {
            tree = Object.Instantiate(selectedTree);
            tree.name = "Tree_" + Random.Range(1, 1000);
            tree.transform.localScale = Vector3.one * Random.Range(0.8f, 1.4f);
        }
        else
        {
            // 폴백: 기본 Primitive
            tree = new GameObject("Tree_" + Random.Range(1, 1000));
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.transform.parent = tree.transform;
            trunk.transform.localPosition = new Vector3(0, 1.5f, 0);
            trunk.transform.localScale = new Vector3(0.3f, 1.5f, 0.3f);
            GameObject leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaves.transform.parent = tree.transform;
            leaves.transform.localPosition = new Vector3(0, 3.5f, 0);
            leaves.transform.localScale = new Vector3(2f, 2f, 2f);
        }

        tree.transform.position = position;
        tree.transform.parent = null;
        tree.tag = "EditorOnly";
    }

    /// <summary>
    /// 바위 생성
    /// </summary>
    private static void CreateRock(Vector3 position)
    {
        GameObject rock;

        // 랜덤으로 바위 종류 선택 (4가지 변형)
        GameObject[] rockPrefabs = { rockPrefab, rock2Prefab, rock3Prefab, rock4Prefab };
        GameObject selectedRock = rockPrefabs[Random.Range(0, rockPrefabs.Length)];

        if (selectedRock != null)
        {
            rock = Object.Instantiate(selectedRock);
            rock.name = "Rock_" + Random.Range(1, 1000);
            rock.transform.localScale = Vector3.one * Random.Range(0.7f, 1.3f);
        }
        else
        {
            rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rock.name = "Rock_" + Random.Range(1, 1000);
            rock.transform.localScale = new Vector3(Random.Range(1f, 2f), Random.Range(0.5f, 1.5f), Random.Range(1f, 2f));
        }

        rock.transform.position = position + Vector3.up * 0.15f;
        rock.transform.rotation = Quaternion.Euler(Random.Range(0, 30), Random.Range(0, 360), Random.Range(0, 30));
        rock.tag = "EditorOnly";
    }

    /// <summary>
    /// 풀/꽃/덩굴 클러스터 생성
    /// </summary>
    private static void CreateGrassCluster(Vector3 centerPos)
    {
        GameObject cluster = new GameObject("GrassCluster_" + Random.Range(1, 1000));
        cluster.transform.position = centerPos;
        cluster.tag = "EditorOnly";

        // 더 무성한 초원 느낌 - 더 많은 풀 배치
        int grassCount = Random.Range(15, 25);
        for (int i = 0; i < grassCount; i++)
        {
            // 더 좁은 범위에 집중 배치
            Vector3 offset = new Vector3(Random.Range(-1.5f, 1.5f), 0, Random.Range(-1.5f, 1.5f));

            // 초원 느낌 - 풀을 더 많이,其他地方는 적게
            float rand = Random.value;
            GameObject prefab = null;

            if (rand < 0.1f && bushPrefab != null)
                prefab = bushPrefab;
            else if (rand < 0.25f)
            {
                // 꽃 변형 선택
                GameObject[] flowerPrefabs = { flowersPrefab, flowers2Prefab, flowers3Prefab };
                prefab = flowerPrefabs[Random.Range(0, flowerPrefabs.Length)];
            }
            else if (rand < 0.35f)
            {
                // 버섯 변형 선택
                GameObject[] stumpPrefabs = { stumpPrefab, stump2Prefab };
                prefab = stumpPrefabs[Random.Range(0, stumpPrefabs.Length)];
            }
            else
            {
                // 풀 변형 선택 (4가지 모두 사용)
                GameObject[] grassPrefabs = { grassPrefab, grass2Prefab };
                prefab = grassPrefabs[Random.Range(0, grassPrefabs.Length)];
            }

            if (prefab != null)
            {
                GameObject grass = Object.Instantiate(prefab);
                grass.transform.parent = cluster.transform;
                grass.transform.position = centerPos + offset + Vector3.up * 0.15f;
                // 초원처럼 무성하게 - 크기 약간 작게, 밀도 높게
                grass.transform.localScale = Vector3.one * Random.Range(0.7f, 1.1f);
            }
            else
            {
                CreateSimpleGrass(cluster.transform, centerPos + offset);
            }
        }
    }

    /// <summary>
    /// 덩굴 생성
    /// </summary>
    private static void CreateBush(Vector3 position)
    {
        if (bushPrefab != null)
        {
            GameObject bush = Object.Instantiate(bushPrefab);
            bush.name = "Bush_" + Random.Range(1, 1000);
            bush.transform.position = position + Vector3.up * 0.15f;
            bush.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            bush.transform.localScale = Vector3.one * Random.Range(0.8f, 1.3f);
            bush.tag = "EditorOnly";
        }
    }

    /// <summary>
    /// 꽃 생성
    /// </summary>
    private static void CreateFlowers(Vector3 position)
    {
        GameObject[] flowerPrefabs = { flowersPrefab, flowers2Prefab, flowers3Prefab };
        GameObject selectedFlower = flowerPrefabs[Random.Range(0, flowerPrefabs.Length)];

        if (selectedFlower != null)
        {
            GameObject flowers = Object.Instantiate(selectedFlower);
            flowers.name = "Flowers_" + Random.Range(1, 1000);
            flowers.transform.position = position + Vector3.up * 0.1f;
            flowers.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            flowers.transform.localScale = Vector3.one * Random.Range(0.7f, 1.1f);
            flowers.tag = "EditorOnly";
        }
    }

    /// <summary>
    /// 버섯 생성
    /// </summary>
    private static void CreateMushroom(Vector3 position)
    {
        GameObject[] mushroomPrefabs = { stumpPrefab, stump2Prefab };
        GameObject selectedMushroom = mushroomPrefabs[Random.Range(0, mushroomPrefabs.Length)];

        if (selectedMushroom != null)
        {
            GameObject mushroom = Object.Instantiate(selectedMushroom);
            mushroom.name = "Mushroom_" + Random.Range(1, 1000);
            mushroom.transform.position = position + Vector3.up * 0.05f;
            mushroom.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            mushroom.transform.localScale = Vector3.one * Random.Range(0.6f, 1.0f);
            mushroom.tag = "EditorOnly";
        }
    }

    /// <summary>
    /// 맵 전체에 퍼뜨린 개별 풀 생성 (무성한 초원)
    /// </summary>
    private static void CreateScatteredGrass(Vector3 position)
    {
        GameObject[] grassPrefabs = { grassPrefab, grass2Prefab };
        GameObject selectedGrass = grassPrefabs[Random.Range(0, grassPrefabs.Length)];

        if (selectedGrass != null)
        {
            GameObject grass = Object.Instantiate(selectedGrass);
            grass.name = "ScatteredGrass_" + Random.Range(1, 10000);
            grass.transform.position = position + Vector3.up * 0.1f;
            grass.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            // 작고密密麻麻하게
            grass.transform.localScale = Vector3.one * Random.Range(0.5f, 0.9f);
            grass.tag = "EditorOnly";
        }
    }

    /// <summary>
    /// 간단한 풀 (폴백용)
    /// </summary>
    private static void CreateSimpleGrass(Transform parent, Vector3 position)
    {
        GameObject grass = GameObject.CreatePrimitive(PrimitiveType.Quad);
        grass.transform.parent = parent;
        grass.transform.position = position + Vector3.up * 0.15f;
        grass.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        grass.GetComponent<Collider>().enabled = false;
    }

    /// <summary>
    /// 플레이어 시작 지점 생성
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

        Debug.Log($"플레이어 시작 지점 생성: {playerStartPosition}");
    }

    /// <summary>
    /// 적 공룡 스폰 위치 생성
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

        Debug.Log("적 공룡 스폰 위치 생성 완료 (레벨 1-20)");
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

        spawnPoint.AddComponent<GrasslandEnemySpawnData>().level = level;
    }

    private static Color GetLevelColor(int level)
    {
        float t = (float)level / 20f;
        return Color.Lerp(Color.green, Color.red, t);
    }
}

public class GrasslandEnemySpawnData : MonoBehaviour
{
    public int level;
}