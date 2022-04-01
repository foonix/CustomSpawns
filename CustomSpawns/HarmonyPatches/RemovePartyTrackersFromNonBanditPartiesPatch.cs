﻿using CustomSpawns.Data;
using HarmonyLib;
using SandBox.ViewModelCollection.MobilePartyTracker;
using System.Linq;
using TaleWorlds.CampaignSystem.Party;

namespace CustomSpawns.HarmonyPatches
{
    [HarmonyPatch(typeof(MobilePartyTrackerVM), "CanAddParty")]
    public class RemovePartyTrackersFromNonBanditPartiesPatch
    {
        static void Postfix(MobileParty party, ref bool __result)
        {
            if (__result)
            {
                var isolatedPartyStringId = CampaignUtils.IsolateMobilePartyStringID(party);
                if (SpawnDataManager.Instance.Data.Any(spawnData =>
                    isolatedPartyStringId.Equals(spawnData.PartyTemplate.GetName().ToString())))
                    __result = false;
            }
        }
    }
}