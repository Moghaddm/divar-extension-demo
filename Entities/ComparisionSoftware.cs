using DivarExtensionDemo.Enums;

namespace DivarExtensionDemo.Entities;

public sealed class ComparisionSoftware
{
    public string SoftwareId { get; init; } = null!;
    public float Percentage { get; init; }
    public ComparisionSoftwareStatus Status { get; init; }
}