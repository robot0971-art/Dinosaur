using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Dino.Infrastructure.Data
{
    public class ExcelDataService : IDataService, IExcelConverter
    {
        private readonly Dictionary<string, object> _dataCache = new Dictionary<string, object>();
        private readonly string _dataPath;

        public ExcelDataService()
        {
            _dataPath = Path.Combine(Application.dataPath, "GameData");
            if (!Directory.Exists(_dataPath))
            {
                Directory.CreateDirectory(_dataPath);
            }
        }

        public T LoadData<T>(string key) where T : class
        {
            if (_dataCache.TryGetValue(key, out var cached))
            {
                return cached as T;
            }

            var filePath = Path.Combine(_dataPath, $"{key}.json");
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"Data file not found: {filePath}");
                return null;
            }

            var json = File.ReadAllText(filePath);
            var data = JsonUtility.FromJson<T>(json);
            _dataCache[key] = data;
            return data;
        }

        public void SaveData<T>(string key, T data) where T : class
        {
            _dataCache[key] = data;
            var filePath = Path.Combine(_dataPath, $"{key}.json");
            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText(filePath, json);
        }

        public bool HasData(string key)
        {
            if (_dataCache.ContainsKey(key))
                return true;

            var filePath = Path.Combine(_dataPath, $"{key}.json");
            return File.Exists(filePath);
        }

        public void ClearData(string key)
        {
            _dataCache.Remove(key);
            var filePath = Path.Combine(_dataPath, $"{key}.json");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        public void ClearAll()
        {
            _dataCache.Clear();
        }

        public List<T> ReadExcel<T>(string filePath, string sheetName = null) where T : class, new()
        {
            var result = new List<T>();

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    var workbook = new NPOI.XSSF.UserModel.XSSFWorkbook(stream);
                    var sheet = sheetName != null 
                        ? workbook.GetSheet(sheetName) 
                        : workbook.GetSheetAt(0);

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
            }
            catch (Exception e)
            {
                Debug.LogError($"Error reading Excel file: {e.Message}");
            }

            return result;
        }

        public void WriteExcel<T>(string filePath, string sheetName, List<T> data) where T : class
        {
            try
            {
                var workbook = new NPOI.XSSF.UserModel.XSSFWorkbook();
                var sheet = workbook.CreateSheet(sheetName);

                var properties = typeof(T).GetProperties();

                var headerRow = sheet.CreateRow(0);
                for (int i = 0; i < properties.Length; i++)
                {
                    headerRow.CreateCell(i).SetCellValue(properties[i].Name);
                }

                for (int row = 0; row < data.Count; row++)
                {
                    var dataRow = sheet.CreateRow(row + 1);
                    for (int col = 0; col < properties.Length; col++)
                    {
                        var value = properties[col].GetValue(data[row]);
                        SetCellValue(dataRow.CreateCell(col), value);
                    }
                }

                using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    workbook.Write(stream);
                }

                Debug.Log($"Excel file created: {filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error writing Excel file: {e.Message}");
            }
        }

        public void CreateTemplate<T>(string filePath, string sheetName) where T : class
        {
            try
            {
                var workbook = new NPOI.XSSF.UserModel.XSSFWorkbook();
                var sheet = workbook.CreateSheet(sheetName);

                var properties = typeof(T).GetProperties();
                var headerRow = sheet.CreateRow(0);

                for (int i = 0; i < properties.Length; i++)
                {
                    var cell = headerRow.CreateCell(i);
                    cell.SetCellValue(properties[i].Name);

                    var cellStyle = workbook.CreateCellStyle();
                    var font = workbook.CreateFont();
                    font.IsBold = true;
                    cellStyle.SetFont(font);
                    cell.CellStyle = cellStyle;
                }

                var exampleRow = sheet.CreateRow(1);
                for (int i = 0; i < properties.Length; i++)
                {
                    var propType = properties[i].PropertyType;
                    var cell = exampleRow.CreateCell(i);

                    if (propType == typeof(int))
                        cell.SetCellValue(0);
                    else if (propType == typeof(float))
                        cell.SetCellValue(0.0);
                    else if (propType == typeof(bool))
                        cell.SetCellValue(false);
                    else
                        cell.SetCellValue($"Sample {properties[i].Name}");
                }

                var dir = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    workbook.Write(stream);
                }

                Debug.Log($"Template created: {filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error creating template: {e.Message}");
            }
        }

        private object GetCellValue(NPOI.SS.UserModel.ICell cell, Type targetType)
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

        private void SetCellValue(NPOI.SS.UserModel.ICell cell, object value)
        {
            if (value == null)
            {
                cell.SetCellValue("");
                return;
            }

            if (value is int intVal)
                cell.SetCellValue(intVal);
            else if (value is float floatVal)
                cell.SetCellValue(floatVal);
            else if (value is double doubleVal)
                cell.SetCellValue(doubleVal);
            else if (value is bool boolVal)
                cell.SetCellValue(boolVal);
            else
                cell.SetCellValue(value.ToString());
        }
    }
}