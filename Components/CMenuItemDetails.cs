using KitchenData;
using RealisticOrdering.Definitions;
using Unity.Entities;

namespace RealisticOrdering.Components;

public struct CMenuItemDetails : IComponentData
{
     public MenuType PrimaryMenu;
     public DishType Type;
     public int FillingValue;
     public int ItemID;
     public int DishID;
     public float BonusTime;
}