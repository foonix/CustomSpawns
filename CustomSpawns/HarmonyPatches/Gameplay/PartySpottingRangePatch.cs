﻿using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;

namespace CustomSpawns.HarmonyPatches.Gameplay
{
    [HarmonyPatch(typeof(DefaultMapVisibilityModel), "GetPartySpottingRange")]
    static class PartySpottingRangePatch
    {

        public static int AdditionalSpottingRange { get; set; } = 0;

        static void Postfix(ref ExplainedNumber __result)
        {
            if(AdditionalSpottingRange == 0)
            {
                return;
            }
            __result.AddFactor(AdditionalSpottingRange, new TaleWorlds.Localization.TextObject("CustomSpawns HAX"));
        }

    }
}
