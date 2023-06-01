using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace COMaterialEditor.MaterialManager
{
	internal class TrackedMaterial
	{
		public readonly Material Material;
		public int MatIndex { get; private set; }
		public TBodySkin BodySkin { get; private set; }
		public string MaidGuid { get; private set; }
		public Dictionary<string, object> OriginalProperties { get; private set; } = new Dictionary<string, object>();
		public HashSet<string> PropertiesWithMods { get;} = new HashSet<string>();

		public TrackedMaterial(Material material, TBodySkin tBodySkin)
		{
			Material = material;
			BodySkin = tBodySkin;
			UpdateSelf();
			CaptureOriginalProperties();
		}

		public void UpdateSelf(TBodySkin skin = null)
		{
			BodySkin = BodySkin ?? skin;

			if (BodySkin == null)
			{
				CoMaterialEditor.PluginLogger.LogWarning("TBodySkin not found for TrackedMaterial!");
				return;
			}

			MatIndex = BodySkin.GetMaterialIndex(Material);
			MaidGuid = BodySkin?.body?.maid?.status?.guid ?? string.Empty;
		}

		public void CaptureOriginalProperties()
		{
			//No support for free-color-able items as of now.
			if (BodySkin?.TextureCache.tex_dic_.Count > 0)
			{
				MaterialTracker.DeleteMaterialSwaps(this);
				MaterialTracker.RemoveTrackMaterial(BodySkin);
				return;
			}

			var currentTextures = Material.GetTextures();

			foreach (var tex in currentTextures)
			{
				var modSwap = MaterialTracker.GetApplicableModSwap<TextureMod>(this, tex.Key);

				if (modSwap == null || modSwap.IsActive(Material) == false)
				{
					OriginalProperties[tex.Key] = tex.Value;
				}
			}

			var colors = Material.GetColors();

			foreach (var tex in colors)
			{
				var modSwap = MaterialTracker.GetApplicableModSwap<ColorMod>(this, tex.Key);

				if (modSwap == null || modSwap.IsActive(Material) == false)
				{
					OriginalProperties[tex.Key] = tex.Value;
				}
			}

			var floats = Material.GetFloats();

			foreach (var tex in floats)
			{
				var modSwap = MaterialTracker.GetApplicableModSwap<FloatMod>(this, tex.Key);

				if (modSwap == null || modSwap.IsActive(Material) == false)
				{
					OriginalProperties[tex.Key] = tex.Value;
				}
			}

			ApplyMods();
		}

		public void UpdateProperty(string property, Type type)
		{
			if (BodySkin.TextureCache.tex_dic_.Count > 0)
			{
				MaterialTracker.DeleteMaterialSwaps(this);
				MaterialTracker.RemoveTrackMaterial(BodySkin);
				return;
			}

			if (type == typeof(Texture))
			{
				UpdateTexture(property);
			}
			else if(type == typeof(Color))
			{
				UpdateColor(property);
			}
			else if (type == typeof(float))
			{
				UpdateFloat(property);
			}
		}

		public void UpdateTexture(string property)
		{
			if (PropertiesWithMods.Contains(property))
			{
				var modSwap = MaterialTracker.GetApplicableModSwap<TextureMod>(this, property);

				if (modSwap == null || modSwap.IsActive(Material))
				{
					return;
				}
			}

			OriginalProperties[property] = Material.GetTexture(property);
			ApplyMods();
		}

		public void UpdateColor(string property)
		{
			if (PropertiesWithMods.Contains(property))
			{
				var modSwap = MaterialTracker.GetApplicableModSwap<TextureMod>(this, property);

				if (modSwap == null || modSwap.IsActive(Material))
				{
					return;
				}
			}

			OriginalProperties[property] = Material.GetColor(property);
			ApplyMods();
		}
		public void UpdateFloat(string property)
		{
			if (PropertiesWithMods.Contains(property))
			{
				var modSwap = MaterialTracker.GetApplicableModSwap<FloatMod>(this, property);

				if (modSwap == null || modSwap.IsActive(Material))
				{
					return;
				}
			}

			OriginalProperties[property] = Material.GetFloat(property);
			ApplyMods();
		}

		public void ApplyMods()
		{
			PropertiesWithMods.Clear();
			var possibleTexSwaps = MaterialTracker.GetApplicableModSwaps<MaterialMod>(this);
			var materialMods = possibleTexSwaps as MaterialMod[] ?? possibleTexSwaps.ToArray();
			foreach (var swap in materialMods)
			{
				swap.ApplyMod(Material);
				PropertiesWithMods.Add(swap.Property);
			}
		}

		public bool IsThisMyDelParent(TBodySkin tbodyObject)
		{
			return (BodySkin == tbodyObject || tbodyObject.listDEL.Contains(Material));
		}
		public void RevertAllToOriginal(bool removeSwaps = false)
		{
			foreach (var pair in OriginalProperties)
			{
				switch (pair.Value)
				{
					case Texture tex:
						Material.SetTexture(pair.Key, tex);
						if (removeSwaps)
						{
							MaterialTracker.DeleteMaterialSwaps(this, pair.Key);
						}
						break;
					case Color col:
						Material.SetColor(pair.Key, col);
						if (removeSwaps)
						{
							MaterialTracker.DeleteMaterialSwaps(this, pair.Key);
						}
						break;
					case float flt:
						Material.SetFloat(pair.Key, flt);
						if (removeSwaps)
						{
							MaterialTracker.DeleteMaterialSwaps(this, pair.Key);
						}
						break;
				}

				PropertiesWithMods.Clear();
			}
		}
		public void RevertToOriginal(string property)
		{
			if (!OriginalProperties.TryGetValue(property, out var obj))
			{
				return;
			}

			switch (obj)
			{
				case Texture tex:
					Material.SetTexture(property, tex);
					MaterialTracker.DeleteMaterialSwaps(this, property);
					break;
				case Color col:
					Material.SetColor(property, col);
					MaterialTracker.DeleteMaterialSwaps(this, property);
					break;
				case float flt:
					Material.SetFloat(property, flt);
					MaterialTracker.DeleteMaterialSwaps(this, property);
					break;
			}

			PropertiesWithMods.Remove(property);
		}
		public void AddOrUpdateTextureMod(string property, Texture texture)
		{
			var newSwap = MaterialTracker.AddOrUpdateTextureMod(this, property, texture);
			newSwap.ApplyMod(Material);
			PropertiesWithMods.Add(property);
		}
		public void AddOrUpdateColorMod(string property, Color color)
		{
			var newSwap = MaterialTracker.AddOrUpdateColorMod(this, property, color);
			newSwap.ApplyMod(Material);
			PropertiesWithMods.Add(property);
		}
		public void AddOrUpdateFloatMod(string property, float value)
		{
			var newSwap = MaterialTracker.AddOrUpdateFloatMod(this, property, value);
			newSwap.ApplyMod(Material);
			PropertiesWithMods.Add(property);
		}
	}
}