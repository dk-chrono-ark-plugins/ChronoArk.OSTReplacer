using Mcm.Api.Displayables;

namespace Ost.Implementation;

internal class OstPage
{
    private static readonly Lazy<OstPage> _instance = new(() => new());
    private bool _deferred;

    private OstPage()
    {
        MyOstPage = OstMod.Layout!.AddPage("OST", ICompositeLayout.LayoutGroup.Vertical, true);
        MyOstPage.Title = "OST Replacer Primitive Prototype Exemplum v1";
        MyOstPage.Style = MyOstPage.Style with {
            OutlineSize = new(5f, 5f),
        };
    }

    public IPage MyOstPage { get; }
    public static OstPage Instance => _instance.Value;
    public List<OstTrackEntry> TrackEntries => MyOstPage.Elements.OfType<OstTrackEntry>().ToList();

    public void RebuildOstLayout()
    {
        OstMod.ModInfo!.audioinfo.init();

        OstSwapGenerator.Instance.LoadRules();

        MyOstPage.Clear();

        MyOstPage.AddButton("Add Track", OstCustomTrack.CreateFromFile);
        MyOstPage.AddButton("Rebuild Tracks", RebuildOstLayout);
        MyOstPage.AddSeparator();

        OstManager.Instance.PopulateTrackEntries().Do(MyOstPage.Add);

        MyOstPage.AddSeparator();
        MyOstPage.AddText("~~This is the end~~");

        MyOstPage.Add(new OstControlGroup());

        CoroutineHelper.Deferred(OstMod.Layout!.CloseAllPage);
        _deferred = false;
    }

    public void QueueForRebuild()
    {
        if (_deferred) {
            return;
        }

        _deferred = true;
        CoroutineHelper.Deferred(RebuildOstLayout, () => !OstMod.Layout!.IsPageActive(MyOstPage));
    }
}