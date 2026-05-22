using System.IO;
using System;
using DinoGrow.Core.Data;
using DinoGrow.Infrastructure.Data;
using UnityEditor;
using UnityEngine;

public static class ExcelDataMenu
{
    private const string DefaultExcelFolder = "Assets/GameData/Excel";
    private const string DefaultGeneratedFolder = "Assets/GameData/Generated";

    [MenuItem("Tools/Dino Game/Data/Create Game Data Excel Template")]
    public static void CreateGameDataExcelTemplate()
    {
        var path = EditorUtility.SaveFilePanel("Create Game Data Excel Template", DefaultExcelFolder, "GameData", "xlsx");
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        try
        {
            var dataService = new ExcelDataService();
            dataService.CreateGameDataTemplate(path);
            AssetDatabase.Refresh();

            Debug.Log($"Game data Excel template created: {ToAssetPath(path) ?? path}");
        }
        catch (IOException ex)
        {
            ShowFileAccessError("Create Game Data Excel Template", path, ex);
        }
    }

    [MenuItem("Tools/Dino Game/Data/Convert Game Data Excel To ScriptableObjects")]
    public static void ConvertGameDataExcelToScriptableObjects()
    {
        var excelPath = EditorUtility.OpenFilePanel("Select Game Data Excel", DefaultExcelFolder, "xlsx");
        if (string.IsNullOrEmpty(excelPath))
        {
            return;
        }

        Directory.CreateDirectory(DefaultGeneratedFolder);
        var assetFolder = DefaultGeneratedFolder;

        try
        {
            var dataService = new ExcelDataService();
            var playerRows = dataService.LoadPlayerRows(excelPath);
            var enemyDinoRows = dataService.LoadEnemyDinoRows(excelPath);
            var itemRows = dataService.LoadItemRows(excelPath);
            var stageRows = dataService.LoadStageRows(excelPath);
            var spawnRows = dataService.LoadSpawnRows(excelPath);
            var playerGrowthRows = dataService.LoadPlayerGrowthRows(excelPath);

            SavePlayerDatabase(Path.Combine(assetFolder, "PlayerDatabase.asset").Replace('\\', '/'), playerRows);
            SaveEnemyDinoDatabase(Path.Combine(assetFolder, "EnemyDinoDatabase.asset").Replace('\\', '/'), enemyDinoRows);
            SaveItemDatabase(Path.Combine(assetFolder, "ItemDatabase.asset").Replace('\\', '/'), itemRows);
            SaveStageDatabase(Path.Combine(assetFolder, "StageDatabase.asset").Replace('\\', '/'), stageRows);
            SaveSpawnDatabase(Path.Combine(assetFolder, "SpawnDatabase.asset").Replace('\\', '/'), spawnRows);
            SavePlayerGrowthDatabase(Path.Combine(assetFolder, "PlayerGrowthDatabase.asset").Replace('\\', '/'), playerGrowthRows);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Converted game data Excel to ScriptableObjects. Player: {playerRows.Count}, EnemyDino: {enemyDinoRows.Count}, Item: {itemRows.Count}, Stage: {stageRows.Count}, Spawn: {spawnRows.Count}, PlayerGrowth: {playerGrowthRows.Count}");
        }
        catch (IOException ex)
        {
            ShowFileAccessError("Convert Game Data Excel To ScriptableObjects", excelPath, ex);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("Dino Game Data", ex.Message, "OK");
        }
    }

    private static void SavePlayerDatabase(string assetPath, System.Collections.Generic.IEnumerable<PlayerDataRecord> records)
    {
        var database = ScriptableObject.CreateInstance<PlayerDatabase>();
        database.SetRecords(records);
        SaveAsset(assetPath, database);
    }

    private static void SaveEnemyDinoDatabase(string assetPath, System.Collections.Generic.IEnumerable<DinoDataRecord> records)
    {
        var database = ScriptableObject.CreateInstance<EnemyDinoDatabase>();
        database.SetRecords(records);
        SaveAsset(assetPath, database);
    }

    private static void SaveItemDatabase(string assetPath, System.Collections.Generic.IEnumerable<ItemDataRecord> records)
    {
        var database = ScriptableObject.CreateInstance<ItemDatabase>();
        database.SetRecords(records);
        SaveAsset(assetPath, database);
    }

    private static void SaveStageDatabase(string assetPath, System.Collections.Generic.IEnumerable<StageDataRecord> records)
    {
        var database = ScriptableObject.CreateInstance<StageDatabase>();
        database.SetRecords(records);
        SaveAsset(assetPath, database);
    }

    private static void SaveSpawnDatabase(string assetPath, System.Collections.Generic.IEnumerable<SpawnDataRecord> records)
    {
        var database = ScriptableObject.CreateInstance<SpawnDatabase>();
        database.SetRecords(records);
        SaveAsset(assetPath, database);
    }

    private static void SavePlayerGrowthDatabase(string assetPath, System.Collections.Generic.IEnumerable<PlayerGrowthDataRecord> records)
    {
        var database = ScriptableObject.CreateInstance<PlayerGrowthDatabase>();
        database.SetRecords(records);
        SaveAsset(assetPath, database);
    }

    private static void SaveAsset(string assetPath, ScriptableObject database)
    {
        AssetDatabase.DeleteAsset(assetPath);
        AssetDatabase.CreateAsset(database, assetPath);
    }

    private static void ShowFileAccessError(string title, string path, IOException exception)
    {
        Debug.LogWarning($"{title} failed because the Excel file is locked: {path}\n{exception.Message}");
        EditorUtility.DisplayDialog(
            title,
            $"Cannot access this Excel file.\n\n{path}\n\nClose it in Excel or choose a different file name, then try again.",
            "OK");
    }

    private static string ToAssetPath(string absolutePath)
    {
        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (projectRoot == null)
        {
            return null;
        }

        var normalizedRoot = projectRoot.Replace('\\', '/').TrimEnd('/');
        var normalizedPath = absolutePath.Replace('\\', '/');
        return normalizedPath.StartsWith(normalizedRoot)
            ? normalizedPath.Substring(normalizedRoot.Length + 1)
            : null;
    }
}
