using System.IO;
using Mcm.Common;
using Ost.Api;

namespace Ost.Implementation;

internal class OstSwapGenerator
{
    private static readonly Lazy<OstSwapGenerator> _instance = new(() => new());

    private readonly List<ISwapRule> _swaps = [];

    private OstSwapGenerator()
    {
    }

    public static OstSwapGenerator Instance => _instance.Value;

    private static string RuleCerealPath =>
        Path.Combine(Application.persistentDataPath, $@"Mod\Ost\{nameof(OstSwapGenerator)}.json");

    public void AddRule(ISwapRule swap)
    {
        if (swap.Target.Parts.Any(HasSwapForPart)) {
            Debug.Log($"a rule exists for {swap.Target.Name}, x-swapping not supported yet");
            return;
        }

        _swaps.Add(swap);
        SaveRules();
    }

    public void AddRuleFor(ITrack target, ISwapRule.SwapType type, ITrack[] candidates)
    {
        AddRule(new OstSwapRule {
            Target = target,
            Type = type,
            Candidates = candidates,
        });
    }

    public void RemoveRule(ISwapRule swap)
    {
        _swaps.Remove(swap);
        SaveRules();
    }

    public void RemoveRuleFor(ITrack target)
    {
        if (!target.Parts.Any(HasSwapForPart)) {
            return;
        }

        _swaps.RemoveAll(s => s.Target.Name == target.Name);
        SaveRules();
    }

    // inclusive
    public void RemoveRuleForPart(string trackPart)
    {
        if (!HasSwapForPart(trackPart)) {
            return;
        }

        _swaps.RemoveAll(s => s.Target.Parts.Contains(trackPart));
        SaveRules();
    }

    public bool HasSwapFor(ITrack target)
    {
        return target.Parts.Any(HasSwapForPart);
    }

    public bool HasSwapForPart(string trackPart)
    {
        return _swaps.Any(s => s.Target.Parts.Contains(trackPart));
    }

    public string[] GetSwapPlaylist(string trackPart)
    {
        return _swaps.FirstOrDefault(s => s.Target.Parts.Contains(trackPart))?.Apply() ?? [];
    }

    public string[] GetSwapPlaylist(ITrack target)
    {
        return _swaps.FirstOrDefault(s => s.Target.Name == target.Name)?.Apply() ?? [];
    }

    public ISwapRule? GetRuleFor(ITrack target)
    {
        return _swaps.FirstOrDefault(s => s.Target.Name == target.Name);
    }

    public void SaveRules()
    {
        InvalidateRules();
        var cereal = _swaps.Select(s => new SwapRuleCereal(
            s.Target as OstTrack,
            s.Type,
            s.Candidates.Select(c => c as OstTrack).ToArray()));
        ConfigCereal.WriteConfig(cereal, RuleCerealPath);
    }

    public void LoadRules()
    {
        try {
            ConfigCereal.ReadConfig(RuleCerealPath, out SwapRuleCereal[]? cereal);
            _swaps.Clear();
            cereal!.Select(c => new OstSwapRule {
                Target = c.Target!,
                Type = c.Type,
                Candidates = c.Candidates.OfType<ITrack>().ToArray(),
            }).Do(_swaps.Add);
            InvalidateRules();
        } catch (Exception ex) {
            Debug.Log($"failed to load rules : {ex.Message}");
            // noexcept
        }
    }

    public void InvalidateRules()
    {
        _swaps.RemoveAll(s => OstManager.Instance.Fetch(s.Target.Name) is null ||
                              s.Candidates.Any(c => OstManager.Instance.Fetch(c.Name) is null));
    }

    private sealed record SwapRuleCereal(OstTrack? Target, ISwapRule.SwapType Type, OstTrack?[] Candidates);
}