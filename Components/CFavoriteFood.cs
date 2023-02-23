using KitchenData;
using Unity.Entities;

namespace RealisticOrdering.Components;

public struct CFavoriteFood : IComponentData
{
    public Dish FavoriteItem;
    public bool TimeMatters; 
}