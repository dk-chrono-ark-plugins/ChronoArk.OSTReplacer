using DarkTonic.MasterAudio;

namespace Ost.Implementation;

[HarmonyPatch]
internal static class MasterAudioPatch
{
    private static readonly Dictionary<string, InterjectAudioGroup> _activeInterjects = [];

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MasterAudio), nameof(MasterAudio.PlaySound))]
    internal static bool OnPlaySound(ref string sType)
    {
        if (!OstManager.Instance.AllTrackParts.Contains(sType)) {
            return true;
        }

        Debug.Log($"playing {sType}");

        // 1) sound that's already swapped
        var current = sType;
        if (_activeInterjects.TryGetValue(current, out var interject)) {
            Debug.Log("playing an active interjected list, ignore");
            // do nothing
            return true;
        }

        // 2) sound already swapped starts looping
        var activeList = _activeInterjects.Where(itj => itj.Value.Playlist.Contains(current)).ToArray();
        if (activeList.Length > 0) {
            Debug.Log("looping the active interjects, to next or loop");
            var group = activeList[0].Value;
            var next = Mathf.Min(group.Playlist.IndexOf(current) + 1, group.Playlist.Count - 1);
            sType = group.Playlist[next];
            return true;
        }

        // 3) sound that's not swapped and has no rules
        if (!OstSwapGenerator.Instance.HasSwapForPart(sType)) {
            Debug.Log("no interject rule, ignore");
            return true;
        }

        var pl = OstSwapGenerator.Instance.GetSwapPlaylist(sType);

        // 4) rule for remove
        if (pl.Length == 0) {
            Debug.Log("remove rule");
            return false;
        }

        // 5) apply rule
        _activeInterjects.Add(current, new(pl[0], pl.ToList()));
        Debug.Log($"apply new rule {pl[0]}");
        sType = pl[0];
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MasterAudio), nameof(MasterAudio.StopAllOfSound))]
    internal static bool OnStopSound(ref string sType)
    {
        if (!_activeInterjects.TryGetValue(sType, out var interject)) {
            return true;
        }

        sType = interject.Current;
        _activeInterjects.Remove(sType);

        return true;
    }

    private sealed record InterjectAudioGroup(string Current, List<string> Playlist);
}