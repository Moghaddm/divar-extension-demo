using System.Text.Encodings.Web;
using System.Text.Json;
using DivarExtensionDemo.Entities;
using DivarExtensionDemo.Enums;
using DivarExtensionDemo.Infrastructure.Divar.Models;
using DivarExtensionDemo.Models.Comparision;
using DivarExtensionDemo.Services.Interfaces;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using OpenAI;
using OpenAI.Chat;

namespace DivarExtensionDemo.Services;

public sealed class ComparisionService(
    IConfiguration configuration,
    IMongoDatabase mongoDatabase,
    OpenAIClient openAiClient,
    IHttpClientFactory httpClientFactory
) : IComparisionService
{
    private const string RetrievePostInformationUrl = "https://open-api.divar.ir/v1/open-platform/finder/post/";
    private const string RetrieveUserInformationUrl = "https://open-api.divar.ir/v1/open-platform/users";

    private readonly IMongoCollection<Software> _softwareCollection =
        mongoDatabase.GetCollection<Software>("Softwares");

    private readonly IMongoCollection<Comparision> _comparisionCollection =
        mongoDatabase.GetCollection<Comparision>(nameof(Comparision));

    public async Task<string> CreateAsync(string postToken, string[] softwareIds, string divarAccessToken,
        CancellationToken cancellationToken)
    {
        var postData = await SendRequestAsync(postToken, cancellationToken);

        var softwareEntities = await _softwareCollection
            .AsQueryable()
            .Where(s => softwareIds.Contains(s.Id))
            .Select(s => new { s.Id, s.Name })
            .ToListAsync(cancellationToken);

        if (softwareIds.Length is 0) throw new ArgumentException("Send expected software surely.");

        var softwares = string.Join('\n',
            softwareEntities.Select(s => $"Item Identifier(ID): {s.Id}, Name: {s.Name}").ToList());

        var basePrompt =
            $"""
             # System Prompt
             - You are a knowledgeable and friendly assistant helping users evaluate digital products (laptops, PCs, or mobile phones) listed on Divar, an Iranian e-commerce platform. 
             - Your task is to analyze the hardware specifications of a given product from a Divar post and compare them against the system requirements of specific software or games provided by the user.

             ## Instructions
             - I will provide details from a Divar post about a digital product. 
             - I will also provide a list of software or games along with their minimum and recommended system requirements (if available). If requirements are not provided, use your knowledge of typical requirements for those items.
             - Evaluate the product’s performance for each software/game by comparing its hardware specs to the minimum and recommended system requirements.
             - Assign a performance capability percentage (0–100%) for each software/game based on how well the hardware meets or exceeds the requirements:
                 0–30%: Hardware falls significantly below minimum requirements.
                 31–50%: Hardware meets or barely exceeds minimum requirements but is far from recommended specs.
                 51–70%: Hardware meets minimum requirements and partially meets recommended specs.
                 71–85%: Hardware meets or exceeds recommended specs with minor limitations.
                 86–100%: Hardware significantly exceeds recommended specs for optimal performance.
             - Assign a performance status in Persian for each software/game:
                0: NotAdaptable, 1: AlmostAdaptable, 2: Adaptable, 4: ReadyCompletely.
             - Put other field values like this:
                Software Id: The identifier of software which I will provide you besides to name of game.
                Positive Conclusion: Describe the benefits and strengths of the product for running the provided games/software, using a friendly and conversational tone, as if talking to a friend.
                Negative Conclusion: Describe potential risks, limitations, or issues of the product for running the provided games/software, keeping the tone informal and approachable.
                Advice: Provide suggestions for improving performance or compatibility (e.g., hardware upgrades or configuration changes), using the same friendly and informal tone.  
                
             ## Response Style
             - Use Persian (Farsi) for all text fields in your response, except for the names of software or games, which should remain in their original language (usually English).
             - Maintain an informal, friendly, and engaging tone to make the user feel comfortable, as if they are chatting with a tech-savvy friend offering helpful advice.

             ## Softwares
             List of softwares provided for doing comparision:
             {softwares}

             ## Post Data
             Divar post data for comparision:
             {postData}
             """;

        var obje = new ComparisionResponse("Good", "Bad",
        [
            new ComparisionSoftwareResponse("Id returned as software identifier.", 70, 0)
        ], "Mustbebetter");
        var serialized = JsonSerializer.Serialize(obje);
        /*var responseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
            jsonSchemaFormatName: typeof(ComparisionResponse).ToString(),
            jsonSchema: BinaryData.FromString(serialized)
        );*/

        var responseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
            jsonSchemaFormatName: "ComparisionResponse", // ✅ simple name, no namespace, no dots
            jsonSchema: BinaryData.FromString(@"
    {
      ""type"": ""object"",
      ""properties"": {
        ""PositiveConclusion"": { ""type"": ""string"" },
        ""NegativeConclusion"": { ""type"": ""string"" },
        ""Softwares"": {
          ""type"": ""array"",
          ""items"": {
            ""type"": ""object"",
            ""properties"": {
              ""Id"": { ""type"": ""string"" },
              ""Percentage"": { ""type"": ""integer"" },
              ""Status"": { ""type"": ""integer"" }
            },
            ""required"": [""Id"", ""Percentage"", ""Status""]
          }
        },
        ""Advice"": { ""type"": ""string"" }
      },
      ""required"": [""PositiveConclusion"", ""NegativeConclusion"", ""Softwares"", ""Advice""]
    }
    ")
        );


        var aiResponse = await openAiClient
            .GetChatClient("gpt-4o-mini")
            .CompleteChatAsync(
                messages: [basePrompt],
                options: new ChatCompletionOptions
                {
                    Temperature = 0f,
                    ResponseFormat = responseFormat
                },
                cancellationToken: cancellationToken
            );

        var comparisionResult = JsonSerializer.Deserialize<ComparisionResponse>(aiResponse.Value.Content[0].Text);
        if (comparisionResult is null) throw new Exception("LLM problem, call the support team or try again!");

        var comparision = new Comparision
        {
            Id = Guid.CreateVersion7().ToString(),
            PositiveConclusion = comparisionResult.PositiveConclusion,
            NegativeConclusion = comparisionResult.NegativeConclusion,
            Softwares = comparisionResult.Softwares.Select(s => new ComparisionSoftware
            {
                SoftwareId = s.Id,
                Status = s.Status,
                Percentage = s.Percentage
            }).ToList(),
            Advice = comparisionResult.Advice
        };
        await _comparisionCollection.InsertOneAsync(comparision, new InsertOneOptions(), cancellationToken);

        var divarApiKey = configuration.GetSection("Divar:Extension:ApiKey").Value!;
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, RetrieveUserInformationUrl);
        request.Headers.Add("Accept", "application/json");
        request.Headers.Add("X-API-Key", divarApiKey);
        request.Headers.Add("Authorization", $"Bearer {divarAccessToken}");
        var clientResponse = await client.SendAsync(request, cancellationToken);
        clientResponse.EnsureSuccessStatusCode();
        var responseAsJson = await clientResponse.Content.ReadAsStringAsync(cancellationToken);
        var response = JsonSerializer.Deserialize<UserInfoResponse>(responseAsJson, new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        });
        var phoneNumber = response!.PhoneNumber;

        Console.WriteLine(phoneNumber); // TODO: SEND COMPARISION RESULT SMS TO USER USING PHONE .

        return comparision.Id;
    }

    public async Task<ComparisionDto> GetAsync(string id, CancellationToken cancellationToken)
    {
        var softwares = await _softwareCollection.AsQueryable()
            .Select(s => new { s.Id, s.Name })
            .ToListAsync(cancellationToken);

        var comparision = await _comparisionCollection.AsQueryable()
            .Where(c => c.Id == id)
            .Select(c => new ComparisionDto(
                c.PositiveConclusion,
                c.NegativeConclusion,
                c.Softwares
                    .Select(si =>
                        new ComparisionSoftwareDto(
                            softwares.FirstOrDefault(s => s.Id == si.SoftwareId)!.Name,
                            si.Percentage,
                            si.Status
                        ))
                    .ToList(),
                c.Advice
            ))
            .SingleOrDefaultAsync(cancellationToken);

        return comparision ?? throw new Exception("Comparision not found!");
    }

    private async Task<string> SendRequestAsync(string postToken, CancellationToken cancellationToken)
    {
        var divarApiKey = configuration.GetSection("Divar:Extension:ApiKey").Value!;

        var client = httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, RetrievePostInformationUrl + postToken);

        request.Headers.Add("Accept", "application/json");
        request.Headers.Add("X-API-Key", divarApiKey);

        var clientResponse = await client.SendAsync(request, cancellationToken);
        clientResponse.EnsureSuccessStatusCode();

        var responseAsJson = await clientResponse.Content.ReadAsStringAsync(cancellationToken);

        return responseAsJson;
    }
}