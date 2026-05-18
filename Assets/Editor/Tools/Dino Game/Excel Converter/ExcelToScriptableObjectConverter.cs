using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Dino.Editor.Tools
{
    public static class ExcelToScriptableObjectConverter
    {
        private const string ExcelPath = "Assets/GameData/Excel";
        private const string OutputPath = "Assets/GameData/Generated";

        [MenuItem("Tools/Dino Game/Excel Converter/Convert DinoTable to ScriptableObject")]
        public static void ConvertDinoTable()
        {
            ConvertExcelToSO<DinoTableData, DinoTableEntry>("DinoTable.xlsx", "DinoData", "DinoTable");
        }

        [MenuItem("Tools/Dino Game/Excel Converter/Convert GrowthTable to ScriptableObject")]
        public static void ConvertGrowthTable()
        {
            ConvertExcelToSO<GrowthTableData, GrowthTableEntry>("GrowthTable.xlsx", "GrowthData", "GrowthTable");
        }

        [MenuItem("Tools/Dino Game/Excel Converter/Convert SpawnTable to ScriptableObject")]
        public static void ConvertSpawnTable()
        {
            ConvertExcelToSO<SpawnTableData, SpawnTableEntry>("SpawnTable.xlsx", "SpawnData", "SpawnTable");
        }

        [MenuItem("Tools/Dino Game/Excel Converter/Convert StageTable to ScriptableObject")]
        public static void ConvertStageTable()
        {
            ConvertExcelToSO<StageTableData, StageTableEntry>("StageTable.xlsx", "StageData", "StageTable");
        }

        [MenuItem("Tools/Dino Game/Excel Converter/Convert All Tables")]
        public static void ConvertAllTables()
        {
            ConvertDinoTable();
            ConvertGrowthTable();
            ConvertSpawnTable();
            ConvertStageTable();
            AssetDatabase.Refresh();
            Debug.Log("All tables converted to ScriptableObjects!");
        }

        private static void ConvertExcelToSO<TData, TEntry>(string excelFile, string sheetName, string soName)
            where TData : ScriptableObject, ITableData<TEntry>, new()
            where TEntry : class, new()
        {
            var excelFilePath = Path.Combine(ExcelPath, excelFile);

            if (!File.Exists(excelFilePath))
            {
                Debug.LogError($"Excel file not found: {excelFilePath}");
                return;
            }

            try
            {
                var entries = ReadExcelEntries<TEntry>(excelFilePath, sheetName);

                var soFilePath = Path.Combine(OutputPath, $"{soName}.asset");

                if (!Directory.Exists(OutputPath))
                {
                    Directory.CreateDirectory(OutputPath);
                }

                var existingSO = AssetDatabase.LoadAssetAtPath<TData>(soFilePath);
                TData so;

                if (existingSO != null)
                {
                    so = existingSO;
                    so.SetEntries(entries);
                    EditorUtility.SetDirty(so);
                }
                else
                {
                    so = ScriptableObject.CreateInstance<TData>();
                    so.SetEntries(entries);
                    AssetDatabase.CreateAsset(so, soFilePath);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"Converted {excelFile} to {soFilePath} ({entries.Count} entries)");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error converting {excelFile}: {e.Message}\n{e.StackTrace}");
            }
        }

        private static List<T> ReadExcelEntries<T>(string filePath, string sheetName) where T : class, new()
        {
            var result = new List<T>();

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                var workbook = new NPOI.XSSF.UserModel.XSSFWorkbook(stream);
                var sheet = workbook.GetSheet(sheetName);

                if (sheet == null)
                {
                    Debug.LogError($"Sheet not found: {sheetName}");
                    return result;
                }

                var headerRow = sheet.GetRow(0);
                if (headerRow == null) return result;

                var properties = typeof(T).GetProperties();
                var headerMap = new Dictionary<int, string>();

                for (int i = 0; i < headerRow.LastCellNum; i++)
                {
                    var cell = headerRow.GetCell(i);
                    if (cell != null)
                    {
                        headerMap[i] = cell.StringCellValue;
                    }
                }

                for (int row = 1; row <= sheet.LastRowNum; row++)
                {
                    var dataRow = sheet.GetRow(row);
                    if (dataRow == null) continue;

                    var item = new T();
                    foreach (var kvp in headerMap)
                    {
                        var prop = typeof(T).GetProperty(kvp.Value);
                        if (prop == null) continue;

                        var cell = dataRow.GetCell(kvp.Key);
                        if (cell == null) continue;

                        var value = GetCellValue(cell, prop.PropertyType);
                        prop.SetValue(item, value);
                    }
                    result.Add(item);
                }
            }

            return result;
        }

        private static object GetCellValue(NPOI.SS.UserModel.ICell cell, Type targetType)
        {
            if (cell == null) return null;

            switch (cell.CellType)
            {
                case NPOI.SS.UserModel.CellType.Numeric:
                    if (targetType == typeof(int))
                        return (int)cell.NumericCellValue;
                    if (targetType == typeof(float))
                        return (float)cell.NumericCellValue;
                    if (targetType == typeof(double))
                        return cell.NumericCellValue;
                    return cell.NumericCellValue;

                case NPOI.SS.UserModel.CellType.String:
                    return cell.StringCellValue;

                case NPOI.SS.UserModel.CellType.Boolean:
                    return cell.BooleanCellValue;

                default:
                    return null;
            }
        }
    }

    public interface ITableData<T>
    {
        void SetEntries(List<T> entries);
        List<T> GetEntries();
    }

    [Serializable]
    public class DinoTableEntry
    {
        public int ID;
        public string Name;
        public int Level;
        public float MoveSpeed;
        public float Scale;
        public int ExpReward;
        public string PrefabPath;
        public string Description;
    }

    [Serializable]
    public class GrowthTableEntry
    {
        public int Level;
        public int RequiredExp;
        public float ScaleMultiplier;
        public float MoveSpeed;
        public float CameraDistance;
        public float CameraHeight;
    }

    [Serializable]
    public class SpawnTableEntry
    {
        public int ID;
        public int StageID;
        public int DinoID;
        public int MinLevel;
        public int MaxLevel;
        public float SpawnWeight;
        public float SpawnRadius;
    }

    [Serializable]
    public class StageTableEntry
    {
        public int ID;
        public string Name;
        public int MinPlayerLevel;
        public int MaxPlayerLevel;
        public float MapSize;
        public string SceneName;
    }

    public class DinoTableData : ScriptableObject, ITableData<DinoTableEntry>
    {
        public List<DinoTableEntry> entries = new List<DinoTableEntry>();

        public void SetEntries(List<DinoTableEntry> newEntries)
        {
            entries = newEntries;
        }

        public List<DinoTableEntry> GetEntries()
        {
            return entries;
        }
    }

    public class GrowthTableData : ScriptableObject, ITableData<GrowthTableEntry>
    {
        public List<GrowthTableEntry> entries = new List<GrowthTableEntry>();

        public void SetEntries(List<GrowthTableEntry> newEntries)
        {
            entries = newEntries;
        }

        public List<GrowthTableEntry> GetEntries()
        {
            return entries;
        }
    }

    public class SpawnTableData : ScriptableObject, ITableData<SpawnTableEntry>
    {
        public List<SpawnTableEntry> entries = new List<SpawnTableEntry>();

        public void SetEntries(List<SpawnTableEntry> newEntries)
        {
            entries = newEntries;
        }

        public List<SpawnTableEntry> GetEntries()
        {
            return entries;
        }
    }

    public class StageTableData : ScriptableObject, ITableData<StageTableEntry>
    {
        public List<StageTableEntry> entries = new List<StageTableEntry>();

        public void SetEntries(List<StageTableEntry> newEntries)
        {
            entries = newEntries;
        }

        public List<StageTableEntry> GetEntries()
        {
            return entries;
        }
    }
}