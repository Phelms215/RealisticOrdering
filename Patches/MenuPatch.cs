using System;
using System.Collections.Generic;
using System.Dynamic;
using HarmonyLib;  
using Kitchen;
using Kitchen.Modules; 
using UnityEngine;

namespace RealisticOrdering.Patches
{
    [HarmonyPatch(typeof(GameOptionsMenu<PauseMenuAction>), "Setup")]
    internal class OptionsMenuPatch
    {

        [HarmonyPrefix]
        private static void Prefix(GameOptionsMenu<PauseMenuAction> __instance)
        {
            Option<bool> orderingOption = new Option<bool>(new List<bool>()
            {
                false,
                true
            }, (Preferences.Get<bool>(RealisticOrdering.Pref) ? 1 : 0) != 0, new List<string>()
            {
                "Off",
                "On"
            });
            orderingOption.OnChanged += (EventHandler<bool>)((_, f) =>
            {
                Preferences.Set<bool>(RealisticOrdering.Pref, f);
                Utility.UpdateSaveConfig(f);
            });

            AccessTools.Method(__instance.GetType(), "AddLabel").Invoke(__instance, new object[]
            {
                "Realistic Ordering"
            });
            AccessTools.Method(__instance.GetType(), "Add", null, new []{ typeof(bool)}).Invoke(__instance, new object[]
            {
                orderingOption
            });
        }
    }
}