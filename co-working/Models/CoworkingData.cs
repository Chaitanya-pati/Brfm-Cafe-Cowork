namespace co_working.Models
{
    public class CoworkingData
    {
        public HeroSection Hero { get; set; } = new();
        public CoworkAbout About { get; set; } = new();
        public CoworkPlansHeader PlansHeader { get; set; } = new();
        public List<CoworkPlan> Plans { get; set; } = new();
        public List<CoworkAmenity> Amenities { get; set; } = new();
    }

    public class CoworkAbout
    {
        public string Eyebrow { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class CoworkPlansHeader
    {
        public string Eyebrow { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class CoworkPlan
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string VisualLabel { get; set; } = string.Empty;
        public string VisualClass { get; set; } = string.Empty;
        public string BlockClass { get; set; } = string.Empty;
        public bool IsReversed { get; set; }
        public string CtaText { get; set; } = string.Empty;
        public string CtaClass { get; set; } = string.Empty;
        public string CtaHref { get; set; } = string.Empty;
        public List<PlanRate> Rates { get; set; } = new();
    }

    public class PlanRate
    {
        public string Label { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;
        public string Period { get; set; } = string.Empty;
    }

    public class CoworkAmenity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }
}
