namespace DivarExtensionDemo.Models.Divar;

public sealed class PostResponse
{
    public string Token { get; init; } = null!;
    public string Category { get; init; } = null!;
    public string City { get; init; } = null!;
    public string District { get; init; } = null!;
    public bool ChatEnabled { get; init; }
    public bool SupplierChatAssistantEnabled { get; init; }

    public class Data
    {
        public string BrandModel { get; init; } = null!;
        public string Color { get; init; } = null!;
        public string Description { get; init; } = null!;
        public int ExpireDays { get; init; }
        public List<string> Images { get; init; } = null!;
        public string InternalStorage { get; init; } = null!;
        public int NewPrice { get; init; }
        public string Originality { get; init; } = null!;
        public string PrefilledTitle { get; init; } = null!;

        public class Price
        {
            public string Mode { get; init; } = null!;
            public int Value { get; init; }
        }

        public string RamMemory { get; init; } = null!;
        public string SimCardSlot { get; init; } = null!;
        public string Status { get; init; } = null!;
        public string Title { get; init; } = null!;
    }

    public class BusinessData
    {
        public string BusinessType { get; init; } = null!;
        public string BusinessName { get; init; } = null!;
    }
}