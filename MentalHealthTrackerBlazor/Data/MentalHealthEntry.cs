using System.ComponentModel.DataAnnotations;

namespace MentalHealthTrackerBlazor.Data
{
    public class MentalHealthEntry
    {

        public int UserId { get; set; }
        [Key]
        public int EntryId { get; set; }
        public DateTime Date { get; set; }
        public int Mood { get; set; }
        public string Notes { get; set; }
        public string Triggers { get; set; }
    }
}
