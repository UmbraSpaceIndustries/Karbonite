using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace OpenResourceSystem
{
    public class ORSHeatMapScanner : PartModule
    {
        [KSPField(isPersistant = false)]
        public string resourceName = "";

        [KSPEvent(guiActive = true, guiName = "Enable Heat Map", active = true)]
        public void DisplayResource()
        {
            ORSHeatMap.setDisplayedResource(resourceName);
        }

        [KSPEvent(guiActive = true, guiName = "Disable Heat Map", active = true)]
        public void HideResource()
        {
            ORSHeatMap.setDisplayedResource("");
        }

        public override void OnStart(PartModule.StartState state)
        {
            if (state == StartState.Editor) { return; }
            this.part.force_activate();
        }

        public override void OnUpdate()
        {
            Events["DisplayResource"].active = Events["DisplayResource"].guiActive = !ORSHeatMap.resourceIsDisplayed(resourceName);
            Events["DisplayResource"].guiName = "Display " + resourceName + " Heat Map";
            Events["HideResource"].active = Events["HideResource"].guiActive = ORSHeatMap.resourceIsDisplayed(resourceName);
            Events["HideResource"].guiName = "Hide " + resourceName + " Heat Map";
            ORSHeatMap.updatePlanetaryResourceMap();
        }

    }
}
