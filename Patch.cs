using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using OC2Randomizer.Extension;
using Team17.Online.Multiplayer.Messaging;
using UnityEngine;
using GameModes.Horde;

namespace OC2Randomizer
{
    public static class Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ServerHordeFlowController), "FoodDelivered")]
        [HarmonyPatch(typeof(ServerKitchenFlowControllerBase), "FoodDelivered")]
        public static void IKitchenOrderHandlerFoodDeliveredPatch()
        {
            if (!RandomizerSettings.enabledIgnition) return;
            Randomizer.Ignite();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ClientKitchenFlowControllerBase), "ApplyServerEvent")]
        public static void ClientKitchenFlowControllerBaseApplyServerEventPacth(ClientKitchenFlowControllerBase __instance, Serialisable serialisable)
        {
            KitchenFlowMessage kitchenFlowMessage = (KitchenFlowMessage)serialisable;
            if (kitchenFlowMessage.m_msgType == KitchenFlowMessage.MsgType.ScoreOnly)
            {
                GameUtils.TriggerAudio(GameOneShotAudioTag.RecipeTimeOut, LayerMask.NameToLayer("Default"));
                __instance.UpdateScoreUI(kitchenFlowMessage.m_teamID);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ServerLimitedQuantityItemManager), "AddItemToList")]
        public static bool ServerLimitedQuantityItemManagerAddItemToListPatch(ServerLimitedQuantityItemManager __instance)
        {
            if (RandomizerSettings.enabledIngredient && Randomizer.flowController != null)
            {
                if (__instance.get_m_AllObjects_Count() >= Randomizer.limitedQuantity)
                    ScorePenalty(Randomizer.penaltyKill);
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ServerLimitedQuantityItemManager), "Awake")]
        public static bool ServerLimitedQuantityItemManagerAwakePatch(ServerLimitedQuantityItemManager __instance)
        {
            if (RandomizerSettings.enabledIngredient)
                __instance.GetComponent<LimitedQuantityItemManager>().m_MaxObjects = Randomizer.limitedQuantity;
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ServerContentsDisposalBehaviour), "AddToDisposer", new Type[] { typeof(ICarrier), typeof(IDisposer) })]
        public static bool ServerContentsDisposalBehaviourAddToDisposer(ServerContentsDisposalBehaviour __instance)
        {
            if (RandomizerSettings.enabledIngredient && Randomizer.flowController != null)
            {
                if (!__instance.IsEmpty())
                    ScorePenalty(Randomizer.penaltyKill);
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ServerPlayerRespawnManager), "KillOrRespawn")]
        public static bool ServerPlayerRespawnManagerKillOrRespawnPatch(GameObject _gameObject)
        {
            if (RandomizerSettings.enabledIngredient && Randomizer.flowController != null)
            {
                IRespawnBehaviour respawnBehaviour = _gameObject.RequestInterfaceRecursive<IRespawnBehaviour>();
                int penalty = (respawnBehaviour == null) ? Randomizer.penaltyKill : Randomizer.penaltyRespawn;
                ScorePenalty(penalty);
            }
            return true;
        }

        private static void ScorePenalty(int penalty)
        {
            TeamID teamID = TeamID.One;
            if (Randomizer.flowController is ServerCompetitiveFlowController)
                teamID = (UnityEngine.Random.Range(0, 2) == 0) ? TeamID.One : TeamID.Two;
            // OrderID orderID = new OrderID(255);
            ServerTeamMonitor teamMonitor = Randomizer.flowController.GetMonitorForTeam(teamID);
            if (teamMonitor.Score.TotalBaseScore >= penalty)
                teamMonitor.Score.TotalBaseScore -= penalty;
            else
                teamMonitor.Score.TotalTimeExpireDeductions += penalty;

            KitchenFlowMessage data = new KitchenFlowMessage();
            data.Initialise_ScoreOnly(teamID);
            data.SetScoreData(teamMonitor.Score);
            Randomizer.flowController.SendServerEvent(data);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FrontendCoopTabOptions), "OnOnlinePublicClicked")]
        [HarmonyPatch(typeof(FrontendVersusTabOptions), "OnOnlinePublicClicked")]
        public static bool OnOnlinePublicClickedPatch()
        {
            RandomizerSettings.enabledIngredient = false;
            RandomizerSettings.enabledIgnition= false;
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ServerPickupItemSpawner), "HandlePickup")]
        public static bool ServerPickupItemSpawnerHandlePickup(ServerPickupItemSpawner __instance, ICarrier _carrier)
        {
            if (!RandomizerSettings.enabledIngredient) return true;
            Vector3 position = __instance.gameObject.transform.position;
            IParentable parentable = _carrier.AccessGameObject().RequestInterface<IParentable>();
            
            PickupItemSpawner spawner = __instance.get_m_pickupItemSpawner();
            if (!Randomizer.spawnerPermuteDict.ContainsKey(spawner) || !Randomizer.spawnerPermuteDict[spawner].isActiveAndEnabled)
                Randomizer.FindObjects();
            PickupItemSpawner spawnerNew = Randomizer.spawnerPermuteDict[spawner];
            if (parentable as MonoBehaviour != null)
                position = parentable.GetAttachPoint(spawnerNew.m_itemPrefab).position;
            GameObject @object = NetworkUtils.ServerSpawnPrefab(spawnerNew.gameObject, spawnerNew.m_itemPrefab, position, Quaternion.identity);
            _carrier.CarryItem(@object);
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ServerFlowControllerBase), "StartFlow")]
        public static void ServerFlowControllerBaseStartFlowPatch(ServerFlowControllerBase __instance)
        {
            __instance.RoundActivatedCallback += Randomizer.OnStart;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ServerFlowControllerBase), "OnUpdateInRound")]
        public static void ServerFlowControllerBaseOnUpdateInRoundPatch()
        {
            Randomizer.Update();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(T17TabPanel), "OnTabSelected")]
        public static void T17TabPanelOnTabSelectedPatch()
        {
            RandomizerSettings.AddUI();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ToggleOption), "OnToggleButtonPressed")]
        public static bool ToggleOptionOnToggleButtonPressedPatch(ToggleOption __instance, bool bValue)
        {
            if (RandomizerSettings.randomizerOptions[0] == null) return true;
            if (__instance == RandomizerSettings.randomizerOptions[0])
            {
                RandomizerSettings.enabledIngredient = bValue;
                return false;
            }
            if (__instance == RandomizerSettings.randomizerOptions[1])
            {
                RandomizerSettings.enabledIgnition = bValue;
                return false;
            }
            return true;
        }
    }
}
