using Argos.Common.Configuration;

namespace Argos.Business.Services.Privacy;

public sealed class IpPseudonymizer(IPrivacyConfig config) : IIpPseudonymizer
{
    public IpPseudonymizationSession Begin() => new(config.PseudonymizeIps);
}
