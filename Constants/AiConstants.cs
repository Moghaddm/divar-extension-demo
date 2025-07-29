namespace DivarExtensionDemo.Constants;

public static class AiConstants
{
    public const string DefaultCompletionModel = "gpt-4o-mini";

    public const string BaseComparisionPrompt =
        """
        I will provide you with information about a technological or digital product post from an Iranian e-commerce platform called Divar. The product can only be a laptop, PC, or mobile phone.Your task is to:Analyze the hardware specifications in the product post.
        Compare the given hardware against the system requirements of each game or software I provide.
        Return a comparison result that evaluates how well the device can run each item, represented as a percentage (0–100%), indicating performance capability, along with a status for each item.
        Your response must: Use Persian (Farsi) language for text fields, except for software/game names, with an informal and friendly tone to make the user feel comfortable and engaged.
        Be in the form of a JSON object that can be easily deserialized into the following C# POCO model:
        public sealed class ComparisionVm
        {
        public string PositiveConclusion { get; init; } = null!;
        public string NegativeConclusion { get; init; } = null!;
        public List<SoftwareItem> Items { get; init; } = null!;
        public string Advice { get; init; } = null!;
        }
        public sealed class SoftwareItem
        {
        public string Name { get; init; } = null!;
        public float Percentage { get; init; }
        public string Status { get; init; } = null!;
        }
        PositiveConclusion: Describe the benefits and strengths of the product for running the provided games/software, using a friendly and conversational tone, as if talking to a friend.
        NegativeConclusion: Describe potential risks, limitations, or issues of the product for running the provided games/software, keeping the tone informal and approachable.
        Items: A list of objects, each containing:Name: The game/software name (in English, exactly as I provide).
        Percentage: Compatibility percentage (0–100%).
        Status: A status in Persian, chosen from: "تقریباً اوکیه", "خوبه", "کاملاً آماده‌ست", "سازگار نیست".
        Advice: Provide suggestions for improving performance or compatibility (e.g., hardware upgrades or configuration changes), using the same friendly and informal tone.
        Example expected output (in JSON):
        {
        "PositiveConclusion": "این لپ‌تاپ برای ویرایش ویدئو و بازی‌های سبک حسابی به‌درد می‌خوره!",
        "NegativeConclusion": "ولی برای بازی‌های سنگین ممکنه یه کم لگ بندازه و اذیتت کنه.",
        "Items": [
        {
        "Name": "Premiere 2024",
        "Percentage": 70.0,
        "Status": "خوبه"
        },
        {
        "Name": "After Effects 2024",
        "Percentage": 60.0,
        "Status": "تقریباً اوکیه"
        },
        {
        "Name": "Red Dead Redemption 2",
        "Percentage": 30.0,
        "Status": "سازگار نیست"
        }
        ],
        "Advice": "برای اینکه حالشو ببری، یه رم 16 گیگ بنداز یا یه SSD بذار روش!"
        }
        Only reply with the JSON object and nothing else. Just the JSON without ``` or additional styling.Now, I will give you the product post and list of items to compare.
        """;
}