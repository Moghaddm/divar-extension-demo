using DivarExtensionDemo.Enums;

namespace DivarExtensionDemo.Models.Comparision;

public record ComparisionSoftwareResponse(
    string Id,  
    float Percentage,
    ComparisionSoftwareStatus Status
);