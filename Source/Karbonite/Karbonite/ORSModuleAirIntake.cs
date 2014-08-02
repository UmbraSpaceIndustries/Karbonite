using OpenResourceSystem;

namespace Karbonite
{
    class ORSModuleAirIntake : ORSResourceSuppliableModule
    {
        [KSPField(isPersistant = false)]
        public float scoopair = 0;

        [KSPField(isPersistant = false)]
        public bool autoActivate = true;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Intake Flow")]
        public string resflow;

        [KSPField(isPersistant = false)]
        public string intakeResourceStr = "ScoopedAir";

        protected float resflowf = 0;


        public override void OnStart(PartModule.StartState state)
        {
            if (state == StartState.Editor) { return; }
            if (vessel != null)
            {
                if (autoActivate)
                {
                    this.part.force_activate();
                }
            }
        }


        public override void OnUpdate()
        {
            resflow = resflowf.ToString("0.0000");
        }


        public override void OnFixedUpdate()
        {
            if (vessel != null)
            {
                double airdensity = part.vessel.atmDensity;
                double airspeed = part.vessel.srf_velocity.magnitude + 40.0;
                double air = airspeed*airdensity*scoopair;


                if (vessel.altitude <= ORSHelper.getMaxAtmosphericAltitude(vessel.mainBody))
                {
                    double scoopedAtm = air;
                    resflowf =
                        (float)
                            ORSHelper.fixedRequestResource(part, intakeResourceStr, -scoopedAtm*TimeWarp.fixedDeltaTime);
                    resflowf = -resflowf/TimeWarp.fixedDeltaTime;
                }
            }
        }


        public override string getResourceManagerDisplayName()
        {
            return "Atmospheric Intake";
        }
    }
}
