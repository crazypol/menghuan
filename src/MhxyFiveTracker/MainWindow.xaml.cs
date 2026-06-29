using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using MhxyFiveTracker.Models;
using MhxyFiveTracker.Services;

namespace MhxyFiveTracker;

public partial class MainWindow : Window
{
    private readonly DataStore _dataStore = new();
    private TrackerData _data = new();

    public MainWindow()
    {
        InitializeComponent();
        _data = _dataStore.Load();
        LoadLatestRecordIntoForm();
        RefreshUi();
    }

    private void SaveDailyRecord_Click(object sender, RoutedEventArgs e)
    {
        var record = BuildRecordFromForm();
        var existing = _data.DailyRecords.FirstOrDefault(item => item.Date.Date == record.Date.Date);

        if (existing is null)
        {
            _data.DailyRecords.Add(record);
        }
        else
        {
            existing.OnlineHours = record.OnlineHours;
            existing.CashIncome = record.CashIncome;
            existing.MaterialValue = record.MaterialValue;
            existing.ServiceIncome = record.ServiceIncome;
            existing.Expense = record.Expense;
            existing.Note = record.Note;
        }

        _dataStore.Save(_data);
        RefreshUi();
    }

    private void DeleteRecord_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not Guid id)
        {
            return;
        }

        var record = _data.DailyRecords.FirstOrDefault(item => item.Id == id);
        if (record is null)
        {
            return;
        }

        _data.DailyRecords.Remove(record);
        _dataStore.Save(_data);
        RefreshUi();
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "导出收益记录",
            Filter = "JSON 文件 (*.json)|*.json",
            FileName = $"mhxy-five-tracker-{DateTime.Today:yyyyMMdd}.json"
        };

        if (dialog.ShowDialog(this) == true)
        {
            _dataStore.Export(_data, dialog.FileName);
            MessageBox.Show(this, "导出完成。", "梦幻五开收益记录器", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "导入收益记录",
            Filter = "JSON 文件 (*.json)|*.json"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var imported = _dataStore.Import(dialog.FileName);
        _data = imported;
        _dataStore.Save(_data);
        RefreshUi();
        MessageBox.Show(this, "导入完成。", "梦幻五开收益记录器", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Input_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        var preview = BuildRecordFromForm();
        TodayNetText.Text = Currency(preview.NetProfit);
        TodayHourlyText.Text = Currency(preview.HourlyProfit);
    }

    private DailyRecord BuildRecordFromForm()
    {
        return new DailyRecord
        {
            Date = DateInput.SelectedDate ?? DateTime.Today,
            OnlineHours = ReadDecimal(OnlineHoursInput.Text),
            CashIncome = ReadDecimal(CashIncomeInput.Text),
            MaterialValue = ReadDecimal(MaterialValueInput.Text),
            ServiceIncome = ReadDecimal(ServiceIncomeInput.Text),
            Expense = ReadDecimal(ExpenseInput.Text),
            Note = NoteInput.Text.Trim()
        };
    }

    private void LoadLatestRecordIntoForm()
    {
        var latest = _data.DailyRecords.OrderByDescending(item => item.Date).FirstOrDefault();
        if (latest is null)
        {
            DateInput.SelectedDate = DateTime.Today;
            return;
        }

        DateInput.SelectedDate = latest.Date;
        OnlineHoursInput.Text = latest.OnlineHours.ToString("0.##", CultureInfo.InvariantCulture);
        CashIncomeInput.Text = latest.CashIncome.ToString("0.##", CultureInfo.InvariantCulture);
        MaterialValueInput.Text = latest.MaterialValue.ToString("0.##", CultureInfo.InvariantCulture);
        ServiceIncomeInput.Text = latest.ServiceIncome.ToString("0.##", CultureInfo.InvariantCulture);
        ExpenseInput.Text = latest.Expense.ToString("0.##", CultureInfo.InvariantCulture);
        NoteInput.Text = latest.Note;
    }

    private void RefreshUi()
    {
        var orderedRecords = _data.DailyRecords.OrderByDescending(item => item.Date).ToList();
        var latest = orderedRecords.FirstOrDefault();
        var totalNet = _data.DailyRecords.Sum(item => item.NetProfit) - _data.Settings.InitialInvestment;
        var paybackProgress = _data.Settings.PaybackTarget <= 0
            ? 0
            : Math.Clamp(_data.DailyRecords.Sum(item => item.NetProfit) / _data.Settings.PaybackTarget * 100m, 0m, 999m);
        var todayPreview = BuildRecordFromForm();

        TodayNetText.Text = Currency(todayPreview.NetProfit);
        TodayHourlyText.Text = Currency(todayPreview.HourlyProfit);
        TotalNetText.Text = Currency(totalNet);
        PaybackText.Text = $"{paybackProgress:0.#}%";
        ServerSummaryText.Text = $"{_data.Settings.ServerName} · 第 {Math.Max(1, (DateTime.Today - _data.Settings.StartDate.Date).Days + 1)} 天";
        DataPathText.Text = $"数据文件：{_dataStore.DataFilePath}";

        RecordsGrid.ItemsSource = orderedRecords.Select(item => new DailyRecordRow(
            item.Id,
            item.Date.ToString("MM-dd", CultureInfo.InvariantCulture),
            Currency(item.TotalIncome),
            Currency(item.Expense),
            Currency(item.NetProfit),
            Currency(item.HourlyProfit))).ToList();

        RefreshPriceCards(latest?.Date ?? DateTime.Today);
    }

    private void RefreshPriceCards(DateTime date)
    {
        var latestPrice = _data.PriceRecords
            .OrderByDescending(item => item.Date)
            .FirstOrDefault(item => item.Date.Date <= date.Date)
            ?? _data.PriceRecords.OrderByDescending(item => item.Date).FirstOrDefault();

        if (latestPrice is null)
        {
            GemText.Text = BeastBookText.Text = FiveTreasureText.Text = TokenText.Text = EquipmentText.Text = CharmText.Text = FlowerText.Text = InstrumentText.Text = StoryServiceText.Text = "-";
            return;
        }

        GemText.Text = Currency(latestPrice.Gem);
        BeastBookText.Text = Currency(latestPrice.BeastBook);
        FiveTreasureText.Text = Currency(latestPrice.FiveTreasure);
        TokenText.Text = Currency(latestPrice.Token);
        EquipmentText.Text = Currency(latestPrice.Equipment);
        CharmText.Text = Currency(latestPrice.TemporaryCharm);
        FlowerText.Text = Currency(latestPrice.Flower);
        InstrumentText.Text = Currency(latestPrice.Instrument);
        StoryServiceText.Text = Currency(latestPrice.StoryService);
    }

    private static decimal ReadDecimal(string value)
    {
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0m;
    }

    private static string Currency(decimal value)
    {
        return $"¥{value:0.##}";
    }

    public sealed record DailyRecordRow(Guid Id, string DateText, string IncomeText, string ExpenseText, string NetText, string HourlyText);
}
