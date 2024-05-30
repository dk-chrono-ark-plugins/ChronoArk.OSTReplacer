using System.IO;
using ChronoArkMod.ModData;
using ChronoArkMod.ModEditor;
using Newtonsoft.Json;
using Ost.Api;

namespace Ost.Implementation;

internal class OstCustomTrack
{
    public static void CreateFromFile()
    {
        var wavFile = new OpenFileName {
            initialDir = "",
            title = "Add sound asset",
            filter = "waveform (wav)\0*.wav",
            defExt = "wav",
        };
        if (!FileDialogue.GetOpenFileName(wavFile)) {
            return;
        }

        Debug.Log($"picked file {wavFile.file}");

        var source = wavFile.file;
        var filenameExt = Path.GetFileName(source);
        var filename = Path.GetFileNameWithoutExtension(source);

        var remotePath = Path.Combine(Application.persistentDataPath, "Mod\\Ost");
        Directory.CreateDirectory(remotePath);
        File.Copy(source, Path.Combine(remotePath, filenameExt), true);

        var localPath = Path.Combine(OstMod.ModInfo!.assetInfo.AssetDirectory, "AudioFiles");
        Directory.CreateDirectory(localPath);
        File.Copy(source, Path.Combine(localPath, filenameExt), true);

        var audio = new ModAudioInfo.ModAudio {
            Name = filename,
            path = $"AudioFiles\\{filenameExt}",
            AssetBundlePath = "",
            Loop = false,
            Bus = "FieldBGM",
        };

        var jsonPath = Path.Combine(OstMod.ModInfo.DirectoryName, "Audio");
        using var sw = new StreamWriter(Path.Combine(jsonPath, $"{filename}.json"));
        sw.Write(JsonConvert.SerializeObject(audio, Formatting.Indented));
        File.Copy(Path.Combine(jsonPath, $"{filename}.json"), Path.Combine(remotePath, $"{filename}.json"), true);
    }

    public static void RemoveCustomTrack(ITrack track)
    {
        OstManager.Instance.Remove(track);

        var filename = track.Name;
        var remotePath = Path.Combine(Application.persistentDataPath, "Mod\\Ost");
        var json = $"{filename}.json";
        var wave = $"{filename}.wav";
        List<string> deleter = [
            Path.Combine(remotePath, json),
            Path.Combine(remotePath, wave),
            Path.Combine(OstMod.ModInfo!.DirectoryName, "Audio", json),
            Path.Combine(OstMod.ModInfo.assetInfo.AssetDirectory, "AudioFiles", wave),
        ];
        deleter.Where(File.Exists).Do(File.Delete);

        OstPage.Instance.QueueForRebuild();
    }
}