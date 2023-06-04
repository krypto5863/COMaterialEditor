using System;
using System.Collections.Generic;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using COM3D2API;
using COMaterialEditor.MaterialManager;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;
using SecurityAction = System.Security.Permissions.SecurityAction;

//These two lines tell your plugin to not give a flying fuck about accessing private variables/classes whatever. It requires a publicized stubb of the library with those private objects though.
[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace COMaterialEditor
{
	//Instance is the metadata set for your plugin.
	[BepInPlugin("Bepinex.COMaterialEditor", "COMaterialEditor", "0.95")]
	[BepInDependency("org.bepinex.plugins.unityinjectorloader", BepInDependency.DependencyFlags.SoftDependency)]
	public class CoMaterialEditor : BaseUnityPlugin
	{
		//static saving of the main __instance. Instance makes it easier to run stuff like co-routines from static methods or accessing non-static vars.
		internal static CoMaterialEditor Instance;

		//Static var for the logger so you can log from other classes.
		internal static ManualLogSource PluginLogger => Instance.Logger;

		//Config entry variable. You set your configs to this.
		private static ConfigEntry<bool> _uiUseHotKey;
		private static ConfigEntry<KeyboardShortcut> _uiToggleConfig;
		

		internal static readonly Texture2D Transparent = FillColorAlpha();

		internal static bool DrawGui;

		internal const string BaseIcon64 =
			"iVBORw0KGgoAAAANSUhEUgAAABwAAAAcCAYAAAByDd + UAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsIAAA7CARUoSoAAAAQJSURBVEhLtZZdSFxHFMf / q5DE4kdcu7b0QWPWryqGpUsNNiYqUdNqUJuPFgoSS5u8tG9ZWOhT7YOkpRAhbwYXTcCHgMQXRTCxGkLWaDcrSpIVxBI / wMZuVkU30Yi358ydXe9qdu9NSX9wODNndu9 / 5t6ZM8cEIJHsfbIPyNLIEsjiyN4F22RBshdki2R+MmTabLbzExMTj5X/Cbfb/SdrkNYhFjzKATm2h56eHqW9vV2Zn58Xfa/Xq7S2tipDQ0Oi39HRoXR2dioLCwui7XK5lOXlZTGmhRfEWiz4hYztweFwKDSulJeXKxkZGYrP5xP9uro64fv7+4Vna2trC7d7e3vlEyKhsRr+Vu+RvRGaFUgIlZWVmJ2dxejoqIhXV1cLPzY2JjzT19cnWzFJYMGoGyQuTh3Kzc1Fc3MzkpKSRD8EvTrhk5OTjQrKJ+rg9/tB3wirq6syohKaUFZWFjY3N1FYWCj6sYgpyCva2NjA3Nwc6BshIYFPDLC+vi58WhqfIiA/P1/4vLw84WMRU7CmpkasqqWlBY2NjWhoaEBxcTGcTifS09NRUVEhfsevlMnOzhZej/NyE0VlcXFRtlSmp6dl6+1gLc40LHiLlVdWVsROHB4extLSEodgsVhQUlICs9kMOq+YmpoS8fj4eBQUFIhVTU5OhuMh+PXW1taK34QwmUxfRQiOj4+j/eopfP/lc2zTfLYpMfG8XN009qwE1y67EUf/4L1iIv+cEtY3TgvuXFcnt5tbow40/3JFTI5hQfbhV8rZ4+vPScMXab85oHxk2Rv3dKsHfXc8ZJUnjyu06eTT1Vdq6FhkZwA5mbLzFlit1vDqQhgSPEK7PUekXX2eTtO18xlw8dd6XGi69N8ErbS6Y5/IjgG2YEbD2YsiIezGkCBTf1L1L1+qPhbf1r/AH92nMTg4KCM7GBZMTVF9IDK77eFjOvu/O1WjW0NGdzAsGIKPglFCqVCLruDr17Ih8QdkIwq8ac7+CDT9lAy6N2V0B13BR09kQ7KyJhsxeBaw4/LP91FWViYjO+gKencJBg1sGo/Hg66uLpEm19YiZ6i/Qq5EJHcfkOAr2YmClZKEj+7iphNXcPvmd6JS0MKCXMpF5dFT2SB8f+mvcN8+StyHVZune5QvBA3bLBh+REpKCvYfOCB7oMuXXo9mhVMz+ivUcjA1VTxTQ5Bvi6NULLmKiooKeDYDAwO43X0DMzMzsNvtOGI7Bq/nHgKBAOyflotSwjM2JPqlpaWw5tjw0H1H3DRa+No6c+4CqqqqkJiYiJGREQ9dcz/w2CEuUrW1KWf4YDAo/NbWlvDarP+mce5rTft7rkllIZzJK+RSn4uTD8nMZFw26m4mg/D+4E/GJf7fAP75F6PTE5/9UaS2AAAAAElFTkSuQmCC";

		private void Awake()
		{
			//Useful for engaging co-routines or accessing variables non-static variables. Completely optional though.
			Instance = this;

			//Binds the configuration. In other words it sets your ConfigEntry var to your config setup.
			_uiUseHotKey = Config.Bind("General", "Use Keyboard Shortcut", false, "Allows you to use a keyboard shortcut (set below) to toggle the UI.");
			_uiToggleConfig = Config.Bind("General", "Toggle UI", new KeyboardShortcut(KeyCode.C, KeyCode.LeftAlt), "It toggles the UI. It's not rocket science.");

			//Installs the patches in the COMaterialEditor class.

			var harmony = Harmony.CreateAndPatchAll(typeof(SerializeManager));
			harmony.PatchAll(typeof(CommonHooks));
			harmony.PatchAll(typeof(NormalHooks));

			SystemShortcutAPI.AddButton("AdvancedMaterialModifier", () =>
			{
				DrawGui = !DrawGui;

			}, "COMaterialEditor", Convert.FromBase64String(BaseIcon64));
		}

		private static Texture2D FillColorAlpha()
		{
			var tex2D = new Texture2D(2, 2, TextureFormat.RGBA32, false);
			var fillPixels = new Color[tex2D.width * tex2D.height];
			for (var i = 0; i < fillPixels.Length; i++)
			{
				fillPixels[i] = Color.clear;
			}
			tex2D.SetPixels(fillPixels);
			tex2D.Apply();
			return tex2D;
		}

		private void Update()
		{
			if (_uiUseHotKey.Value && _uiToggleConfig.Value.IsDown())
			{
				DrawGui = !DrawGui;
			}
		}

		private void OnGUI()
		{
			if (DrawGui)
			{
				SampleUi.DrawUi();
			}
		}

		public static class CommonHooks
		{
			[HarmonyPatch(typeof(TBodySkin), "DeleteObj")]
			[HarmonyPrefix]
			private static void BodySkinDelObjTracker(ref TBodySkin __instance)
			{
				MaterialTracker.RemoveTrackMaterial(__instance);
			}
			[HarmonyPatch(typeof(Object), "Destroy", typeof(Object), typeof(float))]
			[HarmonyPatch(typeof(Object), "DestroyObject", typeof(Object), typeof(float))]
			[HarmonyPatch(typeof(Object), "DestroyImmediate", typeof(Object), typeof(bool))]
			[HarmonyPrefix]
			private static bool DestroyTracker(ref object __0)
			{
				if (!(__0 is Material mat))
				{
					return true;
				}

				MaterialTracker.RemoveTrackMaterial(mat);

				return true;
			}

			[HarmonyPatch(typeof(Material), MethodType.Constructor, typeof(Material))]
			[HarmonyPrefix]
			private static void CaptureClonedMaterials_Prefix(ref Material __0)
			{
				MaterialTracker.UndoModSwaps(__0);
			}
			[HarmonyPatch(typeof(Material), MethodType.Constructor, typeof(Material))]
			[HarmonyPostfix]
			private static void CaptureClonedMaterials_Postfix(ref Material __instance, ref Material __0)
			{
				//CoMaterialEditor.PluginLogger.LogDebug($"{__instance.name} was cloned from {__0}");
				MaterialTracker.ReapplyModSwaps(__0);
#if (DEBUG)
				CoMaterialEditor.PluginLogger.LogDebug("Clone capture hook called!");
#endif
			}

			private static Material[] _materialsAboutToInstance;
			[HarmonyPatch(typeof(Renderer), "materials", MethodType.Getter)]
			[HarmonyPrefix]
			private static void CaptureInstancesGetter_Prefix(ref Renderer __instance)
			{
				foreach (var mat in __instance.sharedMaterials)
				{
					MaterialTracker.UndoModSwaps(mat);
				}

				_materialsAboutToInstance = __instance.sharedMaterials;
			}
			[HarmonyPatch(typeof(Renderer), "materials", MethodType.Getter)]
			[HarmonyPostfix]
			private static void CaptureInstancesGetter_Postfix(ref Renderer __instance, ref Material[] __result)
			{
				try
				{
					for (var i = 0; i < __result.Length; i++)
					{
						var mat = __result[i];

						if (mat != _materialsAboutToInstance[i])
						{
							var tBodySkin = __instance.GetParentTBodySkin();
							if (tBodySkin == null || tBodySkin.m_bMan || tBodySkin.TextureCache.tex_dic_.Count > 0)
							{
								continue;
							}

							MaterialTracker.UpdateOrAddTrackMaterial(mat, tBodySkin);
							MaterialTracker.RemoveTrackMaterial(_materialsAboutToInstance[i]);
						}
						else
						{
							MaterialTracker.ReapplyModSwaps(mat);
						}
					}
				}
				finally
				{
					_materialsAboutToInstance = null;
				}

#if (DEBUG)
				CoMaterialEditor.PluginLogger.LogDebug("Instancing hook called!");
#endif
			}
			/*
			[HarmonyPatch(typeof(Renderer), "materials", MethodType.Setter)]
			[HarmonyPrefix]
			private static void CaptureInstancesSetter_Prefix(ref Renderer __instance, ref Material[] __0)
			{
				for (int i = 0; i < __0.Length; i++)
				{
					if (__instance.sharedMaterials.Length <= i)
					{
						break;
					}

					var newMat = __0[i];

					if (__instance.sharedMaterials[i] != newMat)
					{
						MaterialTracker.InstancedMaterial(__instance.sharedMaterials[i], newMat);
					}
				}
			}
			*/

			private static Material _materialAboutToInstance;
			[HarmonyPatch(typeof(Renderer), "material", MethodType.Getter)]
			[HarmonyPrefix]
			private static void CaptureInstanceGetter_Prefix(ref Renderer __instance)
			{
				MaterialTracker.UndoModSwaps(__instance.sharedMaterial);
				_materialAboutToInstance = __instance.sharedMaterial;
			}
			[HarmonyPatch(typeof(Renderer), "material", MethodType.Getter)]
			[HarmonyPostfix]
			private static void CaptureInstanceGetter_Postfix(ref Renderer __instance, ref Material __result)
			{
				try
				{
					if (__result != _materialAboutToInstance)
					{
						var tBodySkin = __instance.GetParentTBodySkin();

						if (tBodySkin == null || tBodySkin.m_bMan || tBodySkin.TextureCache.tex_dic_.Count > 0)
						{
							return;
						}

						MaterialTracker.UpdateOrAddTrackMaterial(__result, tBodySkin);
						MaterialTracker.RemoveTrackMaterial(_materialAboutToInstance);
					}
					else
					{
						MaterialTracker.ReapplyModSwaps(__result);
					}
				}
				finally
				{
					_materialsAboutToInstance = null;
				}

#if (DEBUG)
				CoMaterialEditor.PluginLogger.LogDebug("Instancing hook called!");
#endif
			}
			/*
			[HarmonyPatch(typeof(Renderer), "material", MethodType.Setter)]
			[HarmonyPrefix]
			private static void CaptureInstanceSetter_Prefix(ref Renderer __instance, ref Material __0)
			{
				if (__instance.sharedMaterial != __0)
				{
					MaterialTracker.InstancedMaterial(__instance.sharedMaterial, __0);
				}
			}
			*/
		}

		public static class NormalHooks
		{
			[HarmonyPatch(typeof(TBodySkin), "Load", typeof(MPN), typeof(Transform), typeof(Transform), typeof(Dictionary<string, Transform>), typeof(string), typeof(string), typeof(string), typeof(string), typeof(int), typeof(bool), typeof(int))]
			[HarmonyPostfix]
			private static void CaptureLoadedMaterials(TBodySkin __instance)
			{
				if (__instance.m_bMan)
				{
					return;
				}

				foreach (var material in __instance.GetMaterials())
				{
					MaterialTracker.UpdateOrAddTrackMaterial(material, __instance);
				}

#if (DEBUG)
				CoMaterialEditor.PluginLogger.LogDebug("Load hook called!");
#endif
			}
			[HarmonyPatch(typeof(ImportCM), "ReadMaterial")]
			[HarmonyPostfix]
			private static void UpdateMaterials(Material __result)
			{
				MaterialTracker.UpdateTrackMaterial(__result);
#if (DEBUG)
				CoMaterialEditor.PluginLogger.LogDebug("ReadMaterialHook");
#endif
			}
			[HarmonyPatch(typeof(TBody), "ChangeTex")]
			[HarmonyPostfix]
			private static void UpdateMaterials(ref TBody __instance, ref string __0, ref string __2)
			{
				var num = (int)TBody.hashSlotName[__0];
				var tbodySkin = __instance.goSlot[num];
				MaterialTracker.UpdateTrackMaterial(tbodySkin, __2, typeof(Texture));
#if (DEBUG)
				CoMaterialEditor.PluginLogger.LogDebug("ChangeTexHook");
#endif
			}
			[HarmonyPatch(typeof(TBody), "ChangeCol")]
			[HarmonyPostfix]
			private static void UpdateColorProperty(ref TBody __instance, ref string __0, ref string __2)
			{
				var num = (int)TBody.hashSlotName[__0];
				var tbodySkin = __instance.goSlot[num];
				MaterialTracker.UpdateTrackMaterial(tbodySkin, __2, typeof(Color));
#if (DEBUG)
				CoMaterialEditor.PluginLogger.LogDebug("ChangeColHook");
#endif
			}
			[HarmonyPatch(typeof(TBodySkin), "SetMaterialProperty")]
			[HarmonyPostfix]
			private static void UpdatePropertyChange(ref TBodySkin __instance)
			{
				MaterialTracker.UpdateTrackMaterial(__instance);
#if (DEBUG)
				CoMaterialEditor.PluginLogger.LogDebug("SetMaterialPropertyHook");
#endif
			}
		}
	}
}