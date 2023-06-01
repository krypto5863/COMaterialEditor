using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using BepInEx;
using COMaterialEditor.MaterialManager;
using Mono.Cecil.Cil;
using UnityEngine;
using Screen = UnityEngine.Screen;

namespace COMaterialEditor
{
	internal static class SampleUi
	{
		private static Rect _uiRect = new Rect(Screen.width / 3f, Screen.height / 4f, Screen.width / 3f, Screen.height / 1.5f);
		private static Rect _dragWindow = new Rect(0, 0, 10000, 20);
		private static Rect _closeButton = new Rect(0, 0, 25, 15);
		private static Vector2 _scrollPosition;

		private static readonly GUIStyle MainWindow = new GUIStyle(GUI.skin.window)
		{
			normal =
			{
				background = UiToolbox.MakeWindowTex(new Color(0, 0, 0, 0.05f), new Color(0, 0, 0, 0.5f)),
				textColor = new Color(1, 1, 1, 0.05f)
			},
			hover =
			{
				background = UiToolbox.MakeWindowTex(new Color(0.3f, 0.3f, 0.3f, 0.3f), new Color(1, 1, 0, 0.5f)),
				textColor = new Color(1, 1, 1, 0.3f)
			},
			onNormal =
			{
				background = UiToolbox.MakeWindowTex(new Color(0.3f, 0.3f, 0.3f, 0.6f), new Color(1, 1, 0, 0.5f))
			}
		};

		private static readonly GUIStyle Sections = new GUIStyle(GUI.skin.box)
		{
			normal =
			{
				background = UiToolbox.MakeTex(2, 2, new Color(0, 0, 0, 0.3f))
			}
		};

		private static readonly GUIStyle Sections2 = new GUIStyle(GUI.skin.box)
		{
			normal =
			{
				background = UiToolbox.MakeTexWithRoundedCorner(new Color(0, 0, 0, 0.6f))
			}
		};

		private static readonly GUIStyle Sections3 = new GUIStyle(GUI.skin.box)
		{
			normal =
			{
				background = UiToolbox.MakeTexWithRoundedCorner(new Color(1, 1, 0, 0.5f))
			}
		};

		private static readonly GUIStyle ToggleLarge = new GUIStyle(GUI.skin.label)
		{
			fontSize = GUI.skin.label.fontSize + 20
		};

		private static string _openCategory = string.Empty;
		private static TrackedMaterial _openMaterial;
		private static bool _importMenu;
		private static bool _exportMenu;

		private static readonly string ExportPath = Paths.GameRootPath + "\\COME_Overlay Exports";
		private static readonly OpenFileDialog OpenFileDialog = new OpenFileDialog { InitialDirectory = Paths.GameRootPath };
		private static readonly OpenFileDialog SaveFileDialog = new OpenFileDialog { InitialDirectory = Paths.GameRootPath };

		internal static void DrawUi()
		{
			// Make a background box
			_uiRect = GUILayout.Window(32472, _uiRect, GuiWindowControls, "COMaterialEditor", MainWindow);
		}

		private static void GuiWindowControls(int id)
		{
			_closeButton.x = _uiRect.width - (_closeButton.width + 5);
			_dragWindow.width = _uiRect.width - (_closeButton.width + 5);

			GUI.DragWindow(_dragWindow);

			if (GUI.Button(_closeButton, "X"))
			{
				CoMaterialEditor.DrawGui = false;
			}

			_scrollPosition = GUILayout.BeginScrollView(_scrollPosition, alwaysShowHorizontal: false, alwaysShowVertical: true);
			var hideFooter = false;

			if (_exportMenu)
			{
				hideFooter = true;
			} 
			else if (_importMenu)
			{
				hideFooter = true;
			}
			else
			{
				var groupedTrackedMaterials = MaterialTracker.TrackedMaterials
					.GroupBy(r => r.BodySkin.Category)
					.OrderBy(r => r.Key);

				foreach (var trackedMat in groupedTrackedMaterials)
				{
					DrawMaterialSection(trackedMat);
				}
			}

			GUILayout.EndScrollView();

			if (hideFooter)
			{
				GUILayout.BeginHorizontal();
				if (GUILayout.Button("Export"))
				{
					_importMenu = true;
				}

				if (GUILayout.Button("Import"))
				{
					_exportMenu = true;
				}

				GUILayout.EndHorizontal();
			}

			UiToolbox.ChkMouseClick(_uiRect);
		}

		private static void DrawMaterialSection(IGrouping<string, TrackedMaterial> trackedMat)
		{
			GUILayout.BeginVertical(style: Sections);

			GUILayout.BeginHorizontal();
			GUILayout.Label(trackedMat.Key, ToggleLarge);
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("☰"))
			{
				_openCategory = _openCategory == trackedMat.Key ? string.Empty : trackedMat.Key;
			}

			GUILayout.EndHorizontal();

			if (_openCategory != trackedMat.Key)
			{
				GUILayout.EndVertical();
				return;
			}

			foreach (var mat in trackedMat.OrderBy(r => r.Material?.name ?? string.Empty))
			{
				if (mat.OriginalProperties.Count <= 0)
				{
					continue;
				}

				GUILayout.BeginVertical(Sections);

				GUILayout.BeginHorizontal();
				GUILayout.Label(mat.Material?.name);
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("☰"))
				{
					_openMaterial = _openMaterial == mat ? null : mat;
				}

				GUILayout.EndHorizontal();

				if (_openMaterial != mat)
				{
					GUILayout.EndVertical();
					continue;
				}

				foreach (var texture in mat.OriginalProperties)
				{
					if (texture.Value is Texture tex)
					{
						DrawTexturePropertySection(texture.Key, tex, mat);
					} 
					else if (texture.Value is Color)
					{
						DrawColorPropertySection(texture.Key, mat);
					}
					else if (texture.Value is float)
					{
						DrawFloatPropertySection(texture.Key, mat);
					}
				}

				GUILayout.EndVertical();
			}

			GUILayout.EndVertical();
		}

		private static void DrawTexturePropertySection(string property, Texture texture, TrackedMaterial mat)
		{
			GUILayout.BeginVertical(style: Sections);
			GUILayout.BeginHorizontal(style: (mat.PropertiesWithMods.Contains(property) ? Sections3 : Sections2));
			GUILayout.FlexibleSpace();
			GUILayout.Label(property);
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.Box(mat.Material.GetTexture(property), GUILayout.MaxWidth(_uiRect.width / 2),
				GUILayout.MaxHeight(_uiRect.height / 4));
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Whiteout"))
			{
				mat.AddOrUpdateTextureMod(property, Texture2D.whiteTexture);
			}

			if (GUILayout.Button("Transparent"))
			{
				mat.AddOrUpdateTextureMod(property, CoMaterialEditor.Transparent);
			}

			if (GUILayout.Button("Un-mod"))
			{
				mat.RevertToOriginal(property);
			}

			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();

			if (GUILayout.Button("Export Original"))
			{
				if (texture is Texture2D tex)
				{
					Directory.CreateDirectory(ExportPath);
					File.WriteAllBytes(ExportPath + $"\\{tex.name}.png", tex.EncodeToPNG());
				}
			}

			if (GUILayout.Button("Export Current"))
			{
				if (mat.Material.GetTexture(property) is Texture2D tex)
				{
					Directory.CreateDirectory(ExportPath);
					File.WriteAllBytes(ExportPath + $"\\{ (string.IsNullOrEmpty(tex.name) ? mat.Material.name + property : tex.name) }.png", tex.EncodeToPNG());
				}
			}

			if (GUILayout.Button("Import"))
			{
				OpenFileDialog.ShowDialog();
				var newTexture = new Texture2D(1, 1);
				newTexture.LoadImage(File.ReadAllBytes(OpenFileDialog.FileName));
				mat.AddOrUpdateTextureMod(property, newTexture);
			}

			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
		}

		private static void DrawColorPropertySection(string property, TrackedMaterial mat)
		{
			GUILayout.BeginVertical(style: Sections);
			GUILayout.BeginHorizontal(style: (mat.PropertiesWithMods.Contains(property) ? Sections3 : Sections2));
			GUILayout.FlexibleSpace();
			GUILayout.Label(property);
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			var originalColor = mat.Material.GetColor(property);
			var currentColor = originalColor;
			currentColor.r = HorizontalSliderWithInputBox(currentColor.r, -1, 1, "R");
			currentColor.g = HorizontalSliderWithInputBox(currentColor.g, -1, 1, "G");
			currentColor.b = HorizontalSliderWithInputBox(currentColor.b, -1, 1, "B");
			currentColor.a = HorizontalSliderWithInputBox(currentColor.a, -1, 1, "A");

			if (GUILayout.Button("Un-mod"))
			{
				mat.RevertToOriginal(property);
			}

			GUILayout.EndVertical();

			if (originalColor != currentColor)
			{
				mat.AddOrUpdateColorMod(property, currentColor);
			}
		}

		private static void DrawFloatPropertySection(string property, TrackedMaterial mat)
		{
			GUILayout.BeginVertical(style: Sections);
			GUILayout.BeginHorizontal(style: (mat.PropertiesWithMods.Contains(property) ? Sections3 : Sections2));
			GUILayout.FlexibleSpace();
			GUILayout.Label(property);
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			var originalVal = mat.Material.GetFloat(property);
			var floatVal = originalVal;
			floatVal = HorizontalSliderWithInputBox(floatVal, -10f, 50f);

			if (GUILayout.Button("Un-mod"))
			{
				mat.RevertToOriginal(property);
			}

			GUILayout.EndVertical();

			if (originalVal != floatVal)
			{
				mat.AddOrUpdateFloatMod(property, floatVal);
			}
		}

		private static readonly Regex NotNumPeriod = new Regex("[^0-9.-]");
		internal static float FloatField(float initialVal, float min = 0, float max = 100)
		{
			var stringReturn = GUILayout.TextField(initialVal.ToString("0.0#####"), GUILayout.Width(75));
			stringReturn = NotNumPeriod.Replace(stringReturn, "");
			stringReturn = stringReturn.IsNullOrWhiteSpace() ? "0" : stringReturn;

			return float.TryParse(stringReturn, out var floatReturn) ? floatReturn : initialVal;
		}

		internal static float HorizontalSliderWithInputBox(float initialVal, float min = 0, float max = 100, string label = null, bool doButtons = true)
		{
			GUILayout.BeginHorizontal();

			if (label.IsNullOrWhiteSpace() == false)
			{
				GUILayout.Label(label);
			}

			initialVal = GUILayout.HorizontalSlider(initialVal, min, max, GUILayout.MaxWidth(9999));

			if (doButtons)
			{
				GUILayout.BeginHorizontal();

				GUILayout.FlexibleSpace();

				if (GUILayout.Button("<"))
				{
					initialVal -= (max * 0.01f);
				}

				if (GUILayout.Button("0"))
				{
					initialVal = 0;
				}

				if (GUILayout.Button(">"))
				{
					initialVal += (max * 0.01f);
				}

				GUILayout.EndHorizontal();
			}

			initialVal = FloatField(initialVal, min, max);
			GUILayout.EndHorizontal();

			return initialVal;
		}

		/*
		private static void DisplayExportMenu()
		{
			foreach (var maid in Extensions.GetAllMaids())
			{
				if (GUILayout.Button(maid.status.fullNameJpStyle))
				{
					SaveFileDialog.ShowDialog();
				}
			}
		}
		private static void DisplayImportMenu()
		{
			foreach (var maid in Extensions.GetAllMaids())
			{
				if (GUILayout.Button(maid.status.fullNameJpStyle))
				{
					OpenFileDialog.ShowDialog();
				}
			}
		}
		*/
	}
}
