using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using DinoGrow.Core.Data;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace DinoGrow.Infrastructure.Data
{
    public sealed class ExcelDataService : IDataService
    {
        private static readonly string[] PlayerHeaders =
        {
            "id",
            "표시이름",
            "레벨",
            "경험치",
            "이동속도",
            "크기",
            "프리팹",
            "최대목숨"
        };

        private static readonly string[] EnemyDinoHeaders =
        {
            "id",
            "표시이름",
            "레벨",
            "경험치",
            "이동속도",
            "크기",
            "AI유형",
            "색상유형",
            "프리팹"
        };

        private static readonly string[] ItemHeaders =
        {
            "id",
            "표시이름",
            "효과타입",
            "효과값",
            "프리팹"
        };

        private static readonly string[] StageHeaders =
        {
            "stageId",
            "표시이름",
            "스폰중심X",
            "스폰중심Z",
            "스폰범위X",
            "스폰범위Z",
            "스폰높이Y",
            "플레이어최소거리",
            "제한시간"
        };

        private static readonly string[] SpawnHeaders =
        {
            "stageId",
            "dinoId",
            "최소레벨",
            "최대레벨",
            "생성수",
            "처치경험치",
            "가중치",
            "최소배회속도",
            "최대배회속도"
        };

        private static readonly string[] PlayerGrowthHeaders =
        {
            "레벨",
            "필요경험치",
            "크기배율",
            "카메라거리",
            "카메라높이"
        };

        public IReadOnlyList<PlayerDataRecord> LoadPlayerRows(string xlsxPath)
        {
            using var workbook = OpenWorkbook(xlsxPath);
            var sheet = workbook.GetSheet("PlayerTable") ?? throw new InvalidDataException("PlayerTable sheet was not found.");
            var formatter = new DataFormatter(CultureInfo.InvariantCulture);
            var headerMap = ReadHeaderMap(sheet, formatter);
            var records = new List<PlayerDataRecord>();

            for (var rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                var row = sheet.GetRow(rowIndex);
                if (row == null || IsRowEmpty(row, formatter))
                {
                    continue;
                }

                var record = new PlayerDataRecord
                {
                    id = ReadString(row, headerMap, "id", formatter),
                    displayName = ReadString(row, headerMap, "displayName", formatter),
                    level = ReadInt(row, headerMap, "level", formatter, 1),
                    exp = ReadInt(row, headerMap, "exp", formatter, 0),
                    speed = ReadFloat(row, headerMap, "speed", formatter, 1f),
                    size = ReadFloat(row, headerMap, "size", formatter, 1f),
                    prefab = ReadString(row, headerMap, "prefab", formatter),
                    maxLives = ReadInt(row, headerMap, "maxLives", formatter, 3)
                };

                if (!string.IsNullOrWhiteSpace(record.id))
                {
                    records.Add(record);
                }
            }

            return records;
        }

        public IReadOnlyList<DinoDataRecord> LoadEnemyDinoRows(string xlsxPath)
        {
            using var workbook = OpenWorkbook(xlsxPath);
            var sheet = workbook.GetSheet("EnemyDinoTable") ?? throw new InvalidDataException("EnemyDinoTable sheet was not found.");
            var formatter = new DataFormatter(CultureInfo.InvariantCulture);
            var headerMap = ReadHeaderMap(sheet, formatter);
            var records = new List<DinoDataRecord>();

            for (var rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                var row = sheet.GetRow(rowIndex);
                if (row == null || IsRowEmpty(row, formatter))
                {
                    continue;
                }

                var record = new DinoDataRecord
                {
                    id = ReadString(row, headerMap, "id", formatter),
                    displayName = ReadString(row, headerMap, "displayName", formatter),
                    level = ReadInt(row, headerMap, "level", formatter, 1),
                    exp = ReadInt(row, headerMap, "exp", formatter, 10),
                    speed = ReadFloat(row, headerMap, "speed", formatter, 1f),
                    size = ReadFloat(row, headerMap, "size", formatter, 1f),
                    aiType = ReadString(row, headerMap, "aiType", formatter),
                    colorType = ReadString(row, headerMap, "colorType", formatter),
                    prefab = ReadString(row, headerMap, "prefab", formatter)
                };

                if (!string.IsNullOrWhiteSpace(record.id))
                {
                    records.Add(record);
                }
            }

            return records;
        }

        public IReadOnlyList<ItemDataRecord> LoadItemRows(string xlsxPath)
        {
            using var workbook = OpenWorkbook(xlsxPath);
            var sheet = workbook.GetSheet("ItemTable") ?? throw new InvalidDataException("ItemTable sheet was not found.");
            var formatter = new DataFormatter(CultureInfo.InvariantCulture);
            var headerMap = ReadHeaderMap(sheet, formatter);
            var records = new List<ItemDataRecord>();

            for (var rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                var row = sheet.GetRow(rowIndex);
                if (row == null || IsRowEmpty(row, formatter))
                {
                    continue;
                }

                var record = new ItemDataRecord
                {
                    id = ReadString(row, headerMap, "id", formatter),
                    displayName = ReadString(row, headerMap, "displayName", formatter),
                    effectType = ReadString(row, headerMap, "effectType", formatter),
                    effectValue = ReadInt(row, headerMap, "effectValue", formatter, 1),
                    prefab = ReadString(row, headerMap, "prefab", formatter)
                };

                if (!string.IsNullOrWhiteSpace(record.id))
                {
                    records.Add(record);
                }
            }

            return records;
        }

        public IReadOnlyList<StageDataRecord> LoadStageRows(string xlsxPath)
        {
            using var workbook = OpenWorkbook(xlsxPath);
            var sheet = workbook.GetSheet("StageTable") ?? throw new InvalidDataException("StageTable sheet was not found.");
            var formatter = new DataFormatter(CultureInfo.InvariantCulture);
            var headerMap = ReadHeaderMap(sheet, formatter);
            var records = new List<StageDataRecord>();

            for (var rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                var row = sheet.GetRow(rowIndex);
                if (row == null || IsRowEmpty(row, formatter))
                {
                    continue;
                }

                var record = new StageDataRecord
                {
                    stageId = ReadInt(row, headerMap, "stageId", formatter, 1),
                    displayName = ReadString(row, headerMap, "displayName", formatter),
                    spawnCenterX = ReadFloat(row, headerMap, "spawnCenterX", formatter, 0f),
                    spawnCenterZ = ReadFloat(row, headerMap, "spawnCenterZ", formatter, 0f),
                    spawnSizeX = ReadFloat(row, headerMap, "spawnSizeX", formatter, 80f),
                    spawnSizeZ = ReadFloat(row, headerMap, "spawnSizeZ", formatter, 80f),
                    spawnY = ReadFloat(row, headerMap, "spawnY", formatter, 0.75f),
                    minDistanceFromPlayer = ReadFloat(row, headerMap, "minDistanceFromPlayer", formatter, 8f),
                    timeLimit = ReadFloat(row, headerMap, "timeLimit", formatter, 0f)
                };

                if (record.stageId > 0)
                {
                    records.Add(record);
                }
            }

            return records;
        }

        public IReadOnlyList<SpawnDataRecord> LoadSpawnRows(string xlsxPath)
        {
            using var workbook = OpenWorkbook(xlsxPath);
            var sheet = workbook.GetSheet("SpawnTable") ?? throw new InvalidDataException("SpawnTable sheet was not found.");
            var formatter = new DataFormatter(CultureInfo.InvariantCulture);
            var headerMap = ReadHeaderMap(sheet, formatter);
            var records = new List<SpawnDataRecord>();

            for (var rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                var row = sheet.GetRow(rowIndex);
                if (row == null || IsRowEmpty(row, formatter))
                {
                    continue;
                }

                var record = new SpawnDataRecord
                {
                    stageId = ReadInt(row, headerMap, "stageId", formatter, 1),
                    dinoId = ReadString(row, headerMap, "dinoId", formatter),
                    minLevel = ReadInt(row, headerMap, "minLevel", formatter, 1),
                    maxLevel = ReadInt(row, headerMap, "maxLevel", formatter, 1),
                    count = ReadInt(row, headerMap, "count", formatter, 1),
                    weight = ReadInt(row, headerMap, "weight", formatter, 1),
                    minWanderSpeed = ReadFloat(row, headerMap, "minWanderSpeed", formatter, 2.4f),
                    maxWanderSpeed = ReadFloat(row, headerMap, "maxWanderSpeed", formatter, 4.2f)
                };
                record.defeatExp = ReadInt(row, headerMap, "defeatExp", formatter, Math.Max(1, record.maxLevel) * 10);

                if (record.stageId > 0 && !string.IsNullOrWhiteSpace(record.dinoId))
                {
                    records.Add(record);
                }
            }

            return records;
        }

        public IReadOnlyList<PlayerGrowthDataRecord> LoadPlayerGrowthRows(string xlsxPath)
        {
            using var workbook = OpenWorkbook(xlsxPath);
            var sheet = workbook.GetSheet("PlayerGrowthTable") ?? throw new InvalidDataException("PlayerGrowthTable sheet was not found.");
            var formatter = new DataFormatter(CultureInfo.InvariantCulture);
            var headerMap = ReadHeaderMap(sheet, formatter);
            var records = new List<PlayerGrowthDataRecord>();

            for (var rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                var row = sheet.GetRow(rowIndex);
                if (row == null || IsRowEmpty(row, formatter))
                {
                    continue;
                }

                var record = new PlayerGrowthDataRecord
                {
                    level = ReadInt(row, headerMap, "level", formatter, 1),
                    requiredExp = ReadInt(row, headerMap, "requiredExp", formatter, 100),
                    scaleMultiplier = ReadFloat(row, headerMap, "scaleMultiplier", formatter, 1f),
                    cameraDistance = ReadFloat(row, headerMap, "cameraDistance", formatter, 6f),
                    cameraHeight = ReadFloat(row, headerMap, "cameraHeight", formatter, 4f)
                };

                if (record.level > 0)
                {
                    records.Add(record);
                }
            }

            return records;
        }

        public void CreateDinoTemplate(string xlsxPath)
        {
            EnsureOutputDirectory(xlsxPath);

            var workbook = new XSSFWorkbook();
            CreatePlayerSheet(workbook);
            CreateEnemyDinoSheet(workbook);
            CreateItemSheet(workbook);
            WriteWorkbook(workbook, xlsxPath);
        }

        public void CreateGameDataTemplate(string xlsxPath)
        {
            EnsureOutputDirectory(xlsxPath);

            var workbook = new XSSFWorkbook();
            CreatePlayerSheet(workbook);
            CreateEnemyDinoSheet(workbook);
            CreateItemSheet(workbook);
            CreateStageSheet(workbook);
            CreateSpawnSheet(workbook);
            CreatePlayerGrowthSheet(workbook);
            WriteWorkbook(workbook, xlsxPath);
        }

        private static XSSFWorkbook OpenWorkbook(string xlsxPath)
        {
            if (string.IsNullOrWhiteSpace(xlsxPath))
            {
                throw new ArgumentException("Excel path is empty.", nameof(xlsxPath));
            }

            if (!File.Exists(xlsxPath))
            {
                throw new FileNotFoundException("Excel file not found.", xlsxPath);
            }

            using var stream = new FileStream(xlsxPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return new XSSFWorkbook(stream);
        }

        private static void EnsureOutputDirectory(string xlsxPath)
        {
            if (string.IsNullOrWhiteSpace(xlsxPath))
            {
                throw new ArgumentException("Excel path is empty.", nameof(xlsxPath));
            }

            var directory = Path.GetDirectoryName(xlsxPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private static void WriteWorkbook(IWorkbook workbook, string xlsxPath)
        {
            using var output = new FileStream(xlsxPath, FileMode.Create, FileAccess.Write);
            workbook.Write(output);
        }

        private static void CreatePlayerSheet(IWorkbook workbook)
        {
            var sheet = workbook.CreateSheet("PlayerTable");
            WriteHeaders(workbook, sheet, PlayerHeaders);
            WritePlayerSampleRow(sheet.CreateRow(1), "player", "플레이어", 1, 0, 5f, 1f, "Player", 3);
            AutoSizeColumns(sheet, PlayerHeaders.Length);
        }

        private static void CreateEnemyDinoSheet(IWorkbook workbook)
        {
            var sheet = workbook.CreateSheet("EnemyDinoTable");
            WriteHeaders(workbook, sheet, EnemyDinoHeaders);
            WriteEnemyDinoSampleRow(sheet.CreateRow(1), "dino_001", "초식공룡", 1, 10, 4.2f, 0.8f, "Wander", "Green", "EnemyDinoSmall");
            WriteEnemyDinoSampleRow(sheet.CreateRow(2), "dino_002", "중형공룡", 2, 20, 4.8f, 1.2f, "Wander", "Brown", "EnemyDinoMedium");
            WriteEnemyDinoSampleRow(sheet.CreateRow(3), "dino_003", "대형공룡", 3, 30, 3.6f, 1.8f, "Wander", "Red", "EnemyDinoLarge");
            AutoSizeColumns(sheet, EnemyDinoHeaders.Length);
        }

        private static void CreateItemSheet(IWorkbook workbook)
        {
            var sheet = workbook.CreateSheet("ItemTable");
            WriteHeaders(workbook, sheet, ItemHeaders);
            WriteItemSampleRow(sheet.CreateRow(1), "heart", "하트", "Heart", 1, "HeartPickup");
            AutoSizeColumns(sheet, ItemHeaders.Length);
        }

        private static void CreateStageSheet(IWorkbook workbook)
        {
            var sheet = workbook.CreateSheet("StageTable");
            WriteHeaders(workbook, sheet, StageHeaders);
            WriteStageSampleRow(sheet.CreateRow(1), 1, "초원", 0f, 0f, 80f, 80f, 0.75f, 8f, 0f);
            WriteStageSampleRow(sheet.CreateRow(2), 2, "넓은 초원", 0f, 0f, 100f, 100f, 0.75f, 10f, 0f);
            AutoSizeColumns(sheet, StageHeaders.Length);
        }

        private static void CreateSpawnSheet(IWorkbook workbook)
        {
            var sheet = workbook.CreateSheet("SpawnTable");
            WriteHeaders(workbook, sheet, SpawnHeaders);
            WriteSpawnSampleRow(sheet.CreateRow(1), 1, "dino_001", 1, 1, 10, 10, 60, 0f, 0f);
            WriteSpawnSampleRow(sheet.CreateRow(2), 1, "dino_002", 1, 2, 8, 20, 30, 2.4f, 3.4f);
            WriteSpawnSampleRow(sheet.CreateRow(3), 1, "dino_003", 2, 3, 6, 30, 10, 3.0f, 4.2f);
            WriteSpawnSampleRow(sheet.CreateRow(4), 2, "dino_002", 2, 4, 12, 40, 50, 2.6f, 3.8f);
            WriteSpawnSampleRow(sheet.CreateRow(5), 2, "dino_003", 3, 5, 10, 50, 50, 3.2f, 4.4f);
            AutoSizeColumns(sheet, SpawnHeaders.Length);
        }

        private static void CreatePlayerGrowthSheet(IWorkbook workbook)
        {
            var sheet = workbook.CreateSheet("PlayerGrowthTable");
            WriteHeaders(workbook, sheet, PlayerGrowthHeaders);

            for (var level = 1; level <= 20; level++)
            {
                var row = sheet.CreateRow(level);
                var scale = 1f + (level - 1) * 0.08f;
                var cameraDistance = GetCameraDistance(level);
                var cameraHeight = GetCameraHeight(level);
                WritePlayerGrowthSampleRow(row, level, 100, scale, cameraDistance, cameraHeight);
            }

            AutoSizeColumns(sheet, PlayerGrowthHeaders.Length);
        }

        private static float GetCameraDistance(int level)
        {
            if (level <= 4)
            {
                return 6f;
            }

            if (level <= 8)
            {
                return 7f;
            }

            if (level <= 12)
            {
                return 8f;
            }

            if (level <= 16)
            {
                return 9f;
            }

            return 10f;
        }

        private static float GetCameraHeight(int level)
        {
            if (level <= 4)
            {
                return 4f;
            }

            if (level <= 8)
            {
                return 4.5f;
            }

            if (level <= 12)
            {
                return 5f;
            }

            if (level <= 16)
            {
                return 6f;
            }

            return 7f;
        }

        private static void WriteHeaders(IWorkbook workbook, ISheet sheet, IReadOnlyList<string> headers)
        {
            var headerStyle = CreateHeaderStyle(workbook);
            var headerRow = sheet.CreateRow(0);

            for (var i = 0; i < headers.Count; i++)
            {
                var cell = headerRow.CreateCell(i);
                cell.SetCellValue(headers[i]);
                cell.CellStyle = headerStyle;
            }
        }

        private static void AutoSizeColumns(ISheet sheet, int columnCount)
        {
            for (var i = 0; i < columnCount; i++)
            {
                sheet.AutoSizeColumn(i);
            }
        }

        private static Dictionary<string, int> ReadHeaderMap(ISheet sheet, DataFormatter formatter)
        {
            var headerRow = sheet.GetRow(0) ?? throw new InvalidDataException("Table needs a header row.");
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < headerRow.LastCellNum; i++)
            {
                var header = formatter.FormatCellValue(headerRow.GetCell(i)).Trim();
                if (string.IsNullOrEmpty(header) || map.ContainsKey(header))
                {
                    continue;
                }

                map.Add(header, i);
                foreach (var alias in GetHeaderAliases(header))
                {
                    if (!map.ContainsKey(alias))
                    {
                        map.Add(alias, i);
                    }
                }
            }

            return map;
        }

        private static IReadOnlyList<string> GetHeaderAliases(string header)
        {
            return header switch
            {
                "표시이름" => new[] { "displayName" },
                "레벨" => new[] { "level" },
                "경험치" => new[] { "exp" },
                "이동속도" => new[] { "speed" },
                "크기" => new[] { "size" },
                "AI유형" => new[] { "aiType" },
                "색상유형" => new[] { "colorType" },
                "프리팹" => new[] { "prefab" },
                "최대목숨" => new[] { "maxLives" },
                "효과타입" => new[] { "effectType" },
                "효과값" => new[] { "effectValue" },
                "스폰중심X" => new[] { "spawnCenterX" },
                "스폰중심Z" => new[] { "spawnCenterZ" },
                "스폰범위X" => new[] { "spawnSizeX" },
                "스폰범위Z" => new[] { "spawnSizeZ" },
                "스폰높이Y" => new[] { "spawnY" },
                "플레이어최소거리" => new[] { "minDistanceFromPlayer" },
                "제한시간" => new[] { "timeLimit" },
                "최소레벨" => new[] { "minLevel" },
                "최대레벨" => new[] { "maxLevel" },
                "생성수" => new[] { "count" },
                "처치경험치" => new[] { "defeatExp" },
                "가중치" => new[] { "weight" },
                "최소배회속도" => new[] { "minWanderSpeed" },
                "최대배회속도" => new[] { "maxWanderSpeed" },
                "필요경험치" => new[] { "requiredExp" },
                "크기배율" => new[] { "scaleMultiplier" },
                "카메라거리" => new[] { "cameraDistance" },
                "카메라높이" => new[] { "cameraHeight" },
                _ => Array.Empty<string>()
            };
        }

        private static bool IsRowEmpty(IRow row, DataFormatter formatter)
        {
            for (var i = row.FirstCellNum; i < row.LastCellNum; i++)
            {
                if (!string.IsNullOrWhiteSpace(formatter.FormatCellValue(row.GetCell(i))))
                {
                    return false;
                }
            }

            return true;
        }

        private static string ReadString(IRow row, IReadOnlyDictionary<string, int> headerMap, string header, DataFormatter formatter)
        {
            return headerMap.TryGetValue(header, out var index)
                ? formatter.FormatCellValue(row.GetCell(index)).Trim()
                : "";
        }

        private static int ReadInt(IRow row, IReadOnlyDictionary<string, int> headerMap, string header, DataFormatter formatter, int fallback)
        {
            var value = ReadString(row, headerMap, header, formatter);
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : fallback;
        }

        private static float ReadFloat(IRow row, IReadOnlyDictionary<string, int> headerMap, string header, DataFormatter formatter, float fallback)
        {
            var value = ReadString(row, headerMap, header, formatter);
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : fallback;
        }

        private static ICellStyle CreateHeaderStyle(IWorkbook workbook)
        {
            var style = workbook.CreateCellStyle();
            var font = workbook.CreateFont();
            font.IsBold = true;
            style.SetFont(font);
            return style;
        }

        private static void WritePlayerSampleRow(IRow row, string id, string displayName, int level, int exp, float speed, float size, string prefab, int maxLives)
        {
            row.CreateCell(0).SetCellValue(id);
            row.CreateCell(1).SetCellValue(displayName);
            row.CreateCell(2).SetCellValue(level);
            row.CreateCell(3).SetCellValue(exp);
            row.CreateCell(4).SetCellValue(speed);
            row.CreateCell(5).SetCellValue(size);
            row.CreateCell(6).SetCellValue(prefab);
            row.CreateCell(7).SetCellValue(maxLives);
        }

        private static void WriteEnemyDinoSampleRow(IRow row, string id, string displayName, int level, int exp, float speed, float size, string aiType, string colorType, string prefab)
        {
            row.CreateCell(0).SetCellValue(id);
            row.CreateCell(1).SetCellValue(displayName);
            row.CreateCell(2).SetCellValue(level);
            row.CreateCell(3).SetCellValue(exp);
            row.CreateCell(4).SetCellValue(speed);
            row.CreateCell(5).SetCellValue(size);
            row.CreateCell(6).SetCellValue(aiType);
            row.CreateCell(7).SetCellValue(colorType);
            row.CreateCell(8).SetCellValue(prefab);
        }

        private static void WriteItemSampleRow(IRow row, string id, string displayName, string effectType, int effectValue, string prefab)
        {
            row.CreateCell(0).SetCellValue(id);
            row.CreateCell(1).SetCellValue(displayName);
            row.CreateCell(2).SetCellValue(effectType);
            row.CreateCell(3).SetCellValue(effectValue);
            row.CreateCell(4).SetCellValue(prefab);
        }

        private static void WriteStageSampleRow(IRow row, int stageId, string displayName, float spawnCenterX, float spawnCenterZ, float spawnSizeX, float spawnSizeZ, float spawnY, float minDistanceFromPlayer, float timeLimit)
        {
            row.CreateCell(0).SetCellValue(stageId);
            row.CreateCell(1).SetCellValue(displayName);
            row.CreateCell(2).SetCellValue(spawnCenterX);
            row.CreateCell(3).SetCellValue(spawnCenterZ);
            row.CreateCell(4).SetCellValue(spawnSizeX);
            row.CreateCell(5).SetCellValue(spawnSizeZ);
            row.CreateCell(6).SetCellValue(spawnY);
            row.CreateCell(7).SetCellValue(minDistanceFromPlayer);
            row.CreateCell(8).SetCellValue(timeLimit);
        }

        private static void WriteSpawnSampleRow(IRow row, int stageId, string dinoId, int minLevel, int maxLevel, int count, int defeatExp, int weight, float minWanderSpeed, float maxWanderSpeed)
        {
            row.CreateCell(0).SetCellValue(stageId);
            row.CreateCell(1).SetCellValue(dinoId);
            row.CreateCell(2).SetCellValue(minLevel);
            row.CreateCell(3).SetCellValue(maxLevel);
            row.CreateCell(4).SetCellValue(count);
            row.CreateCell(5).SetCellValue(defeatExp);
            row.CreateCell(6).SetCellValue(weight);
            row.CreateCell(7).SetCellValue(minWanderSpeed);
            row.CreateCell(8).SetCellValue(maxWanderSpeed);
        }

        private static void WritePlayerGrowthSampleRow(IRow row, int level, int requiredExp, float scaleMultiplier, float cameraDistance, float cameraHeight)
        {
            row.CreateCell(0).SetCellValue(level);
            row.CreateCell(1).SetCellValue(requiredExp);
            row.CreateCell(2).SetCellValue(scaleMultiplier);
            row.CreateCell(3).SetCellValue(cameraDistance);
            row.CreateCell(4).SetCellValue(cameraHeight);
        }
    }
}
