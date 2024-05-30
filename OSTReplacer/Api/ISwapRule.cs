namespace Ost.Api;

internal interface ISwapRule
{
    enum SwapType
    {
        // singular
        Remove,
        Replace,
        InsertAhead,
        InsertAfter,

        // playlist
        ReplaceSequential,
        ReplaceRandom,

        // ui only
        Reset,
    }

    ITrack Target { get; }
    SwapType Type { get; }
    ITrack[] Candidates { get; }

    string[]? Apply();
}