using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using UnityEngine;

namespace Shock12.Client
{
    internal sealed class Shock12VisualMarker : MonoBehaviour
    {
        private Renderer[] _renderers;
        private Material[][] _originalMaterials;
        private Material[][] _paintedMaterials;

        internal void Initialize(Renderer[] renderers, Color paintColor)
        {
            _renderers = renderers;
            _originalMaterials = new Material[renderers.Length][];
            _paintedMaterials = new Material[renderers.Length][];

            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Material[] originals = renderers[rendererIndex].sharedMaterials;
                Material[] painted = new Material[originals.Length];
                _originalMaterials[rendererIndex] = originals;

                for (int materialIndex = 0; materialIndex < originals.Length; materialIndex++)
                {
                    Material source = originals[materialIndex];
                    if (source == null)
                    {
                        painted[materialIndex] = null;
                        continue;
                    }

                    Material replacement = new Material(source);
                    replacement.name = source.name + " (Shock-12 dark blue)";
                    if (replacement.HasProperty("_Color"))
                    {
                        replacement.SetColor("_Color", paintColor);
                    }
                    if (replacement.HasProperty("_BaseColor"))
                    {
                        replacement.SetColor("_BaseColor", paintColor);
                    }

                    painted[materialIndex] = replacement;
                }

                _paintedMaterials[rendererIndex] = painted;
            }
        }

        internal void ShowShock12()
        {
            SetMaterials(_paintedMaterials);
        }

        internal void ShowOriginalFtx()
        {
            SetMaterials(_originalMaterials);
        }

        private void SetMaterials(Material[][] materialSets)
        {
            if (_renderers == null || materialSets == null)
            {
                return;
            }

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                {
                    _renderers[i].sharedMaterials = materialSets[i];
                }
            }
        }
    }

    internal static class Shock12Visuals
    {
        private const string FtxTemplateId = "5d6e68e6a4b9361c140bcfe0";

        private static readonly Color DarkBlue =
            new Color(0.06f, 0.13f, 0.34f, 1f);

        internal static bool UsesFtxModel(Item item)
        {
            if (item == null)
            {
                return false;
            }

            string templateId = item.TemplateId.ToString();
            return templateId == Shock12Constants.TemplateId ||
                   templateId == FtxTemplateId;
        }

        internal static void Apply(Item item, GameObject itemObject)
        {
            if (item == null || itemObject == null || !UsesFtxModel(item))
            {
                return;
            }

            Shock12VisualMarker marker = itemObject.GetComponent<Shock12VisualMarker>();
            if (marker == null)
            {
                marker = itemObject.AddComponent<Shock12VisualMarker>();
                marker.Initialize(
                    itemObject.GetComponentsInChildren<Renderer>(true),
                    DarkBlue);
            }

            if (item.TemplateId.ToString() == Shock12Constants.TemplateId)
            {
                marker.ShowShock12();
            }
            else
            {
                marker.ShowOriginalFtx();
            }
        }
    }

    [HarmonyPatch]
    internal static class SynchronousItemVisualPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (MethodInfo method in AccessTools.GetDeclaredMethods(typeof(ObjectsFactory)))
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (method.ReturnType == typeof(GameObject) &&
                    parameters.Length > 0 && parameters[0].ParameterType == typeof(Item))
                {
                    yield return method;
                }
            }
        }

        private static void Postfix(Item __0, GameObject __result)
        {
            Shock12Visuals.Apply(__0, __result);
        }
    }

    [HarmonyPatch]
    internal static class AsynchronousItemVisualPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (MethodInfo method in AccessTools.GetDeclaredMethods(typeof(ObjectsFactory)))
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (method.ReturnType == typeof(Task<GameObject>) &&
                    parameters.Length > 0 && parameters[0].ParameterType == typeof(Item))
                {
                    yield return method;
                }
            }
        }

        private static void Postfix(Item __0, ref Task<GameObject> __result)
        {
            if (!Shock12Visuals.UsesFtxModel(__0) ||
                __result == null)
            {
                return;
            }

            __result = ApplyWhenReady(__0, __result);
        }

        private static async Task<GameObject> ApplyWhenReady(
            Item item,
            Task<GameObject> itemTask)
        {
            GameObject itemObject = await itemTask;
            Shock12Visuals.Apply(item, itemObject);
            return itemObject;
        }
    }

    [HarmonyPatch(typeof(IconsHash), nameof(IconsHash.GetItemHash))]
    internal static class ItemIconHashPatch
    {
        private const int VisualRevision = 0x53484F01;

        private static void Postfix(Item item, ref int __result)
        {
            if (item != null && item.TemplateId.ToString() == Shock12Constants.TemplateId)
            {
                __result ^= VisualRevision;
            }
        }
    }
}
