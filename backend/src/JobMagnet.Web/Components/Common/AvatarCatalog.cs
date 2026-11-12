namespace JobMagnet.Web.Components.Common;

// Sixteen illustrated people: one face shape, varied by hair, skin, shirt and accessories.
public static class AvatarCatalog
{
    public enum Hair { Cropped, Flowing, Bun, Bald, Cap, Curly }

    public record Avatar(string Name, Hair Hair, string Skin, string HairColor, string Shirt, bool Glasses);

    // Skin tones span the full range; shirts stay muted so a crowd of avatars doesn't turn into confetti.
    public static readonly IReadOnlyList<Avatar> All =
    [
        new("Ilze", Hair.Flowing, "#f2c8a5", "#c9a24b", "#2f5d7c", false),
        new("Jānis", Hair.Cropped, "#e9b48c", "#4a3020", "#7a2e3b", false),
        new("Amara", Hair.Curly, "#8a5a3c", "#1e1512", "#3f6b4f", false),
        new("Kenji", Hair.Cropped, "#f0c9a0", "#15151a", "#3b3f52", true),
        new("Olga", Hair.Bun, "#f5d0b5", "#8c4a2f", "#5b4a7a", false),
        new("Diego", Hair.Cap, "#c98e62", "#22222a", "#9a5b2a", false),
        new("Priya", Hair.Flowing, "#b77a52", "#20161a", "#8f2f4f", true),
        new("Marta", Hair.Cropped, "#f6d5bd", "#d9c39a", "#2e6e6a", false),
        new("Kwame", Hair.Bald, "#6b4430", "#000000", "#2f4a73", true),
        new("Lina", Hair.Bun, "#e2a982", "#2b1c14", "#6c7a3a", false),
        new("Artūrs", Hair.Curly, "#efc19c", "#6b4a2c", "#384050", false),
        new("Sofia", Hair.Flowing, "#d79b72", "#5a2f1f", "#2f6a8a", false),
        new("Tomas", Hair.Cap, "#f1c7a3", "#a8433a", "#3d3d3d", true),
        new("Ayaan", Hair.Cropped, "#a8714c", "#14110f", "#7b5a2a", false),
        new("Eva", Hair.Curly, "#f4cfb0", "#b5562f", "#4a5d8a", true),
        new("Viktor", Hair.Bald, "#e8b590", "#000000", "#5c2f2f", false)
    ];

    public static Avatar Get(int id) => All[Math.Clamp(id, 0, All.Count - 1)];

    // Darkens a #rrggbb colour; used for mouths and shirt shading so every palette stays consistent.
    public static string Shade(string hex)
    {
        const double Factor = 0.78;
        var r = (int)(Convert.ToInt32(hex.Substring(1, 2), 16) * Factor);
        var g = (int)(Convert.ToInt32(hex.Substring(3, 2), 16) * Factor);
        var b = (int)(Convert.ToInt32(hex.Substring(5, 2), 16) * Factor);
        return $"#{r:x2}{g:x2}{b:x2}";
    }
}
