using System;

namespace SmogShield_Pakistan.Models
{
    public class Reading
    {
        public string Code { get; set; } = "";
        public string City { get; set; } = "";
        public string Station { get; set; } = "";
        public string Location { get; set; } = "";
        public double Pm25 { get; set; }
        public double Pm10 { get; set; }
        public double Aqi { get; set; }
        public DateTime ObservedAt { get; set; }
        public string Source { get; set; } = "Sample";
        public bool IsLive => Source == "OpenAQ";

        public string AqiCategory => Aqi switch
        {
            <= 50 => "Good",
            <= 100 => "Moderate",
            <= 150 => "Unhealthy (sensitive)",
            <= 200 => "Unhealthy",
            <= 300 => "Very unhealthy",
            _ => "Hazardous"
        };

        public string Pm25Category => Pm25 switch
        {
            <= 12.0 => "Good",
            <= 35.4 => "Moderate",
            <= 55.4 => "Unhealthy (sensitive)",
            <= 150.4 => "Unhealthy",
            <= 250.4 => "Very unhealthy",
            _ => "Hazardous"
        };
    }
}
