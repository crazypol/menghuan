# MHXY Five Tracker Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a first Windows-native WPF version of the MHXY five-account profit tracker.

**Architecture:** A single WPF desktop project stores all data in a local JSON file under the user's Documents folder. The first version uses code-behind for speed and keeps models and storage service separate so the UI can be refactored later.

**Tech Stack:** .NET 8, WPF, C#, System.Text.Json.

---

### Task 1: Project Skeleton

**Files:**
- Create: `src/MhxyFiveTracker/MhxyFiveTracker.csproj`
- Create: `src/MhxyFiveTracker/App.xaml`
- Create: `src/MhxyFiveTracker/App.xaml.cs`

- [x] Create the WPF project file targeting `net8.0-windows`.
- [x] Add the application entry point.

### Task 2: Data Model And Storage

**Files:**
- Create: `src/MhxyFiveTracker/Models/TrackerData.cs`
- Create: `src/MhxyFiveTracker/Services/DataStore.cs`

- [x] Define settings, daily records, and price records.
- [x] Implement JSON load/save with sample data fallback.

### Task 3: Main UI

**Files:**
- Create: `src/MhxyFiveTracker/MainWindow.xaml`
- Create: `src/MhxyFiveTracker/MainWindow.xaml.cs`

- [x] Build the double-column WPF interface.
- [x] Add colorful dashboard cards, daily form, history table, and price chips.
- [x] Wire save, delete, import, and export buttons.

### Task 4: Verification

**Files:**
- Inspect all created files.

- [x] Confirm all expected source files exist.
- [x] Run static text checks for unresolved placeholders.
- [x] Note that WPF build cannot run in this macOS environment because `dotnet` is unavailable.
