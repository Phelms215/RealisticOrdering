using System; 
using System.IO; 
using JetBrains.Annotations;
using Kitchen;
using KitchenData;
using RealisticOrdering.Components; 
using Unity.Serialization.Json;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RealisticOrdering;

public static class Utility
{
    private static readonly string ConfigFile = Application.persistentDataPath + "/realisticordering.config"; 
    public static void Log(string message)
    {
        Debug.Log("[" + RealisticOrdering.ModName + "] " + DateTime.Now + " - " + message);
    }
    public static bool InKitchen()
    {
        return GameInfo.CurrentScene == SceneType.Kitchen;
    }

    public static bool InLobby()
    {
        return GameInfo.CurrentScene == SceneType.Franchise;
    }

    public static bool EnteredLobby(GenericSystemBase instance)
    {
        if (!instance.HasSingleton<SCreateScene>()) return false;
        return instance.GetSingleton<SCreateScene>().Type == SceneType.Franchise;
    }
    
    public static bool EnteredKitchen(GenericSystemBase instance)
    {
        if (!instance.HasSingleton<SCreateScene>()) return false;
        return instance.GetSingleton<SCreateScene>().Type == SceneType.Kitchen;
    }
    
    public static bool IsClient()
    {
        return Session.CurrentGameNetworkMode != GameNetworkMode.Host;
    }

    public static bool HasDoubleHelpings()
    {
        if (!GameData.Main.TryGet<ICard>(2055765569, out var thisCard)) return false;
        if (GameInfo.AllCurrentCards.Contains(thisCard)) return true;
        return false;
        
    }

    public static int OddsCalculation()
    { 
        var randomNumber= Random.Range(0, 99); 
        return randomNumber;
    }

    public static bool ShouldCustomerEat(int hungerLevel)
    {
        var oddsCalculation = OddsCalculation();
        return hungerLevel switch
        { 
            > 50 and <= 60 => (oddsCalculation is > 0 and < 39),
            > 60 and <= 75 => (oddsCalculation is > 89 and < 100),
            > 75 => (oddsCalculation is > 45 and < 51),
            _ => true
        };
    }

    public static bool ShouldAddSide(int hungerLevel, CMenuItemDetails mainDish)
    {
        return hungerLevel switch
        { 
            >= 60 => false,
            _ => FactorMainMeal(mainDish), 
        };
    }

    private static bool FactorMainMeal(CMenuItemDetails mainDish)
    {
        var thisOddsRoll = Utility.OddsCalculation();
        return !(mainDish.FillingValue switch
        {
            < 15 => thisOddsRoll switch
            {
                > 60 => false,
                _ => true
            },
            > 15 and < 50 => thisOddsRoll switch
            {
                > 50 => false,
                _ => true
            },
            _ => true,
        });
    }



    [CanBeNull]
    public static (UnlockCard, Dish)? GetDishOrCard(int cardID)
    {
        if (GameData.Main.TryGet<Dish>(cardID, out var thisDish))
            return (null, thisDish);

        if (GameData.Main.TryGet<UnlockCard>(cardID, out var thisCard))
            return (thisCard, null);

        return null;
    }

    public static void UpdateSaveConfig(bool modEnabled)
    {
        var newConfig = new ConfigFormat()
        {
            Enabled = modEnabled
        };
        var jsonString = JsonSerialization.ToJson(newConfig);
        File.WriteAllText(ConfigFile, jsonString);
    }

    public static ConfigFormat FetchConfigData()
    {
        if (!File.Exists(ConfigFile))
            UpdateSaveConfig(false);
        return JsonSerialization.FromJson<ConfigFormat>(File.ReadAllText(ConfigFile)) ?? new ConfigFormat();
    }

    public class ConfigFormat
    {
        public bool Enabled;
    }
    
}