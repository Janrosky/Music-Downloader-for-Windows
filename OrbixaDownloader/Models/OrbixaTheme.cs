using System.Drawing;

namespace OrbixaDownloader.Models;

public enum ThemePreset
{
    OrbixaMineral,
    CoralNocturno,
    TurquesaNocturno,
    CieloArtico,
    GrafitoSuave,
    AmbarNocturno,
    MineralTurquesa,
    MineralCoral,
    CieloCobalto,
    OlivaLimon,
    AmapolaAzul,
    ArcillaCielo,
    SystemHighContrast,
    Personalizado,
    ObsidianaAmbar,
    MarfilGrafito,
    BosqueCobre,
    AzulTintaMandarina,
    PizarraLima,
    OrbixaElevated,
    OrbixaQuiet,
    OrbixaNight,
    GraphiteCalm,
    OceanGlass,
    SunsetGlass
}

public enum UiLanguage
{
    Espanol,
    English
}

public enum CuratedColor
{
    Mineral,
    Turquesa,
    Coral,
    Cielo,
    Grafito
}

public sealed class CustomThemeSettings
{
    public CuratedColor Canvas { get; set; } = CuratedColor.Mineral;
    public CuratedColor Surface { get; set; } = CuratedColor.Mineral;
    public CuratedColor Field { get; set; } = CuratedColor.Mineral;
    public CuratedColor Primary { get; set; } = CuratedColor.Turquesa;
    public CuratedColor Secondary { get; set; } = CuratedColor.Coral;
    public CuratedColor Focus { get; set; } = CuratedColor.Turquesa;
}

public sealed record ThemePalette(
    Color Canvas,
    Color RaisedCanvas,
    Color Surface,
    Color SurfaceStrong,
    Color SurfaceSolid,
    Color Field,
    Color Text,
    Color Muted,
    Color Primary,
    Color PrimaryDark,
    Color Secondary,
    Color Focus,
    bool Opaque,
    Color Border,
    Color BorderHighlight,
    Color Success,
    Color Warning,
    Color Error)
{
    public ThemePalette(Color canvas, Color surface, Color field, Color text, Color muted, Color primary,
        Color secondary, Color focus, bool opaque, Color border, Color success, Color warning, Color error)
        : this(canvas, canvas, surface, surface, field, field, text, muted, primary, primary, secondary, focus,
            opaque, border, border, success, warning, error) { }

    public static ThemePalette SystemHighContrast => new(
        SystemColors.Window, SystemColors.Control, SystemColors.Control, SystemColors.Highlight,
        SystemColors.Control, SystemColors.Control, SystemColors.WindowText, SystemColors.GrayText, SystemColors.Highlight,
        SystemColors.Highlight, SystemColors.HotTrack, SystemColors.Highlight, true,
        SystemColors.WindowText, SystemColors.WindowText, SystemColors.Highlight,
        SystemColors.Highlight, SystemColors.Highlight);

    public bool HasReadableContrast()
        => ContrastRatio(Text, SurfaceSolid) >= 4.5
            && ContrastRatio(Muted, SurfaceSolid) >= 3
            && ContrastRatio(Text, Canvas) >= 4.5
            && ContrastRatio(Primary, SurfaceSolid) >= 3
            && ContrastRatio(Focus, SurfaceSolid) >= 3
            && ContrastRatio(Success, SurfaceSolid) >= 3
            && ContrastRatio(Warning, SurfaceSolid) >= 3
            && ContrastRatio(Error, SurfaceSolid) >= 3;

    public bool HasReadableContrastForLargeText()
        => ContrastRatio(Text, SurfaceSolid) >= 3
            && ContrastRatio(Text, Canvas) >= 3
            && ContrastRatio(Muted, SurfaceSolid) >= 3;

    public bool HasReadableInteractionContrast()
        => ContrastRatio(Text, SurfaceSolid) >= 4.5
            && Math.Max(ContrastRatio(Text, InteractionSurface(Primary, SurfaceSolid, 0.10)), ContrastRatio(Canvas, InteractionSurface(Primary, SurfaceSolid, 0.10))) >= 4.5
            && Math.Max(ContrastRatio(Text, InteractionSurface(Primary, SurfaceSolid, 0.16)), ContrastRatio(Canvas, InteractionSurface(Primary, SurfaceSolid, 0.16))) >= 4.5;

    private static Color InteractionSurface(Color accent, Color surface, double amount)
        => Blend(surface, accent, amount);

    public static Color Blend(Color baseColor, Color overlay, double opacity)
        => Color.FromArgb(
            (int)Math.Round(baseColor.R + (overlay.R - baseColor.R) * opacity),
            (int)Math.Round(baseColor.G + (overlay.G - baseColor.G) * opacity),
            (int)Math.Round(baseColor.B + (overlay.B - baseColor.B) * opacity));

    public static Color Composite(Color background, Color overlay, byte alpha)
        => Blend(background, overlay, alpha / 255d);

    public static double ContrastRatio(Color foreground, Color background)
    {
        static double Channel(byte value)
        {
            double linear = value / 255d;
            return linear <= 0.03928 ? linear / 12.92 : Math.Pow((linear + 0.055) / 1.055, 2.4);
        }

        double foregroundLuminance = 0.2126 * Channel(foreground.R) + 0.7152 * Channel(foreground.G) + 0.0722 * Channel(foreground.B);
        double backgroundLuminance = 0.2126 * Channel(background.R) + 0.7152 * Channel(background.G) + 0.0722 * Channel(background.B);
        return (Math.Max(foregroundLuminance, backgroundLuminance) + 0.05) /
            (Math.Min(foregroundLuminance, backgroundLuminance) + 0.05);
    }
}

public static class OrbixaThemes
{
    private static readonly Color BaseCanvas = Color.FromArgb(16, 23, 25);
    private static readonly Color RaisedCanvas = Color.FromArgb(24, 35, 38);
    private static readonly Color SurfaceSolid = Color.FromArgb(32, 44, 46);
    private static readonly Color TextColor = Color.FromArgb(243, 247, 244);
    private static readonly Color MutedColor = Color.FromArgb(169, 187, 183);
    private static readonly Color PrimaryColor = Color.FromArgb(255, 128, 104);
    private static readonly Color PrimaryDarkColor = Color.FromArgb(216, 95, 80);
    private static readonly Color PositiveColor = Color.FromArgb(147, 214, 176);
    private static readonly Color FocusColor = Color.FromArgb(184, 233, 255);

    public static ThemePalette Resolve(ThemePreset preset, CustomThemeSettings? custom = null)
    {
        if (SystemInformation.HighContrast || preset == ThemePreset.SystemHighContrast)
            return ThemePalette.SystemHighContrast;

        if (preset == ThemePreset.OrbixaElevated)
            return CreatePalette(RaisedCanvas, SurfaceSolid, PrimaryColor, FocusColor);
        if (preset == ThemePreset.OrbixaQuiet)
            return CreatePalette(BaseCanvas, Color.FromArgb(28, 38, 40), PrimaryColor, FocusColor);
        if (preset == ThemePreset.Personalizado)
            return ResolveCustomFamily(custom ?? new CustomThemeSettings());

        return preset switch
        {
            ThemePreset.OrbixaNight => CreatePalette(
                Color.FromArgb(10, 17, 20), Color.FromArgb(24, 33, 37), PrimaryColor, FocusColor),
            ThemePreset.CoralNocturno or ThemePreset.SunsetGlass => CreatePalette(
                Color.FromArgb(27, 20, 22), Color.FromArgb(46, 35, 38), PrimaryColor, FocusColor),
            ThemePreset.TurquesaNocturno or ThemePreset.OceanGlass => CreatePalette(
                Color.FromArgb(12, 28, 29), Color.FromArgb(27, 48, 48), Color.FromArgb(121, 214, 204), FocusColor),
            ThemePreset.CieloArtico or ThemePreset.CieloCobalto => CreatePalette(
                Color.FromArgb(15, 25, 36), Color.FromArgb(31, 46, 59), FocusColor, PrimaryColor),
            ThemePreset.GrafitoSuave or ThemePreset.GraphiteCalm => CreatePalette(
                Color.FromArgb(24, 26, 27), Color.FromArgb(43, 47, 48), Color.FromArgb(255, 155, 133), FocusColor),
            ThemePreset.AmbarNocturno or ThemePreset.ObsidianaAmbar => CreatePalette(
                Color.FromArgb(29, 24, 17), Color.FromArgb(49, 40, 27), Color.FromArgb(255, 191, 104), FocusColor),
            ThemePreset.MarfilGrafito => CreatePalette(
                Color.FromArgb(22, 27, 27), Color.FromArgb(38, 47, 47), Color.FromArgb(143, 220, 205), FocusColor),
            ThemePreset.BosqueCobre => CreatePalette(
                Color.FromArgb(15, 28, 23), Color.FromArgb(28, 48, 40), Color.FromArgb(147, 214, 176), Color.FromArgb(255, 177, 126)),
            ThemePreset.AzulTintaMandarina => CreatePalette(
                Color.FromArgb(14, 25, 33), Color.FromArgb(28, 45, 56), Color.FromArgb(139, 207, 255), Color.FromArgb(255, 166, 125)),
            ThemePreset.PizarraLima => CreatePalette(
                Color.FromArgb(24, 29, 25), Color.FromArgb(41, 50, 43), Color.FromArgb(190, 224, 126), FocusColor),
            ThemePreset.OlivaLimon => CreatePalette(
                Color.FromArgb(25, 29, 18), Color.FromArgb(43, 49, 28), Color.FromArgb(190, 224, 126), FocusColor),
            ThemePreset.AmapolaAzul => CreatePalette(
                Color.FromArgb(27, 21, 31), Color.FromArgb(45, 35, 51), Color.FromArgb(203, 156, 255), PrimaryColor),
            ThemePreset.ArcillaCielo => CreatePalette(
                Color.FromArgb(30, 23, 20), Color.FromArgb(49, 38, 32), Color.FromArgb(255, 168, 135), FocusColor),
            _ => CreatePalette(BaseCanvas, SurfaceSolid, PrimaryColor, FocusColor)
        };

        #if false
        return preset switch
        {
            ThemePreset.MineralTurquesa => new(
                Color.FromArgb(235, 247, 245), Color.FromArgb(249, 253, 252), Color.FromArgb(222, 241, 238),
                Color.FromArgb(24, 48, 51), Color.FromArgb(72, 103, 104), Color.FromArgb(0, 141, 145),
                Color.FromArgb(235, 105, 92), Color.FromArgb(0, 112, 116), false, Color.FromArgb(181, 205, 201),
                Color.FromArgb(33, 122, 75), Color.FromArgb(138, 90, 0), Color.FromArgb(163, 59, 50)),
            ThemePreset.MineralCoral => new(
                Color.FromArgb(250, 243, 240), Color.FromArgb(255, 251, 249), Color.FromArgb(244, 229, 224),
                Color.FromArgb(57, 39, 39), Color.FromArgb(111, 81, 78), Color.FromArgb(211, 88, 76),
                Color.FromArgb(0, 141, 145), Color.FromArgb(168, 62, 53), false, Color.FromArgb(210, 195, 190),
                Color.FromArgb(33, 122, 75), Color.FromArgb(138, 90, 0), Color.FromArgb(163, 59, 50)),
            ThemePreset.CieloCobalto => new(
                Color.FromArgb(237, 244, 252), Color.FromArgb(252, 254, 255), Color.FromArgb(222, 234, 248),
                Color.FromArgb(24, 42, 64), Color.FromArgb(76, 99, 124), Color.FromArgb(30, 92, 154),
                Color.FromArgb(217, 112, 65), Color.FromArgb(20, 70, 125), false, Color.FromArgb(190, 205, 220),
                Color.FromArgb(33, 122, 75), Color.FromArgb(138, 90, 0), Color.FromArgb(163, 59, 50)),
            ThemePreset.OlivaLimon => new(
                Color.FromArgb(243, 247, 232), Color.FromArgb(253, 254, 248), Color.FromArgb(229, 237, 204),
                Color.FromArgb(43, 53, 27), Color.FromArgb(91, 103, 68), Color.FromArgb(91, 116, 35),
                Color.FromArgb(190, 91, 47), Color.FromArgb(67, 88, 22), false, Color.FromArgb(205, 215, 181),
                Color.FromArgb(33, 122, 75), Color.FromArgb(138, 90, 0), Color.FromArgb(163, 59, 50)),
            ThemePreset.AmapolaAzul => new(
                Color.FromArgb(241, 239, 249), Color.FromArgb(254, 253, 255), Color.FromArgb(231, 228, 244),
                Color.FromArgb(42, 36, 62), Color.FromArgb(94, 87, 116), Color.FromArgb(80, 71, 148),
                Color.FromArgb(205, 67, 72), Color.FromArgb(57, 50, 113), false, Color.FromArgb(203, 198, 219),
                Color.FromArgb(33, 122, 75), Color.FromArgb(138, 90, 0), Color.FromArgb(163, 59, 50)),
            ThemePreset.ArcillaCielo => new(
                Color.FromArgb(250, 240, 232), Color.FromArgb(255, 252, 249), Color.FromArgb(241, 222, 208),
                Color.FromArgb(62, 47, 41), Color.FromArgb(116, 91, 80), Color.FromArgb(157, 78, 55),
                Color.FromArgb(38, 112, 149), Color.FromArgb(119, 55, 39), false, Color.FromArgb(217, 199, 188),
                Color.FromArgb(33, 122, 75), Color.FromArgb(138, 90, 0), Color.FromArgb(163, 59, 50)),
            ThemePreset.Personalizado => ResolveCustom(custom ?? new CustomThemeSettings()),
            ThemePreset.ObsidianaAmbar => new(
                Color.FromArgb(22, 26, 29), Color.FromArgb(32, 38, 42), Color.FromArgb(48, 56, 59),
                Color.FromArgb(247, 249, 247), Color.FromArgb(199, 209, 208), Color.FromArgb(240, 169, 75),
                Color.FromArgb(103, 197, 192), Color.FromArgb(255, 209, 102), false, Color.FromArgb(81, 94, 96),
                Color.FromArgb(117, 214, 155), Color.FromArgb(240, 179, 90), Color.FromArgb(255, 125, 125)),
            ThemePreset.MarfilGrafito => new(
                Color.FromArgb(244, 241, 234), Color.FromArgb(255, 253, 248), Color.FromArgb(238, 235, 227),
                Color.FromArgb(28, 39, 38), Color.FromArgb(77, 91, 88), Color.FromArgb(0, 107, 104),
                Color.FromArgb(182, 95, 60), Color.FromArgb(0, 90, 128), false, Color.FromArgb(190, 187, 178),
                Color.FromArgb(33, 122, 75), Color.FromArgb(138, 90, 0), Color.FromArgb(163, 59, 50)),
            ThemePreset.BosqueCobre => new(
                Color.FromArgb(239, 244, 239), Color.FromArgb(250, 252, 248), Color.FromArgb(232, 240, 232),
                Color.FromArgb(27, 42, 34), Color.FromArgb(76, 94, 82), Color.FromArgb(35, 107, 82),
                Color.FromArgb(182, 106, 61), Color.FromArgb(27, 85, 113), false, Color.FromArgb(185, 197, 187),
                Color.FromArgb(35, 122, 75), Color.FromArgb(138, 90, 0), Color.FromArgb(163, 61, 56)),
            ThemePreset.AzulTintaMandarina => new(
                Color.FromArgb(239, 243, 247), Color.FromArgb(251, 252, 254), Color.FromArgb(234, 240, 245),
                Color.FromArgb(25, 40, 50), Color.FromArgb(76, 92, 105), Color.FromArgb(20, 92, 130),
                Color.FromArgb(194, 97, 61), Color.FromArgb(14, 70, 104), false, Color.FromArgb(184, 196, 205),
                Color.FromArgb(40, 115, 77), Color.FromArgb(139, 92, 0), Color.FromArgb(168, 62, 56)),
            ThemePreset.PizarraLima => new(
                Color.FromArgb(32, 37, 34), Color.FromArgb(43, 50, 46), Color.FromArgb(59, 68, 62),
                Color.FromArgb(246, 249, 239), Color.FromArgb(198, 210, 196), Color.FromArgb(184, 217, 107),
                Color.FromArgb(117, 199, 190), Color.FromArgb(212, 237, 138), false, Color.FromArgb(83, 98, 88),
                Color.FromArgb(131, 211, 155), Color.FromArgb(224, 180, 92), Color.FromArgb(255, 137, 128)),
            _ => new(
                Color.FromArgb(239, 244, 242), Color.FromArgb(252, 254, 253), Color.FromArgb(225, 238, 235),
                Color.FromArgb(28, 45, 46), Color.FromArgb(78, 101, 101), Color.FromArgb(0, 137, 140),
                Color.FromArgb(218, 91, 79), Color.FromArgb(0, 105, 108), false, Color.FromArgb(183, 204, 199),
                Color.FromArgb(33, 122, 75), Color.FromArgb(138, 90, 0), Color.FromArgb(163, 59, 50))
        };
        #endif
    }

    private static ThemePalette CreatePalette(Color canvas, Color solid, Color primary, Color focus)
    {
        Color surface = ThemePalette.Composite(canvas, Color.FromArgb(239, 247, 244), 26);
        Color strong = ThemePalette.Composite(canvas, Color.FromArgb(239, 247, 244), 41);
        Color primaryDark = ThemePalette.Blend(primary, Color.Black, .18);
        Color secondary = ThemePalette.Blend(primary, Color.White, .18);
        return new(canvas, ThemePalette.Blend(canvas, Color.White, .08), surface, strong, solid, solid, TextColor, MutedColor, primary,
            primaryDark, secondary, focus, false,
            ThemePalette.Composite(canvas, Color.FromArgb(221, 242, 235), 46),
            Color.FromArgb(255, 255, 255, 87), PositiveColor, Color.FromArgb(255, 215, 157), primaryDark);
    }

    private static ThemePalette ResolveCustomFamily(CustomThemeSettings custom)
    {
        Color canvas = custom.Canvas == CuratedColor.Grafito ? Color.FromArgb(14, 20, 22) : BaseCanvas;
        Color solid = custom.Field == CuratedColor.Grafito ? Color.FromArgb(35, 47, 49) : SurfaceSolid;
        Color primary = custom.Primary == CuratedColor.Coral ? PrimaryColor : PositiveColor;
        return CreatePalette(canvas, solid, primary, FocusColor) with { Secondary = primary };
    }

    public static bool IsValid(ThemePreset preset, CustomThemeSettings? custom = null)
    {
        if (custom != null && new[] { custom.Canvas, custom.Surface, custom.Field, custom.Primary, custom.Secondary, custom.Focus }.Any(color => !Enum.IsDefined(typeof(CuratedColor), color)))
            return false;
        var palette = Resolve(preset, custom);
        return palette.HasReadableContrast() && palette.HasReadableContrastForLargeText() && palette.HasReadableInteractionContrast();
    }

    private static ThemePalette ResolveCustom(CustomThemeSettings custom)
    {
        Color canvas = ColorFor(custom.Canvas, ColorRole.Canvas);
        Color surface = ColorFor(custom.Surface, ColorRole.Surface);
        Color field = ColorFor(custom.Field, ColorRole.Field);
        Color primary = ColorFor(custom.Primary, ColorRole.Primary);
        Color secondary = ColorFor(custom.Secondary, ColorRole.Secondary);
        Color focus = ColorFor(custom.Focus, ColorRole.Focus);
        return new(canvas, surface, field, Color.FromArgb(28, 45, 46), Color.FromArgb(78, 101, 101), primary, secondary, focus,
            false, Color.FromArgb(176, 191, 188), Color.FromArgb(33, 122, 75), Color.FromArgb(138, 90, 0), Color.FromArgb(163, 59, 50));
    }

    private enum ColorRole { Canvas, Surface, Field, Primary, Secondary, Focus }

    private static Color ColorFor(CuratedColor color, ColorRole role) => role switch
    {
        ColorRole.Canvas => color switch
        {
            CuratedColor.Turquesa => Color.FromArgb(235, 247, 245),
            CuratedColor.Coral => Color.FromArgb(250, 243, 240),
            CuratedColor.Cielo => Color.FromArgb(235, 243, 250),
            CuratedColor.Grafito => Color.FromArgb(232, 237, 237),
            _ => Color.FromArgb(239, 244, 242)
        },
        ColorRole.Surface => color switch
        {
            CuratedColor.Turquesa => Color.FromArgb(249, 253, 252),
            CuratedColor.Coral => Color.FromArgb(255, 251, 249),
            CuratedColor.Cielo => Color.FromArgb(250, 253, 255),
            CuratedColor.Grafito => Color.FromArgb(245, 248, 248),
            _ => Color.FromArgb(252, 254, 253)
        },
        ColorRole.Field => color switch
        {
            CuratedColor.Turquesa => Color.FromArgb(222, 241, 238),
            CuratedColor.Coral => Color.FromArgb(244, 229, 224),
            CuratedColor.Cielo => Color.FromArgb(222, 237, 248),
            CuratedColor.Grafito => Color.FromArgb(218, 226, 226),
            _ => Color.FromArgb(225, 238, 235)
        },
        ColorRole.Primary => color switch
        {
            CuratedColor.Coral => Color.FromArgb(211, 88, 76),
            CuratedColor.Cielo => Color.FromArgb(0, 105, 168),
            CuratedColor.Grafito => Color.FromArgb(45, 78, 78),
            _ => Color.FromArgb(0, 137, 140)
        },
        ColorRole.Secondary => color switch
        {
            CuratedColor.Turquesa => Color.FromArgb(0, 112, 116),
            CuratedColor.Cielo => Color.FromArgb(42, 91, 128),
            CuratedColor.Grafito => Color.FromArgb(91, 64, 64),
            _ => Color.FromArgb(218, 91, 79)
        },
        _ => color switch
        {
            CuratedColor.Coral => Color.FromArgb(168, 62, 53),
            CuratedColor.Cielo => Color.FromArgb(0, 82, 132),
            CuratedColor.Grafito => Color.FromArgb(35, 67, 67),
            _ => Color.FromArgb(0, 105, 108)
        }
    };
}