using System;
using COM3D2.NPRShader.Managed;
using COM3D2.NPRShader.Plugin;
using HarmonyLib;
using System.Security.AccessControl;
using COMaterialEditor.MaterialManager;
using UnityEngine;
using static OVRLipSync;

namespace COMaterialEditor
{
	public static class NPRHooks
	{
		//It's hideous, but it works...
		[HarmonyPatch(typeof(EnvironmentWindow), "ChangeLightDirButton_CheckedChanged")]
		[HarmonyPatch(typeof(MaterialPane), "getMaterial")]
		[HarmonyPatch(typeof(MaterialPane), "resetMaterial")]
		[HarmonyPatch(typeof(MaterialPane), "setMaterial")]
		[HarmonyPatch(typeof(ObjectPane), "getMaterial")]
		[HarmonyPatch(typeof(ObjectPane), "resetMaterial")]
		[HarmonyPatch(typeof(ObjectPane), "setMaterial")]
		[HarmonyPatch(typeof(ObjectWindow), "UpdaateMaterial")]
		[HarmonyPostfix]
		public static void NPRSReadMatHook()
		{
			foreach (var go in UnityEngine.Object.FindObjectsOfType<GameObject>())
			{
				foreach (var tBody in go?.GetComponentsInChildren<TBody>(true))
				{
					foreach (var tBodySkin in tBody.goSlot)
					{
						if (tBodySkin.m_bMan)
						{
							continue;
						}

						var materials = tBodySkin?.GetMaterials();

						if (materials == null)
						{
							continue;
						}

						foreach (var material in materials)
						{
							if (material == null)
							{
								continue;
							}
							MaterialTracker.UpdateOrAddTrackMaterial(material, tBodySkin);
						}
					}
				}
			}
		}
	}
}