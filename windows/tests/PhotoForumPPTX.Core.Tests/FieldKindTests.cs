using PhotoForumPPTX.Core;

namespace PhotoForumPPTX.Core.Tests;

public class FieldKindTests
{
    [Theory]
    [InlineData(FieldKind.StudentId, 0)]
    [InlineData(FieldKind.Name, 0)]
    [InlineData(FieldKind.Title, 0)]
    [InlineData(FieldKind.Camera, 1)]
    [InlineData(FieldKind.Lens, 1)]
    [InlineData(FieldKind.Focal, 3)]
    [InlineData(FieldKind.Shutter, 2)]
    [InlineData(FieldKind.Aperture, 2)]
    [InlineData(FieldKind.Iso, 2)]
    [InlineData(FieldKind.Note, 2)]
    public void Group_MatchesTemplateLayout(FieldKind kind, int expected)
    {
        Assert.Equal(expected, kind.Group());
    }

    [Theory]
    [InlineData(FieldKind.StudentId, 10.5)]
    [InlineData(FieldKind.Title, 11)]
    [InlineData(FieldKind.Camera, 8)]
    [InlineData(FieldKind.Focal, 20)]
    [InlineData(FieldKind.Note, 7.5)]
    public void FontSize_MatchesTemplate(FieldKind kind, double expected)
    {
        Assert.Equal(expected, kind.FontSize());
    }
}
