namespace DivarExtensionDemo.Models.Comparision;

public sealed class ComparisionVm
{
    public string Text { get; init; } = null!;
    public Dictionary<string, float> Items { get; init; } = null!;
}