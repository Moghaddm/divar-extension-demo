using DivarExtensionDemo.Enums;

namespace DivarExtensionDemo.Models.Comparision;

public record ComparisionSoftwareDto(
    string Name,    
    float Percentage,
    ComparisionSoftwareStatus Status
);