using System.Windows.Media;

namespace DbdOverlay.Model;

public record class Perk
{
    public required string For { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required ImageSource Icon { get; init; }

    private PerkTag? tags;
    public PerkTag Tags => tags ??= GetTags();

    public override string ToString() => $"[{(For == "Killer" ? 'K' : 'S')}] {Title}";

    private PerkTag GetTags()
    {
        var tags = (PerkTag)0;

        if (Title.StartsWith("Hex", StringComparison.OrdinalIgnoreCase))
            tags |= PerkTag.Hex;
        if (Title.StartsWith("Boon", StringComparison.OrdinalIgnoreCase))
            tags |= PerkTag.Boon;
        if (Title.StartsWith("Scourge", StringComparison.OrdinalIgnoreCase))
            tags |= PerkTag.Scourge;

        if (Description.Contains("hook", StringComparison.OrdinalIgnoreCase))
            tags |= PerkTag.Hook;
        if (Description.Contains("heal", StringComparison.OrdinalIgnoreCase))
            tags |= PerkTag.Heal;
        if (Description.Contains("generator", StringComparison.OrdinalIgnoreCase) || Description.Contains("repair", StringComparison.OrdinalIgnoreCase))
            tags |= PerkTag.Generator;
        if (Description.Contains("healthy", StringComparison.OrdinalIgnoreCase))
            tags |= PerkTag.Healthy;
        if (Description.Contains("injure", StringComparison.OrdinalIgnoreCase))
            tags |= PerkTag.Injured;
        if (Description.Contains("dying state", StringComparison.OrdinalIgnoreCase))
            tags |= PerkTag.Dying;

        return tags;
    }
}

[Flags]
public enum PerkTag
{
    Hex = 1 << 0,
    Boon = 1 << 1,
    Hook = 1 << 2,
    Heal = 1 << 3,
    Generator = 1 << 4,
    Scourge = Hook | 1 << 5,
    Healthy = 1 << 6,
    Injured = 1 << 7,
    Dying = 1 << 8,
}