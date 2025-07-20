namespace DivarExtensionDemo.Constants;

public static class AiConstants
{
    public const string DefaultCompletionModel = "gpt-4o-mini";

    public const string BaseComparisionPrompt =
        """
        I will provide you with information about a technological or digital product post from an Iranian e-commerce platform called Divar. The product can only be a laptop, PC, or mobile phone.
        Your task is to:
        1. Analyze the hardware specifications in the product post.
        2. Compare the given hardware against the system requirements of each game or software I provide.
        3. Return a comparison result that shows how well the device can run each item. Represent this as a percentage (0–100%), indicating performance capability. For example: ""Premiere 2024"": 65%
        Your response must be:
        - In Persian (Farsi) language.
        - In the form of a JSON object that can be easily deserialized into the following C# POCO model:
        public sealed class ComparisionResponse
        {
            public string Text { get; init; } = null!;
            public Dictionary<string, float> Items { get; init; } = null!;
        }
        Use `Text` to provide a short summary in Persian, such as whether the system is generally good or weak for games/software.
        Use `Items` to list each game/software name (in English, exactly as I provide them) and its compatibility percentage.
        Example expected output (in JSON):
        {
          ""Text"": ""این گوشی برای اجرای نرم‌افزارهای سنگین مناسب نیست."",
          ""Items"": {
            ""Premiere 2024"": 30.0,
            ""After Effects 2024"": 20.0,
            ""Red Dead Redemption 2"": 10.0
          }
        }
        Only reply with the JSON object and nothing else. Just the json without ``` like things or code additional styling.
        Now, I will give you the product post and list of items to compare.
        """;
}