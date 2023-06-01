using Newtonsoft.Json;
using UnityEngine;

namespace COMaterialEditor.MaterialManager
{
	internal abstract class MaterialMod
	{
		public string MaidGuid { get; internal set; }
		public readonly string Category;
		public readonly int MatIndex;
		public readonly string Property;

		public MaterialMod(int matIndex, string category, string maidGuid, string property)
		{
			MatIndex = matIndex;
			Category = category;
			MaidGuid = maidGuid;
			Property = property;
		}

		public abstract void ApplyMod(Material material);
		public abstract bool IsActive(Material material);

		public bool IsItMe(TrackedMaterial trackedMat)
		{
			return trackedMat.MatIndex == MatIndex && trackedMat.BodySkin?.Category == Category && trackedMat.MaidGuid == MaidGuid;
		}

		public bool IsItMe(TrackedMaterial trackedMat, string property)
		{
			return trackedMat.MatIndex == MatIndex && trackedMat.BodySkin?.Category == Category && trackedMat.MaidGuid == MaidGuid && Property.Equals(property);
		}
	}

	internal class TextureMod : MaterialMod
	{
		[JsonConverter(typeof(TextureJsonConverter))]
		public Texture ModTexture { get; internal set; }

		public TextureMod(int matIndex, string category, string maidGuid, Texture modTexture, string property) : base(matIndex, category, maidGuid, property)
		{
			ModTexture = modTexture;
		}

		public override bool IsActive(Material material)
		{
			return material.GetTexture(Property) == ModTexture;
		}

		public override void ApplyMod(Material material)
		{
			material.SetTexture(Property, ModTexture);
		}
	}
	internal class FloatMod : MaterialMod
	{
		public float Value { get; internal set; }

		public FloatMod(int matIndex, string category, string maidGuid, string property, float value) : base(matIndex, category, maidGuid, property)
		{
			Value = value;
		}
		public override bool IsActive(Material material)
		{
			return material.GetFloat(Property) == Value;
		}

		public override void ApplyMod(Material material)
		{
			material.SetFloat(Property, Value);
		}
	}

	internal class ColorMod : MaterialMod
	{
		[JsonConverter(typeof(ColorJsonConverter))]
		public Color Color { get; internal set; }

		public ColorMod(int matIndex, string category, string maidGuid, string property, Color color) : base(matIndex, category, maidGuid, property)
		{
			Color = color;
		}
		public override bool IsActive(Material material)
		{
			return material.GetColor(Property) == Color;
		}

		public override void ApplyMod(Material material)
		{
			material.SetColor(Property, Color);
		}
	}
}