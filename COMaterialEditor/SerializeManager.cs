using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using COMaterialEditor.MaterialManager;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace COMaterialEditor
{
	internal static class SerializeManager
	{
		internal const string SaveExtension = ".COMaterialSave";
		internal const string PresetExtension = ".COMaterialPreset";

		[HarmonyPatch(typeof(GameMain), "Serialize")]
		[HarmonyPostfix]
		internal static void Postfix(int __0)
		{
			if (MaterialTracker.MaterialMods.Count <= 0)
			{
				return;
			}

			var madeSave = GameMain.instance.MakeSavePathFileName(__0);
			var textureSwapsToSave = JsonConvert.SerializeObject(MaterialTracker.MaterialMods, Formatting.Indented, new JsonSerializerSettings()
			{
				TypeNameHandling = TypeNameHandling.Auto,
			});
			File.WriteAllText(madeSave + SaveExtension, textureSwapsToSave);
		}

		[HarmonyPatch(typeof(GameMain), "Deserialize")]
		[HarmonyPostfix]
		internal static void Postfix2(int __0)
		{
			var madeSave = GameMain.instance.MakeSavePathFileName(__0);
			if (File.Exists(madeSave + SaveExtension) == false)
			{
				return;
			}

			var jsonString = File.ReadAllText(madeSave + SaveExtension);
			var textureSwapsToSave = JsonConvert.DeserializeObject<List<MaterialMod>>(jsonString, new JsonSerializerSettings()
			{
				TypeNameHandling = TypeNameHandling.Auto
			});
			MaterialTracker.MaterialMods = textureSwapsToSave.Distinct().ToList();
		}

		[HarmonyPatch(typeof(CharacterMgr), "PresetSave")]
		[HarmonyTranspiler]
		internal static IEnumerable<CodeInstruction> HookIntoPresetSave(IEnumerable<CodeInstruction> instructions)
		{
			var matcher = new CodeMatcher(instructions)
				.MatchForward(true, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(File), "WriteAllBytes")))
				.Insert(new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Ldloc_S, 7),
					new CodeInstruction(OpCodes.Ldloc_S, 5),
					new CodeInstruction(
						Transpilers.EmitDelegate<System.Action<Maid, string, string>>((maid, presetDirectory, presetName) =>
							{
								var materialMods = MaterialTracker.GetApplicableModSwaps<MaterialMod>(maid.status.guid);
								if (!materialMods.Any())
								{
									return;
								}

								var textureSwapsToSave = JsonConvert.SerializeObject(materialMods, Formatting.Indented, new JsonSerializerSettings()
								{
									TypeNameHandling = TypeNameHandling.Auto,
									ContractResolver = new IgnorePropertiesResolver("MaidGuid")
								});

								File.WriteAllText(presetDirectory + "\\" + presetName + PresetExtension, textureSwapsToSave);
							})));


			return matcher.InstructionEnumeration();
		}

		[HarmonyPatch(typeof(CharacterMgr), "PresetSet", typeof(Maid), typeof(CharacterMgr.Preset))]
		[HarmonyPostfix]
		internal static void HookIntoPresetLoad(ref CharacterMgr __instance, ref Maid __0, ref CharacterMgr.Preset __1)
		{
			MaterialTracker.DeleteMaterialSwaps(__0.status.guid);

			var saveFile = __instance.PresetDirectory + "\\" + __1.strFileName + PresetExtension;

			if (File.Exists(__instance.PresetDirectory + "\\" + __1.strFileName + PresetExtension) == false)
			{
				return;
			}

			var jsonString = File.ReadAllText(saveFile);
			var textureSwapsToSave = JsonConvert.DeserializeObject<List<MaterialMod>>(jsonString, new JsonSerializerSettings()
			{
				TypeNameHandling = TypeNameHandling.Auto
			});

			foreach (var matMod in textureSwapsToSave)
			{
				matMod.MaidGuid = __0.status.guid;
				MaterialTracker.AddMaterialMod(matMod);
			}
		}
	}
	public class TextureJsonConverter : JsonConverter
	{
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if (value is Texture2D texture)
			{
				writer.WriteValue(texture.EncodeToPNG());
			}
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var b64RawTexData = reader.ReadAsBytes();
			var finishedTexture = new Texture2D(1, 1);
			finishedTexture.LoadImage(b64RawTexData);
			return finishedTexture;
		}

		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(Texture2D);
		}
	}
	public class ColorJsonConverter : JsonConverter
	{
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if (value is Color col)
			{
				writer.WriteValue(ColorUtility.ToHtmlStringRGBA(col));
			}
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var htmlHex = reader.Value as string;
			ColorUtility.TryParseHtmlString("#" + htmlHex, out var col);
			return col;
		}

		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(Color);
		}
	}
	public class IgnorePropertiesResolver : DefaultContractResolver
	{
		private readonly HashSet<string> _ignoreProps;
		public IgnorePropertiesResolver(params string[] propNamesToIgnore)
		{
			_ignoreProps = new HashSet<string>(propNamesToIgnore);
		}
		protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
		{
			var property = base.CreateProperty(member, memberSerialization);
			if (_ignoreProps.Contains(property.PropertyName))
			{
				property.ShouldSerialize = _ => false;
			}
			return property;
		}
	}
}
