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
            var dinoRows = dataService.LoadDinoRows(excelPath);
            var stageRows = dataService.LoadStageRows(excelPath);
            var spawnRows = dataService.LoadSpawnRows(excelPath);
            var playerGrowthRows = dataService.LoadPlayerGrowthRows(excelPath);

            SaveDatabase(Path.Combine(assetFolder, "DinoDatabase.asset").Replace('\\', '/'), dinoRows);
            SaveDatabase(Path.Combine(assetFolder, "StageDatabase.asset").Replace('\\', '/'), stageRows);
            SaveDatabase(Path.Combine(assetFolder, "SpawnDatabase.asset").Replace('\\', '/'), spawnRows);
            SaveDatabase(Path.Combine(assetFolder, "PlayerGrowthDatabase.asset").Replace('\\', '/'), playerGrowthRows);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Converted game data Excel to ScriptableObjects. Dino: {dinoRows.Count}, Stage: {stageRows.Count}, Spawn: {spawnRows.Count}, PlayerGrowth: {playerGrowthRows.Count}");
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

    private static void SaveDatabase(string assetPath, System.Collections.Generic.IEnumerable<DinoDataRecord> records)
    {
        var database = ScriptableObject.CreateInstance<DinoDatabase>();
        database.SetRecords(records);
        SaveAsset(assetPath, database);
    }

    private static void SaveDatabase(string assetPath, System.Collections.Generic.IEnumerable<StageDataRecord> records)
    {
        var database = ScriptableObject.CreateInstance<StageDatabase>();
        database.SetRecords(records);
        SaveAsset(assetPath, database);
    }

    private static void SaveDatabase(string assetPath, System.Collections.Generic.IEnumerable<SpawnDataRecord> records)
    {
        var database = ScriptableObject.CreateInstance<SpawnDatabase>();
        database.SetRecords(records);
        SaveAsset(assetPath, database);
    }

    private static void SaveDatabase(string assetPath, System.Collections.Generic.IEnumerable<PlayerGrowthDataRecord> records)
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
