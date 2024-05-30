using Mcm.Api.Displayables;
using Mcm.Common;
using Mcm.Implementation.Displayables;
using Ost.Api;

namespace Ost.Implementation;

internal class OstControlGroup : McmStylable
{
    private readonly McmVertical _controlGroup;
    private readonly List<IStylable> _controls = [];
    private readonly OstPlayer _ostPlayer;
    private readonly McmVertical _swapRules;
    private readonly McmButton _usertrackControl;
    private OstTrackEntry? _pickFrom;
    private ISwapRule.SwapType _pickType;

    public OstControlGroup()
        : base(McmStyle.Default())
    {
        Instance = this;

        var configurableStyle = Style with {
            Size = new(800f, 100f),
            LayoutPadding = new(5, 5, 2, 2),
            LayoutSpacing = McmStyle.SettingLayout.SettingSpacingInner,
            OutlineSize = new(3f, 3f),
        };

        _ostPlayer = new() {
            Style = configurableStyle,
        };
        _controls.Add(_ostPlayer);

        _usertrackControl = new(configurableStyle) {
            Content = new McmText {
                Content = "Delete",
            },
            OnClick = DeleteUserTrack,
        };
        _controls.Add(_usertrackControl);

        var enumValues = Enum.GetValues(typeof(ISwapRule.SwapType))
            .OfType<ISwapRule.SwapType>()
            .ToList();
        var swapRuleStyle = configurableStyle with {
            Size = new(800f, 100f * enumValues.Count),
        };
        _swapRules = new(swapRuleStyle) {
            Composites = [],
        };

        foreach (var ev in enumValues) {
            var button = new McmButton(swapRuleStyle) {
                Content = new McmText {
                    Content = ev.ToString(),
                },
                OnClick = () => EnterPickModeFor(ev),
            };
            _swapRules.Composites.Add(new(button, swapRuleStyle.Size.Value));
        }

        _controls.Add(_swapRules);
        _controlGroup = new(configurableStyle) {
            Composites = _controls
                .Select(c => new ICompositeLayout.Composite(c, c.Style.Size!.Value))
                .ToList(),
        };
    }

    public static OstControlGroup? Instance { get; private set; }

    public OstTrackEntry? Attached { get; private set; }
    public bool PickMode { get; private set; }

    public override Transform Render(Transform parent)
    {
        if (Ref != null) {
            return Ref!.transform;
        }

        var controlGroup = _controlGroup.Render<RectTransform>(parent);
        CoroutineHelper.Deferred(Detach);

        return base.Render(controlGroup);
    }

    public void AttachToTrack(OstTrackEntry track)
    {
        var lastAttached = Attached;
        Detach();
        if (lastAttached == track) {
            return;
        }

        Attached = track;
        if (PickMode) {
            ExitPickMode();
            return;
        }

        CoroutineHelper.Deferred(() => {
            Ref?.transform.SetSiblingIndex((track.Ref?.transform.GetSiblingIndex() ?? 0) + 1);
            Show();
            if (track.Track.UserTrack) {
                _usertrackControl.Show();
                _swapRules.Hide();
            } else {
                _usertrackControl.Hide();
            }
        });
    }

    public void Detach()
    {
        Hide();
        Ref?.transform.SetAsLastSibling();
        Attached = null;
    }

    private void EnterPickModeFor(ISwapRule.SwapType type)
    {
        switch (type) {
            case ISwapRule.SwapType.Reset:
            case ISwapRule.SwapType.Remove:
                Attached?.SetSwapRule(null, type);
                return;
            case ISwapRule.SwapType.Replace:
            case ISwapRule.SwapType.InsertAhead:
            case ISwapRule.SwapType.InsertAfter:
            case ISwapRule.SwapType.ReplaceSequential:
            case ISwapRule.SwapType.ReplaceRandom:
            default:
                break;
        }

        PickMode = true;
        _pickType = type;
        _pickFrom = Attached;
        OstPage.Instance.TrackEntries.Do(inc => inc.NotifyChange());
        CoroutineHelper.Deferred(() => PickMode = false, () => Ref == null || !PickMode);
        Detach();
    }

    private void ExitPickMode()
    {
        if (_pickFrom != Attached) {
            _pickFrom?.SetSwapRule(Attached?.Track, _pickType);
        }

        PickMode = false;
        _pickFrom = null;
        OstPage.Instance.TrackEntries.Do(inc => inc.NotifyApply());
        Detach();
    }

    private void DeleteUserTrack()
    {
        if (Attached is null) {
            return;
        }

        Attached.Hide();
        OstCustomTrack.RemoveCustomTrack(Attached.Track);
        Detach();
    }
}