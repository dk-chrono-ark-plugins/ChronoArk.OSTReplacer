namespace Ost.Api;

internal interface ITrack
{
    int Order { get; }
    string Name { get; }
    string[] Parts { get; }

    bool UserTrack { get; }
}