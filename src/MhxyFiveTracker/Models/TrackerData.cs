namespace MhxyFiveTracker.Models;

public sealed class TrackerData
{
    public TrackerSettings Settings { get; set; } = new();
    public List<DailyRecord> DailyRecords { get; set; } = [];
    public List<PriceRecord> PriceRecords { get; set; } = [];
    public List<ServiceOrder> ServiceOrders { get; set; } = [];
    public List<InventoryItem> InventoryItems { get; set; } = [];
}

public sealed class TrackerSettings
{
    public string ServerName { get; set; } = "紫禁城畅玩服";
    public DateTime StartDate { get; set; } = DateTime.Today;
    public decimal InitialInvestment { get; set; } = 2680m;
    public decimal PaybackTarget { get; set; } = 2680m;
}

public sealed class DailyRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Date { get; set; } = DateTime.Today;
    public decimal OnlineHours { get; set; }
    public decimal CashIncome { get; set; }
    public decimal MaterialValue { get; set; }
    public decimal ServiceIncome { get; set; }
    public decimal Expense { get; set; }
    public string Note { get; set; } = "";

    public decimal TotalIncome => CashIncome + MaterialValue + ServiceIncome;
    public decimal NetProfit => TotalIncome - Expense;
    public decimal HourlyProfit => OnlineHours <= 0 ? 0 : NetProfit / OnlineHours;
}

public sealed class PriceRecord
{
    public DateTime Date { get; set; } = DateTime.Today;
    public decimal Gem { get; set; }
    public decimal BeastBook { get; set; }
    public decimal FiveTreasure { get; set; }
    public decimal Token { get; set; }
    public decimal Equipment { get; set; }
    public decimal Flower { get; set; }
    public decimal Instrument { get; set; }
    public decimal TemporaryCharm { get; set; }
    public decimal StoryService { get; set; }
    public string Note { get; set; } = "";
}

public sealed class ServiceOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Date { get; set; } = DateTime.Today;
    public string Title { get; set; } = "";
    public decimal Amount { get; set; }
    public string Note { get; set; } = "";
}

public sealed class InventoryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string Note { get; set; } = "";

    public decimal TotalValue => Quantity * UnitPrice;
}
