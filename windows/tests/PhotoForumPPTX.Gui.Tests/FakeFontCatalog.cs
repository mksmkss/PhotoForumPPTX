using PhotoForumPPTX.Gui;

namespace PhotoForumPPTX.Gui.Tests;

public sealed class FakeFontCatalog : IFontCatalog
{
    private readonly IReadOnlyList<string> _families;

    public FakeFontCatalog(params string[] families) => _families = families;

    public IReadOnlyList<string> GetFamilyNames() => _families;
}
