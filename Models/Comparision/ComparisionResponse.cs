namespace DivarExtensionDemo.Models.Comparision;

public record ComparisionResponse(
    string PositiveConclusion,
    string NegativeConclusion,
    List<ComparisionSoftwareResponse> Softwares,
    string Advice
);