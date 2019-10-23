using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;


namespace EvaFuel
{
	[KSPAddon(KSPAddon.Startup.MainMenu, false)]
	public class EVAFuelGlobals : MonoBehaviour
	{
		public static bool changeEVAPropellent;
		void Start()
		{
			changeEVAPropellent = false;
		}
	}

	[KSPAddon(KSPAddon.Startup.EveryScene, false)]
	class SelectEVAFuelType : MonoBehaviour
	{
		public static SelectEVAFuelType Instance;

		public enum Answer
		{
			inActive,
			notAnswered,
			cancel,
			answered
		};
		public Answer answer = Answer.inActive;
		public float lastTimeTic = 0;

		private Rect settingsRect = new Rect(200, 200, 275, 400);

		int curResIndex = -1;
		Vector2 scrollPosition1 = Vector2.zero;
		static List<PartResourceDefinition> allResources = null;
		static List<string> allResourcesDisplayNames = null;
		static List<string> fuelResources = null;
		static List<string> bannedResources = null;

		public string selectedFuel;

		public SelectEVAFuelType()
		{
		}
		GUIStyle smallButtonStyle, smallScrollBar;

		void Start()
		{
			if (String.IsNullOrEmpty(ROOT_PATH))
			{
				ROOT_PATH = KSPUtil.ApplicationRootPath;
			}

			Instance = this;
			smallButtonStyle = new GUIStyle(HighLogic.Skin.button);
			smallButtonStyle.stretchHeight = false;
			smallButtonStyle.fixedHeight = 20f;

			smallScrollBar = new GUIStyle(HighLogic.Skin.verticalScrollbar);
			smallScrollBar.fixedWidth = 8f;
		}

		void OnGUI()
		{
			if (answer == Answer.inActive)
				return;
			if (Time.realtimeSinceStartup - lastTimeTic > 0.25)
			{
				answer = Answer.inActive;
				return;
			}
#if false
            DifficultyOptionsMenu dom = (DifficultyOptionsMenu)FindObjectOfType(typeof(DifficultyOptionsMenu));
            List<GameObject> GameObjects = new List<GameObject>(FindObjectsOfType<GameObject>());
            foreach (var go in GameObjects)
                if (go.name == "GameDifficulty dialog handler")
                {
                    //   go.SetActive(false);
                    var o = go.GetComponents<DialogGUIToggleButton>();
                    foreach (var o1 in o)
                        Log.Info("o1: " + o1.label);
                    
                }
            
            if (dom)
            {
                Log.Info("DifficultyOptionsMenu found");
                dom.enabled = false;


            }
#endif
			Draw();
		}

		string setLabel()
		{
			return "xxx";
		}
		public void Draw()
		{
			if (allResources == null)
				getAllResources();

			// The settings are only available in the space center
			GUI.skin = HighLogic.Skin;
			settingsRect = GUILayout.Window("EVAFuelSettings".GetHashCode(),
													  settingsRect,
													  SettingsWindowFcn,
													  "EVA Fuel Settings",
													  GUILayout.ExpandWidth(true),
													  GUILayout.ExpandHeight(true));
		}
		public static String ROOT_PATH = String.Empty;
		public readonly static string MOD = Assembly.GetAssembly(typeof(EvaFuelManager)).GetName().Name;
		static string EVA_FUELRESOURCES = "FUELRESOURCES";
		static string BANNED_RESOURCES = "BANNED";
		public static String EVAFUEL_NODE = MOD;

		public List<String> getFuelResources(bool banned = false)
		{
			ConfigNode configFile = new ConfigNode();
			ConfigNode configFileNode = new ConfigNode();
			ConfigNode configDataNode;
			List<string> fr = new List<String>();
			string fname = ROOT_PATH + "GameData/" + MOD + "/PluginData/fuelResources.cfg";

			configFile = ConfigNode.Load(fname);
			if (configFile != null)
			{
				configFileNode = configFile.GetNode(EVAFUEL_NODE);

				if (configFileNode != null)
				{
					if (banned)
						configDataNode = configFileNode.GetNode(BANNED_RESOURCES);
					else
						configDataNode = configFileNode.GetNode(EVA_FUELRESOURCES);
					if (configDataNode != null)
						fr = configDataNode.GetValuesList("resource");
				}
				else
					Log.Error("NODENAME not found: " + EVAFUEL_NODE);
			}
			else
				Log.Error("File not found: " + fname);

			return fr;
		}


		void fillResourceDisplayNames()
		{
			if (fuelResources == null || allResources == null)
				getAllResources();
			allResourcesDisplayNames = new List<string>();
			int cnt = 0;
			if (fuelRes && fuelResources.Count > 0)
			{

				foreach (var s in fuelResources)
				{
					try
					{
						var ar = allResources.Find(o => o.name == s);
						if (ar.displayName != null)
							allResourcesDisplayNames.Add(ar.displayName);
						else
							allResourcesDisplayNames.Add(ar.name);
						if (ar.name == HighLogic.CurrentGame.Parameters.CustomParams<EVAFuelSettings>().ShipPropellantName)
							curResIndex = cnt;
						cnt++;

					}
					catch
					{
						Log.Error("Can't find resource: " + s + " in allResources");
					}
				}
			}
			else
			{
				foreach (var ar in allResources)
				{
					if (bannedResources.Contains(ar.name))
						continue;
					if (ar.displayName != null)
						allResourcesDisplayNames.Add(ar.displayName);
					else
						allResourcesDisplayNames.Add(ar.name);
					if (ar.name == HighLogic.CurrentGame.Parameters.CustomParams<EVAFuelSettings>().ShipPropellantName)
						curResIndex = cnt;
					cnt++;
				}

			}
		}

		void getAllResources()
		{
			allResources = new List<PartResourceDefinition>();

			foreach (PartResourceDefinition rs in PartResourceLibrary.Instance.resourceDefinitions)
			{
				allResources.Add(rs);
			}
			allResources = allResources.OrderBy(o => o.displayName).ToList();
			fuelResources = getFuelResources();
			bannedResources = getFuelResources(true);

			fillResourceDisplayNames();

		}
		bool allRes = false;
		bool fuelRes = true;
		void SettingsWindowFcn(int windowID)
		{

			GUILayout.BeginVertical();
			GUILayout.BeginHorizontal();
			GUILayout.Label("Select EVA Propellent from list below");
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.BeginVertical();
			var newallRes = GUILayout.Toggle(allRes, "All resources");
			GUILayout.EndVertical();
			GUILayout.BeginVertical();
			var newfuelRes = GUILayout.Toggle(fuelRes, "Fuel resources");
			GUILayout.EndVertical();
			if (newfuelRes && allRes)
			{
				allRes = false;
				fuelRes = true;
				fillResourceDisplayNames();
			}
			else
				 if (newallRes & fuelRes)
			{
				allRes = true;
				fuelRes = false;
				fillResourceDisplayNames();
			}
			GUILayout.EndHorizontal();
			GUILayout.Space(10);
			scrollPosition1 = GUILayout.BeginScrollView(scrollPosition1);

			curResIndex = GUILayout.SelectionGrid(curResIndex, allResourcesDisplayNames.ToArray(), 1, smallButtonStyle);

			GUILayout.EndScrollView();
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("OK"))
			{
				answer = Answer.answered;
				if (allRes)
				{
					selectedFuel = allResources[curResIndex].name;
				}
				else
				{
					int cnt = 0;
					for (int i = 0; i < fuelResources.Count; i++)
					{
						try
						{
							var ar = allResources.Find(o => fuelResources[i] == o.name);

							if (cnt == curResIndex)
							{
								selectedFuel = ar.name;
								break;
							}
							cnt++;
						}
						catch
						{ }
					}
				}
			}
			if (GUILayout.Button("Cancel"))
				answer = Answer.cancel;
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
			// This call allows the user to drag the window around the screen
			GUI.DragWindow();
		}
	}



	// http://forum.kerbalspaceprogram.com/index.php?/topic/147576-modders-notes-for-ksp-12/#comment-2754813
	// search for "Mod integration into Stock Settings
	// HighLogic.CurrentGame.Parameters.CustomParams<EVAFuelSettings>()


	public class EvaFuelDifficultySettings : GameParameters.CustomParameterNode
	{
		public override string Title { get { return ""; } }
		public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
		public override string Section { get { return "EVA Fuel"; } }
		public override string DisplaySection { get { return "EVA Fuel"; } }
		public override int SectionOrder { get { return 1; } }
		public override bool HasPresets { get { return false; } }

		[GameParameters.CustomParameterUI("Enable mod for this save?")]
		public bool ModEnabled = true;

		//[GameParameters.CustomStringParameterUI("Only works if KIS is installed")]
		//public string KISInfo = "";
		[GameParameters.CustomParameterUI("Enable KIS integration?")]
		public bool KISIntegrationEnabled = true;

		[GameParameters.CustomParameterUI("Show fuel transfer message?")]
		public bool ShowInfoMessage = false;

		[GameParameters.CustomParameterUI("Show low fuel warning?")]
		public bool ShowLowFuelWarning = true;

		[GameParameters.CustomParameterUI("Disable warning when landed/splashed?")]
		public bool DisableLowFuelWarningLandSplash = true;

#if false
        [GameParameters.CustomParameterUI("Fill from Pod", toolTip = "(if false, unable to refuel for entire mission")]
        public bool fillFromPod = true;
#endif
		public override void SetDifficultyPreset(GameParameters.Preset preset)
		{
			ModEnabled = true;
			KISIntegrationEnabled = true;
			ShowInfoMessage = false;
			DisableLowFuelWarningLandSplash = true;
			//      fillFromPod = true;
		}
		public override bool Interactible(MemberInfo member, GameParameters parameters)
		{
			if (HighLogic.CurrentGame == null)
				return true;
			return !EVAFuelGlobals.changeEVAPropellent;
		}
	}

	public class EVAFuelSettings : GameParameters.CustomParameterNode
	{
		public override string Title { get { return ""; } }
		public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
		public override string Section { get { return "EVA Fuel"; } }
		public override string DisplaySection { get { return "EVA Fuel"; } }
		public override int SectionOrder { get { return 2; } }
		public override bool HasPresets { get { return false; } }



		[GameParameters.CustomFloatParameterUI("EVA Fuel Tank Max", minValue = 0.5f, maxValue = 15.0f, asPercentage = false, displayFormat = "0.0",
			toolTip = "Maximum amount of EVA fuel")]
		public double EvaTankFuelMax = 5.0f;

		[GameParameters.CustomFloatParameterUI("EVA Fuel Conversion Factor", minValue = 0.1f, maxValue = 10.0f, asPercentage = false, displayFormat = "0.0",
		  toolTip = "Each 1 unit of ship fuel will become this many units of Eva Fuel")]
		public double FuelConversionFactor = 1.0f;

		// [GameParameters.CustomStringParameterUI("EVA Propellent Type", autoPersistance = true, lines = 1, title = "EVA Propellent Type")]       
		public string EvaPropellantName = "EVA Propellant";

		[GameParameters.CustomParameterUI("Change EVA Propellent Type")]
		public bool changeEVAPropellent = false;

		[GameParameters.CustomStringParameterUI("EVA Propellent Type", autoPersistance = true, lines = 1, title = "EVA Propellent Type")]
		public string ShipPropellantName = "MonoPropellant";


		[GameParameters.CustomParameterUI("Add resource to command pods", toolTip = "Command pod is defined as parts with crew & MonoProp)")]
		public bool addToCmdPods = true;

		[GameParameters.CustomFloatParameterUI("Amount of resource to add:", minValue = 0.1f, maxValue = 10.0f, asPercentage = false, displayFormat = "0.0")]
		public double resourcesAmtToAdd = 5.0f;

		[GameParameters.CustomParameterUI("Multiply resource added by max crew")]
		public bool resourcePerCrew = true;


		[GameParameters.CustomStringParameterUI("Ship Electricity Name", autoPersistance = true, lines = 2, title = "Ship Electricity Name")]
		public string ShipElectricityName = "ElectricCharge";

		[GameParameters.CustomIntParameterUI("Screen Message Life", maxValue = 10)]
		public int ScreenMessageLife = 5;

		// Currently not used
		//[GameParameters.CustomIntParameterUI("Screen Message Warning Life", maxValue = 10)]
		//public int ScreenMessageWarningLife = 10;


		public override void SetDifficultyPreset(GameParameters.Preset preset)
		{
			EvaTankFuelMax = 5.0f;
			FuelConversionFactor = 1.0f;
			ShipPropellantName = "MonoPropellant";
			ShipElectricityName = "ElectricCharge";
			ScreenMessageLife = 5;
			//ScreenMessageWarningLife = 10;


		}

		public override bool Enabled(MemberInfo member, GameParameters parameters)
		{
			EVAFuelGlobals.changeEVAPropellent = changeEVAPropellent;
			return true; //otherwise return true
		}
		private const string controlLock = "EVAFuelSettings";



		public override bool Interactible(MemberInfo member, GameParameters parameters)
		{
			if (changeEVAPropellent)
			{
				SelectEVAFuelType.Instance.lastTimeTic = Time.realtimeSinceStartup;
				switch (SelectEVAFuelType.Instance.answer)
				{
					case SelectEVAFuelType.Answer.inActive:
						SelectEVAFuelType.Instance.answer = SelectEVAFuelType.Answer.notAnswered;
						InputLockManager.SetControlLock(ControlTypes.KEYBOARDINPUT, controlLock);
						return false;

					case SelectEVAFuelType.Answer.answered:
						changeEVAPropellent = false;
						SelectEVAFuelType.Instance.answer = SelectEVAFuelType.Answer.inActive;
						ShipPropellantName = SelectEVAFuelType.Instance.selectedFuel;
						InputLockManager.RemoveControlLock(controlLock);
						break;

					case SelectEVAFuelType.Answer.cancel:
						SelectEVAFuelType.Instance.answer = SelectEVAFuelType.Answer.inActive;
						changeEVAPropellent = false;
						InputLockManager.RemoveControlLock(controlLock);

						break;
				}
				return false;
			}
			return true; //otherwise return true
		}

		public override IList ValidValues(MemberInfo member)
		{
			return null;
		}

	}

}