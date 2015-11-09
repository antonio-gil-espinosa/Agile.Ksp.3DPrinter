using Agile.Ksp.Collections;
using Agile.Ksp.Extensions;
using Agile.Ksp.Wrappers.KIS;
using System;
using System.Linq;
using UnityEngine;

namespace Agile.Ksp.Printer3D
{
    [KSPModule("3D Printer")]
    public partial class Printer : PausableWorkerPartModule
    {
        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "Part", isPersistant = false)]
        public string buildingPartTitle = "None";

        [KSPField(guiActive = false, guiActiveEditor = false, isPersistant = true)]
        public double lostPercentage;

        private readonly Lazy<KISInventoryModuleWrapper> _inventory;

        private readonly Lazy<PartSelectionWindow> _partSelectionWindow;

        private AvailablePart _buildingPart;

        [KSPField(guiActive = false, guiActiveEditor = false, isPersistant = true)]
        private string buildingPartName;

        public Printer()
        {
            _inventory = Lazy.For(() => KISInventoryModuleWrapper.FromComponent((PartModule)part.GetComponent("ModuleKISInventory")));
            _partSelectionWindow = Lazy.For(() => new PartSelectionWindow(this, AvailablePartsForPrinting()));
        }

        public AvailablePart BuildingPart
        {
            get { return _buildingPart; }
            set
            {
                if (value != null)
                {
                    buildingPartTitle = value.title;
                    buildingPartName = value.partPrefab.name;
                    _buildingPart = value;
                }
                else
                {
                    buildingPartTitle = "None";
                    buildingPartName = null;
                    _buildingPart = null;
                }
            }
        }

        [KSPField]
        public double ElectricChargeConsumption { get; set; } = 100;

        public double MassPrinted
        {
            get
            {
                if (progress < 0 || BuildingPart == null)
                    return 0;

                return progress * BuildingPart.partPrefab.mass / 100;
            }
            set
            {
                if (value > 0 && BuildingPart != null)
                    progress = value / BuildingPart.partPrefab.mass * 100;
            }
        }

        protected override string CancelWorkLabel => "Cancel 3D Printing";

        protected KISInventoryModuleWrapper Inventory => _inventory.Value;

        protected override string PauseWorkLabel => "Pause 3D Printing";

        protected override string ResumeWorkLabel => "Resume 3D Printing";

        protected override string WorkingStatusLabel => "Printing";

        [KSPEvent(active = true, guiActive = true, guiName = "Start 3D Printing", guiActiveUnfocused = true)]
        public void BeginPrint()
        {
            try
            {
                AvailablePartsForPrinting();
                _partSelectionWindow.Value.Show();
            }
            catch (Exception ex)
            {
                print(ex);
                throw;
            }
        }

        public override string GetInfo()
        {
            var kgPerScond = 0.0001;
            var oreUnits = kgPerScond / PartResourceLibrary.Instance.GetDefinition("Ore").density;

            return "Inputs:".Color("bada55") +
                   Environment.NewLine +
                   " - " + Math.Round(oreUnits + (oreUnits * lostPercentage / 100), 4) + " Ore/s" +
                   Environment.NewLine +
                   " - " + ElectricChargeConsumption + " EC/s" +
                   Environment.NewLine +
                   "Output:".Color("bada55") +
                   Environment.NewLine +
                    " - " + Math.Round(kgPerScond / 1000, 2) + " Kg/s";
        }

        public float GetRequiredOreUnits(AvailablePart availablePart)
        {
            double withoutWaste = (availablePart.partPrefab.mass) / PartResourceLibrary.Instance.GetDefinition("Ore").density;
            return (float)(withoutWaste + (withoutWaste * lostPercentage / 100));
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            if (!string.IsNullOrEmpty(buildingPartName))
            {
                BuildingPart = PartLoader.LoadedPartsList.SingleOrDefault(x => x.name == buildingPartName);
            }
        }

        protected override double GetProgressPercentage()
        {
            return MassPrinted * 100 / (BuildingPart.partPrefab.mass);
        }

        protected override void OnCompleteWork()
        {
            if (BuildingPart != null)
            {
                ConfigNode configNode = new ConfigNode("PART");
                configNode.AddNode(BuildingPart.partConfig);

                Part newPart = BuildingPart.SpawnPart(Vector3.zero, Quaternion.identity, Vector3.zero, Vector3.zero);
                if (newPart.Resources != null)
                    foreach (PartResource resource in newPart.Resources)
                        resource.amount = 0;

                KISItemWrapper kisItem = Inventory.AddItem(newPart);

                if (kisItem != null)
                {
                    var storageName = Inventory.Name;
                    if (string.IsNullOrEmpty(storageName))
                        storageName = this.part.partInfo.title;

                    ScreenMessages.PostScreenMessage(BuildingPart.title + " in " + storageName + " completed.");
                }
            }
        }

        protected override void OnStartWork()
        {
            BaseEvent beginPrintEvent = Events["BeginPrint"];
            beginPrintEvent.active = false;
            beginPrintEvent.guiActive = false;
            beginPrintEvent.guiActiveUnfocused = false;
        }

        protected override void OnStopWork()
        {
            base.OnStopWork();

            BuildingPart = null;

            BaseEvent beginPrintEvent = Events["BeginPrint"];
            beginPrintEvent.active = true;
            beginPrintEvent.guiActive = true;
            beginPrintEvent.guiActiveUnfocused = true;
        }

        protected override void OnWork(double deltaTime)
        {
            lostPercentage = 0.5;

            double extruded = 0.0001 * deltaTime;
            var oreUnits = extruded / PartResourceLibrary.Instance.GetDefinition("Ore").density;
            double oreRequired = oreUnits + (oreUnits * lostPercentage / 100);

            var requiredElectricCharge = ElectricChargeConsumption * deltaTime;
            if (!part.HasResource("ElectricCharge", requiredElectricCharge, ResourceFlowMode.ALL_VESSEL))
            {
                overrideStatus = "Not enough electric charge";
                return;
            }

            if (!part.HasResource("Ore", oreRequired, ResourceFlowMode.STACK_PRIORITY_SEARCH))
            {
                overrideStatus = "Not enough ore";
                return;
            }

            part.RequestResource("Ore", oreRequired, ResourceFlowMode.STACK_PRIORITY_SEARCH);
            part.RequestResource("ElectricCharge", requiredElectricCharge, ResourceFlowMode.ALL_VESSEL);

            MassPrinted += extruded;

            Inventory.RefreshMassAndVolume();
        }

        private ListMapping<PartCategories, AvailablePart> AvailablePartsForPrinting()
        {
            ListMapping<PartCategories, AvailablePart> ret = new ListMapping<PartCategories, AvailablePart>();
            AvailablePart[] availableParts = PartLoader.LoadedPartsList.ToArray();

            foreach (AvailablePart availablePart in availableParts)
            {
                KISItemModuleWrapper mki = KISItemModuleWrapper.FromComponent(availablePart.partPrefab.GetComponent("ModuleKISItem"));
                if (mki != null)
                {
                    if (mki.VolumeOverride <= Inventory.MaxVolume)
                        ret.Add(availablePart.category, availablePart);
                }
                else
                {
                    try
                    {
                        if (availablePart.partPrefab.GetVolume() <= Inventory.MaxVolume)
                            ret.Add(availablePart.category, availablePart);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            return ret;
        }
    }
}