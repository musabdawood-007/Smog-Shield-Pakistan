# SmogShield Pakistan

A Windows desktop application for monitoring air quality across Pakistan's major cities. It tracks PM2.5 / PM10 readings, converts them to US AQI, ranks cities by pollution, compares locations, and visualizes nearby monitoring stations — built as a Data Structures & Algorithms (DSA) driven project.

**8 screens:** Visitor Gateway · Admin Login · Dashboard · Pollution Ranking · Location Comparison · Search & Filter · Settings · Admin Dashboard

---

## Technology Stack

| Layer | Technology |
|---|---|
| Framework | .NET 10 (`net10.0-windows`), C# 14, WPF (XAML) |
| UI | XAML resource dictionaries, custom vector icon set, custom chrome (borderless windows) |
| Live Data | OpenAQ API v3 (REST/JSON over HTTPS via `HttpClient`) |
| Database | Microsoft SQL Server LocalDB (`(localdb)\MSSQLLocalDB`) via `Microsoft.Data.SqlClient` 7.1.1 |
| Schema/Procedures | T-SQL setup script (`SQL/SmogShield_Setup.sql`) — `VisitorLogs`, `AirReadings` tables + stored procedures |
| Caching / Fallback | `System.Text.Json` in-memory cache (15 min TTL), CSV backup data (`Data/sample_readings.csv`), built-in sample dataset |
| Settings | JSON file persistence (`%LocalAppData%\SmogShield\settings.json`) |
| Scheduling | WPF `DispatcherTimer` (1-second world clock + sync countdown) |
| Data Export | CSV export (visitor logs) |

### Data source priority

```
Live OpenAQ API  →  CSV backup data  →  Built-in sample dataset
```

Every screen labels its source honestly with a badge: `LIVE OPENAQ DATA` / `CSV SAMPLE DATA` / `SAMPLE DATA`.

---

## DSA Structures Used

The `DSA/` folder contains the core data structure implementations the project is built on:

| Structure | Implementation | Used For |
|---|---|---|
| Graph + BFS | `StationGraph` (adjacency list, `BfsLevels`, `BfsOrder`) | Nearby-station relationship graph in Location Comparison |
| Heap | `MaxHeap<T>` (priority-based) | Top/heap-based ranking operations |
| Queue | `FifoQueue<T>` | FIFO processing pipelines |
| Hashing | `StationHashTable` (bucket hash table) | O(1) station lookup by code, city indexing |
| Sorting | Merge Sort, Quick Sort, Bubble Sort, Insertion Sort | City rankings (O(n log n) merge/quick as primary) |
| Searching | Binary Search, Linear Search | City lookup, threshold-range queries |

Complexities are surfaced in the UI (e.g. ranking screen shows the sort algorithm and its complexity).

---

## Project Structure

```
SmogShield Pakistan/
├── App.xaml / App.xaml.cs        # Application entry, world clock start, DB init
├── MainWindow.xaml               # Shell hosting all 8 views
├── Assets/
│   ├── SmogShield.ico            # App + taskbar icon
│   ├── Images/                   # City hero images
│   └── Icons/Smog_Icons/         # Vector icon library (Icons.xaml)
├── Views/                        # 8 screens (XAML + code-behind)
├── Services/
│   ├── OpenAqService.cs          # OpenAQ v3 client, cache, trend, nearby stations
│   ├── DatabaseHelper.cs         # SQL LocalDB access, stored procedures
│   ├── CsvDataService.cs         # CSV fallback reader + CSV export
│   ├── AppSettings.cs            # Unit/threshold/session settings
│   └── WorldClock.cs             # 1-second live clock
├── Models/                       # Reading, VisitorLog
├── DSA/                          # Graph, Hashing, Heap, Queue, Searching, Sorting
├── Data/sample_readings.csv      # Backup dataset
└── SQL/SmogShield_Setup.sql      # Database schema + stored procedures
```

---

## Getting Started

**Requirements:** Windows 10/11, .NET 10 SDK, SQL Server LocalDB (for visitor logs / persistence).

```bash
dotnet build "SmogShield Pakistan.csproj"
dotnet run --project "SmogShield Pakistan.csproj"
```

**Database setup (once):**

```bash
sqlcmd -S (localdb)\MSSQLLocalDB -i SQL/SmogShield_Setup.sql
```

The app still runs without the database — visitor logging degrades gracefully and air-quality data falls back to CSV/sample data.

---

## Features

- **Visitor gateway** with email validation and session profile (name, purpose → header on every screen)
- **Live AQI dashboard** with threshold alerts, hourly trends, and unit toggle (µg/m³ ↔ US AQI)
- **City pollution ranking** with sortable bars and complexity labels
- **Location comparison** with BFS-leveled station graph visualization
- **Search & filter** across all synced cities (56-city catalog)
- **Settings** with persisted unit/threshold preferences
- **Admin dashboard** with live system health, sync countdown, visitor log management (view/delete/export CSV), and force-refresh controls
- **Honest data badges** — every screen states whether data is Live, CSV, or Sample

---

## Data Attribution

Air quality data sourced from [OpenAQ](https://openaq.org) (openaq.org). Fallback/sample data is clearly labeled as such in the UI.

---

## License & Commercial Usage

This project is licensed under the **GNU Affero General Public License v3.0 (AGPL-3.0)**.

If you are a hobbyist or developer working on an open-source project, you are free to use, modify, and distribute this software under the terms of the AGPLv3.

**Commercial Use & Proprietary Applications:**
If you want to use this software, integrate it into a commercial product, or host it on a network cloud service WITHOUT being forced to open-source your own proprietary code, you are **required to obtain prior written permission or a commercial license** from the developer.

For commercial licensing inquiries or custom permissions, please contact me at:

* **Email:** musabdaud007@gmail.com
* **Website / Contact Form:** https://musab-007.netlify.app
