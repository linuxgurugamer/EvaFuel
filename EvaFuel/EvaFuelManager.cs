using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSP.UI.Screens.Flight;


namespace EvaFuel
{
    public class kerbalEVAFueldata
    {
        public string name;
        public double evaPropAmt;
    }


    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    public class EvaFuelManager : MonoBehaviour
    {

        static Dictionary<string, kerbalEVAFueldata> kerbalEVAlist;

        private void Start()
        {
            Log.Info("Start");
            DontDestroyOnLoad(this);
        }

        public void Awake()
        {
            Log.Info("Awake");
            GameEvents.onCrewOnEva.Add(this.onEvaStart);
            GameEvents.onCrewBoardVessel.Add(this.onEvaEnd);
            
            FileOperations fileops = new FileOperations();
            if (kerbalEVAlist == null)
                kerbalEVAlist = FileOperations.Instance.loadKerbalEvaData();
            if (kerbalEVAlist == null)
                Log.Error("Awake, kerbalEVAlist is null");


        }


        public bool ModEnabled { get { return HighLogic.CurrentGame.Parameters.CustomParams<EvaFuelDifficultySettings>().ModEnabled; } }
        public bool ShowInfoMessage { get { return HighLogic.CurrentGame.Parameters.CustomParams<EvaFuelDifficultySettings>().ShowInfoMessage; } }
        public bool ShowLowFuelWarning { get { return HighLogic.CurrentGame.Parameters.CustomParams<EvaFuelDifficultySettings>().ShowLowFuelWarning; } }
        public bool DisableLowFuelWarningLandSplash { get { return HighLogic.CurrentGame.Parameters.CustomParams<EvaFuelDifficultySettings>().DisableLowFuelWarningLandSplash; } }

        public string resourceName { get { return HighLogic.CurrentGame.Parameters.CustomParams<EVAFuelSettings>().EvaPropellantName; } }
        public string shipPropName { get { return HighLogic.CurrentGame.Parameters.CustomParams<EVAFuelSettings>().ShipPropellantName; } }

        public void onEvaStart(GameEvents.FromToAction<Part, Part> data)
        {
            Log.Info("onEvaStart, ModEnabled: " + ModEnabled.ToString());
            if (ModEnabled)
            {
                if (data.to != null && data.from != null)
                {
                    Log.Info("data.to/from not null");
                    this.onEvaHandler(data);
                }

				double evaTankFuelMax = HighLogic.CurrentGame.Parameters.CustomParams<EVAFuelSettings> ().EvaTankFuelMax;
				// Check available volume for EVA fuel - it might be reduced by some external factors
				// Default value for EVA fuel is 5 units however EVAFuel may be configured for greater amount
				// Treat propellantResourceDefaultAmount as multiplier where 5 units correspond to 100%
				//
				if (data.to.vessel.evaController != null) {
					double defaultEVAFuel = data.to.vessel.evaController.propellantResourceDefaultAmount;
					Log.Info ("default EVA resource amount: " + defaultEVAFuel);
					evaTankFuelMax *= defaultEVAFuel / 5;
				}

                double takenFuel = data.from.RequestResource(
					HighLogic.CurrentGame.Parameters.CustomParams<EVAFuelSettings>().ShipPropellantName, 
					evaTankFuelMax / 
						HighLogic.CurrentGame.Parameters.CustomParams<EVAFuelSettings>().FuelConversionFactor);
				
                double fuelRequest = takenFuel * HighLogic.CurrentGame.Parameters.CustomParams<EVAFuelSettings>().FuelConversionFactor;

                Log.Info("takenFuel: " + takenFuel.ToString() + "    fuelRequest: " + fuelRequest.ToString());
                bool rescueShip = false;
                if (fuelRequest == 0)
                { //Only check for rescue vessel status if there's no EVA fuel in the current ship.
                    var crewList = data.to.vessel.GetVesselCrew();
                    if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
                    {
                        if (Contracts.ContractSystem.Instance != null)
                        {
                            var contracts = Contracts.ContractSystem.Instance.Contracts;
                            for (int currentCrew = 0; currentCrew < crewList.Count; currentCrew++)
                            {
                                for (int currentContract = 0; currentContract < contracts.Count; currentContract++)
                                {
                                    if (contracts[currentContract].Title.Contains(crewList[currentCrew].name) && data.from.vessel.name.Contains(crewList[currentCrew].name.Split(null)[0]))
                                    {//Please do not rename your ship to have a rescue contract Kerbal name in it if the contract is active.
                                        rescueShip = true;
                                    }
                                }
                            }
                        }
                    }
                }

                //Floats and doubles don't like exact numbers. :/ Need to test for similarity rather than equality.
				if (fuelRequest + 0.001 > evaTankFuelMax)
                {
					data.to.RequestResource(resourceName, evaTankFuelMax - fuelRequest);
                    if (ShowInfoMessage)
                    {
                        ScreenMessages.PostScreenMessage("Filled EVA tank with " + Math.Round(takenFuel, 2).ToString() + " units of " + resourceName + ".", HighLogic.CurrentGame.Parameters.CustomParams<EVAFuelSettings>().ScreenMessageLife, ScreenMessageStyle.UPPER_CENTER);
                    }
                }
                else if (rescueShip == true && fuelRequest == 0)
                {
					data.to.RequestResource(resourceName, evaTankFuelMax - 1);//give one unit of eva propellant
                    if (ShowLowFuelWarning && (!DisableLowFuelWarningLandSplash || !data.from.vessel.LandedOrSplashed))
                    {
                        PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "Rescue fuel!", "Warning! There was no fuel aboard ship, so only one single unit of " + resourceName + " was able to be scrounged up for the journey!", "OK", false, HighLogic.UISkin);
                    }
                }
                else
                {
					data.to.RequestResource(resourceName, evaTankFuelMax - fuelRequest);
                    if (ShowLowFuelWarning && (!DisableLowFuelWarningLandSplash || !data.from.vessel.LandedOrSplashed))
                    {
                        PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "Low EVA Fuel!", "Warning! Only " + Math.Round(takenFuel, 2).ToString() + " units of " + HighLogic.CurrentGame.Parameters.CustomParams<EVAFuelSettings>().ShipPropellantName + " were available for EVA! Meaning you only have " + Math.Round(fuelRequest, 2).ToString() + " units of " + resourceName + "!", "OK", false, HighLogic.UISkin);
                    }
                }
            }
        }

        public void onEvaEnd(GameEvents.FromToAction<Part, Part> data)
        {
            Log.Info("onEvaEnd");
            if (ModEnabled)
            {
                double fuelLeft = data.from.RequestResource(resourceName, HighLogic.CurrentGame.Parameters.CustomParams<EVAFuelSettings>().EvaTankFuelMax);

                double fuelStored = data.to.RequestResource(HighLogic.CurrentGame.Parameters.CustomParams<EVAFuelSettings>().ShipPropellantName, -fuelLeft / HighLogic.CurrentGame.Parameters.CustomParams<EVAFuelSettings>().FuelConversionFactor);
                if (ShowInfoMessage)
                {
                    ScreenMessages.PostScreenMessage("Returned " + Math.Round(fuelLeft / HighLogic.CurrentGame.Parameters.CustomParams<EVAFuelSettings>().FuelConversionFactor, 2).ToString() + " units of " + HighLogic.CurrentGame.Parameters.CustomParams<EVAFuelSettings>().ShipPropellantName + " to ship.", HighLogic.CurrentGame.Parameters.CustomParams<EVAFuelSettings>().ScreenMessageLife, ScreenMessageStyle.UPPER_CENTER);
                }
                Log.Info("fuelLeft: " + fuelLeft.ToString() + "    fuelStored: " + fuelStored.ToString());
                data.from.RequestResource(resourceName,  fuelStored + fuelLeft);
                onBoardHandler(data);
            }
        }



        private void OnDestroy()
        {
            Log.Info("OnDestroy");
            GameEvents.onCrewOnEva.Remove(this.onEvaStart);
            
            GameEvents.onCrewBoardVessel.Remove(this.onEvaEnd);
        }


        private void onEvaHandler(GameEvents.FromToAction<Part, Part> data)
        {
            Log.Info("onEvaHandler");

            if (data.to == null || data.from == null)
                return;

            Log.Info("onEvaHandler, from: " + data.from.partInfo.name + "   to: " + data.to.partInfo.name);
            PartResource resource = null;
            var rL = data.from.Resources.Where(p => p.resourceName == resourceName);
            if (rL.Count() > 0)
                resource = data.from.Resources.Where(p => p.resourceName == resourceName).First();
            if (resource == null)
            {
                Log.Info("Resource not found: " + resourceName + " in part: " + data.from.partInfo.title);
                return;
            }
            PartResource kerbalResource = null;
            KerbalEVA kEVA = data.to.FindModuleImplementing<KerbalEVA>();
            
            var kerbalResourceList = data.to.Resources.Where(p => p.resourceName == kEVA.propellantResourceName);
            if (kerbalResourceList.Count() > 0)
                kerbalResource = kerbalResourceList.First();
            else
                Log.Info("Resource not found: " + kEVA.propellantResourceName);
            if (kerbalEVAlist == null)
                Log.Error("kerbalEVAlist is null");
            else
            {
                kerbalEVAFueldata ked;
                if (!kerbalEVAlist.TryGetValue(data.to.partInfo.title, out ked))
                {
                    ked = new kerbalEVAFueldata();
                    ked.name = data.to.partInfo.title;
                    ked.evaPropAmt = kerbalResource.maxAmount;  // New kerbals always get the maxAmount

                    kerbalEVAlist.Add(ked.name, ked);
                }

                // fillFromPod controls whether the Kerbal has a fixed amount of fuel for the entire mission or not.
                // if false, then the kerbal is not able to fill from the pod

                if (HighLogic.CurrentGame.Parameters.CustomParams<EvaFuelDifficultySettings>().fillFromPod)
                {
                    double giveBack = kerbalResource.maxAmount - ked.evaPropAmt;                    

                    double sentBackAmount = data.from.RequestResource(this.resourceName, -1 * giveBack);
                    kerbalResource.amount = ked.evaPropAmt;

                    Log.Info(string.Format("Returned {0} {1} to {2}",
                        sentBackAmount,
                        this.resourceName,
                        data.from.partInfo.title));
                }
                FileOperations.Instance.saveKerbalEvaData(kerbalEVAlist);
            }
            

          //  lastPart = data.to;

            Log.Info(
                string.Format("[{0}] Caught OnCrewOnEva event to part ({1}) containing this resource ({2})",
                    this.GetType().Name,
                    data.to.partInfo.title,
                    this.resourceName));
        }

        private void onBoardHandler(GameEvents.FromToAction<Part, Part> data)
        {
            Log.Info("onBoardHandler");
            if (data.to == null || data.from == null)
                return;
            
            KerbalEVA kEVA = data.from.FindModuleImplementing<KerbalEVA>();
            // resourceName = kEVA.propellantResourceName;

            var fromResource = data.from.Resources.Where(p => p.info.name == resourceName).First();
            if (fromResource == null)
            {
                Log.Info("Resource not found: " + resourceName + " in part: " + data.from.partInfo.title);
                return;
            }
            kerbalEVAFueldata ked;

            if (kerbalEVAlist.TryGetValue(data.from.partInfo.title, out ked))
            {
                ked.evaPropAmt = fromResource.amount;
            }
            else
            {
                // This is needed here in case the mod is added to an existing game while a kerbal is
                // already on EVA
                ked = new kerbalEVAFueldata();

                ked.name = data.from.partInfo.title;
                ked.evaPropAmt = fromResource.amount;
                kerbalEVAlist.Add(ked.name, ked);
            }
            FileOperations.Instance.saveKerbalEvaData(kerbalEVAlist);

            if (HighLogic.CurrentGame.Parameters.CustomParams<EvaFuelDifficultySettings>().fillFromPod)
            {
                double sentAmount = data.to.RequestResource(this.resourceName, -fromResource.amount);

                fromResource.amount += sentAmount;

                Log.Info(string.Format("Returned {0} {1} to {2}",
                    -sentAmount,
                    this.resourceName,
                    data.to.partInfo.title));
            }
        }
    }
}