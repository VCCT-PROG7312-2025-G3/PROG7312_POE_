namespace PROG7312_POE.Domain
{
    public enum EventCategory { Water, Electricity, Traffic, Community, Safety, Other }

    public class MunicipalEvent
    {
        public int Id { get; set; }                                  // in-memory ID
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public EventCategory Category { get; set; } = EventCategory.Other;
        public DateTime Start { get; set; } = DateTime.UtcNow;
        public string Location { get; set; } = string.Empty;
        /// <summary>1 = low … 5 = urgent</summary>
        public int Priority { get; set; } = 1;
    }
}
