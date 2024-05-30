using Ost.Api;
using Random = System.Random;

namespace Ost.Implementation;

internal class OstSwapRule : ISwapRule
{
    public required ITrack Target { get; init; }
    public required ISwapRule.SwapType Type { get; init; }
    public required ITrack[] Candidates { get; init; }

    public string[]? Apply()
    {
        var random = new Random();
        return Type switch {
            ISwapRule.SwapType.Remove => null,
            ISwapRule.SwapType.Replace => [..Candidates[0].Parts],
            ISwapRule.SwapType.InsertAhead => [..Candidates.SelectMany(c => c.Parts), ..Target.Parts],
            ISwapRule.SwapType.InsertAfter => [..Target.Parts, ..Candidates.SelectMany(c => c.Parts)],
            ISwapRule.SwapType.ReplaceSequential => Candidates.SelectMany(c => c.Parts).ToArray(),
            ISwapRule.SwapType.ReplaceRandom => Candidates.OrderBy(_ => random.Next()).SelectMany(c => c.Parts)
                .ToArray(),
            ISwapRule.SwapType.Reset => null,
            _ => throw new NotImplementedException(),
        };
    }
}