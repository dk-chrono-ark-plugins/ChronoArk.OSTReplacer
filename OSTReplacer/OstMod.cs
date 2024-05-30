global using HarmonyLib;
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using UnityEngine;
global using CoroutineHelper = Mcm.Helper.CoroutineHelper;
global using Debug = Ost.Common.Debug;
using ChronoArkMod;
using ChronoArkMod.ModData;
using ChronoArkMod.Plugin;
using Mcm.Api;
using Ost.Implementation;


namespace Ost;

public class OstMod : ChronoArkPlugin
{
    private Harmony? _harmony;

    public static ModInfo? ModInfo => ModManager.getModInfo(Instance!.ModId);
    public static OstMod? Instance { get; private set; }
    internal static OstConfig? Config { get; private set; }
    internal static IModLayout? Layout { get; private set; }

    public override void Dispose()
    {
        Instance = null;
    }

    public override void Initialize()
    {
        Instance = this;
        Config ??= new();

        _harmony = new(GetGuid());
        _harmony.PatchAll();

        CoroutineHelper.Deferred(RegisterConfig);
    }

    private void RegisterConfig()
    {
        var mcm = McmProxy.GetInstance(IModConfigurationMenu.Version.V1);
        Layout = mcm.Register(ModId);

        var config = Config ?? new();
        Layout.AddSliderOption(
            nameof(config.FadeOutDuration),
            "Fade out duration",
            "How long to fade out and fade in when transiting tracks",
            0f,
            5f,
            0.1f,
            config.FadeOutDuration,
            value => config.FadeOutDuration = value
        );

        OstPage.Instance.RebuildOstLayout();
    }
}