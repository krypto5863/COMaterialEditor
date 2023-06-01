using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace COMaterialEditor
{
	public static class Extensions
	{
		internal static readonly string[] TextureProperties = {
			"_RenderTex",
			"_MainTex",
			"_ShadowTex",
			"_OutlineTex",
			"_ToonRamp",
			"_ShadowRateToon",
			"_OutlineToonRamp"
		};

		internal static readonly string[] ColorProperties = {
			"_Color",
			"_ShadowColor",
			"_RimColor",
			"_OutlineColor"
		};

		internal static readonly string[] FloatProperties = {
			"_OutlineWidth",
			"_Shininess",
			"_RimPower",
			"_RimShift",
			"_Cutoff",
			"_FloatValue1"
		};

		public static int GetMaterialIndex(this TBodySkin tBodySkin, Material material)
		{
			var position = -1;

			if (tBodySkin?.obj == null)
			{
				return position;
			}

			foreach (var transform in tBodySkin?.obj?.transform?.GetComponentsInChildren<Transform>(true))
			{
				var component = transform.GetComponent<Renderer>();

				if (component?.sharedMaterial == null)
				{
					continue;
				}

				for (var i = 0; i < component.sharedMaterials.Length; i++)
				{
					var heldMaterial = component.sharedMaterials[i];
					if (heldMaterial != material)
					{
						continue;
					}

					position = i;
					break;
				}
			}

			return position;
		}

		public static TBodySkin GetParentTBodySkin(this Renderer render)
		{
			return render?.GetComponentInParent<TBody>()?
				.goSlot?
				.FirstOrDefault(r => r?.obj?.transform.GetComponentInChildren<Renderer>() == render);
		}

		public static Material[] GetMaterials(this TBodySkin skin)
		{
			return skin?.obj?.transform?.GetComponentsInChildren<Transform>(true)
				.Select(t => t?.GetComponent<Renderer>())
				.Where(r => r?.sharedMaterials != null)
				.SelectMany(r => r.sharedMaterials)
				.Where(m => m != null)
				.ToArray();
		}

		public static Dictionary<string,Texture> GetTextures(this Material material)
		{
			return TextureProperties.Where(material.HasProperty)
				.ToDictionary(r => r, material.GetTexture);
		}

		public static Dictionary<string, Color> GetColors(this Material material)
		{
			return ColorProperties.Where(material.HasProperty)
				.ToDictionary(r => r, material.GetColor);
		}
		public static Dictionary<string, float> GetFloats(this Material material)
		{
			return FloatProperties.Where(material.HasProperty)
				.ToDictionary(r => r, material.GetFloat);
		}
		public static IEnumerable<Maid> GetAllMaids()
		{
			return GameMain.Instance.CharacterMgr.GetStockMaidList();
		}
	}
}