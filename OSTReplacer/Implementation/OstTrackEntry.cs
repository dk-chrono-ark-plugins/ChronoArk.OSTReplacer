using Mcm.Api;
using Mcm.Api.Displayables;
using Mcm.Common;
using Mcm.Implementation.Displayables;
using Ost.Api;
using TMPro;

namespace Ost.Implementation;

internal class OstTrackEntry : McmStylable, INotifyChange
{
    private readonly McmText _modifyState;
    private readonly McmHorizontal _trackGroup;

    public OstTrackEntry(ITrack track)
        : base(McmStyle.Default())
    {
        Track = track;

        var textStyle = Style with {
            Size = McmStyle.SettingLayout.NameText,
            TextFontSize = 34f,
            TextAlignment = TextAlignmentOptions.Left,
            OutlineSize = new(3f, 3f),
            LayoutSpacing = McmStyle.SettingLayout.ToggleSpacing,
        };
        var order = new McmText(textStyle) {
            Content = $"{track.Order,3}",
        };
        EntryInfo = new(textStyle) {
            Content = TrackInfoName(),
        };

        var trackInfoStyle = textStyle with {
            Size = new(1000f, 100f),
        };
        var info = new McmOverlap(trackInfoStyle) {
            Composites = [
                new McmImage(trackInfoStyle),
                new McmHorizontal(trackInfoStyle) {
                    Composites = [
                        new(order, new(100f, 100f)),
                        new(EntryInfo, new(900f, 300f)),
                    ],
                },
            ],
        };

        var controlStyle = trackInfoStyle with {
            Size = McmStyle.SettingLayout.Setting,
        };
        _modifyState = new() {
            Content = "Modify",
        };
        var control = new McmButton(controlStyle) {
            Content = _modifyState,
            OnClick = () => OstControlGroup.Instance?.AttachToTrack(this),
        };

        var configurableStyle = controlStyle with {
            Size = new(1000f, 100f),
            LayoutPadding = McmStyle.SettingLayout.TogglePadding,
            LayoutSpacing = McmStyle.SettingLayout.SettingSpacingInner,
        };
        _trackGroup = new(configurableStyle) {
            Composites = [
                new(info, configurableStyle.Size.Value),
                new(control, controlStyle.Size.Value),
            ],
        };
    }

    public ITrack Track { get; init; }
    public McmText EntryInfo { get; }

    public void NotifyChange(object? payload = null)
    {
        _modifyState.Content = OstControlGroup.Instance?.Attached == this ? "| Cancel |" : ">> Select <<";
    }

    public void NotifyApply(object? payload = null)
    {
        _modifyState.Content = "Modify";
    }

    public void NotifyReset(object? payload = null)
    {
        _modifyState.Content = "Modify";
    }

    public override Transform Render(Transform parent)
    {
        if (Ref != null) {
            return Ref.transform;
        }

        var trackInfo = _trackGroup.Render<RectTransform>(parent);
        CoroutineHelper.Deferred(UpdateSwapInfo);

        return base.Render(trackInfo);
    }

    public void SetSwapRule(ITrack? track, ISwapRule.SwapType type = ISwapRule.SwapType.Reset)
    {
        OstSwapGenerator.Instance.RemoveRuleFor(Track);
        switch (type) {
            case ISwapRule.SwapType.Reset:
                break;
            case ISwapRule.SwapType.Remove:
                OstSwapGenerator.Instance.AddRuleFor(Track, type, []);
                break;
            case ISwapRule.SwapType.Replace:
            case ISwapRule.SwapType.InsertAhead:
            case ISwapRule.SwapType.InsertAfter:
                OstSwapGenerator.Instance.AddRuleFor(Track, type, [track!]);
                break;
            // TODO Sequential & Random playlist
            case ISwapRule.SwapType.ReplaceSequential:
            case ISwapRule.SwapType.ReplaceRandom:
                break;
        }

        UpdateSwapInfo();
    }

    public void UpdateSwapInfo()
    {
        EntryInfo.Content = TrackInfoName();

        var rule = OstSwapGenerator.Instance.GetRuleFor(Track);
        if (rule is null) {
            return;
        }

        EntryInfo.Content += $"<b><color=#f1c40fff><size=24>  >>  {rule.Type}</size></color></b>";
        if (rule.Type == ISwapRule.SwapType.Remove) {
            return;
        }

        // TODO Sequential & Random playlist
        var infoString =
            $"<color=#f1c40fff><size=24>  >>  {rule.Candidates[0].Order} {rule.Candidates[0].Name}</size></color>";
        EntryInfo.Content += infoString;
    }

    private string TrackInfoName()
    {
        var userTrack = Track.UserTrack ? "<color=#f1c40fff><size=24>(New!)</size></color>" : string.Empty;
        return $"{userTrack} {Track.Name}";
    }
}