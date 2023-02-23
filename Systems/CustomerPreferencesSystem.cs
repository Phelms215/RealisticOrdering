using System.Collections.Generic;
using System.Linq;
using Kitchen;
using KitchenData;
using KitchenMods;
using RealisticOrdering.Components; 
using Unity.Collections; 
using UnityEngine;
using ComponentType = Unity.Entities.ComponentType;
using EntityQuery = Unity.Entities.EntityQuery;

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
        return Utility.OddsCalculation() switch
        {
            > 0 and < 11 => Random.Range(0,25), 
            > 10 and < 21 => Random.Range(15,45), 
            > 20 and < 31 => Random.Range(50,60), 
            _ => Random.Range(20,65),
        };
    }
    
}