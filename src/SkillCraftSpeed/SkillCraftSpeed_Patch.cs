using BehaviorDesigner.Runtime.Tasks.Basic.Math;
using BehaviorDesigner.Runtime.Tasks.Basic.UnityString;
using HarmonyLib;
using Outputs;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkillCraftSpeed.Patches;
public class SkillCraftSpeedPatches
{
    [HarmonyPatch(typeof(Recipe), nameof(Recipe.GetProcessTime))]
    [HarmonyPostfix]
    public static int Postfix(int __result, Recipe __instance)
    {
        float skillMultiplier = (float)(-0.075 * PlayerStats.instance.GetSkill(__instance.RelevantSkill) + 1);

        Plugin.Log.LogInfo("Affect Processing Time");

        return (int)(__result * skillMultiplier);
    }

    [HarmonyPatch(typeof(manufacturing), nameof(manufacturing.Craft))]
    [HarmonyPrefix]
    public static bool PrefixCraft(manufacturing __instance)
    {
        if (!__instance.PoweredAndActive)
        {
            if (__instance.manuActive)
            {
                ManufacturingPanel.instance.manufacturingPanelUI.manufacturingPower.FlashPowerIcon();
            }
            return false;
        }
        if (__instance.CraftingInProgress < __instance.AllowSimultaneous)
        {
            SoundInfo manufactureSound = __instance.ManufactureSound;
            if (manufactureSound != null)
            {
                manufactureSound.PlaySound();
            }
            if (__instance.animator != null && !string.IsNullOrEmpty(__instance.animationCrafting))
            {
                __instance.animator.SetBool(__instance.animationCrafting, true);
            }
            List<KeyValuePair<ItemStack, int>> usedItemReferences;
            CraftingBase.CraftingInputData inputData;
            __instance.RemoveItems(__instance.selectedRecipe, out usedItemReferences, out inputData, 1);
            int skill = PlayerStats.instance.GetSkill(__instance.selectedRecipe.RelevantSkill);
            float multiplier = (float)(-0.075 * skill + 1);
            int processTime = (int)(__instance.selectedRecipe.ProcessTime * multiplier);

            if (processTime > 0)
            {
                __instance.CraftingInProgress++;
                float num = __instance.RequirePlayer ? 0.5f : 1f;
                Relay onStartCreating = __instance.onStartCreating;
                if (onStartCreating != null)
                {
                    onStartCreating.triggerOutputs();
                }
                CraftInProgress item = new CraftInProgress(new TimeOfDayAzure.Timer(new Action<TimeOfDayAzure.Timer>(__instance.CraftDelayed), (int)(float)(processTime * num / __instance.EquipmentQuality)), __instance.selectedRecipe, usedItemReferences, inputData);
                __instance.craftsInProgresses.Add(item);
                if (__instance.manuActive)
                {
                    ManufacturingPanel.instance.UpdateItemsInProgress(__instance.craftsInProgresses, __instance.AllowSimultaneous);
                }
            }
            else
            {
                __instance.OnCraftDone(__instance.selectedRecipe, usedItemReferences, inputData, 0);
            }
            if (InventoryNavigationHandler.Instance)
            {
                InventoryNavigationHandler.Instance.ExitRecipeOpenedPanel();
                InventoryNavigationHandler.Instance.InitializeManufacturingPanelProgressSlots(false);
            }
            RecipeController.instance.AddRecipe(__instance.selectedRecipe, true);
            if (CraftingBase.currentCraftingBase == __instance)
            {
                __instance.CheckItems(false);
            }
        }
        __instance.UpdateVisibleItems();

        return false;
    }
}