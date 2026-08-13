using System;
using System.Collections.Generic;
using EFT.InventoryLogic;
using HarmonyLib;

namespace Shock12.Client
{
    [HarmonyPatch(typeof(AmmoTemplate), nameof(AmmoTemplate.GetCachedReadonlyQualities))]
    internal static class AmmoInspectionPatch
    {
        private static void Postfix(
            AmmoTemplate __instance,
            List<ItemAttribute> __result)
        {
            if (__instance == null ||
                __result == null ||
                __instance.StringId != Shock12Constants.TemplateId ||
                HasShockAttributes(__result))
            {
                return;
            }

            __result.Add(CreateAttribute(
                EItemAttributeId.HeavyBleedingDelta,
                "CAUSES EXTREME TREMOR",
                "15 s"));
            __result.Add(CreateAttribute(
                EItemAttributeId.HpResource,
                "CAUSES PAIN",
                "60 s"));
            __result.Add(CreateAttribute(
                EItemAttributeId.RecoilBack,
                "CAUSES CONCUSSION",
                "30 s"));
            __result.Add(CreateAttribute(
                EItemAttributeId.PoisonedWeapon,
                "CAUSES PANIC ATTACK",
                "5 s"));
            __result.Add(CreateAttribute(
                EItemAttributeId.CenterOfImpact,
                "STAMINA DAMAGE",
                "Extreme (body + arms)"));
            __result.Add(CreateAttribute(
                EItemAttributeId.LightBleedingDelta,
                "BLEED / FRAGMENTATION",
                "None"));
        }

        private static ItemAttribute CreateAttribute(
            EItemAttributeId id,
            string name,
            string value)
        {
            return new ItemAttribute(id)
            {
                Name = name,
                Base = () => 1f,
                StringValue = () => value,
                FullStringValue = () => name + ": " + value,
                DisplayType = () => EItemAttributeDisplayType.Compact,
                IsTextValueDisplayable = true,
            };
        }

        private static bool HasShockAttributes(List<ItemAttribute> attributes)
        {
            for (int i = 0; i < attributes.Count; i++)
            {
                if (string.Equals(
                    attributes[i].Name,
                    "CAUSES EXTREME TREMOR",
                    StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
