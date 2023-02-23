using System;
using System.Linq;
using KitchenData;
using RealisticOrdering.Components;

namespace RealisticOrdering.Definitions;

public static class MealClassification
{

    private static readonly string[] BreakfastWords = new string[]
    {
        "egg",
        "pancake",
        "waffle",
        "bread",
        "toast",
        "oatmeal",
        "coffee",
        "smoothie",
        "bagel",
        "bacon",
        "juice",
        "hash",
    };
    
    private static readonly string[] LunchWords = new string[]
    {
        "dog",
        "pizza",
        "burger",
        "taco",
        "salad",
        "chip",
        "ring",
        "tea"
    };
    
    private static readonly string[] DinnerWords = new string[]
    {
        "soup",
        "turkey",
        "steak",
        "dumpling",
        "turkey",
        "stir",
        "roast",
        "fish",
        "pie",
    };

    private static readonly string[] HealthyOptions = new string[]
    {
        "steak",
        "fish",
        "stir",
        "salad",
        "sushi", 
        "roast",
        "broccoli",
        "roast",
        "juice",
        "corn",
        "soup",
        "tea",
        "bamboo"
    };

    private static readonly string[] Drinks = new string[]
    {
        "tea",
        "coffee",
        "milkshake",
        "soda",
        "float",
        "juice",
    };
 
    public static int HungerFilling(ItemValue itemValue)
    { 
        return itemValue switch
        {
            ItemValue.None => 0,
            ItemValue.Small => 25,
            ItemValue.Medium => 35,
            ItemValue.Large => 45,
            ItemValue.MediumLarge => 55,
            ItemValue.ExtraLarge => 65,
            ItemValue.SideSmall => 20,
            ItemValue.SideMedium => 30,
            ItemValue.SideLarge => 40, 
            _ => 0
        };
    }

    public static CMenuItemDetails GetClassification(int dishId, int itemId)
    {
        if (!GameData.Main.TryGet<Item>(itemId, out var thisItem)) throw new NullReferenceException();
        if (!GameData.Main.TryGet<Dish>(dishId, out var thisDish)) throw new NullReferenceException();
        var type = GetMenuType(thisDish.Name.ToLower());

        return new CMenuItemDetails()
        {
            PrimaryMenu = type,
            FillingValue = HungerFilling(thisItem.ItemValue),
            Type = thisDish.Type,
            DishID = dishId,
            ItemID = itemId,
            BonusTime = thisItem.ExtraTimeGranted
        };
    }

    public static bool ValidSide(int mainCourse, int sideRequest) {
        if (!GameData.Main.TryGet<Dish>(mainCourse, out var thisDish)) return false;
        if (!GameData.Main.TryGet<Dish>(sideRequest, out var thisSide)) return false;

        var isHealthyMain = (HealthyOptions.Any(i => thisSide.Name.ToLower().Contains(i)));
        if(thisDish.Type is DishType.Base)
            isHealthyMain = (HealthyOptions.Any(i => thisDish.Name.ToLower().Contains(i)));
        var isHealthySide = (HealthyOptions.Any(i => thisSide.Name.ToLower().Contains(i)));
        if (thisDish == thisSide) return false;
        
        if(IsDrink(thisDish.Name.ToLower()) && IsDrink(thisSide.Name.ToLower())) return false;
        if(GetMenuType(thisDish.Name.ToLower()) == GetMenuType(thisSide.Name.ToLower()))
            return true;

        if (isHealthyMain)
            return isHealthySide;

        return !isHealthySide;
    }

    public static MenuType GetMenuType(string name)
    {
        var type = MenuType.Dinner;
        if (BreakfastWords.Any(name.Contains))
            type = MenuType.Breakfast;
        if (LunchWords.Any(name.Contains))
            type = MenuType.Lunch;
        if (DinnerWords.Any(name.Contains))
            type = MenuType.Dinner;
        return type;
    }
    
    public static bool IsDrink(string name)
    {
        return Drinks.Any(name.Contains);
    }
    
    
}