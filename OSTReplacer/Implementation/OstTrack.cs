using Ost.Api;

namespace Ost.Implementation;

internal class OstTrack : ITrack
{
    public int Order { get; init; }
    public required string Name { get; init; }
    public required string[] Parts { get; init; }
    public required bool UserTrack { get; init; }
}