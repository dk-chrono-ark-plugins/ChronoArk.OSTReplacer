using System.Collections;
using DarkTonic.MasterAudio;
using DG.Tweening;
using Ost.Api;

namespace Ost.Implementation;

internal class OstManager
{
    private static readonly Lazy<OstManager> _instance = new(() => new());

    private readonly List<QueueTrack> _playlist = [];
    private List<FadeGroup> _activeFadedGroups = [];
    private List<ITrack> _tracks;

    private OstManager()
    {
        _tracks = OstStub
            .StubOstTracks()
            .OfType<ITrack>()
            .ToList();
    }

    public static OstManager Instance => _instance.Value;
    public ITrack? ActiveTrack { get; private set; }
    public bool Stopped => ActiveTrack is null || _playlist.Count == 0;
    public List<string> AllTrackParts => _tracks.SelectMany(t => t.Parts).ToList();

    public void Add(ITrack track)
    {
        Debug.Log($"adding track#{track.Order} {track.Name}");
        if (_tracks.Contains(track)) {
            Debug.Log("..already exists");
        } else if (Fetch(track.Order) != null) {
            Debug.Log($"..track with same order already exists - {Fetch(track.Order)!.Name}");
        } else {
            _tracks.Add(track);
        }
    }

    public void Remove(ITrack track)
    {
        Debug.Log($"removing track#{track.Order} {track.Name}");
        _tracks.Remove(track);
    }

    public ITrack? Fetch(int order)
    {
        return _tracks.FirstOrDefault(t => t.Order == order);
    }

    public ITrack? Fetch(string name)
    {
        return _tracks.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public List<OstTrackEntry> PopulateTrackEntries()
    {
        ReorderTracks();

        var order = _tracks.Last().Order + 1;
        foreach (var (name, audio) in OstMod.ModInfo!.audioinfo.audios) {
            if (_tracks.FirstOrDefault(t => t.Name == name) is not null) {
                Debug.Log($"skipping existing additional track {name}");
                continue;
            }

            var entry = new OstTrack {
                Name = name,
                Parts = [
                    audio.Name,
                ],
                Order = order++,
                UserTrack = true,
            };
            _tracks.Add(entry);
        }

        Debug.Log($"populating entries for {_tracks.Count} tracks");
        return _tracks.Select(t => new OstTrackEntry(t)).ToList();
    }

    public void Play(ITrack track, Action<float>? infoCallback = null)
    {
        if (ActiveTrack == track) {
            Stop();
            return;
        }

        Stop();
        CoroutineHelper.Deferred(() => PlayConcurrent(track, infoCallback));
    }

    public void PlayConcurrent(ITrack track, Action<float>? infoCallback = null)
    {
        PlayEnqueued(track, infoCallback);

        ActiveTrack = track;
        StartPlaylist(infoCallback);
    }

    public void PlayEnqueued(ITrack track, Action<float>? infoCallback = null)
    {
        foreach (var audio in track.Parts) {
            var probe = PlayProbe(audio);
            if (Mathf.Approximately(probe, 0f)) {
                Debug.Log($"failed to enqueue track {track.Name}|{audio}");
                continue;
            }

            _playlist.Add(new(audio, probe));
            Debug.Log($"enqueued track {track.Name}|{audio}|{probe:F2}");
        }
    }

    public void Stop()
    {
        ActiveTrack = null;
        _playlist.Clear();
        MasterAudio.StopEverything();
    }

    public void FadeOutAllSounds()
    {
        _activeFadedGroups = MasterAudio.RuntimeSoundGroupNames
            .Where(MasterAudio.IsSoundGroupPlaying)
            .Select(g => new FadeGroup(g, MasterAudio.GrabGroup(g).groupVariations[0].VarAudio.volume))
            .ToList();
        _activeFadedGroups.Do(sg => MasterAudio.FadeSoundGroupToVolume(sg.Group, 0f, 1f));
        CoroutineHelper.Deferred(MasterAudio.StopEverything, 1f);
    }

    public void FadeInAllSounds()
    {
        Stop();
        _activeFadedGroups.Do(sg => PlayInternal(sg.Group, sg.Volume));
        _activeFadedGroups.Clear();
    }

    private float PlayProbe(string groupName)
    {
        var res = MasterAudio.PlaySound(groupName, 0f);
        if (res?.ActingVariation?.VarAudio?.clip != null) {
            var length = res.ActingVariation.VarAudio.clip.length;
            res.ActingVariation.Stop();
            return length;
        }

        Debug.Log("probing failed");
        return 0f;
    }

    private PlaySoundResult PlayInternal(string soundGroup, float volume = 1f)
    {
        var res = MasterAudio.PlaySound(soundGroup, isChaining: true);
        // unrecommended but I also don't want to mess with group buses
        DOVirtual.Float(0f, volume, 1f, value => {
                if (res?.ActingVariation?.VarAudio != null) {
                    res.ActingVariation.VarAudio.volume = value;
                }
            })
            .SetEase(Ease.InOutQuad);
        return res;
    }

    private void StartPlaylist(Action<float>? infoCallback = null)
    {
        if (_playlist.Count <= 0) {
            Stop();
            return;
        }

        if (infoCallback is not null) {
            CoroutineHelper.Deferred(() => infoCallback(0f), () => Stopped);
        }

        CoroutineHelper.Immediate(QueueNextSequential());
    }

    private IEnumerator QueueNextSequential()
    {
        while (!Stopped) {
            var track = _playlist[0];
            var end = Time.time + track.Length;

            Debug.Log($"releasing queued track {track.Group} for {track.Length:F2} seconds");
            PlayInternal(track.Group, SaveManager.NowSaveSlot.SoundBGMVolume / 100f);

            yield return new WaitUntil(() => Time.time >= end || Stopped);
            _playlist.Remove(track);
        }

        Debug.Log("finished queue");
        Stop();
    }

    private void ReorderTracks()
    {
        _tracks = _tracks.OrderBy(t => t.Order).ToList();
    }

    private sealed record FadeGroup(string Group, float Volume);

    private sealed record QueueTrack(string Group, float Length);
}