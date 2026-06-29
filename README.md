# 梦幻五开收益记录器

Windows 原生 WPF 桌面应用，用来记录梦幻西游畅玩服五开收益、支出、物价和回本进度。

## 运行要求

- Windows 10/11
- .NET 8 SDK

## 本地运行

```powershell
dotnet run --project .\src\MhxyFiveTracker\MhxyFiveTracker.csproj
```

## 数据位置

应用会把数据保存到：

```text
文档\MHXYFiveAccountTracker\data.json
```

## 第一版功能

- 每日收益录入：在线小时、现金收入、物资估值、服务单、支出、备注
- 自动统计：今日净收益、今日小时收益、累计净收益、回本进度
- 历史记录表格
- 今日物价快照
- JSON 导入/导出

## 后续可加

- 物价手动录入页面
- 服务单独立管理
- 库存估值
- 图表趋势
- 打包成单个 `.exe`
