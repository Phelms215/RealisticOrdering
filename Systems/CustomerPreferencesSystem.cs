using System;
using System.Collections.Generic;
using System.Linq;
using Kitchen;
using KitchenData;
using KitchenMods;
using RealisticOrdering.Components; 
using Unity.Collections;
using ComponentType = Unity.Entities.ComponentType;
using EntityQuery = Unity.Entities.EntityQuery;
using Random = UnityEngine.Random;

namespace RealisticOrdering.Systems;
 
public class CustomerPreferencesSystem : GenericSystemBase, IModSystem
{
    private EntityQuery _customerQuery;
    private EntityContext _thisContext;

    private static readonly List<Dish> DishList = new List<Dish>();

    protected override void Initialise()
    {
        if (Utility.IsClient()) return;
        if (Utility.InLobby()) return;
        _thisContext = new EntityContext(EntityManager);
        
        foreach (var dish in RealisticOrdering.AllDishes.Where(i => i.Key is not DishType.Extra))
        {
            if(DishList.Contains(dish.Value)) continue;
            DishList.Add(dish.Value);
        }
        _customerQuery = GetEntityQuery(new QueryHelper().All((ComponentType)typeof(CCustomerGroup))
            .None(typeof(CCustomerOrderPreferences)));
        RequireForUpdate(_customerQuery);
    }

    protected override void OnUpdate()
    {
        if (Utility.IsClient()) return;
        if (!Utility.InKitchen()) return;
        if (Preferences.Get<bool>(RealisticOrdering.Pref) == false) return;
        foreach (var group in _customerQuery.ToEntityArray(Allocator.Temp))
        {
            if (!HasBuffer<CCustomerOrderPreferences>(group))
                _thisContext.AddBuffer<CCustomerOrderPreferences>(group);
            var groupMembers = EntityManager.GetBuffer<CGroupMember>(group);
            var i = 0;
            foreach (var member in groupMembers.Reinterpret<CGroupMember>())
            { 
                _thisContext.AppendToBuffer<CCustomerOrderPreferences>(group, new CCustomerOrderPreferences()
                {             
                    HungerFillLevel = DetermineHungerLevel(),
                    MemberIndex = i,
                    TotalMealCount = 0,
                    IsFull = false
                });
                i += 1;
            }
        }
    }

    private int DetermineHungerLevel()
    {
        var oddsRoll = Utility.OddsCalculation();
        return GameInfo.CurrentDay switch
        {
            < 4 => oddsRoll switch
            { 
                < 60 => Random.Range(50, 70), 
                _ => Random.Range(40, 65)
            },
            > 5 and < 8 => oddsRoll switch
            {
                < 10 => Random.Range(20, 40),  
                > 30 and < 45 => Random.Range(25, 50),
                _ => Random.Range(30, 65)
            },
            > 9 => oddsRoll switch
            {
                0 => 0,
                > 0 and < 5 => Random.Range(50, 65),  
                > 10 and < 30 => Random.Range(25, 45), 
                > 95 => Random.Range(5, 15),  
                _ => Random.Range(10, 65)
            },
            _ => throw new ArgumentOutOfRangeException()
        };

    }

}