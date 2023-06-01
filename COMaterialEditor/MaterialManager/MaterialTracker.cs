using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace COMaterialEditor.MaterialManager
{
	internal static class MaterialTracker
	{
		private static List<TrackedMaterial> _trackedMaterials = new List<TrackedMaterial>();
		internal static List<MaterialMod> MaterialMods = new List<MaterialMod>();

		public static ReadOnlyCollection<TrackedMaterial> TrackedMaterials => new ReadOnlyCollection<TrackedMaterial>(_trackedMaterials.ToArray());

		/*
		[CanBeNull]
		public static TrackedMaterial InstancedMaterial(Material baseMaterial, Material instanceMaterial)
		{
			var trackedMaterial = _trackedMaterials.FirstOrDefault(r => r.Material == baseMaterial);

			if (trackedMaterial == null)
				return null;

			CloneMaterial(instanceMaterial, trackedMaterial);
			RemoveTrackMaterial(baseMaterial);

			return trackedMaterial;
		}
		public static TrackedMaterial CloneMaterial(Material material, TrackedMaterial trackedMat)
		{
			var trackedMaterial = _trackedMaterials.FirstOrDefault(r => r.Material == material) ?? new TrackedMaterial(material, trackedMat.BodySkin, trackedMat.MatIndex, trackedMat.MaidGuid);
			if (_trackedMaterials.Contains(trackedMaterial) == false)
			{
				_trackedMaterials.Add(trackedMaterial);
			}
			else
			{
				trackedMaterial.UpdateSelf(trackedMat.BodySkin);
				trackedMaterial.CaptureOriginalProperties();
			}

			return trackedMaterial;
		}
		
		[CanBeNull]
		public static TrackedMaterial GetTrackMaterial(Material material)
		{
			return _trackedMaterials.FirstOrDefault(r => r.Material == material);
		}
		*/
		public static TrackedMaterial UpdateOrAddTrackMaterial(Material material, TBodySkin tBodySkin)
		{
			var trackedMaterial = _trackedMaterials.FirstOrDefault(r => r.Material == material) ?? new TrackedMaterial(material, tBodySkin);
			if (_trackedMaterials.Contains(trackedMaterial) == false)
			{
				_trackedMaterials.Add(trackedMaterial);
			}
			else
			{
				trackedMaterial.UpdateSelf(tBodySkin);
				trackedMaterial.CaptureOriginalProperties();
			}

			return trackedMaterial;
		}
		public static void RemoveTrackMaterial(TBodySkin tBodySkin)
		{
			_trackedMaterials = _trackedMaterials
				.Where(r => r.IsThisMyDelParent(tBodySkin) == false)
				.ToList();
		}
		public static void RemoveTrackMaterial(Material material)
		{
			_trackedMaterials = _trackedMaterials
				.Where(r => r.Material != material)
				.ToList();
		}
		public static void UpdateTrackMaterial(TBodySkin tBodySkin)
		{
			var mats = TrackedMaterials.Where(r => r.BodySkin == tBodySkin);

			foreach (var trackedMaterial in mats)
			{
				trackedMaterial.CaptureOriginalProperties();
			}
		}
		public static void UpdateTrackMaterial(TBodySkin tBodySkin, string property, Type type)
		{
			var mats = TrackedMaterials.Where(r => r.BodySkin == tBodySkin);

			foreach (var trackedMaterial in mats)
			{
				trackedMaterial.UpdateProperty(property, type);
			}
		}
		public static void UpdateTrackMaterial(Material material)
		{
			var mats = TrackedMaterials.Where(r => r.Material == material);

			foreach (var trackedMaterial in mats)
			{
				trackedMaterial.CaptureOriginalProperties();
			}
		}
		public static void AddMaterialMod(MaterialMod mod)
		{
			if (MaterialMods.Contains(mod) == false)
			{
				MaterialMods.Add(mod);
			}
		}
		public static TextureMod AddOrUpdateTextureMod(TrackedMaterial tracked, string property, Texture texture)
		{
			var textureThing = GetApplicableModSwap<TextureMod>(tracked, property) ?? new TextureMod(tracked.MatIndex, tracked.BodySkin.Category, tracked.MaidGuid, texture,
				property);
			textureThing.ModTexture = texture;

			if (MaterialMods.Contains(textureThing) == false)
			{
				MaterialMods.Add(textureThing);
			}

			return textureThing;
		}

		public static FloatMod AddOrUpdateFloatMod(TrackedMaterial tracked, string property, float value)
		{
			var floatMod = GetApplicableModSwap<FloatMod>(tracked, property) ?? new FloatMod(tracked.MatIndex, tracked.BodySkin.Category, tracked.MaidGuid,
				property, value);

			floatMod.Value = value;

			if (MaterialMods.Contains(floatMod) == false)
			{
				MaterialMods.Add(floatMod);
			}

			return floatMod;
		}
		public static ColorMod AddOrUpdateColorMod(TrackedMaterial tracked, string property, Color color)
		{
			var colorThing = GetApplicableModSwap<ColorMod>(tracked, property) ?? new ColorMod(tracked.MatIndex, tracked.BodySkin.Category, tracked.MaidGuid,
				property, color);
			colorThing.Color = color;

			if (MaterialMods.Contains(colorThing) == false)
			{
				MaterialMods.Add(colorThing);
			}

			return colorThing;
		}

		public static void ReapplyModSwaps(Material material)
		{
			var applicants = TrackedMaterials.Where(r => r.Material == material);

			foreach (var applicant in applicants)
			{
				applicant.ApplyMods();
			}
		}
		public static void UndoModSwaps(Material material)
		{
			var applicants = TrackedMaterials.Where(r => r.Material == material);

			foreach (var applicant in applicants)
			{
				applicant.RevertAllToOriginal();
			}
		}

		public static void DeleteMaterialSwaps(TrackedMaterial tracked, string property)
		{
			var applicants = GetApplicableModSwaps<MaterialMod>(tracked);

			foreach (var applicant in applicants)
			{
				if (applicant.Property == property)
				{
					MaterialMods.Remove(applicant);
				}
			}
		}
		public static void DeleteMaterialSwaps(TrackedMaterial tracked)
		{
			var applicants = GetApplicableModSwaps<MaterialMod>(tracked);

			foreach (var applicant in applicants)
			{
				MaterialMods.Remove(applicant);
			}
		}
		public static void DeleteMaterialSwaps(string maidGUID)
		{
			var applicants = GetApplicableModSwaps<MaterialMod>(maidGUID);

			foreach (var applicant in applicants)
			{
				MaterialMods.Remove(applicant);
			}
		}
		/*
		public static void RevertAllOriginal()
		{
			foreach (var mat in _trackedMaterials)
			{
				mat.RevertAllToOriginal();
			}
		}

		public static void WhiteOutAll()
		{
			foreach (var mat in _trackedMaterials)
			{
				foreach (var prop in mat.OriginalProperties.Keys)
				{
					mat.AddOrUpdateTextureMod(prop, Texture2D.whiteTexture);
				}
			}
		}
		*/
		public static IEnumerable<T> GetApplicableModSwaps<T>(TrackedMaterial tracked)
		{
			var materialMods = MaterialMods.Where(r => r.IsItMe(tracked));

			var genericCollection = new List<T>();

			foreach (var materialMod in materialMods)
			{
				if (materialMod is T properTypeMod)
				{
					genericCollection.Add(properTypeMod);
				}
			}

			return genericCollection;
		}
		public static IEnumerable<T> GetApplicableModSwaps<T>(string maidGuid)
		{
			var materialMods = MaterialMods.Where(r => r.MaidGuid.Equals(maidGuid));

			var genericCollection = new List<T>();

			foreach (var materialMod in materialMods)
			{
				if (materialMod is T properTypeMod)
				{
					genericCollection.Add(properTypeMod);
				}
			}

			return genericCollection;
		}

		[CanBeNull]
		public static T GetApplicableModSwap<T>(TrackedMaterial tracked, string property)
		{
			var materialMod = MaterialMods.FirstOrDefault(r => r.IsItMe(tracked, property));

			if (materialMod is T actualThing)
			{
				return actualThing;
			}

			return default;
		}
	}
}
