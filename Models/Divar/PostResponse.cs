namespace DivarExtensionDemo.Models.Divar;

public class PostResponse
{
    public string Token { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string City { get; set; } = null!;
    public string District { get; set; } = null!;
    public PostData Data { get; set; } = null!;
    public string State { get; set; } = null!;
    public DateTime FirstPublishedAt { get; set; }
    public bool ChatEnabled { get; set; }
    public BusinessData BusinessData { get; set; } = null!;
    public DateTime LastModifiedAt { get; set; }
}

public class PostData
{
    public bool HasWebcam { get; set; }
    public string Os { get; set; } = null!;
    public string ScreenResolution { get; set; } = null!;
    public Price Price { get; set; } = null!;
    public string RamMemory { get; set; } = null!;
    public int WarrantyRemainingMonths { get; set; }
    public string InternalStorage { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Color { get; set; } = null!;
    public bool IsNotNegotiable { get; set; }
    public bool Exchange { get; set; }
    public string ScreenSize { get; set; } = null!;
    public string ScreenRefreshRateHz { get; set; } = null!;
    public bool TouchScreen { get; set; }
    public List<string> Images { get; set; } = null!;
    public string Model { get; set; } = null!;
    public bool HasBox { get; set; }
    public bool HasBag { get; set; }
    public bool HasBacklitKeyboard { get; set; }
    public string Processor { get; set; } = null!;
    public string Status { get; set; } = null!;
    public bool OriginalOs { get; set; }
    public string Title { get; set; } = null!;
    public bool HasHdmiPort { get; set; }
    public string Brand { get; set; } = null!;
    public decimal NewPrice { get; set; }
    public string StorageType { get; set; } = null!;
    public bool HasOriginalCharger { get; set; }
}

public class Price
{
    public string Mode { get; set; } = null!;
    public decimal Value { get; set; }
}

public class BusinessData
{
    public string BusinessType { get; set; } = null!;
    public string BusinessName { get; set; } = null!;
}