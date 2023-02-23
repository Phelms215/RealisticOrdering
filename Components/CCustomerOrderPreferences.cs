using KitchenData; 
using Unity.Entities;

namespace RealisticOrdering.Components;
[InternalBufferCapacity(12)]
public struct CCustomerOrderPreferences : IBufferElementData
{ 
    public int MemberIndex;
    public int HungerFillLevel; 
    public int TotalMealCount;
    public bool IsFull;
}