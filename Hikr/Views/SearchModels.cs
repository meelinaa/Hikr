namespace Hikr.Views;

/// <summary>Ein Eintrag im Suchverlauf (Dummy-Daten).</summary>
public class HistoryEntry
{
    public string Icon        { get; set; } = "🕐";
    public string PrimaryText { get; set; } = "";
    public string SecondaryText { get; set; } = "";
    public string DistanceText { get; set; } = "";
    public string? SearchQuery { get; set; }
    public double? Lat { get; set; }
    public double? Lon { get; set; }
    public bool HasSubtitle => !string.IsNullOrEmpty(SecondaryText);
    public bool HasDistance => !string.IsNullOrEmpty(DistanceText);
}

/// <summary>Ein Live-Suchergebnis mit berechneter Entfernung.</summary>
public class SearchResultEntry
{
    public Hikr.Models.NominatimResult? NominatimResult { get; set; }
    public string Icon          { get; set; } = "📍";
    public string DistanceText  { get; set; } = "";
    public string PrimaryText   { get; set; } = "";
    public string SecondaryText { get; set; } = "";
    public bool HasSubtitle => !string.IsNullOrEmpty(SecondaryText);
}
