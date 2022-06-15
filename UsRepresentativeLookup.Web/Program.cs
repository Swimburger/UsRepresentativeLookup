using Twilio.AspNet.Core.MinimalApi;
using Twilio.TwiML;
using Twilio.TwiML.Messaging;
using UsRepresentativeLookup.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var gcpApiKey = configuration["GcpApiKey"] ?? throw new Exception("GcpApiKey is not configured.");
    return new RepresentativeLookupClient(gcpApiKey);
});

var app = builder.Build();

app.MapPost("/message", async (
    HttpRequest request,
    HttpResponse response,
    IServiceProvider serviceProvider,
    ILogger<Program> logger
) =>
{
    var messagingResponse = new MessagingResponse();

    if (bool.Parse(request.Cookies["HasBeenGreeted"] ?? "False") == false)
    {
        response.Cookies.Append("HasBeenGreeted", "True");
        messagingResponse.Message("Welcome to U.S. Representative lookup bot. Respond with your address.");
        return Results.Extensions.TwiML(messagingResponse);
    }

    var form = await request.ReadFormAsync().ConfigureAwait(false);
    var body = form["Body"][0];

    var representativeLookupClient = serviceProvider.GetRequiredService<RepresentativeLookupClient>();
    try
    {
        var representative = await representativeLookupClient.GetRepresentativeByAddress(body);
        messagingResponse.Message(
            $"Your representative is {representative.RepresentativeName} ({representative.Party})" +
            $", representing {representative.DistrictName}."
        );

        if (representative.PhotoUrl is not null)
        {
            messagingResponse.Append(new Message().Media(new Uri(representative.PhotoUrl)));
        }
    }
    catch (FailedToParseAddressException)
    {
        messagingResponse.Message("The address you entered is invalid.");
    }
    catch (RepresentativeNotFoundException)
    {
        messagingResponse.Message("Your representative could not be determined. " +
                                  "This may be because there's no representative or multiple representatives for the given location. " +
                                  "Try entering a more specific address.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An unexpected error occured when looking up representative");
        messagingResponse.Message("An unexpected error occured.");
    }

    return Results.Extensions.TwiML(messagingResponse);
});

app.Run();