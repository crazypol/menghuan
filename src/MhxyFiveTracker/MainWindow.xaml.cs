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
    private readonly List<Grid> _pages = [];

    public MainWindow()
    {
        InitializeComponent();
        _pages.AddRange([OverviewPage, DailyPage, PricesPage, ServicesPage, InventoryPage, SettingsPage]);
        _data = _dataStore.Load();
        LoadSettingsIntoForm();
        ServiceDateInput.SelectedDate = DateTime.Today;
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

    private void Navigate_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string pageName)
        {
            return;
        }

        ShowPage(pageName);
    }

    private void ShowPage(string pageName)
    {
        foreach (var page in _pages)
        {
            page.Visibility = Visibility.Collapsed;
        }

        var selectedPage = pageName switch
        {
            "Daily" => DailyPage,
            "Prices" => PricesPage,
            "Services" => ServicesPage,
            "Inventory" => InventoryPage,
            "Settings" => SettingsPage,
            _ => OverviewPage
        };

        selectedPage.Visibility = Visibility.Visible;
    }

    private void SaveService_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadDecimal(ServiceAmountInput, "服务单金额", out var amount, showError: true))
        {
            return;
        }

        var title = ServiceTitleInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            MessageBox.Show(this, "服务名称不能为空。", "输入有误", MessageBoxButton.OK, MessageBoxImage.Warning);
            ServiceTitleInput.Focus();
            return;
        }

        _data.ServiceOrders.Add(new ServiceOrder
        {
            Date = ServiceDateInput.SelectedDate ?? DateTime.Today,
            Title = title,
            Amount = amount,
            Note = ServiceNoteInput.Text.Trim()
        });

        _dataStore.Save(_data);
        RefreshUi();
    }

    private void DeleteService_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not Guid id)
        {
            return;
        }

        var item = _data.ServiceOrders.FirstOrDefault(order => order.Id == id);
        if (item is null)
        {
            return;
        }

        _data.ServiceOrders.Remove(item);
        _dataStore.Save(_data);
        RefreshUi();
    }

    private void SaveInventory_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadDecimal(InventoryQuantityInput, "库存数量", out var quantity, showError: true) ||
            !TryReadDecimal(InventoryUnitPriceInput, "库存单价", out var unitPrice, showError: true))
        {
            return;
        }

        var name = InventoryNameInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show(this, "库存物品名称不能为空。", "输入有误", MessageBoxButton.OK, MessageBoxImage.Warning);
            InventoryNameInput.Focus();
            return;
        }

        _data.InventoryItems.Add(new InventoryItem
        {
            Name = name,
            Quantity = quantity,
            UnitPrice = unitPrice,
            Note = InventoryNoteInput.Text.Trim()
        });

        _dataStore.Save(_data);
        RefreshUi();
    }

    private void DeleteInventory_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not Guid id)
        {
            return;
        }

        var item = _data.InventoryItems.FirstOrDefault(inventory => inventory.Id == id);
        if (item is null)
        {
            return;
        }

        _data.InventoryItems.Remove(item);
        _dataStore.Save(_data);
        RefreshUi();
    }

    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadDecimal(InitialInvestmentInput, "初始投入", out var initialInvestment, showError: true) ||
            !TryReadDecimal(PaybackTargetInput, "目标回本金额", out var paybackTarget, showError: true))
        {
            return;
        }

        _data.Settings.ServerName = string.IsNullOrWhiteSpace(ServerNameInput.Text)
            ? "未命名服务器"
            : ServerNameInput.Text.Trim();
        _data.Settings.StartDate = StartDateInput.SelectedDate ?? DateTime.Today;
        _data.Settings.InitialInvestment = initialInvestment;
        _data.Settings.PaybackTarget = paybackTarget;

        _dataStore.Save(_data);
        RefreshUi();
        MessageBox.Show(this, "设置已保存。", "梦幻五开收益记录器", MessageBoxButton.OK, MessageBoxImage.Information);
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

        if (imported.DailyRecords.Count == 0 &&
            imported.PriceRecords.Count == 0 &&
            imported.ServiceOrders.Count == 0 &&
            imported.InventoryItems.Count == 0)
        {
            MessageBox.Show(this, "导入文件没有日报或物价记录，已取消导入。", "导入失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var backupPath = _dataStore.BackupCurrentDataFile("before-import");
        _data = imported;
        _dataStore.Save(_data);
        LoadSettingsIntoForm();
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
            LoadLatestPriceIntoForm(DateTime.Today);
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
        var serviceTotal = _data.ServiceOrders.Sum(item => item.Amount);
        var inventoryTotal = _data.InventoryItems.Sum(item => item.TotalValue);
        var totalNet = _data.DailyRecords.Sum(item => item.NetProfit) + serviceTotal + inventoryTotal - _data.Settings.InitialInvestment;
        var paybackProgress = _data.Settings.PaybackTarget <= 0
            ? 0
            : Math.Clamp((_data.DailyRecords.Sum(item => item.NetProfit) + serviceTotal + inventoryTotal) / _data.Settings.PaybackTarget * 100m, 0m, 999m);
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
        DailyRecordsGrid.ItemsSource = RecordsGrid.ItemsSource;
        PriceRecordsGrid.ItemsSource = _data.PriceRecords
            .OrderByDescending(item => item.Date)
            .Select(item => new PriceRecordRow(
                item.Date.ToString("MM-dd", CultureInfo.InvariantCulture),
                Currency(item.Gem),
                Currency(item.BeastBook),
                Currency(item.FiveTreasure),
                Currency(item.Token),
                Currency(item.Equipment),
                Currency(item.Flower),
                Currency(item.Instrument),
                Currency(item.TemporaryCharm),
                Currency(item.StoryService)))
            .ToList();
        ServiceOrdersGrid.ItemsSource = _data.ServiceOrders
            .OrderByDescending(item => item.Date)
            .Select(item => new ServiceOrderRow(
                item.Id,
                item.Date.ToString("MM-dd", CultureInfo.InvariantCulture),
                item.Title,
                Currency(item.Amount),
                item.Note))
            .ToList();
        InventoryGrid.ItemsSource = _data.InventoryItems
            .OrderBy(item => item.Name)
            .Select(item => new InventoryRow(
                item.Id,
                item.Name,
                item.Quantity.ToString("0.##", CultureInfo.InvariantCulture),
                Currency(item.UnitPrice),
                Currency(item.TotalValue),
                item.Note))
            .ToList();
        InventoryTotalText.Text = $"库存总估值：{Currency(inventoryTotal)}";

        RefreshPriceCards(latest?.Date ?? DateTime.Today);
    }

    private void LoadSettingsIntoForm()
    {
        ServerNameInput.Text = _data.Settings.ServerName;
        StartDateInput.SelectedDate = _data.Settings.StartDate;
        InitialInvestmentInput.Text = _data.Settings.InitialInvestment.ToString("0.##", CultureInfo.InvariantCulture);
        PaybackTargetInput.Text = _data.Settings.PaybackTarget.ToString("0.##", CultureInfo.InvariantCulture);
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
    public sealed record PriceRecordRow(
        string DateText,
        string GemText,
        string BeastBookText,
        string FiveTreasureText,
        string TokenText,
        string EquipmentText,
        string FlowerText,
        string InstrumentText,
        string CharmText,
        string StoryServiceText);
    public sealed record ServiceOrderRow(Guid Id, string DateText, string Title, string AmountText, string Note);
    public sealed record InventoryRow(Guid Id, string Name, string QuantityText, string UnitPriceText, string TotalValueText, string Note);
}
