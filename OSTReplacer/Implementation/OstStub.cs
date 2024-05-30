using System.IO;
using Mcm.Common;

namespace Ost.Implementation;

internal static class OstStub
{
    internal static List<OstTrack> StubOstTracks()
    {
        try {
            var filePath = Path.Combine(OstMod.ModInfo!.assetInfo.AssetDirectory, "ost.json");
            ConfigCereal.ReadConfig(filePath, out List<OstTrack>? tracks);
            return tracks!;
        } catch (Exception ex) {
            Debug.Log($"failed to stub ost track records : {ex.Message}");
            // noexcept
        }

        return [];
    }
}