namespace JobMagnet.Web.Components.Shared;

// Pixel avatars are drawn from a 10×12 sprite: one base face, varied by hair, skin, shirt and accessories.
public static class AvatarCatalog
{
    public const int Width = 10;
    public const int Height = 12;

    public enum Hair { Short, Long, Bun, Bald, Cap, Curly }

    public record Avatar(string Name, Hair Hair, string Skin, string HairColor, string Shirt, bool Glasses);

    private const string Eye = "#1d1f2b";
    private const int EyeRow = 4;
    private const string Lens = "#2a2d3a";

    // Skin tones span the full range; shirts stay muted so a crowd of avatars doesn't turn into confetti.
    public static readonly IReadOnlyList<Avatar> All =
    [
        new("Ilze", Hair.Long, "#f2c8a5", "#c9a24b", "#2f5d7c", false),
        new("Jānis", Hair.Short, "#e9b48c", "#4a3020", "#7a2e3b", false),
        new("Amara", Hair.Curly, "#8a5a3c", "#1e1512", "#3f6b4f", false),
        new("Kenji", Hair.Short, "#f0c9a0", "#15151a", "#3b3f52", true),
        new("Olga", Hair.Bun, "#f5d0b5", "#8c4a2f", "#5b4a7a", false),
        new("Diego", Hair.Cap, "#c98e62", "#22222a", "#9a5b2a", false),
        new("Priya", Hair.Long, "#b77a52", "#20161a", "#8f2f4f", true),
        new("Marta", Hair.Short, "#f6d5bd", "#d9c39a", "#2e6e6a", false),
        new("Kwame", Hair.Bald, "#6b4430", "#000000", "#2f4a73", true),
        new("Lina", Hair.Bun, "#e2a982", "#2b1c14", "#6c7a3a", false),
        new("Artūrs", Hair.Curly, "#efc19c", "#6b4a2c", "#384050", false),
        new("Sofia", Hair.Long, "#d79b72", "#5a2f1f", "#2f6a8a", false),
        new("Tomas", Hair.Cap, "#f1c7a3", "#a8433a", "#3d3d3d", true),
        new("Ayaan", Hair.Short, "#a8714c", "#14110f", "#7b5a2a", false),
        new("Eva", Hair.Curly, "#f4cfb0", "#b5562f", "#4a5d8a", true),
        new("Viktor", Hair.Bald, "#e8b590", "#000000", "#5c2f2f", false)
    ];

    public static Avatar Get(int id) => All[Math.Clamp(id, 0, All.Count - 1)];

    public record Pixel(int X, int Y, string Color);

    public static IReadOnlyList<Pixel> Draw(Avatar avatar)
    {
        var mouth = Shade(avatar.Skin);
        var sprite = BaseSprite(avatar.Hair);
        var pixels = new List<Pixel>(Width * Height);

        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                var color = sprite[y][x] switch
                {
                    'h' => avatar.HairColor,
                    's' => avatar.Skin,
                    'e' => Eye,
                    'm' => mouth,
                    'c' => avatar.Shirt,
                    'k' => Shade(avatar.Shirt),
                    _ => null
                };

                if (color is not null)
                {
                    pixels.Add(new Pixel(x, y, color));
                }
            }
        }

        if (avatar.Glasses)
        {
            // Frames on the eye row: outer rims and the bridge between the eyes.
            pixels.AddRange([new(1, EyeRow, Lens), new(4, EyeRow, Lens), new(5, EyeRow, Lens), new(8, EyeRow, Lens)]);
        }

        return pixels;
    }

    // Rows 7-11 (chin, neck, shoulders) are shared; the hairstyle decides the top of the head.
    private static readonly string[] Face =
    [
        ".seessees.",
        ".ssssssss.",
        ".sssmmsss.",
        "..ssssss..",
        "...ssss...",
        ".kcccccck.",
        "kcccccccck",
        "cccccccccc"
    ];

    private static readonly string[] LongFace =
    [
        "hseesseesh",
        "hssssssssh",
        "hsssmmsssh",
        "hhsssssshh",
        "hh.ssss.hh",
        ".kcccccck.",
        "kcccccccck",
        "cccccccccc"
    ];

    private static string[] BaseSprite(Hair hair) => hair switch
    {
        Hair.Long => ["..hhhhhh..", ".hhhhhhhh.", "hhhhhhhhhh", "hhsssssshh", .. LongFace],
        Hair.Bun => ["...hhhh...", ".hhhhhhhh.", ".hhhhhhhh.", ".hssssssh.", .. Face],
        Hair.Bald => ["..........", "...ssss...", "..ssssss..", ".ssssssss.", .. Face],
        Hair.Cap => ["..hhhhhh..", ".hhhhhhhh.", ".hhhhhhhhh", ".ssssssss.", .. Face],
        Hair.Curly => [".h.hhhh.h.", "hhhhhhhhhh", "hhhhhhhhhh", "hhsssssshh", .. Face],
        _ => ["..........", "..hhhhhh..", ".hhhhhhhh.", ".hssssssh.", .. Face]
    };

    // Darkens a #rrggbb colour; used for mouths and shirt shading so every palette stays consistent.
    private static string Shade(string hex)
    {
        const double Factor = 0.78;
        var r = (int)(Convert.ToInt32(hex.Substring(1, 2), 16) * Factor);
        var g = (int)(Convert.ToInt32(hex.Substring(3, 2), 16) * Factor);
        var b = (int)(Convert.ToInt32(hex.Substring(5, 2), 16) * Factor);
        return $"#{r:x2}{g:x2}{b:x2}";
    }
}
