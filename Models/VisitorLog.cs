using System;

namespace SmogShield_Pakistan.Models
{
    public class VisitorLog
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Page { get; set; } = "";
        public DateTime VisitedAt { get; set; }
        public string Details { get; set; } = "";
        public int VisitCount { get; set; } = 1;
    }
}
