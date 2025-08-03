namespace DivarExtensionDemo.Entities;

public sealed class Comparision
{
    public string Id { get; init; } = null!;
    public string PositiveConclusion { get; init; } = null!;
    public string NegativeConclusion { get; init; } = null!;
    public List<ComparisionSoftware> Softwares { get; init; } = null!;
    public string Advice { get; init; } = null!;
}