using System;
using System.Collections.Generic;

namespace Rota.Models.ViewModels
{
    public class WeeklyShiftInputModel
    {
        public string Name { get; set; }
        public string StartTimeDateOnly { get; set; } // e.g. 2025-05-06
        public string StartTimeTimeOnly { get; set; } // e.g. 09:00
        public string EndTimeTimeOnly { get; set; }   // e.g. 17:00
        public string Location { get; set; }
        public int RequiredStaff { get; set; }
        public string Notes { get; set; }
        public List<string> UserIds { get; set; } = new();
    }
}
