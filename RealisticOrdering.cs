using System.Collections.Generic;
using JetBrains.Annotations;
using KitchenMods; 
using Kitchen;
using KitchenData;
using RealisticOrdering.Patches; 
using UnityEngine;

namespace RealisticOrdering;

public class RealisticOrdering : GenericSystemBase, IModSystem
{
    public const string ModGuid = "com.networkglitch.realisticordering";
    public const string ModName = "RealisticOrdering";
    public static List<KeyValuePair<DishType, Dish>> AllDishes;

    private const string ModVersion = "1.0.0";
    public static Pref Pref = new Pref("Realistic Ordering", "RealisticOrdering");

    protected override void Initialise()
    {
        if (Utility.IsClient()) return;
        Utility.Log("Initializing Mod - Version " + ModVersion);
        LoadDishes();

        if (!Preferences.TryGet<bool>(RealisticOrdering.Pref, out var prefSettings)) {
            Preferences.AddPreference(new BoolPreference(RealisticOrdering.Pref, Utility.FetchConfigData().Enabled));
        }

        if (GameObject.FindObjectOfType<PatchInitializer>() != null) return;
        var patchObject = new GameObject(ModName).AddComponent<PatchInitializer>();
        GameObject.DontDestroyOnLoad(patchObject);
    }

    protected override void OnUpdate()
    {
    }

    [CanBeNull]
    private Dish GetActiveDish()
    {
        if (!GetComponentOfSingletonHolder<CDishChoice, SDishPedestal>(out var dishChoice)) return null;
        return GameData.Main.TryGet<Dish>(dishChoice.Dish, out var thisDish) ? thisDish : null;
    }

    private static void LoadDishes()
    {
        AllDishes = new List<KeyValuePair<DishType, Dish>>();
        foreach (var card in GameData.Main.GetCards())
        {
            var cardInfo = Utility.GetDishOrCard(card.CardID);
            if (Equals(cardInfo?.Item2, null)) continue;
            var thisDish = cardInfo.Value.Item2;
            if (!thisDish.IsUnlockable && thisDish.ID != -959076098) continue;
            AllDishes.Add(new KeyValuePair<DishType, Dish>(thisDish.Type, thisDish));
        }
    }
}