using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SmogShield_Pakistan.Models;

namespace SmogShield_Pakistan.Services
{
    public static class OpenAqService
    {
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(20) };

        private const string ApiKey = "PASTE-YOUR-OPENAQ-API-HERE";
        public const string SourceAttribution = "Data source: OpenAQ (openaq.org)";

        private const int PakistanCountryId = 109;

        public enum DataSourceKind { Live, Csv, Sample }

        public static DataSourceKind SourceKind { get; private set; } = DataSourceKind.Sample;

        public static string SourceLabel => SourceKind switch
        {
            DataSourceKind.Live => "  LIVE OPENAQ DATA",
            DataSourceKind.Csv => "  CSV SAMPLE DATA",
            _ => "  SAMPLE DATA"
        };

        private static readonly (string City, string Province, int[] LocationIds)[] TargetCities =
        {
            ("Lahore", "Punjab", new[] { 1894641, 8664, 4515157 }),
            ("Karachi", "Sindh", new[] { 8156, 1894633, 3089895 }),
            ("Islamabad", "Islamabad Capital Territory", new[] { 233470, 8634, 4515644 }),
            ("Faisalabad", "Punjab", new[] { 4838124, 4866055 }),
            ("Peshawar", "Khyber Pakhtunkhwa", new[] { 8088 }),
            ("Multan", "Punjab", new[] { 4757794 }),
            ("Quetta", "Balochistan", new[] { 4757304 }),
            ("Hyderabad", "Sindh", new[] { 4889110, 4897485 }),
            ("Gujranwala", "Punjab", new[] { 6135303 }),
            ("Rawalpindi", "Punjab", new[] { 4554236 }),
            ("Sialkot", "Punjab", new[] { 4629400 }),
            ("Bahawalpur", "Punjab", new[] { 4790880 }),
            ("Sukkur", "Sindh", new[] { 6135369 }),
            ("Abbottabad", "Khyber Pakhtunkhwa", new[] { 6135285 }),
            ("Sargodha", "Punjab", new[] { 6122104 }),
            ("Sahiwal", "Punjab", new[] { 6551450 }),
            ("Gujrat", "Punjab", new[] { 6551415, 4725232 }),
            ("Sheikhupura", "Punjab", new[] { 4555745, 4527035, 6135351 }),
            ("Kasur", "Punjab", new[] { 4908792, 6551429 }),
            ("Okara", "Punjab", new[] { 6135305 }),
            ("Khanewal", "Punjab", new[] { 4769229 }),
            ("Dera Ghazi Khan", "Punjab", new[] { 6135363 }),
            ("Rahim Yar Khan", "Punjab", new[] { 6137022, 6135291 }),
            ("Muzaffargarh", "Punjab", new[] { 4758328, 4770827, 6135477 }),
            ("Burewala", "Punjab", new[] { 6135355 }),
            ("Arifwala", "Punjab", new[] { 6551450 }),
            ("Pakpattan", "Punjab", new[] { 6551450 }),
            ("Chiniot", "Punjab", new[] { 6122104, 4854008, 6101816 }),
            ("Toba Tek Singh", "Punjab", new[] { 6135355 }),
            ("Lodhran", "Punjab", new[] { 4805270, 6135484, 4802678 }),
            ("Jhelum", "Punjab", new[] { 6135296 }),
            ("Layyah", "Punjab", new[] { 6551417 }),
            ("Mandi Bahauddin", "Punjab", new[] { 6135296 }),
            ("Narowal", "Punjab", new[] { 6138140, 4997881, 6138207 }),
            ("Murree", "Punjab", new[] { 6137242, 6130288, 6130291 }),
            ("Kot Addu", "Punjab", new[] { 4779254 }),
            ("Nankana Sahib", "Punjab", new[] { 6135489, 6122105 }),
            ("Jaranwala", "Punjab", new[] { 4847175, 4847174, 6098490 }),
            ("Samundri", "Punjab", new[] { 6122106, 4852891, 6135511 }),
            ("Taxila", "Punjab", new[] { 4568424, 6135332, 4608422 }),
            ("Chakwal", "Punjab", new[] { 6135322 }),
            ("Sadiqabad", "Punjab", new[] { 6551364 }),
            ("Bahawalnagar", "Punjab", new[] { 6551432 }),
            ("Khairpur", "Sindh", new[] { 6135461 }),
            ("Kotri", "Sindh", new[] { 6135427, 6135361 }),
            ("Tando Allahyar", "Sindh", new[] { 6135376 }),
            ("Kandhkot", "Sindh", new[] { 6135339 }),
            ("Nowshera", "Khyber Pakhtunkhwa", new[] { 4600078, 6135506, 4605118 }),
            ("Kohat", "Khyber Pakhtunkhwa", new[] { 6135499, 4608419, 4609307 }),
            ("Charsadda", "Khyber Pakhtunkhwa", new[] { 6135432, 4607067, 4609354 }),
            ("Haripur", "Khyber Pakhtunkhwa", new[] { 6135335, 6135304, 4556719 }),
            ("Mansehra", "Khyber Pakhtunkhwa", new[] { 6135371, 6130996 }),
            ("Gwadar", "Balochistan", new[] { 6142023 }),
            ("Skardu", "Gilgit-Baltistan", new[] { 6138366 }),
            ("Mirpur", "Azad Kashmir", new[] { 6135327 }),
            ("Muzaffarabad", "Azad Kashmir", new[] { 6149617, 6135288 }),
        };

        public static IReadOnlyList<(string City, string Province)> Catalog =>
            TargetCities.Select(c => (c.City, c.Province)).ToList();

        public static string ProvinceOf(string city)
        {
            foreach (var c in TargetCities)
                if (string.Equals(c.City, city, StringComparison.OrdinalIgnoreCase))
                    return c.Province;
            return "Pakistan";
        }

        public sealed record NearbyStation(string Name, double Lat, double Lon, double Km);

        private static readonly Dictionary<string, (double Lat, double Lon)> CityCoords = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Lahore"] = (31.5204, 74.3587),
            ["Karachi"] = (24.8607, 67.0011),
            ["Islamabad"] = (33.6844, 73.0479),
            ["Faisalabad"] = (31.4504, 73.1350),
            ["Peshawar"] = (34.0151, 71.5249),
            ["Multan"] = (30.1575, 71.5249),
            ["Quetta"] = (30.1798, 66.9750),
            ["Hyderabad"] = (25.3960, 68.3578),
            ["Gujranwala"] = (32.1877, 74.1945),
            ["Rawalpindi"] = (33.5651, 73.0169),
            ["Sialkot"] = (32.4945, 74.5229),
            ["Bahawalpur"] = (29.3956, 71.6836),
            ["Sukkur"] = (27.7052, 68.8574),
            ["Abbottabad"] = (34.1688, 73.2215),
            ["Sargodha"] = (32.0836, 72.6711),
            ["Sahiwal"] = (30.6645, 73.1066),
            ["Gujrat"] = (32.5740, 74.1100),
            ["Sheikhupura"] = (31.7131, 73.9847),
            ["Kasur"] = (31.1172, 74.4568),
            ["Okara"] = (30.8080, 73.4504),
            ["Khanewal"] = (30.3017, 71.9321),
            ["Dera Ghazi Khan"] = (30.0498, 70.6403),
            ["Rahim Yar Khan"] = (28.4212, 70.3042),
            ["Muzaffargarh"] = (30.0706, 71.1685),
            ["Burewala"] = (30.1670, 72.6810),
            ["Arifwala"] = (30.2830, 73.0657),
            ["Pakpattan"] = (30.2039, 73.3936),
            ["Chiniot"] = (31.7167, 72.9783),
            ["Toba Tek Singh"] = (31.0264, 72.5797),
            ["Lodhran"] = (29.5430, 71.6300),
            ["Jhelum"] = (32.9377, 73.4306),
            ["Layyah"] = (30.9609, 70.9396),
            ["Mandi Bahauddin"] = (32.5853, 73.4975),
            ["Narowal"] = (32.1020, 74.8733),
            ["Murree"] = (33.9070, 73.3943),
            ["Kot Addu"] = (30.4649, 70.9649),
            ["Nankana Sahib"] = (31.4501, 73.7086),
            ["Jaranwala"] = (31.5008, 73.1165),
            ["Samundri"] = (31.0641, 72.9694),
            ["Taxila"] = (33.7404, 72.7886),
            ["Chakwal"] = (32.9319, 72.9547),
            ["Sadiqabad"] = (28.3035, 70.3100),
            ["Bahawalnagar"] = (29.9938, 73.2530),
            ["Khairpur"] = (27.5297, 68.7592),
            ["Kotri"] = (25.3670, 68.3400),
            ["Tando Allahyar"] = (25.4587, 68.7167),
            ["Kandhkot"] = (28.4264, 70.0764),
            ["Nowshera"] = (34.0003, 71.9806),
            ["Kohat"] = (33.5889, 71.4421),
            ["Charsadda"] = (34.1480, 71.7396),
            ["Haripur"] = (33.9938, 72.9347),
            ["Mansehra"] = (34.3313, 73.1975),
            ["Gwadar"] = (25.1212, 62.3496),
            ["Skardu"] = (35.3267, 75.5350),
            ["Mirpur"] = (33.1483, 73.5214),
            ["Muzaffarabad"] = (34.3700, 73.4711),
        };

        private static readonly ConcurrentDictionary<string, (List<NearbyStation> Stations, DateTime FetchedAt)> _stationCache = new(StringComparer.OrdinalIgnoreCase);

        public static async Task<List<NearbyStation>?> GetNearbyStationsAsync(string city)
        {
            if (_stationCache.TryGetValue(city, out var cached) &&
                DateTime.UtcNow - cached.FetchedAt < TimeSpan.FromMinutes(15))
                return cached.Stations;

            if (!CityCoords.TryGetValue(city, out var coord)) return null;

            var latText = coord.Lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var lonText = coord.Lon.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var doc = await GetJsonAsync($"https://api.openaq.org/v3/locations?coordinates={latText},{lonText}&radius=25000&parameters_id=2&iso=PK&limit=15");
            if (doc is null) return null;

            try
            {
                using (doc)
                {
                    if (!doc.RootElement.TryGetProperty("results", out var rows)) return null;

                    var picked = new List<(NearbyStation S, double D)>();
                    foreach (var row in rows.EnumerateArray())
                    {
                        if (!row.TryGetProperty("coordinates", out var c) ||
                            !c.TryGetProperty("latitude", out var la) || la.ValueKind != JsonValueKind.Number ||
                            !c.TryGetProperty("longitude", out var lo) || lo.ValueKind != JsonValueKind.Number)
                            continue;

                        double lat = la.GetDouble(), lon = lo.GetDouble();
                        if (Math.Abs(lat) < 0.01 && Math.Abs(lon) < 0.01) continue;

                        if (!row.TryGetProperty("name", out var nm) || nm.ValueKind != JsonValueKind.String) continue;
                        var name = (nm.GetString() ?? "").Trim();
                        if (name.Length == 0) continue;

                        var station = new NearbyStation(name, lat, lon, DistanceKm(coord.Lat, coord.Lon, lat, lon));
                        picked.Add((station, station.Km));
                    }

                    var stations = picked
                        .OrderBy(t => t.D)
                        .Take(10)
                        .Select(t => t.S)
                        .ToList();

                    _stationCache[city] = (stations, DateTime.UtcNow);
                    return stations;
                }
            }
            catch
            {
                return null;
            }
        }

        public static double DistanceKm(double la1, double lo1, double la2, double lo2)
        {
            const double rad = Math.PI / 180.0;
            double dLa = (la2 - la1) * rad, dLo = (lo2 - lo1) * rad;
            double a = Math.Sin(dLa / 2) * Math.Sin(dLa / 2) +
                       Math.Cos(la1 * rad) * Math.Cos(la2 * rad) * Math.Sin(dLo / 2) * Math.Sin(dLo / 2);
            return 6371.0 * 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        private const int MaxRequestsPerMinute = 80;
        private const int MaxConcurrentRequests = 6;
        private static readonly Queue<DateTime> _requestTimes = new();
        private static readonly object _requestLock = new();
        private static readonly SemaphoreSlim _concurrent = new(MaxConcurrentRequests, MaxConcurrentRequests);
        private static readonly ConcurrentDictionary<string, long> _sensorIds = new(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, (double[]? Series, DateTime FetchedAt)> _trendCache = new(StringComparer.OrdinalIgnoreCase);

        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(15);
        private static List<Reading>? _cache;
        private static DateTime _cacheTime = DateTime.MinValue;
        private static List<(long Id, string Name, string Locality)>? _locationCache;
        private static Task<List<Reading>>? _inFlight;

        public static bool IsLive { get; private set; }
        public static bool IsSyncing { get; private set; }
        public static DateTime LastFetchTime => _cacheTime;

        public static async Task<List<Reading>> GetReadingsAsync(bool forceRefresh = false)
        {
            if (!forceRefresh && _cache is { Count: > 0 } && DateTime.UtcNow - _cacheTime < CacheTtl)
                return _cache;

            if (_inFlight is not null)
                return await _inFlight;

            _inFlight = FetchAndCacheAsync();
            try
            {
                return await _inFlight;
            }
            finally
            {
                _inFlight = null;
            }
        }

        private static async Task<List<Reading>> FetchAndCacheAsync()
        {
            IsSyncing = true;
            try
            {
                var live = await FetchAllAsync();

                if (live.Count > 0)
                {
                    _cache = live;
                    _cacheTime = DateTime.UtcNow;
                    IsLive = true;
                    SourceKind = DataSourceKind.Live;
                    return live;
                }

                if (_cache is { Count: > 0 })
                    return _cache;

                IsLive = false;
                var csv = CsvDataService.LoadSampleCsv();
                if (csv.Count > 0)
                {
                    SourceKind = DataSourceKind.Csv;
                    return csv;
                }

                SourceKind = DataSourceKind.Sample;
                return SampleData();
            }
            finally
            {
                IsSyncing = false;
            }
        }

        public static void ClearCache()
        {
            _cache = null;
            _cacheTime = DateTime.MinValue;
            IsLive = false;
            SourceKind = DataSourceKind.Sample;
        }

        private static async Task<bool> ThrottleAsync()
        {
            while (true)
            {
                TimeSpan wait;
                lock (_requestLock)
                {
                    var now = DateTime.UtcNow;
                    while (_requestTimes.Count > 0 && now - _requestTimes.Peek() >= TimeSpan.FromMinutes(1))
                        _requestTimes.Dequeue();

                    if (_requestTimes.Count < MaxRequestsPerMinute)
                    {
                        _requestTimes.Enqueue(now);
                        return true;
                    }

                    wait = TimeSpan.FromMinutes(1) - (now - _requestTimes.Peek()) + TimeSpan.FromMilliseconds(30);
                    if (wait < TimeSpan.FromMilliseconds(30)) wait = TimeSpan.FromMilliseconds(30);
                }

                await Task.Delay(wait);
            }
        }

        private static async Task<JsonDocument?> GetJsonAsync(string url)
        {
            await _concurrent.WaitAsync();
            try
            {
                if (!await ThrottleAsync()) return null;

                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Headers.Add("Accept", "application/json");
                    request.Headers.Add("X-API-Key", ApiKey);

                    var response = await Http.SendAsync(request);

                    if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        await Task.Delay(3_000);
                        return null;
                    }

                    if (!response.IsSuccessStatusCode) return null;

                    string body = await response.Content.ReadAsStringAsync();
                    return JsonDocument.Parse(body);
                }
                catch
                {
                    return null;
                }
            }
            finally
            {
                _concurrent.Release();
            }
        }

        private static async Task<List<Reading>> FetchAllAsync()
        {
            var tasks = TargetCities.Select(c => FetchCityAsync(c.City, c.LocationIds));
            var results = await Task.WhenAll(tasks);

            var live = results.Where(r => r is not null).Cast<Reading>().ToList();

            if (live.Count == 0)
                live.AddRange(await DiscoverFallbackAsync());

            return live;
        }

        private static async Task<Reading?> FetchCityAsync(string city, int[] locationIds)
        {
            foreach (var locId in locationIds)
            {
                var reading = await FetchSensorsAsync(city, locId);
                if (reading is not null)
                    return reading;
            }

            var discovered = await FindLocationIdAsync(city);
            return discovered > 0 ? await FetchSensorsAsync(city, discovered) : null;
        }

        private static async Task<Reading?> FetchSensorsAsync(string city, long locationId)
        {
            var doc = await GetJsonAsync($"https://api.openaq.org/v3/locations/{locationId}/sensors");
            if (doc is null) return null;

            try
            {
                using (doc)
                {
                    if (!doc.RootElement.TryGetProperty("results", out var sensors)) return null;

                    double pm25 = 0, pm10 = 0;
                    DateTime observed = DateTime.MinValue;
                    string station = city;

                    foreach (var sensor in sensors.EnumerateArray())
                    {
                        if (!sensor.TryGetProperty("parameter", out var param)) continue;
                        string? paramName = param.TryGetProperty("name", out var pn) ? pn.GetString() : null;

                        bool isPm25 = string.Equals(paramName, "pm25", StringComparison.OrdinalIgnoreCase);
                        bool isPm10 = string.Equals(paramName, "pm10", StringComparison.OrdinalIgnoreCase);
                        if (!isPm25 && !isPm10) continue;

                        if (!sensor.TryGetProperty("latest", out var latest)) continue;
                        if (!latest.TryGetProperty("value", out var val) || val.ValueKind != JsonValueKind.Number) continue;

                        double value = val.GetDouble();
                        if (value <= 0 || value >= 999) continue;

                        DateTime when = DateTime.MinValue;
                        if (latest.TryGetProperty("datetime", out var dt) &&
                            dt.TryGetProperty("local", out var local) &&
                            DateTime.TryParse(local.GetString(), out var parsed))
                            when = parsed;

                        if (isPm25)
                        {
                            pm25 = value;
                            if (sensor.TryGetProperty("id", out var sensorId) &&
                                sensorId.ValueKind == JsonValueKind.Number)
                                _sensorIds[city] = sensorId.GetInt64();
                        }
                        else pm10 = value;

                        if (when > observed) observed = when;

                        if (sensor.TryGetProperty("location", out var loc) &&
                            loc.TryGetProperty("name", out var locName) &&
                            locName.ValueKind == JsonValueKind.String)
                            station = locName.GetString() ?? station;
                    }

                    if (pm25 <= 0 && pm10 <= 0) return null;
                    if (observed == DateTime.MinValue) observed = DateTime.Now;

                    return new Reading
                    {
                        Code = $"{city.Substring(0, Math.Min(3, city.Length)).ToUpperInvariant()}-{locationId % 1000:D3}",
                        City = city,
                        Station = station,
                        Location = $"{city}, Pakistan",
                        Pm25 = Math.Round(pm25, 1),
                        Pm10 = Math.Round(pm10, 1),
                        Aqi = Pm25ToAqi(pm25 > 0 ? pm25 : pm10),
                        ObservedAt = observed,
                        Source = "OpenAQ"
                    };
                }
            }
            catch
            {
                return null;
            }
        }

        public static async Task<double[]?> GetHourlyTrendAsync(string city)
        {
            if (_trendCache.TryGetValue(city, out var cached) &&
                DateTime.UtcNow - cached.FetchedAt < TimeSpan.FromMinutes(15))
                return cached.Series;

            if (!_sensorIds.TryGetValue(city, out long sensorId)) return null;

            var fromText = DateTime.UtcNow.AddHours(-30).ToString("yyyy-MM-ddTHH:mm:ssZ");
            var url = $"https://api.openaq.org/v3/sensors/{sensorId}/measurements/hourly?datetime_from={fromText}&limit=50";
            var doc = await GetJsonAsync(url);
            if (doc is null)
            {
                await Task.Delay(400);
                doc = await GetJsonAsync(url);
            }
            if (doc is null) return null;

            try
            {
                using (doc)
                {
                    if (!doc.RootElement.TryGetProperty("results", out var rows)) return null;

                    var points = new List<(DateTime T, double V)>();
                    foreach (var row in rows.EnumerateArray())
                    {
                        if (!row.TryGetProperty("value", out var v) || v.ValueKind != JsonValueKind.Number)
                            continue;
                        double val = v.GetDouble();
                        if (val <= 0 || val >= 999) continue;

                        var t = DateTime.MinValue;
                        if (row.TryGetProperty("period", out var period) &&
                            period.TryGetProperty("datetimeFrom", out var from) &&
                            from.TryGetProperty("utc", out var utc) &&
                            DateTime.TryParse(utc.GetString(), null,
                                System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
                            t = parsed;

                        points.Add((t, Math.Round(val, 1)));
                    }

                    if (points.Count < 2)
                    {
                        _trendCache[city] = (null, DateTime.UtcNow);
                        return null;
                    }

                    points.Sort((x, y) => x.T.CompareTo(y.T));
                    if (points[^1].T.ToUniversalTime() < DateTime.UtcNow.AddHours(-36))
                    {
                        _trendCache[city] = (null, DateTime.UtcNow);
                        return null;
                    }

                    var series = points.Select(p => p.V).ToList();
                    if (series.Count > 24) series.RemoveRange(0, series.Count - 24);

                    var result = series.ToArray();
                    _trendCache[city] = (result, DateTime.UtcNow);
                    return result;
                }
            }
            catch
            {
                return null;
            }
        }

        private static async Task<long> FindLocationIdAsync(string city)
        {
            var locations = await GetLocationsAsync();
            foreach (var (id, name, locality) in locations)
            {
                if (ContainsIgnoreCase(name, city) || ContainsIgnoreCase(locality, city))
                    return id;
            }
            return 0;
        }

        private static async Task<List<(long Id, string Name, string Locality)>> GetLocationsAsync()
        {
            if (_locationCache is { Count: > 0 })
                return _locationCache;

            var locations = new List<(long, string, string)>();

            for (int page = 1; page <= 2; page++)
            {
                var doc = await GetJsonAsync(
                    $"https://api.openaq.org/v3/locations?countries_id={PakistanCountryId}&limit=100&page={page}");
                if (doc is null) break;

                using (doc)
                {
                    if (!doc.RootElement.TryGetProperty("results", out var results)) break;

                    foreach (var loc in results.EnumerateArray())
                    {
                        long id = loc.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.Number
                            ? idProp.GetInt64() : 0;
                        if (id == 0) continue;

                        string name = loc.TryGetProperty("name", out var nm) && nm.ValueKind == JsonValueKind.String
                            ? nm.GetString() ?? "" : "";
                        string locality = loc.TryGetProperty("locality", out var lo) && lo.ValueKind == JsonValueKind.String
                            ? lo.GetString() ?? "" : "";

                        locations.Add((id, name, locality));
                    }
                }
            }

            if (locations.Count > 0)
                _locationCache = locations;

            return locations;
        }

        private static async Task<List<Reading>> DiscoverFallbackAsync()
        {
            var live = new List<Reading>();
            var locations = await GetLocationsAsync();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var (id, name, locality) in locations)
            {
                foreach (var (city, _, _) in TargetCities)
                {
                    if (!seen.Contains(city) &&
                        (ContainsIgnoreCase(name, city) || ContainsIgnoreCase(locality, city)))
                    {
                        var reading = await FetchSensorsAsync(city, id);
                        if (reading is not null)
                        {
                            live.Add(reading);
                            seen.Add(city);
                        }
                        break;
                    }
                }
            }

            return live;
        }

        private static bool ContainsIgnoreCase(string source, string value) =>
            !string.IsNullOrEmpty(source) &&
            source.Contains(value, StringComparison.OrdinalIgnoreCase);

        public static List<Reading> SampleData()
        {
            var now = DateTime.Now;
            return new List<Reading>
            {
                new() { Code="LHR-001", City="Lahore",     Station="Township",    Location="Lahore, Punjab",       Pm25=128.6, Pm10=198.0, Aqi=214, ObservedAt=now, Source="Sample" },
                new() { Code="LHR-002", City="Lahore",     Station="Gulberg",     Location="Lahore, Punjab",       Pm25=132.8, Pm10=205.0, Aqi=220, ObservedAt=now, Source="Sample" },
                new() { Code="LHR-003", City="Lahore",     Station="Jail Road",   Location="Lahore, Punjab",       Pm25=125.0, Pm10=190.0, Aqi=208, ObservedAt=now, Source="Sample" },
                new() { Code="LHR-004", City="Lahore",     Station="Model Town",  Location="Lahore, Punjab",       Pm25=118.0, Pm10=182.0, Aqi=198, ObservedAt=now, Source="Sample" },
                new() { Code="LHR-005", City="Lahore",     Station="DHA",         Location="Lahore, Punjab",       Pm25=108.8, Pm10=170.0, Aqi=186, ObservedAt=now, Source="Sample" },
                new() { Code="KHI-001", City="Karachi",    Station="Clifton",     Location="Karachi, Sindh",       Pm25=78.4,  Pm10=120.0, Aqi=162, ObservedAt=now, Source="Sample" },
                new() { Code="KHI-002", City="Karachi",    Station="Gulshan",     Location="Karachi, Sindh",       Pm25=72.5,  Pm10=112.0, Aqi=152, ObservedAt=now, Source="Sample" },
                new() { Code="ISB-001", City="Islamabad",  Station="Sector F-6",  Location="Islamabad, Capital",   Pm25=35.2,  Pm10=68.0,  Aqi=100, ObservedAt=now, Source="Sample" },
                new() { Code="ISB-002", City="Islamabad",  Station="Sector G-8",  Location="Islamabad, Capital",   Pm25=38.0,  Pm10=72.0,  Aqi=106, ObservedAt=now, Source="Sample" },
                new() { Code="LYP-001", City="Faisalabad", Station="Jhang Road",  Location="Faisalabad, Punjab",   Pm25=95.0,  Pm10=148.0, Aqi=176, ObservedAt=now, Source="Sample" },
                new() { Code="PEW-001", City="Peshawar",   Station="University",  Location="Peshawar, KP",         Pm25=88.0,  Pm10=138.0, Aqi=168, ObservedAt=now, Source="Sample" },
                new() { Code="MUX-001", City="Multan",     Station="Cantt",       Location="Multan, Punjab",       Pm25=102.0, Pm10=158.0, Aqi=180, ObservedAt=now, Source="Sample" },
                new() { Code="UET-001", City="Quetta",     Station="Sariab Road", Location="Quetta, Balochistan",  Pm25=65.0,  Pm10=105.0, Aqi=148, ObservedAt=now, Source="Sample" },
                new() { Code="NRL-001", City="Narowal",    Station="Govt Boys High School", Location="Narowal, Punjab", Pm25=47.0, Pm10=95.0, Aqi=129, ObservedAt=now, Source="Sample" },
            };
        }

        public static double Pm25ToAqi(double pm25)
        {
            if (pm25 <= 12.0) return Math.Round((50.0 / 12.0) * pm25);
            if (pm25 <= 35.4) return Math.Round(((100.0 - 50.0) / (35.4 - 12.0)) * (pm25 - 12.0) + 50.0);
            if (pm25 <= 55.4) return Math.Round(((150.0 - 100.0) / (55.4 - 35.4)) * (pm25 - 35.4) + 100.0);
            if (pm25 <= 150.4) return Math.Round(((200.0 - 150.0) / (150.4 - 55.4)) * (pm25 - 55.4) + 150.0);
            if (pm25 <= 250.4) return Math.Round(((300.0 - 200.0) / (250.4 - 150.4)) * (pm25 - 150.4) + 200.0);
            return Math.Round(((500.0 - 300.0) / (500.0 - 250.4)) * (pm25 - 250.4) + 300.0);
        }
    }
}
