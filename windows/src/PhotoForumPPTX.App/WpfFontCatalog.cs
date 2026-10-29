using System.Windows.Media;
using PhotoForumPPTX.Gui;

namespace PhotoForumPPTX.App;

public sealed class WpfFontCatalog : IFontCatalog
{
    public IReadOnlyList<string> GetFamilyNames() =>
        Fonts.SystemFontFamilies
            .Select(f => f.Source)
            .Distinct()
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();
}
