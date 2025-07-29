namespace DivarExtensionDemo.Models.Comparision;

public sealed class ComparisionVm
{
    public string PositiveConclusion { get; init; } = null!;
    public string NegativeConclusion { get; init; } = null!;
    public List<SoftwareItem> Items { get; init; } = null!;
    public string Advice { get; init; } = null!;
}