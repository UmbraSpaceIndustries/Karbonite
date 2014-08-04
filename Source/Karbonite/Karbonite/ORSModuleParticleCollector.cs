using System;
using System.Diagnostics;
using LibNoise.Unity.Operator;
using OpenResourceSystem;

namespace Karbonite
{
    class ORSModuleParticleCollector : ORSResourceSuppliableModule
    {
        [KSPField(isPersistant = true)]
        public bool CollectorIsEnabled = false;

        [KSPField(isPersistant = true)]
        public bool isDisabled = true;

        [KSPField(isPersistant = true)]
        public int currentresource = 0;
        
        [KSPField(isPersistant = false)]
        public float particleRate = 0;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Resource")]
        public string currentresourceStr;

        [KSPField(isPersistant = false)] 
        public float ecRequirement = 0.1f;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Flow")]
        public string resflow;

        protected float resflowf = 0;

		[KSPField(isPersistant = true)]
		public float railsTime;
		[KSPField(isPersistant = true)]
		public float railsFlow;
		protected float railsPump = 0;

        [KSPEvent(guiActive = true, guiName = "Activate Collector", active = true)]
        public void ActivateCollector()
        {
            CollectorIsEnabled = true;
        }

        [KSPEvent(guiActive = true, guiName = "Disable Collector", active = true)]
        public void DisableCollector()
        {
            CollectorIsEnabled = false;
        }


        [KSPEvent(guiActive = true, guiName = "Toggle Resource", active = true)]
        public void ToggleResource()
        {
            currentresource++;


            if (ORSAtmosphericResourceHandler.getAtmosphericResourceName(vessel.mainBody.flightGlobalsIndex, currentresource) == null && ORSAtmosphericResourceHandler.getAtmosphericResourceContent(vessel.mainBody.flightGlobalsIndex, currentresource) > 0 && currentresource != 0)
            {
                ToggleResource();
            }


            if (currentresource >= ORSAtmosphericResourceHandler.getAtmosphericCompositionForBody(vessel.mainBody.flightGlobalsIndex).Count)
            {
                currentresource = 0;
            }
        }


        [KSPAction("Activate Collector")]
        public void ActivateCollectorAction(KSPActionParam param)
        {
            ActivateCollector();
        }


        [KSPAction("Disable Collector")]
        public void DisableCollectorAction(KSPActionParam param)
        {
            DisableCollector();
        }


        [KSPAction("Toggle Collector")]
        public void ToggleCollectorAction(KSPActionParam param)
        {
            if (CollectorIsEnabled)
            {
                DisableCollector();
            }
            else
            {
                ActivateCollector();
            }
        }


        [KSPAction("Toggle Resource")]
        public void ToggleResourceAction(KSPActionParam param)
        {
            ToggleResource();
        }

        public override void OnStart(PartModule.StartState state)
        {
			Actions["ToggleResourceAction"].guiName = Events["ToggleResource"].guiName = String.Format("Toggle Resource");

            if (state == StartState.Editor) { return; }
            this.part.force_activate();

			if (railsTime > 0)
			{
				float timeLapsed = (float)(HighLogic.CurrentGame.UniversalTime - railsTime);
				railsPump = timeLapsed * railsFlow;
			}
		}

        public override void OnUpdate()
        {
            Events["ActivateCollector"].active = (!CollectorIsEnabled) && (!isDisabled);
            Events["DisableCollector"].active = (CollectorIsEnabled) && (!isDisabled);
            Events["ToggleResource"].active = (CollectorIsEnabled) && (!isDisabled);
            Fields["resflow"].guiActive = (CollectorIsEnabled) && (!isDisabled);
            Fields["currentresourceStr"].guiActive = (CollectorIsEnabled) && (!isDisabled);
            double respcent = ORSAtmosphericResourceHandler.getAtmosphericResourceContent(vessel.mainBody.flightGlobalsIndex, currentresource) * 100;
            string resname = ORSAtmosphericResourceHandler.getAtmosphericResourceDisplayName(vessel.mainBody.flightGlobalsIndex, currentresource);
            if (resname != null)
            {
                currentresourceStr = resname + "(" + respcent + "%)";
            }
            resflow = resflowf.ToString("0.0000");
        }

        public override void OnFixedUpdate()
        {
			railsTime = (float) HighLogic.CurrentGame.UniversalTime;
			railsFlow = 0;
            if (CollectorIsEnabled)
            {
                string atmospheric_resource_name = ORSAtmosphericResourceHandler.getAtmosphericResourceName(vessel.mainBody.flightGlobalsIndex, currentresource);
                if (atmospheric_resource_name != null)
                {
					if (railsPump > 0)
					{
                        ORSHelper.fixedRequestResource(part, atmospheric_resource_name, -railsPump);
						railsPump = 0;
					}

                    //range is 10% of the atmosphere
                    var range = ORSHelper.getMaxAtmosphericAltitude(vessel.mainBody) * 1.1;
                    double resourcedensity = PartResourceLibrary.Instance.GetDefinition(atmospheric_resource_name).density;
                    double respcent = ORSAtmosphericResourceHandler.getAtmosphericResourceContent(vessel.mainBody.flightGlobalsIndex, currentresource);

                    //If we're in the narrow band of the upper atmosphere
                    if (vessel.altitude <= range 
                        && respcent > 0 
                        && vessel.altitude >= ORSHelper.getMaxAtmosphericAltitude(vessel.mainBody))
                    {
                        double powerrequirements = particleRate/0.15f*ecRequirement;
                        double particles = particleRate/resourcedensity;
                        double CollectedParticles = particles*respcent;
                        float powerreceived =
                            (float)
                                Math.Max(
                                    part.RequestResource("ElectricCharge", powerrequirements*TimeWarp.fixedDeltaTime),
                                    0);
                        float powerpcnt = (float) (powerreceived/powerrequirements/TimeWarp.fixedDeltaTime);
						railsFlow = (float) CollectedParticles * powerpcnt;
                        resflowf =
                            (float)
                                ORSHelper.fixedRequestResource(part, atmospheric_resource_name,
                                    -CollectedParticles*powerpcnt*TimeWarp.fixedDeltaTime);

                        resflowf = -resflowf/TimeWarp.fixedDeltaTime;

					
					}
                }
            }
        }


        public override string getResourceManagerDisplayName()
        {
            return "Atmospheric Collector";
        }
    }
}
