using Mcm.Api.Displayables;
using Mcm.Common;
using Mcm.Implementation.Displayables;

namespace Ost.Implementation;

internal class OstPlayer : McmStylable
{
    private readonly McmButton _control;
    private readonly McmText _controlState;

    public OstPlayer()
        : base(McmStyle.Default())
    {
        // TODO add control widgets

        var configurableStyle = Style with {
            Size = new(800f, 100f),
            LayoutPadding = new(5, 5, 2, 2),
            LayoutSpacing = McmStyle.SettingLayout.SettingSpacingInner,
            OutlineSize = new(3f, 3f),
        };
        _controlState = new() {
            Content = "Play",
        };
        _control = new(configurableStyle) {
            Content = _controlState,
            OnClick = Play,
        };
    }

    public override Transform Render(Transform parent)
    {
        if (Ref != null) {
            return Ref.transform;
        }

        var controls = _control.Render<RectTransform>(parent);
        OstManager.Instance.FadeOutAllSounds();
        CoroutineHelper.Deferred(
            OstManager.Instance.FadeInAllSounds,
            () => Ref == null);

        return base.Render(controls);
    }

    private void Play()
    {
        if (OstControlGroup.Instance?.Attached is null) {
            return;
        }

        _controlState.Content = "Playing...";
        OstManager.Instance.Play(OstControlGroup.Instance.Attached.Track, _ => _controlState.Content = "Play");
    }
}