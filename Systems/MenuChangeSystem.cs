using Kitchen;
using KitchenMods;
using RealisticOrdering.Components;
using RealisticOrdering.Definitions;
using Unity.Entities;

namespace RealisticOrdering.Systems;
   
public class MenuChangeSystem : GenericSystemBase, IModSystem
{
 
    private EntityQuery _activeMenuQuery; 
    
    protected override void Initialise()
    {
        if (Utility.IsClient()) return;
        if (Utility.InLobby()) return; 
        _activeMenuQuery = GetEntityQuery((ComponentType)typeof(CActiveMenu));  
    } 
    
    
    protected override void OnUpdate() { 
        if (Utility.IsClient()) return; 
        if (!Utility.InKitchen() || !Has<SIsDayTime>()) return;
        if (Preferences.Get<bool>(RealisticOrdering.Pref) == false) return;
        if (_activeMenuQuery.IsEmpty) {
            var thisEntity = EntityManager.CreateEntity((ComponentType)typeof(CActiveMenu));
            EntityManager.SetComponentData<CActiveMenu>(thisEntity, new CActiveMenu() { ActiveMenu = MenuType.Breakfast});
            return;
        }

        if (!Require<STime>(out var currentTime)) return;
        var timeSplit = currentTime.DayLength / 3; 
        
        // breakfast
        if (currentTime.SecondsSinceDayBegan < timeSplit)
        {
            SetActiveMenu(MenuType.Breakfast);
            return;
        }
        
        // Lunch
        if (currentTime.SecondsSinceDayBegan > timeSplit && currentTime.SecondsSinceDayBegan < (timeSplit *2))
        {
            SetActiveMenu(MenuType.Lunch);
            return;
        }
        
        // Dinner
        if (!(currentTime.SecondsSinceDayBegan > timeSplit * 2)) return;
        SetActiveMenu(MenuType.Dinner); 
    }

    private void SetActiveMenu(MenuType thisMenuType)
    {
        if (_activeMenuQuery.First<CActiveMenu>().ActiveMenu == thisMenuType) return; 
        EntityManager.SetComponentData<CActiveMenu>(_activeMenuQuery.First(), new CActiveMenu()
        {
            ActiveMenu = thisMenuType
        });
    }


}