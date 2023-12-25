using BepInEx;
using HarmonyLib;
using System;
using UnityEngine.SceneManagement;

namespace OC2Randomizer
{
    [BepInPlugin("dev.gua.overcooked.randomizer", "Overcooked2 Randomizer Plugin", "1.0")]
    [BepInProcess("Overcooked2.exe")]
    public class RandomizerPlugin : BaseUnityPlugin
    {
        public static RandomizerPlugin pluginInstance;
        private static Harmony patcher;

        public void Awake()
        {
            pluginInstance = this;
            patcher = new Harmony("dev.gua.overcooked.randomizer");
            patcher.PatchAll(typeof(Patch));
            foreach (var patched in patcher.GetPatchedMethods())
                Log("Patched: " + patched.FullDescription());
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
        }

        public static void Log(String msg) { pluginInstance.Logger.LogInfo(msg); }
    }
}
