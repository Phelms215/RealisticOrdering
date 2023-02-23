using System;
using KitchenData;
using RealisticOrdering.Definitions;
using Unity.Entities;

namespace RealisticOrdering.Components;

public struct CActiveMenu : IComponentData
{
    public MenuType ActiveMenu;

    public MenuType DetermineMenuType(int mealCount)
    {
        var thisOddsRoll = Utility.OddsCalculation();
        var thisMealType = ActiveMenu switch
        {
            MenuType.Breakfast => BreakfastMealType(mealCount, thisOddsRoll),
            MenuType.Lunch => LunchMealType(mealCount, thisOddsRoll),
            MenuType.Dinner => DinnerMealType(mealCount, thisOddsRoll),
            _ => throw new ArgumentOutOfRangeException()
        };
        Utility.Log("This meal type is " + thisMealType);
        return thisMealType;
    }

    public DishType DetermineDishType(int mealCount)
    {
        var thisOddsRoll = Utility.OddsCalculation();
        var thisDishType = ActiveMenu switch
        {
            MenuType.Breakfast => BreakfastDishType(mealCount, thisOddsRoll),
            MenuType.Lunch => LunchDishType(mealCount, thisOddsRoll),
            MenuType.Dinner => DinnerDishType(mealCount, thisOddsRoll),
            _ => throw new ArgumentOutOfRangeException()
        };
        Utility.Log("This dish type is " + thisDishType);
        return thisDishType;
    }

    public DishType DetermineSideType(int mealCount)
    {
        var thisOddsRoll = Utility.OddsCalculation();
        return mealCount switch
        {
            0 => thisOddsRoll switch
            {
                > 50 and < 55 => DishType.Dessert, 
                > 65 and < 70 => DishType.Side,
                _ => DishType.Starter
            },
            1 => thisOddsRoll switch
            {
                > 50 and < 55 => DishType.Dessert,
                > 56 and < 60 => DishType.Starter,
                _ => DishType.Side, 
            },
            > 1 => thisOddsRoll switch
            { 
                > 5 and < 20 => DishType.Starter,
                > 30 and < 35 => DishType.Side,
                _ => DishType.Dessert
            },
            _ => DishType.Starter
        };
    }

    private static MenuType BreakfastMealType(int mealCount, int thisOddsRoll)
    {
        return thisOddsRoll switch
        {
            > 70 and < 79 => MenuType.Lunch,
            > 40 and < 50 => MenuType.Dinner,
            _ => MenuType.Breakfast
        };
    }
    
    private static MenuType LunchMealType(int mealCount, int thisOddsRoll)
    {
        return thisOddsRoll switch
        {
            > 30 and < 40 => MenuType.Breakfast,
            > 40 and < 50 => MenuType.Dinner,
            _ => MenuType.Lunch
        };
    }
    private static MenuType DinnerMealType(int mealCount, int thisOddsRoll)
    {
        return thisOddsRoll switch
        {
            > 89 and < 100 => MenuType.Breakfast,
            > 40 and < 50 => MenuType.Lunch,
            _ => MenuType.Dinner
        };
    }
    
    private static DishType BreakfastDishType(int mealCount, int thisOddsRoll)
    {
        return mealCount switch
        {
            0 => thisOddsRoll switch
            {
                > 0 and < 10 => DishType.Starter,
                > 70 and < 81 => DishType.Side,
                > 94 and < 97 => DishType.Dessert,
                _ => DishType.Base
            },
            1 => thisOddsRoll switch
            {
                > 0 and < 30 => DishType.Starter,
                > 60 and < 81 => DishType.Side,
                > 80 and < 97 => DishType.Dessert,
                _ => DishType.Base
            },
            > 1 => thisOddsRoll switch
            { 
                > 80 => DishType.Dessert,
                _ => DishType.Side
            },
            _ => throw new ArgumentOutOfRangeException(nameof(mealCount), mealCount, null)
        }; 
    }
    
    private static DishType LunchDishType(int mealCount, int thisOddsRoll)
    {
        return mealCount switch
        {
            0 => thisOddsRoll switch
            {
                > 0 and < 15 => DishType.Starter,
                > 15 and < 25 => DishType.Dessert,
                > 70 => DishType.Side,
                _ => DishType.Base
            },
            1 => thisOddsRoll switch
            { 
                > 0 and < 25 => DishType.Dessert,
                > 90 => DishType.Side,
                _ => DishType.Base
            },
            > 1 => thisOddsRoll switch
            {  
                > 25 => DishType.Side,
                _ => DishType.Dessert
            },
            _ => throw new ArgumentOutOfRangeException(nameof(mealCount), mealCount, null)
        }; 
    }
    
    private static DishType DinnerDishType(int mealCount, int thisOddsRoll)
    {
        return mealCount switch
        {
            0 => thisOddsRoll switch
            {
                > 0 and < 30 => DishType.Starter,
                > 90 and < 100 => DishType.Dessert,
                > 80 and < 90 => DishType.Side,
                _ => DishType.Base
            },
            1 => thisOddsRoll switch
            {
                > 0 and < 10 => DishType.Starter,
                > 90 and < 100 => DishType.Dessert,
                > 80 and < 90 => DishType.Side,
                _ => DishType.Base
            },
            > 1 => thisOddsRoll switch
            {
                > 0 and < 10 => DishType.Starter,
                < 30 => DishType.Side, 
                _ => DishType.Dessert,
            },
            _ => throw new ArgumentOutOfRangeException(nameof(mealCount), mealCount, null)
        };
    }
}