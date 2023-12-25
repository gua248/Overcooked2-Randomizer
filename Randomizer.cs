using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OC2Randomizer
{
    public static class Randomizer
    {
        private static PickupItemSpawner[] spawners;
        public static Dictionary<PickupItemSpawner, PickupItemSpawner> spawnerPermuteDict;
        private static ServerFlammable[] flammables;
        private static bool hasExtinguisher;
        public static ServerKitchenFlowControllerBase flowController;
        private static ServerUnlimitedRoundTimer timer;

        private const float initialTime = 15f;
        private const float shuffleSpawnerInterval = 15f;
        public const int limitedQuantity = 15;
        private static int shuffleSpawnerCount;
        public const int penaltyKill = 5;
        public const int penaltyRespawn = 10;

        public static Team17.Online.Multiplayer.Client localClient = null;
        public static Team17.Online.Multiplayer.Server localServer = null;

        public static void Update()
        {
            if (timer != null)
            {
                timer.Update();
                if (timer.TimeElapsed > shuffleSpawnerCount * shuffleSpawnerInterval + initialTime)
                    ShuffleSpawner();
            }
        }

        public static void OnStart()
        {
            ServerFireExtinguishSpray[] extinguishers = GameObject.FindObjectsOfType<ServerFireExtinguishSpray>();
            hasExtinguisher = extinguishers.Any(x => !(x is ServerWaterGunSpray));
            FindObjects();
            shuffleSpawnerCount = 0;
            flowController = GameObject.FindObjectOfType<ServerKitchenFlowControllerBase>();
            timer = new ServerUnlimitedRoundTimer();
        }

        public static void FindObjects()
        {
            if (RandomizerSettings.enabledIngredient)
            {
                spawners = GameObject.FindObjectsOfType<PickupItemSpawner>();
                spawnerPermuteDict = new Dictionary<PickupItemSpawner, PickupItemSpawner>();
                foreach (PickupItemSpawner spawner in spawners)
                    spawnerPermuteDict.Add(spawner, spawner);
            }
            if (RandomizerSettings.enabledIgnition)
            {
                flammables = hasExtinguisher ? GameObject.FindObjectsOfType<ServerFlammable>() : null;
            }
        }

        private static void ShuffleSpawner()
        {
            shuffleSpawnerCount++;
            if (spawners == null) return;
            for (int i = 0; i < spawners.Length; i++)
            {
                int j = UnityEngine.Random.Range(i, spawners.Length);
                PickupItemSpawner tmp = spawnerPermuteDict[spawners[i]];
                spawnerPermuteDict[spawners[i]] = spawnerPermuteDict[spawners[j]];
                spawnerPermuteDict[spawners[j]] = tmp;
            }
        }

        public static void Ignite()
        {
            if (flammables == null || flammables.Length == 0) return;
            float r = UnityEngine.Random.Range(0f, 1f);

            int num = 
                r > 0.5f ? 0 : (
                r > 0.15f ? 1 : ( 
                r > 0.05f ? 2 : 3));
            for (int i = 0; i < num; i++)
            {
                ServerFlammable flammable = flammables.GetRandomElement();
                if (flammable.isActiveAndEnabled)
                    flammable.Ignite();
            }
        }
    }
}
