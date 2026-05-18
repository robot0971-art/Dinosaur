using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

public sealed class ExcelConverterWindow : EditorWindow
{
    private const string DefaultOutputFolder = "Assets/GameData/Generated";

    private string inputPath = "";
    private string outputFolder = DefaultOutputFolder;
    private string outputName = "";
    private bool exportJson = true;
    private bool exportCsv = true;
    private bool overwriteExisting = true;
    private int selectedSheetIndex;
    private List<SheetInfo> sheets = new List<SheetInfo>();
    private Vector2 scroll;

    [MenuItem("Tools/Dino Game/Excel Converter")]
    public static void Open()
    {
        var window = GetWindow<ExcelConverterWindow>("Excel Converter");
        window.minSize = new Vector2(460f, 420f);
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.LabelField("Excel Converter", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Use the first row as field names. Empty rows are skipped. Supports .xlsx, .csv, and .tsv.", MessageType.Info);

        EditorGUILayout.Space(6f);
        DrawInputPicker();
        DrawSheetPicker();

        EditorGUILayout.Space(6f);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
        outputName = EditorGUILayout.TextField("Output Name", outputName);
        exportJson = EditorGUILayout.ToggleLeft("Export JSON", exportJson);
        exportCsv = EditorGUILayout.ToggleLeft("Export CSV", exportCsv);
        overwriteExisting = EditorGUILayout.ToggleLeft("Overwrite existing files", overwriteExisting);

        EditorGUILayout.Space(10f);
        using (new EditorGUI.DisabledScope(!CanConvert()))
        {
            if (GUILayout.Button("Convert", GUILayout.Height(34f)))
            {
                Convert();
            }
        }

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("Expected Table Shape", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Row 1: id, type, speed, spawnRate, prefab");
        EditorGUILayout.LabelField("Row 2+: cactus_small, obstacle, 8.5, 1.2, CactusSmall");

        EditorGUILayout.EndScrollView();
    }

    private void DrawInputPicker()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.TextField("Input File", inputPath);

            if (GUILayout.Button("Browse", GUILayout.Width(82f)))
            {
                var picked = EditorUtility.OpenFilePanel("Select Excel or CSV file", Application.dataPath, "xlsx,csv,tsv");
                if (!string.IsNullOrEmpty(picked))
                {
                    inputPath = picked;
                    outputName = Path.GetFileNameWithoutExtension(inputPath);
                    RefreshSheets();
                }
            }
        }
    }

    private void DrawSheetPicker()
    {
        if (!IsXlsx(inputPath))
        {
            return;
        }

        if (sheets.Count == 0)
        {
            EditorGUILayout.HelpBox("No sheets found in this workbook.", MessageType.Warning);
            return;
        }

        selectedSheetIndex = Mathf.Clamp(selectedSheetIndex, 0, sheets.Count - 1);
        selectedSheetIndex = EditorGUILayout.Popup("Sheet", selectedSheetIndex, sheets.Select(sheet => sheet.Name).ToArray());
    }

    private bool CanConvert()
    {
        return File.Exists(inputPath) &&
               !string.IsNullOrWhiteSpace(outputFolder) &&
               !string.IsNullOrWhiteSpace(outputName) &&
               (exportJson || exportCsv);
    }

    private void RefreshSheets()
    {
        selectedSheetIndex = 0;
        sheets.Clear();

        if (!IsXlsx(inputPath) || !File.Exists(inputPath))
        {
            return;
        }

        try
        {
            sheets = XlsxReader.ListSheets(inputPath);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to read workbook sheets: {ex.Message}");
        }
    }

    private void Convert()
    {
        try
        {
            var table = LoadTable();
            if (table.Headers.Count == 0)
            {
                EditorUtility.DisplayDialog("Excel Converter", "No headers were found in the first row.", "OK");
                return;
            }

            var absoluteOutputFolder = ToAbsoluteProjectPath(outputFolder);
            Directory.CreateDirectory(absoluteOutputFolder);

            var convertedFiles = new List<string>();
            if (exportJson)
            {
                var jsonPath = Path.Combine(absoluteOutputFolder, $"{outputName}.json");
                WriteFile(jsonPath, JsonTableWriter.Write(table));
                convertedFiles.Add(ToAssetPath(jsonPath));
            }

            if (exportCsv)
            {
                var csvPath = Path.Combine(absoluteOutputFolder, $"{outputName}.csv");
                WriteFile(csvPath, CsvTableWriter.Write(table));
                convertedFiles.Add(ToAssetPath(csvPath));
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Excel Converter", $"Converted {table.Rows.Count} rows.\n\n{string.Join("\n", convertedFiles)}", "OK");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("Excel Converter", ex.Message, "OK");
        }
    }

    private TableData LoadTable()
    {
        if (IsXlsx(inputPath))
        {
            if (sheets.Count == 0)
            {
                RefreshSheets();
            }

            if (sheets.Count == 0)
            {
                throw new InvalidOperationException("No sheets found in workbook.");
            }

            return XlsxReader.ReadSheet(inputPath, sheets[Mathf.Clamp(selectedSheetIndex, 0, sheets.Count - 1)]);
        }

        var delimiter = Path.GetExtension(inputPath).Equals(".tsv", StringComparison.OrdinalIgnoreCase) ? '\t' : ',';
        return DelimitedTextReader.Read(inputPath, delimiter);
    }

    private void WriteFile(string path, string contents)
    {
        if (!overwriteExisting && File.Exists(path))
        {
            throw new IOException($"Output already exists: {path}");
        }

        File.WriteAllText(path, contents, new UTF8Encoding(false));
    }

    private static bool IsXlsx(string path)
    {
        return Path.GetExtension(path).Equals(".xlsx", StringComparison.OrdinalIgnoreCase);
    }

    private static string ToAbsoluteProjectPath(string assetOrAbsolutePath)
    {
        if (Path.IsPathRooted(assetOrAbsolutePath))
        {
            return assetOrAbsolutePath;
        }

        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (projectRoot == null)
        {
            throw new InvalidOperationException("Unable to resolve project root.");
        }

        return Path.GetFullPath(Path.Combine(projectRoot, assetOrAbsolutePath));
    }

    private static string ToAssetPath(string absolutePath)
    {
        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (projectRoot == null)
        {
            return absolutePath;
        }

        var normalizedRoot = projectRoot.Replace('\\', '/').TrimEnd('/');
        var normalizedPath = absolutePath.Replace('\\', '/');
        return normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase)
            ? normalizedPath.Substring(normalizedRoot.Length + 1)
            : normalizedPath;
    }

    private sealed class SheetInfo
    {
        public string Name;
        public string Path;
        public string RelationshipId;
    }

    private sealed class TableData
    {
        public readonly List<string> Headers = new List<string>();
        public readonly List<Dictionary<string, string>> Rows = new List<Dictionary<string, string>>();
    }

    private static class XlsxReader
    {
        private static readonly XNamespace Spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private static readonly XNamespace Relationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private static readonly XNamespace PackageRelationships = "http://schemas.openxmlformats.org/package/2006/relationships";

        public static List<SheetInfo> ListSheets(string xlsxPath)
        {
            using (var archive = ZipFile.OpenRead(xlsxPath))
            {
                var workbook = LoadXml(archive, "xl/workbook.xml");
                var rels = LoadXml(archive, "xl/_rels/workbook.xml.rels");
                var relationshipTargets = rels.Root
                    .Elements(PackageRelationships + "Relationship")
                    .ToDictionary(
                        rel => (string)rel.Attribute("Id"),
                        rel => NormalizeSheetPath((string)rel.Attribute("Target")));

                return workbook.Root
                    .Element(Spreadsheet + "sheets")
                    .Elements(Spreadsheet + "sheet")
                    .Select(sheet =>
                    {
                        var relationshipId = (string)sheet.Attribute(Relationships + "id");
                        relationshipTargets.TryGetValue(relationshipId, out var path);
                        return new SheetInfo
                        {
                            Name = (string)sheet.Attribute("name") ?? "Sheet",
                            RelationshipId = relationshipId,
                            Path = path
                        };
                    })
                    .Where(sheet => !string.IsNullOrEmpty(sheet.Path))
                    .ToList();
            }
        }

        public static TableData ReadSheet(string xlsxPath, SheetInfo sheet)
        {
            using (var archive = ZipFile.OpenRead(xlsxPath))
            {
                var sharedStrings = ReadSharedStrings(archive);
                var worksheet = LoadXml(archive, sheet.Path);
                var rows = worksheet.Root
                    .Descendants(Spreadsheet + "sheetData")
                    .Elements(Spreadsheet + "row")
                    .Select(row => ReadRow(row, sharedStrings))
                    .Where(row => row.Any(cell => !string.IsNullOrWhiteSpace(cell.Value)))
                    .OrderBy(row => row.Min(cell => cell.ColumnIndex))
                    .ToList();

                return BuildTable(rows);
            }
        }

        private static List<string> ReadSharedStrings(ZipArchive archive)
        {
            var entry = archive.GetEntry("xl/sharedStrings.xml");
            if (entry == null)
            {
                return new List<string>();
            }

            var document = LoadXml(entry);
            return document.Root
                .Elements(Spreadsheet + "si")
                .Select(item => string.Concat(item.Descendants(Spreadsheet + "t").Select(text => text.Value)))
                .ToList();
        }

        private static List<CellValue> ReadRow(XElement row, IReadOnlyList<string> sharedStrings)
        {
            return row.Elements(Spreadsheet + "c")
                .Select(cell => ReadCell(cell, sharedStrings))
                .OrderBy(cell => cell.ColumnIndex)
                .ToList();
        }

        private static CellValue ReadCell(XElement cell, IReadOnlyList<string> sharedStrings)
        {
            var reference = (string)cell.Attribute("r") ?? "";
            var type = (string)cell.Attribute("t") ?? "";
            var rawValue = cell.Element(Spreadsheet + "v")?.Value ?? "";
            var value = rawValue;

            if (type == "s" && int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sharedIndex))
            {
                value = sharedIndex >= 0 && sharedIndex < sharedStrings.Count ? sharedStrings[sharedIndex] : "";
            }
            else if (type == "inlineStr")
            {
                value = string.Concat(cell.Descendants(Spreadsheet + "t").Select(text => text.Value));
            }
            else if (type == "b")
            {
                value = rawValue == "1" ? "true" : "false";
            }

            return new CellValue
            {
                ColumnIndex = ColumnNameToIndex(Regex.Match(reference, "^[A-Z]+", RegexOptions.IgnoreCase).Value),
                Value = value
            };
        }

        private static TableData BuildTable(List<List<CellValue>> rows)
        {
            var table = new TableData();
            if (rows.Count == 0)
            {
                return table;
            }

            var headerCells = rows[0];
            var maxColumn = headerCells.Count == 0 ? 0 : headerCells.Max(cell => cell.ColumnIndex);
            for (var column = 1; column <= maxColumn; column++)
            {
                var header = headerCells.FirstOrDefault(cell => cell.ColumnIndex == column)?.Value?.Trim();
                if (string.IsNullOrWhiteSpace(header))
                {
                    header = $"column_{column}";
                }

                table.Headers.Add(MakeUniqueHeader(header, table.Headers));
            }

            foreach (var row in rows.Skip(1))
            {
                var rowData = new Dictionary<string, string>();
                for (var i = 0; i < table.Headers.Count; i++)
                {
                    var column = i + 1;
                    rowData[table.Headers[i]] = row.FirstOrDefault(cell => cell.ColumnIndex == column)?.Value ?? "";
                }

                if (rowData.Values.Any(value => !string.IsNullOrWhiteSpace(value)))
                {
                    table.Rows.Add(rowData);
                }
            }

            return table;
        }

        private static string NormalizeSheetPath(string target)
        {
            if (string.IsNullOrWhiteSpace(target))
            {
                return "";
            }

            var normalized = target.Replace('\\', '/').TrimStart('/');
            return normalized.StartsWith("xl/", StringComparison.OrdinalIgnoreCase)
                ? normalized
                : $"xl/{normalized}";
        }

        private static XDocument LoadXml(ZipArchive archive, string entryPath)
        {
            var entry = archive.GetEntry(entryPath);
            if (entry == null)
            {
                throw new FileNotFoundException($"Missing workbook entry: {entryPath}");
            }

            return LoadXml(entry);
        }

        private static XDocument LoadXml(ZipArchiveEntry entry)
        {
            using (var stream = entry.Open())
            {
                return XDocument.Load(stream);
            }
        }
    }

    private static class DelimitedTextReader
    {
        public static TableData Read(string path, char delimiter)
        {
            var parsedRows = File.ReadAllLines(path, Encoding.UTF8)
                .Select(line => ParseLine(line, delimiter))
                .Where(row => row.Any(value => !string.IsNullOrWhiteSpace(value)))
                .ToList();

            var table = new TableData();
            if (parsedRows.Count == 0)
            {
                return table;
            }

            foreach (var header in parsedRows[0])
            {
                table.Headers.Add(MakeUniqueHeader(string.IsNullOrWhiteSpace(header) ? "column" : header.Trim(), table.Headers));
            }

            foreach (var row in parsedRows.Skip(1))
            {
                var rowData = new Dictionary<string, string>();
                for (var i = 0; i < table.Headers.Count; i++)
                {
                    rowData[table.Headers[i]] = i < row.Count ? row[i] : "";
                }

                table.Rows.Add(rowData);
            }

            return table;
        }

        private static List<string> ParseLine(string line, char delimiter)
        {
            var values = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var character = line[i];
                if (character == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (character == delimiter && !inQuotes)
                {
                    values.Add(current.ToString());
                    current.Length = 0;
                }
                else
                {
                    current.Append(character);
                }
            }

            values.Add(current.ToString());
            return values;
        }
    }

    private static class JsonTableWriter
    {
        public static string Write(TableData table)
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"rows\": [");

            for (var rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
            {
                var row = table.Rows[rowIndex];
                builder.AppendLine("    {");

                for (var headerIndex = 0; headerIndex < table.Headers.Count; headerIndex++)
                {
                    var header = table.Headers[headerIndex];
                    var comma = headerIndex < table.Headers.Count - 1 ? "," : "";
                    builder.Append("      ");
                    builder.Append(JsonEscape(header));
                    builder.Append(": ");
                    builder.Append(WriteJsonValue(row.TryGetValue(header, out var value) ? value : ""));
                    builder.AppendLine(comma);
                }

                builder.Append("    }");
                builder.AppendLine(rowIndex < table.Rows.Count - 1 ? "," : "");
            }

            builder.AppendLine("  ]");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static string WriteJsonValue(string value)
        {
            if (bool.TryParse(value, out var boolValue))
            {
                return boolValue ? "true" : "false";
            }

            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var numberValue))
            {
                return numberValue.ToString("R", CultureInfo.InvariantCulture);
            }

            return JsonEscape(value);
        }

        private static string JsonEscape(string value)
        {
            var builder = new StringBuilder("\"");
            foreach (var character in value ?? "")
            {
                switch (character)
                {
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        builder.Append(character);
                        break;
                }
            }

            builder.Append('"');
            return builder.ToString();
        }
    }

    private static class CsvTableWriter
    {
        public static string Write(TableData table)
        {
            var builder = new StringBuilder();
            builder.AppendLine(string.Join(",", table.Headers.Select(Escape)));

            foreach (var row in table.Rows)
            {
                builder.AppendLine(string.Join(",", table.Headers.Select(header => Escape(row.TryGetValue(header, out var value) ? value : ""))));
            }

            return builder.ToString();
        }

        private static string Escape(string value)
        {
            value = value ?? "";
            return value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0
                ? $"\"{value.Replace("\"", "\"\"")}\""
                : value;
        }
    }

    private sealed class CellValue
    {
        public int ColumnIndex;
        public string Value;
    }

    private static int ColumnNameToIndex(string columnName)
    {
        var index = 0;
        foreach (var character in columnName.ToUpperInvariant())
        {
            if (character < 'A' || character > 'Z')
            {
                continue;
            }

            index = index * 26 + character - 'A' + 1;
        }

        return Mathf.Max(1, index);
    }

    private static string MakeUniqueHeader(string header, ICollection<string> existingHeaders)
    {
        var sanitized = Regex.Replace(header.Trim(), @"\s+", "_");
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = "column";
        }

        var candidate = sanitized;
        var suffix = 2;
        while (existingHeaders.Contains(candidate))
        {
            candidate = $"{sanitized}_{suffix}";
            suffix++;
        }

        return candidate;
    }
}
