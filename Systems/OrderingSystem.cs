using System.Collections.Generic;
using System.Linq;
using Kitchen;
using KitchenData;
using KitchenLib.Utils;
using KitchenMods;
using RealisticOrdering.Components;
using RealisticOrdering.Definitions;
using Unity.Collections;
using Unity.Entities; 

namespace RealisticOrdering.Systems;

public class OrderingSystem : GenericSystemBase, IModSystem
{
    private EntityQuery _customerOrderQuery;
    private EntityQuery _menuItemsQuery;
    private EntityQuery _activeMenuQuery;
    private EntityQuery _availableIngredient;
    private EntityQuery _encouragers;
    private EntityQuery _extras;

    private EntityContext _thisContext;

    protected override void Initialise()
    {
        if (Utility.IsClient()) return;
        _encouragers = GetEntityQuery((ComponentType)typeof(COrderEncourager), (ComponentType)typeof(CPosition),
            (ComponentType)typeof(CItemHolder));
        _customerOrderQuery = GetEntityQuery((ComponentType)typeof(CGroupChoosingOrder));
        _menuItemsQuery = GetEntityQuery((ComponentType)typeof(CMenuItem), (ComponentType)typeof(CMenuItemDetails));
        _activeMenuQuery = GetEntityQuery((ComponentType)typeof(CActiveMenu));
        _availableIngredient = GetEntityQuery((ComponentType)typeof(CAvailableIngredient));
        _extras = GetEntityQuery((ComponentType)typeof(CPossibleExtra));
        _thisContext = new EntityContext(EntityManager);
    }

    protected override void OnUpdate()
    {
        if (Utility.IsClient()) return;
        if (!Has<SIsDayTime>()) return;
        if (Preferences.Get<bool>(RealisticOrdering.Pref) == false) return;
        if (_customerOrderQuery.IsEmpty) return;
        foreach (var group in _customerOrderQuery.ToEntityArray(Allocator.Temp))
        {
            if (!CalculateRemainingTime(group)) continue;
            // Start Loop through members 
            EntityManager.AddBuffer<CWaitingForItem>(group);
            var groupMembers = EntityManager.GetBuffer<CCustomerOrderPreferences>(group);
            var currentBuffer = groupMembers.Reinterpret<CCustomerOrderPreferences>().ToNativeArray(Allocator.Temp);
            groupMembers.Clear();
            var groupBonusTime = 0f;

            CMenuItemDetails customerOrder;
            foreach (var member in currentBuffer)
            {
                var thisMember = member;
                if (thisMember.HungerFillLevel > 75)
                    thisMember.IsFull = true;

                if (thisMember.IsFull) continue;
                
                Utility.Log("Group #" + group.Index + " Customer #" + thisMember.MemberIndex + " Hunger Level: " +
                            thisMember.HungerFillLevel);
                customerOrder = DetermineNextMeal(member, group);
                Utility.Log(
                    "dish #" + customerOrder.DishID + " classification " + customerOrder.PrimaryMenu +
                    " hunger value of " + customerOrder.FillingValue);
                
                thisMember.HungerFillLevel += customerOrder.FillingValue;
                groupBonusTime += customerOrder.BonusTime;

                if (Utility.ShouldAddSide(thisMember.HungerFillLevel, customerOrder))
                {
                    var thisSideOrder = DetermineSide(member, group, customerOrder.DishID);
                    thisMember.HungerFillLevel += thisSideOrder.FillingValue;
                    groupBonusTime += thisSideOrder.BonusTime;

                    Utility.Log(
                        "side #" + thisSideOrder.DishID + " classification " + thisSideOrder.PrimaryMenu +
                        " hunger value of " + thisSideOrder.FillingValue);
                }
                


                thisMember.TotalMealCount += 1;
                _thisContext.AppendToBuffer<CCustomerOrderPreferences>(group, thisMember);
            }

            if (EntityManager.GetBuffer<CCustomerOrderPreferences>(group).Reinterpret<CCustomerOrderPreferences>()
                    .Length > 0)
                UpdateGroupStatus(group, groupBonusTime);
            else
                SendGroupHome(group);
        }
    }
    private CMenuItemDetails DetermineNextMeal(CCustomerOrderPreferences customerOrder, Entity group)
    {
        // Check for Display stands 
        foreach (var test in _encouragers.ToEntityArray(Allocator.Temp))
        {
            var data = EntityManager.GetComponentData<COrderEncourager>(test);
            var thisItem = EntityManager.GetComponentData<CItemHolder>(test);
            var pos = EntityManager.GetComponentData<CPosition>(test);
            if (!EntityManager.HasComponent<CCreateItem>(thisItem.HeldItem)) continue;

            Utility.Log("Probability " + data.Probability);
            Utility.Log("Item " + thisItem.HeldItem.GetType());
            var thisTest = EntityManager.GetComponentData<CCreateItem>(thisItem.HeldItem);
            if (EntityManager.HasComponent<CDishChoice>(thisItem.HeldItem))
            {
                var thisTest2 = EntityManager.GetComponentData<CDishChoice>(thisItem.HeldItem);
                Utility.Log("HAS DISH!" + thisTest2.Dish.ToString());
            }

            Utility.Log(thisTest.ID.ToString());
            Utility.Log("POS " + pos.Position);
        } // END TEST

 
        var currentMenu = _activeMenuQuery.First<CActiveMenu>();
        var mealType = currentMenu.DetermineMenuType(customerOrder.TotalMealCount);
        var dishType = currentMenu.DetermineDishType(customerOrder.TotalMealCount);

        var thisItemList = _menuItemsQuery
            .ToComponentDataArray<CMenuItemDetails>(Allocator.Temp)
            .Where(i => i.Type == dishType || i.PrimaryMenu == mealType)
            .OrderByDescending(i => i.FillingValue)
            .ToList();

        if (!thisItemList.Any())
        {
            Utility.Log("Entering backup plan for this customer order");
            thisItemList = _menuItemsQuery
                .ToComponentDataArray<CMenuItemDetails>(Allocator.Temp)
                .OrderByDescending(i => i.FillingValue)
                .ToList();
        }

        var selectedItem = thisItemList[UnityEngine.Random.Range(0, thisItemList.Count())];
        HandleCustomerOrder(group, selectedItem, false, customerOrder.MemberIndex);
        return selectedItem;
    }
    private CMenuItemDetails DetermineSide(CCustomerOrderPreferences customerOrder, Entity group, int mainDish)
    { 
        var currentMenu = _activeMenuQuery.First<CActiveMenu>();
        var dishType = currentMenu.DetermineSideType(customerOrder.TotalMealCount);
        var mealType = currentMenu.DetermineMenuType(customerOrder.TotalMealCount); 


        var thisItemList = _menuItemsQuery
            .ToComponentDataArray<CMenuItemDetails>(Allocator.Temp)
            .Where(i => i.Type == dishType || i.PrimaryMenu == mealType)
            .Where(i => MealClassification.ValidSide(mainDish, i.DishID))
            .ToList();

        if (!thisItemList.Any())
        {
            Utility.Log("Entering backup plan for this customer side order");
            thisItemList = _menuItemsQuery
                .ToComponentDataArray<CMenuItemDetails>(Allocator.Temp)
                .Where(i => i.Type is not DishType.Base)
                .ToList();
        }

        var selectedItem = thisItemList[UnityEngine.Random.Range(0, thisItemList.Count())];
        HandleCustomerOrder(group, selectedItem, true, customerOrder.MemberIndex);
        return selectedItem;
    }

    private bool CalculateRemainingTime(Entity group)
    {
        var groupSettings = EntityManager.GetComponentData<CCustomerSettings>(group);
        var groupOrdering = EntityManager.GetComponentData<CGroupChoosingOrder>(group);
        if (groupOrdering.HasSelectedCourse)
        {
            if ((double)groupOrdering.RemainingTime <= 0.0)
                return true;

            groupOrdering.RemainingTime -= Time.DeltaTime;
        }
        else
        {
            groupOrdering.HasSelectedCourse = true;
            groupOrdering.RemainingTime = groupSettings.Patience.Thinking;
        }

        EntityManager.SetComponentData<CGroupChoosingOrder>(group, groupOrdering);
        return false;
    }

    private void UpdateGroupStatus(Entity thisGroup, float bonusTime)
    {
        var groupPatience = EntityManager.GetComponentData<CPatience>(thisGroup);
        var groupSettings = EntityManager.GetComponentData<CCustomerSettings>(thisGroup);
        groupPatience.Reason = PatienceReason.Service;
        groupPatience.ResetTime();
        groupPatience.Active = true;
        groupSettings.AddPatience(ref groupPatience, bonusTime, true);
        EntityManager.SetComponentData<CPatience>(thisGroup, groupPatience);
        EntityManager.SetComponentData<CCustomerSettings>(thisGroup, groupSettings);
        EntityManager.RemoveComponent<CGroupChoosingOrder>(thisGroup);
        EntityManager.AddComponent<CGroupReadyToOrder>(thisGroup);
        EntityManager.AddComponent<CGroupStateChanged>(thisGroup);
    }

    private void SendGroupHome(Entity thisGroup)
    {
        EntityManager.RemoveComponent<CGroupChoosingOrder>(thisGroup);
        EntityManager.AddComponent<CGroupStartLeaving>(thisGroup);
        EntityManager.AddComponent<CGroupStateChanged>(thisGroup);
    }

    private void HandleCustomerOrder(Entity groupEntity, CMenuItemDetails menuInfo, bool isSide, int memberIndex)
    {
        if (!GameData.Main.TryGet<Item>(menuInfo.ItemID, out var itemData)) return;
        var itemList = GetDishData(menuInfo.DishID);
        if (Equals(itemList, null)) return;
        PlaceCustomerOrder(itemData, itemList.Value, groupEntity, memberIndex, menuInfo.DishID, isSide);
    }

    private void PlaceCustomerOrder(Item itemData, ItemList itemComponents,
        Entity groupEntity,
        int memberIndex,
        int sourceMenuItem,
        bool isSide = false)
    {
        var ctx = _thisContext;
        var entity2 = itemData is not ItemGroup
            ? ctx.CreateItem(itemData.ID)
            : ctx.CreateItemGroup(itemData.ID, itemComponents);
        ctx.Set<CRequestItemOf>(entity2, new CRequestItemOf()
        {
            Group = groupEntity
        });

        var extraTemp = new List<int>();
        var extraList = _extras.ToComponentDataArray<CPossibleExtra>(Allocator.Temp);
        if (!itemData.MayRequestExtraItems.IsNullOrEmpty<Item>())
            extraTemp.AddRange(itemData.MayRequestExtraItems.Select(requestExtraItem => requestExtraItem.ID));

        extraTemp.AddRange(from extra in extraList where extra.MenuItem == itemData.ID select extra.Ingredient);

        foreach (var extra in extraTemp)
        {
            Utility.Log("Extra is " + extra);
        }

        ctx.AppendToBuffer(groupEntity, new CWaitingForItem()
        {
            ItemID = itemData.ID,
            Item = entity2,
            Reward = itemData.Reward,
            MemberIndex = memberIndex,
            IsSide = isSide,
            DirtItem = (GameDataObject)itemData.DirtiesTo != (GameDataObject)null ? itemData.DirtiesTo.ID : 0,
            Extra = !extraTemp.IsNullOrEmpty<int>() ? extraTemp[UnityEngine.Random.Range(0, extraTemp.Count)] : 0,
            SourceMenuItem = sourceMenuItem
        });
        ctx.Add<CWaitingForItem.Marker>(groupEntity);
    }

    private ItemList? GetDishData(int dishID)
    {
        if (!this.Data.TryGet<Dish>(dishID, out var thisDish, true)) return null;

        var itemID = thisDish.UnlocksMenuItems[0].Item.ID;
        if (!this.Data.TryGet<Item>(itemID, out var itemData, true)) return null;
        var unlockList = _availableIngredient.ToComponentDataArray<CAvailableIngredient>(Allocator.Temp);

        var tempIngredients = new HashSet<int>();
        foreach (var availableIngredient in unlockList.Where(availableIngredient =>
                     availableIngredient.MenuItem == itemData.ID))
        {
            tempIngredients.Add(availableIngredient.Ingredient);
        }

        return itemData is ItemGroup
            ? this.Data.ItemSetView.GetRandomConfiguration(itemData.ID, tempIngredients)
            : new ItemList(itemData.ID);
    }
}