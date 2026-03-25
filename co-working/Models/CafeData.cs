namespace co_working.Models
{
    public class CafeData
    {
        public HeroSection Hero { get; set; } = new();
        public string MenuSubtitle { get; set; } = string.Empty;
        public List<MenuCategory> Categories { get; set; } = new();
        public List<CafeSpecial> Specials { get; set; } = new();
        public List<CafeAmenity> Amenities { get; set; } = new();
    }

    public class HeroSection
    {
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
    }

    public class MenuCategory
    {
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public List<MenuItem> Items { get; set; } = new();
        public string? Addon { get; set; }
    }

    public class MenuItem
    {
        public string Name { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;
    }

    public class CafeSpecial
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;
    }

    public class CafeAmenity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }
}
