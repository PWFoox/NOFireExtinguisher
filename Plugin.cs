using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace NOFireExtinguisher
{
    [BepInPlugin("NOFireExtinguisher", "NO Fire Extinguisher", "0.1")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        internal static ConfigEntry<KeyCode> ExtinguishKey;
        internal static ConfigEntry<int> MaxCharges;

        private void Awake()
        {
            Log = Logger;

            ExtinguishKey = Config.Bind("General", "ExtinguishKey", KeyCode.B,
                "Клавиша активации огнетушителя");

            MaxCharges = Config.Bind("General", "MaxCharges", 2,
                "Сколько раз можно тушить пожар за один вылет");

            Harmony harmony = new Harmony("NOFireExtinguisher");
            harmony.PatchAll();

            Log.LogInfo($"[NOFireExtinguisher] Мод загружен. Клавиша тушения: {ExtinguishKey.Value}, зарядов: {MaxCharges.Value}");
        }

        private void Update()
        {
            if (Input.GetKeyDown(ExtinguishKey.Value))
            {
                FireExtinguisher.TryExtinguishOwnAircraft();
            }
        }
    }
}