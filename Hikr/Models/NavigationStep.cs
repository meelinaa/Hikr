namespace Hikr.Models;

/// <summary>
/// Model representing a single navigation step from OSRM.
/// </summary>
public class NavigationStep
{
    public double Distance { get; set; }
    public double Duration { get; set; }
    public string StreetName { get; set; } = string.Empty;
    public string ManeuverType { get; set; } = string.Empty;
    public string ManeuverModifier { get; set; } = string.Empty;
    public Mapsui.MPoint Location { get; set; } = null!;

    public string FormattedInstruction => GetGuidanceInfo().Instruction;
    public string Emoji => GetGuidanceInfo().Emoji;
    public string FormattedDistance => Helpers.NavigationCalculator.FormatDistance(Distance);

    private (string Instruction, string Emoji) GetGuidanceInfo()
    {
        string emoji = "⬆️";
        string instruction = "Dem Straßenverlauf folgen";

        if (ManeuverType == "depart")
        {
            instruction = string.IsNullOrWhiteSpace(StreetName) ? "Richtung starten" : $"Richtung {StreetName} starten";
        }
        else if (ManeuverType == "arrive")
        {
            instruction = "Sie haben Ihr Ziel erreicht!";
            emoji = "🏁";
        }
        else if (ManeuverType == "roundabout" || ManeuverType == "rotary")
        {
            emoji = "🔄";
            instruction = string.IsNullOrWhiteSpace(StreetName) ? "Im Kreisverkehr fahren" : $"Im Kreisverkehr auf {StreetName} fahren";
        }
        else if (ManeuverModifier.Contains("left"))
        {
            emoji = ManeuverModifier == "slight left" ? "↖️" : ManeuverModifier == "sharp left" ? "↩️" : "⬅️";
            instruction = string.IsNullOrWhiteSpace(StreetName) ? "Links abbiegen" : $"Links abbiegen auf {StreetName}";
        }
        else if (ManeuverModifier.Contains("right"))
        {
            emoji = ManeuverModifier == "slight right" ? "↗️" : ManeuverModifier == "sharp right" ? "↪️" : "➡️";
            instruction = string.IsNullOrWhiteSpace(StreetName) ? "Rechts abbiegen" : $"Rechts abbiegen auf {StreetName}";
        }
        else if (ManeuverModifier == "uturn")
        {
            emoji = "⤵️";
            instruction = "Bitte wenden";
        }

        return (instruction, emoji);
    }
}
