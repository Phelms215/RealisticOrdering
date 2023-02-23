using HarmonyLib;
using Kitchen;
namespace RealisticOrdering.Patches;
[HarmonyPatch(typeof(GroupHandleChoosingOrder), "Initialise")]
internal class DisableGameOrderingSystem
{
    [HarmonyPrefix]
    private static bool Prefix()
    {
        return Preferences.Get<bool>(RealisticOrdering.Pref) != true;
    }
}
[HarmonyPatch(typeof(GroupHandleChoosingOrder), "OnUpdate")]
internal class DisableGameOrderingSystemUpdates
{
    [HarmonyPrefix]
    protected static bool Prefix()
    {
        return Preferences.Get<bool>(RealisticOrdering.Pref) != true;
    }
}
[HarmonyPatch(typeof(AssignMenuRequests), "Initialise")]
internal class DisableOrderAssignSystemSetup
{
    [HarmonyPrefix]
    private static bool Prefix()
    {
        return Preferences.Get<bool>(RealisticOrdering.Pref) != true;
    }
}
[HarmonyPatch(typeof(AssignMenuRequests), 
    "OnUpdate")]
internal class DisableOrderAssignSystemUpdates
{
    [HarmonyPrefix]
    protected static bool Prefix()
    {
        return Preferences.Get<bool>(RealisticOrdering.Pref) != true;
    }
}