﻿using CustomSpawns.CampaignData;
using CustomSpawns.Data;
using System;
//using CustomSpawns.MCMv3;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Localization;

namespace CustomSpawns.Spawn
{
    class SpawnBehaviour : CampaignBehaviorBase
    {

        #region Data Management

        private int lastRedundantDataUpdate = 0;

        public SpawnBehaviour()
        {
            lastRedundantDataUpdate = 0;
        }

        public void HourlyCheckData()
        {
            if (lastRedundantDataUpdate < ConfigLoader.Instance.Config.UpdatePartyRedundantDataPerHour + 1) // + 1 to give leeway and make sure every party gets updated. 
            {
                lastRedundantDataUpdate++;
            }
            else
            {
                lastRedundantDataUpdate = 0;
            }

            //Now for data checking?
        }

        #endregion


        #region MB API-Registered Behaviours

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyBehaviour);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyBehaviour);
            CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, HourlyPartyBehaviour);
            CampaignEvents.OnPartyRemovedEvent.AddNonSerializedListener(this, OnPartyRemoved);
        }

        public override void SyncData(IDataStore dataStore)
        {

        }

        private bool spawnedToday = false;

        private void HourlyBehaviour()
        {
            HourlyCheckData();
            if (!spawnedToday && Campaign.Current.IsNight)
            {
                RegularSpawn();
                spawnedToday = true;
            }

        }

        //deal with our parties being removed! Also this is more efficient ;)
        private void OnPartyRemoved(PartyBase p)
        {
            MobileParty mb = p.MobileParty;
            if (mb == null)
                return;

            CSPartyData partyData = DynamicSpawnData.Instance.GetDynamicSpawnData(mb);
            if (partyData != null)
            {
                partyData.spawnBaseData.DecrementNumberSpawned();
                //this is a custom spawns party!!
                OnPartyDeath(mb, partyData);
                ModDebug.ShowMessage(mb.StringId + " has died at " + partyData.latestClosestSettlement + ", reducing the total number to: " + partyData.spawnBaseData.GetNumberSpawned(), DebugMessageType.DeathTrack);
                DynamicSpawnData.Instance.RemoveDynamicSpawnData(mb);
            }
        }

        private void HourlyPartyBehaviour(MobileParty mb)
        {
            if (DynamicSpawnData.Instance.GetDynamicSpawnData(mb) == null) //check if it is a custom spawns party
                return;
            DynamicSpawnData.Instance.UpdateDynamicData(mb);
            if (lastRedundantDataUpdate >= ConfigLoader.Instance.Config.UpdatePartyRedundantDataPerHour)
            {
                DynamicSpawnData.Instance.UpdateRedundantDynamicData(mb);
            }
            //for now for all
            Economics.PartyEconomicUtils.PartyReplenishFood(mb);
        }

        private void DailyBehaviour()
        {
            spawnedToday = false;
        }

        #endregion

        private void RegularSpawn()
        {
            try
            {
                var list = SpawnDataManager.Instance.Data;
                Random rand = new Random();
                var isSpawnSoundPlaying = false;
                foreach (Data.SpawnData data in list)
                {
                    for (int i = 0; i < data.RepeatSpawnRolls; i++)
                    {
                        if (data.CanSpawn() && (data.MinimumNumberOfDaysUntilSpawn < (int)Math.Ceiling(Campaign.Current.CampaignStartTime.ElapsedDaysUntilNow)))
                        {
                            float currentChanceOfSpawn = data.ChanceOfSpawn;
                            if (!ConfigLoader.Instance.Config.IsAllSpawnMode && 
                                (float)rand.NextDouble() >= currentChanceOfSpawn * ConfigLoader.Instance.Config.SpawnChanceFlatMultiplier)
                                continue;

                            var spawnSettlement = Spawner.GetSpawnSettlement(data, (s => data.MinimumDevestationToSpawn > DevestationMetricData.Singleton.GetDevestation(s)), rand);
                            //spawn nao!

                            if (spawnSettlement == null)
                            {
                                //no valid spawn settlement

                                break;
                            }

                            MobileParty spawnedParty = Spawner.SpawnParty(spawnSettlement, data.SpawnClan, data.PartyTemplate, data.PartyType, new TextObject(data.Name));
                            if (spawnedParty == null)
                                return;
                            data.IncrementNumberSpawned(); //increment for can spawn and chance modifications
                                                           //dynamic data registration
                            //dynamic spawn tracking
                            DynamicSpawnData.Instance.AddDynamicSpawnData(spawnedParty, new CSPartyData(data, spawnSettlement));
                            //AI Checks!
                            Spawner.HandleAIChecks(spawnedParty, data, spawnSettlement);
                            //accompanying spawns
                            foreach (var accomp in data.SpawnAlongWith)
                            {
                                MobileParty juniorParty = Spawner.SpawnParty(spawnSettlement, data.SpawnClan, accomp.templateObject, data.PartyType, new TextObject(accomp.name));
                                if (juniorParty == null)
                                    continue;
                                Spawner.HandleAIChecks(juniorParty, data, spawnSettlement); //junior party has same AI behaviour as main party. TODO in future add some junior party AI and reconstruction.
                            }
                            //message if available
                            if (data.spawnMessage != null)
                            {
                                UX.ShowParseSpawnMessage(data.spawnMessage, spawnSettlement.Name.ToString());
                                //if (data.SoundEvent != -1 && !isSpawnSoundPlaying && ConfigLoader.Instance.Config.SpawnSoundEnabled)
                                //{
                                //    var sceneEmpty = Scene.CreateNewScene(false);
                                //    SoundEvent sound = SoundEvent.CreateEvent(data.SoundEvent, sceneEmpty);
                                //    sound.Play();
                                //    isSpawnSoundPlaying = true;
                                //}
                            }
                            DailyLogger.ReportSpawn(spawnedParty, currentChanceOfSpawn);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ErrorHandler.HandleException(e);
            }
        }

        private void OnPartyDeath(MobileParty mb, CSPartyData dynamicData)
        {
            HandleDeathMessage(mb, dynamicData);
        }

        #region Behaviour Handlers

        private void HandleDeathMessage(MobileParty mb, CSPartyData dynamicData)
        {
            if (dynamicData.spawnBaseData.deathMessage != null)
            {
                UX.ShowParseDeathMessage(dynamicData.spawnBaseData.deathMessage, dynamicData.latestClosestSettlement.ToString());
            }
        }

        #endregion
    }
}
