using System.IO;
using System.Text.Json;
using MhxyFiveTracker.Models;

namespace MhxyFiveTracker.Services;

public sealed class DataStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public string DataDirectory { get; }
    public string DataFilePath { get; }

    public DataStore()
    {
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        DataDirectory = Path.Combine(documents, "MHXYFiveAccountTracker");
        DataFilePath = Path.Combine(DataDirectory, "data.json");
    }

    public TrackerData Load()
    {
        Directory.CreateDirectory(DataDirectory);

        if (!File.Exists(DataFilePath))
        {
            var sample = CreateSampleData();
            Save(sample);
            return sample;
        }

        try
        {
            var json = File.ReadAllText(DataFilePath);
            return Normalize(JsonSerializer.Deserialize<TrackerData>(json, JsonOptions));
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            BackupCurrentDataFile("invalid");
            var empty = new TrackerData();
            Save(empty);
            return empty;
        }
    }

    public void Save(TrackerData data)
    {
        Directory.CreateDirectory(DataDirectory);
        var json = JsonSerializer.Serialize(data, JsonOptions);
        File.WriteAllText(DataFilePath, json);
    }

    public void Export(TrackerData data, string path)
    {
        var json = JsonSerializer.Serialize(data, JsonOptions);
        File.WriteAllText(path, json);
    }

    public TrackerData Import(string path)
    {
        var json = File.ReadAllText(path);
        return Normalize(JsonSerializer.Deserialize<TrackerData>(json, JsonOptions));
    }

    public string? BackupCurrentDataFile(string reason = "backup")
    {
        if (!File.Exists(DataFilePath))
        {
            return null;
        }

        Directory.CreateDirectory(DataDirectory);
        var backupPath = Path.Combine(
            DataDirectory,
            $"data.{DateTime.Now:yyyyMMdd-HHmmss-ffff}.{reason}.json");

        File.Copy(DataFilePath, backupPath, overwrite: false);
        return backupPath;
    }

    private static TrackerData CreateSampleData()
    {
        var today = DateTime.Today;

        return new TrackerData
        {
            Settings = new TrackerSettings
            {
                ServerName = "紫禁城畅玩服",
                StartDate = today.AddDays(-12),
                InitialInvestment = 2680m,
                PaybackTarget = 2680m
            },
            DailyRecords =
            [
                new DailyRecord
                {
                    Date = today,
                    OnlineHours = 5.5m,
                    CashIncome = 186m,
                    MaterialValue = 94m,
                    ServiceIncome = 60m,
                    Expense = 42m,
                    Note = "抓鬼 2 轮，剧情服务 1 单，宝石价格继续跌。"
                },
                new DailyRecord
                {
                    Date = today.AddDays(-1),
                    OnlineHours = 4.2m,
                    CashIncome = 128m,
                    MaterialValue = 80m,
                    ServiceIncome = 20m,
                    Expense = 30m,
                    Note = "周末活动收益高。"
                },
                new DailyRecord
                {
                    Date = today.AddDays(-2),
                    OnlineHours = 3.8m,
                    CashIncome = 92m,
                    MaterialValue = 50m,
                    ServiceIncome = 0m,
                    Expense = 96m,
                    Note = "补装备，收益偏低。"
                }
            ],
            PriceRecords =
            [
                new PriceRecord
                {
                    Date = today,
                    Gem = 4.8m,
                    BeastBook = 18m,
                    FiveTreasure = 72m,
                    Token = 118m,
                    Equipment = 6.5m,
                    Flower = 12m,
                    Instrument = 9m,
                    TemporaryCharm = 3.2m,
                    StoryService = 30m
                }
            ]
        };
    }

    private static TrackerData Normalize(TrackerData? data)
    {
        data ??= new TrackerData();
        data.Settings ??= new TrackerSettings();
        data.DailyRecords ??= [];
        data.PriceRecords ??= [];
        data.ServiceOrders ??= [];
        data.InventoryItems ??= [];
        return data;
    }
}
