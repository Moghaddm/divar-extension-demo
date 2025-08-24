namespace DivarExtensionDemo.Models.Comparision;

public record ComparisionDto(
    string PositiveConclusion,
    string NegativeConclusion,
    List<ComparisionSoftwareDto> Softwares,
    string Advice
);