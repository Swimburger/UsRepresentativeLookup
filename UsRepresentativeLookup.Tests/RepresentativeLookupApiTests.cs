using Google;
using Microsoft.Extensions.Configuration;
using UsRepresentativeLookup.Api;

namespace UsRepresentativeLookup.Tests;

public class RepresentativeLookupApiTests
{
    [Fact]
    public async Task Client_Should_Return_Representative()
    {
        var address = "11414 Washington Plaza W, Reston, VA 20190";
        var representativeLookupClient = CreateRepresentativeLookupClient();
        var representative = await representativeLookupClient.GetRepresentativeByAddress(address);

        Assert.NotNull(representative);
        Assert.Equal("Virginia's 11th congressional district", representative.DistrictName);
        Assert.Equal("Gerald E. \"Gerry\" Connolly", representative.RepresentativeName);
        Assert.Equal("Democratic Party", representative.Party);
        Assert.Equal("http://bioguide.congress.gov/bioguide/photo/C/C001078.jpg", representative.PhotoUrl);
    }

    [Fact]
    public async Task Client_Should_Throw_RepresentativeNotFoundException()
    {
        var address = "Reston, VA 20190";
        var representativeLookupClient = CreateRepresentativeLookupClient();
        await Assert.ThrowsAsync<RepresentativeNotFoundException>(async () =>
            await representativeLookupClient.GetRepresentativeByAddress(address));
    }

    [Fact]
    public async Task Client_Should_Throw_FailedToParseAddressException()
    {
        var address = "Hi";
        var representativeLookupClient = CreateRepresentativeLookupClient();
        await Assert.ThrowsAsync<FailedToParseAddressException>(async () =>
            await representativeLookupClient.GetRepresentativeByAddress(address));
    }

    private RepresentativeLookupClient CreateRepresentativeLookupClient()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<RepresentativeLookupApiTests>()
            .AddEnvironmentVariables()
            .Build();
        var gcpApiKey = configuration["GcpApiKey"] ?? throw new Exception("GcpApiKey is not configured.");
        return new RepresentativeLookupClient(gcpApiKey);
    }
}