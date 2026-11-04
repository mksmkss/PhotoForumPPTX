namespace PhotoForumPPTX.Core;

public static class DefaultTables
{
    public static readonly IReadOnlyList<MapEntry> Cameras = new[]
    {
        ("ILCE-1", "α1"), ("ILCE-1M2", "α1 II"),
        ("ILCE-9", "α9"), ("ILCE-9M2", "α9 II"), ("ILCE-9M3", "α9 III"),
        ("ILCE-7", "α7"), ("ILCE-7M2", "α7 II"), ("ILCE-7M3", "α7 III"), ("ILCE-7M4", "α7 IV"), ("ILCE-7M5", "α7 V"),
        ("ILCE-7R", "α7R"), ("ILCE-7RM2", "α7R II"), ("ILCE-7RM3", "α7R III"), ("ILCE-7RM3A", "α7R IIIA"),
        ("ILCE-7RM4", "α7R IV"), ("ILCE-7RM4A", "α7R IVA"), ("ILCE-7RM5", "α7R V"),
        ("ILCE-7S", "α7S"), ("ILCE-7SM2", "α7S II"), ("ILCE-7SM3", "α7S III"),
        ("ILCE-7C", "α7C"), ("ILCE-7CM2", "α7C II"), ("ILCE-7CR", "α7CR"),
        ("ILCE-6000", "α6000"), ("ILCE-6100", "α6100"), ("ILCE-6300", "α6300"), ("ILCE-6400", "α6400"),
        ("ILCE-6500", "α6500"), ("ILCE-6600", "α6600"), ("ILCE-6700", "α6700"),
        ("ILCE-5100", "α5100"), ("ILCE-3500", "α3500"),
        ("ZV-E10", "ZV-E10"), ("ZV-E10M2", "ZV-E10 II"), ("ZV-E1", "ZV-E1"), ("ZV-1", "ZV-1"), ("ZV-1M2", "ZV-1 II"),
        ("ILME-FX3", "FX3"), ("ILME-FX30", "FX30"),
        ("DSC-RX1R", "RX1R"), ("DSC-RX1RM2", "RX1R II"), ("DSC-RX1RM3", "RX1R III"),
        ("DSC-RX100", "RX100"), ("DSC-RX100M2", "RX100 II"), ("DSC-RX100M3", "RX100 III"),
        ("DSC-RX100M4", "RX100 IV"), ("DSC-RX100M5", "RX100 V"), ("DSC-RX100M5A", "RX100 VA"),
        ("DSC-RX100M6", "RX100 VI"), ("DSC-RX100M7", "RX100 VII"),
        ("DSC-RX10", "RX10"), ("DSC-RX10M2", "RX10 II"), ("DSC-RX10M3", "RX10 III"), ("DSC-RX10M4", "RX10 IV"),
        ("DSC-RX1", "RX1"), ("DSC-RX0", "RX0"), ("DSC-RX0M2", "RX0 II"),
        ("ILME-FX6", "FX6"), ("ILME-FX2", "FX2"), ("ILCE-6700", "α6700"), ("ILCE-7M3K", "α7 III"),
        ("ILCE-QX1", "QX1"), ("DSC-HX99", "HX99"),
    }.Select(t => new MapEntry(t.Item1, t.Item2)).ToArray();

    /// <summary>レンズはEXIFのLensModelをそのまま使うのが基本。名前を直したいものだけここに足す</summary>
    public static readonly IReadOnlyList<MapEntry> Lenses = new[]
    {
        ("35mm F2.0", "Sonnar T* 35mm f/2.0"), // RX1R III
    }.Select(t => new MapEntry(t.Item1, t.Item2)).ToArray();

    /// <summary>既定値を増やしたら上げる(既存ユーザーの表にも新しい既定値が入る)</summary>
    public const int Version = 3;
}
