using Kitchen;
using KitchenMods;
using RealisticOrdering.Components;
using RealisticOrdering.Definitions;
using Unity.Collections;
using Unity.Entities;

namespace RealisticOrdering.Systems; 

public class MenuItemsSystem : GenericSystemBase, IModSystem
{
 
    private EntityQuery _menuQuery;

    protected override void Initialise()
    {
        if (Utility.IsClient()) return;
        if (Utility.InLobby()) return; 
        _menuQuery = GetEntityQuery(new QueryHelper().All((ComponentType)typeof(CMenuItem))
            .None((ComponentType)typeof(CMenuItemDetails)));
        RequireForUpdate(_menuQuery);
    }

    protected override void OnUpdate()
    {
        if (Utility.IsClient()) return; 
        if (!Utility.InKitchen()) return; 
        if (Preferences.Get<bool>(RealisticOrdering.Pref) == false) return;
        foreach (var menuItem in _menuQuery.ToEntityArray(Allocator.Temp)) {
            var menuItemInfo = EntityManager.GetComponentData<CMenuItem>(menuItem); 
            EntityManager.AddComponentData<CMenuItemDetails>(menuItem,
                MealClassification.GetClassification(menuItemInfo.SourceDish, menuItemInfo.Item));
        }
        
    }
}