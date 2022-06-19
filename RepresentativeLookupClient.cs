using System.Net;
using Google;
using Google.Apis.CivicInfo.v2;
using Google.Apis.CivicInfo.v2.Data;
using Google.Apis.Services;
using static Google.Apis.CivicInfo.v2.RepresentativesResource;

namespace RepresentativeBot;

public class RepresentativeLookupClient
{
    private readonly CivicInfoService service;

    public RepresentativeLookupClient(string gcpApiKey)
    {
        service = new CivicInfoService(new BaseClientService.Initializer
        {
            ApplicationName = "US Representative Lookup",
            ApiKey = gcpApiKey
        });
    }

    public async Task<Representative> GetRepresentativeByAddress(string address)
    {
        var request = new RepresentativeInfoByAddressRequest(service)
        {
            Address = address,
            // Level = Country and Roles = LegislatorLowerBody filters down to U.S. Representatives
            Levels = RepresentativeInfoByAddressRequest.LevelsEnum.Country,
            Roles = RepresentativeInfoByAddressRequest.RolesEnum.LegislatorLowerBody
        };

        RepresentativeInfoResponse response;
        try
        {
            response = await request.ExecuteAsync();
        }
        catch (GoogleApiException e) when (e.HttpStatusCode == HttpStatusCode.BadRequest &&
                                           e.Error.Message == "Failed to parse address")
        {
            throw new FailedToParseAddressException(e);
        }

        // if the address did not resolve to a specific district,
        // for example, no district, or multiple districts
        // then response.Offices will be null
        if (response.Offices == null)
        {
            throw new RepresentativeNotFoundException();
        }

        // only one office, one division, and one official will be returned
        // the office of U.S. representative, the congressional district, and the elected official
        var office = response.Offices[0];
        var division = response.Divisions[office.DivisionId];
        var official = response.Officials[0];
        return new Representative
        {
            DistrictName = division.Name,
            RepresentativeName = official.Name,
            Party = official.Party,
            PhotoUrl = official.PhotoUrl
        };
    }
}
