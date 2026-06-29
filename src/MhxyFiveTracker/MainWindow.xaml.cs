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
        if (!TryBuildRecordFromForm(out var record))
        {
            return;
        }

        if (!TryBuildPriceRecordFromForm(out var priceRecord))
        {
            return;
        }

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

        var existingPrice = _data.PriceRecords.FirstOrDefault(item => item.Date.Date == priceRecord.Date.Date);
        if (existingPrice is null)
        {
            _data.PriceRecords.Add(priceRecord);
        }
        else
        {
            existingPrice.Gem = priceRecord.Gem;
            existingPrice.BeastBook = priceRecord.BeastBook;
            existingPrice.FiveTreasure = priceRecord.FiveTreasure;
            existingPrice.Token = priceRecord.Token;
            existingPrice.Equipment = priceRecord.Equipment;
            existingPrice.Flower = priceRecord.Flower;
            existingPrice.Instrument = priceRecord.Instrument;
            existingPrice.TemporaryCharm = priceRecord.TemporaryCharm;
            existingPrice.StoryService = priceRecord.StoryService;
            existingPrice.Note = priceRecord.Note;
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

        var confirm = MessageBox.Show(
            this,
            "导入会替换当前本地数据。应用会先自动备份现有 data.json。确定继续吗？",
            "确认导入",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        TrackerData imported;
        try
        {
            imported = _dataStore.Import(dialog.FileName);
        }
        catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            MessageBox.Show(this, $"导入失败：{ex.Message}", "梦幻五开收益记录器", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (imported.DailyRecords.Count == 0 && imported.PriceRecords.Count == 0)
        {
            MessageBox.Show(this, "导入文件没有日报或物价记录，已取消导入。", "导入失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var backupPath = _dataStore.BackupCurrentDataFile("before-import");
        _data = imported;
        _dataStore.Save(_data);
        LoadLatestRecordIntoForm();
        RefreshUi();
        var backupMessage = backupPath is null ? "" : $"\n已备份原数据：{backupPath}";
        MessageBox.Show(this, $"导入完成。{backupMessage}", "梦幻五开收益记录器", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Input_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        RefreshPreviewTotals();
    }

    private void DateInput_SelectedDateChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || DateInput.SelectedDate is not { } selectedDate)
        {
            return;
        }

        var record = _data.DailyRecords.FirstOrDefault(item => item.Date.Date == selectedDate.Date);
        if (record is not null)
        {
            OnlineHoursInput.Text = record.OnlineHours.ToString("0.##", CultureInfo.InvariantCulture);
            CashIncomeInput.Text = record.CashIncome.ToString("0.##", CultureInfo.InvariantCulture);
            MaterialValueInput.Text = record.MaterialValue.ToString("0.##", CultureInfo.InvariantCulture);
            ServiceIncomeInput.Text = record.ServiceIncome.ToString("0.##", CultureInfo.InvariantCulture);
            ExpenseInput.Text = record.Expense.ToString("0.##", CultureInfo.InvariantCulture);
            NoteInput.Text = record.Note;
        }
        else
        {
            OnlineHoursInput.Text = "0";
            CashIncomeInput.Text = "0";
            MaterialValueInput.Text = "0";
            ServiceIncomeInput.Text = "0";
            ExpenseInput.Text = "0";
            NoteInput.Text = "";
        }

        LoadLatestPriceIntoForm(selectedDate);
        RefreshPreviewTotals();
    }

    private void RefreshPreviewTotals()
    {
        if (!TryBuildRecordFromForm(out var preview, showError: false))
        {
            return;
        }

        TodayNetText.Text = Currency(preview.NetProfit);
        TodayHourlyText.Text = Currency(preview.HourlyProfit);
    }

    private bool TryBuildRecordFromForm(out DailyRecord record, bool showError = true)
    {
        record = new DailyRecord
        {
            Date = DateInput.SelectedDate ?? DateTime.Today,
            Note = NoteInput.Text.Trim()
        };

        if (!TryReadDecimal(OnlineHoursInput, "在线小时", out var onlineHours, showError) ||
            !TryReadDecimal(CashIncomeInput, "现金收入", out var cashIncome, showError) ||
            !TryReadDecimal(MaterialValueInput, "物资估值", out var materialValue, showError) ||
            !TryReadDecimal(ServiceIncomeInput, "服务单", out var serviceIncome, showError) ||
            !TryReadDecimal(ExpenseInput, "支出", out var expense, showError))
        {
            return false;
        }

        record.OnlineHours = onlineHours;
        record.CashIncome = cashIncome;
        record.MaterialValue = materialValue;
        record.ServiceIncome = serviceIncome;
        record.Expense = expense;
        return true;
    }

    private bool TryBuildPriceRecordFromForm(out PriceRecord record, bool showError = true)
    {
        record = new PriceRecord
        {
            Date = DateInput.SelectedDate ?? DateTime.Today
        };

        if (!TryReadDecimal(GemInput, "宝石", out var gem, showError) ||
            !TryReadDecimal(BeastBookInput, "低兽决", out var beastBook, showError) ||
            !TryReadDecimal(FiveTreasureInput, "五宝", out var fiveTreasure, showError) ||
            !TryReadDecimal(TokenInput, "牌子", out var token, showError) ||
            !TryReadDecimal(EquipmentInput, "环装", out var equipment, showError) ||
            !TryReadDecimal(CharmInput, "临时符", out var charm, showError) ||
            !TryReadDecimal(FlowerInput, "花卉", out var flower, showError) ||
            !TryReadDecimal(InstrumentInput, "乐器", out var instrument, showError) ||
            !TryReadDecimal(StoryServiceInput, "剧情服务", out var storyService, showError))
        {
            return false;
        }

        record.Gem = gem;
        record.BeastBook = beastBook;
        record.FiveTreasure = fiveTreasure;
        record.Token = token;
        record.Equipment = equipment;
        record.Flower = flower;
        record.Instrument = instrument;
        record.TemporaryCharm = charm;
        record.StoryService = storyService;
        return true;
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
        LoadLatestPriceIntoForm(latest.Date);
    }

    private void RefreshUi()
    {
        var orderedRecords = _data.DailyRecords.OrderByDescending(item => item.Date).ToList();
        var latest = orderedRecords.FirstOrDefault();
        var totalNet = _data.DailyRecords.Sum(item => item.NetProfit) - _data.Settings.InitialInvestment;
        var paybackProgress = _data.Settings.PaybackTarget <= 0
            ? 0
            : Math.Clamp(_data.DailyRecords.Sum(item => item.NetProfit) / _data.Settings.PaybackTarget * 100m, 0m, 999m);
        var todayPreview = TryBuildRecordFromForm(out var parsedPreview, showError: false)
            ? parsedPreview
            : new DailyRecord();

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

    private void LoadLatestPriceIntoForm(DateTime date)
    {
        var latestPrice = FindLatestPrice(date);
        if (latestPrice is null)
        {
            return;
        }

        GemInput.Text = latestPrice.Gem.ToString("0.##", CultureInfo.InvariantCulture);
        BeastBookInput.Text = latestPrice.BeastBook.ToString("0.##", CultureInfo.InvariantCulture);
        FiveTreasureInput.Text = latestPrice.FiveTreasure.ToString("0.##", CultureInfo.InvariantCulture);
        TokenInput.Text = latestPrice.Token.ToString("0.##", CultureInfo.InvariantCulture);
        EquipmentInput.Text = latestPrice.Equipment.ToString("0.##", CultureInfo.InvariantCulture);
        CharmInput.Text = latestPrice.TemporaryCharm.ToString("0.##", CultureInfo.InvariantCulture);
        FlowerInput.Text = latestPrice.Flower.ToString("0.##", CultureInfo.InvariantCulture);
        InstrumentInput.Text = latestPrice.Instrument.ToString("0.##", CultureInfo.InvariantCulture);
        StoryServiceInput.Text = latestPrice.StoryService.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private void RefreshPriceCards(DateTime date)
    {
        var latestPrice = FindLatestPrice(date);

        if (latestPrice is null)
        {
            GemInput.Text = BeastBookInput.Text = FiveTreasureInput.Text = TokenInput.Text = EquipmentInput.Text = CharmInput.Text = FlowerInput.Text = InstrumentInput.Text = StoryServiceInput.Text = "";
            return;
        }

        GemInput.Text = latestPrice.Gem.ToString("0.##", CultureInfo.InvariantCulture);
        BeastBookInput.Text = latestPrice.BeastBook.ToString("0.##", CultureInfo.InvariantCulture);
        FiveTreasureInput.Text = latestPrice.FiveTreasure.ToString("0.##", CultureInfo.InvariantCulture);
        TokenInput.Text = latestPrice.Token.ToString("0.##", CultureInfo.InvariantCulture);
        EquipmentInput.Text = latestPrice.Equipment.ToString("0.##", CultureInfo.InvariantCulture);
        CharmInput.Text = latestPrice.TemporaryCharm.ToString("0.##", CultureInfo.InvariantCulture);
        FlowerInput.Text = latestPrice.Flower.ToString("0.##", CultureInfo.InvariantCulture);
        InstrumentInput.Text = latestPrice.Instrument.ToString("0.##", CultureInfo.InvariantCulture);
        StoryServiceInput.Text = latestPrice.StoryService.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private PriceRecord? FindLatestPrice(DateTime date)
    {
        return _data.PriceRecords
            .OrderByDescending(item => item.Date)
            .FirstOrDefault(item => item.Date.Date <= date.Date)
            ?? _data.PriceRecords.OrderByDescending(item => item.Date).FirstOrDefault();
    }

    private bool TryReadDecimal(TextBox input, string fieldName, out decimal value, bool showError)
    {
        input.ClearValue(Control.BorderBrushProperty);

        if (decimal.TryParse(input.Text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out value) ||
            decimal.TryParse(input.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        input.BorderBrush = System.Windows.Media.Brushes.Crimson;
        if (showError)
        {
            MessageBox.Show(this, $"「{fieldName}」请输入数字。", "输入有误", MessageBoxButton.OK, MessageBoxImage.Warning);
            input.Focus();
            input.SelectAll();
        }

        return false;
    }

    private static string Currency(decimal value)
    {
        return $"¥{value:0.##}";
    }

    public sealed record DailyRecordRow(Guid Id, string DateText, string IncomeText, string ExpenseText, string NetText, string HourlyText);
}
