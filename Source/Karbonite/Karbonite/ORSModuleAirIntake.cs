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
        public string intakeResourceStr = "IntakeAtm";

        protected float resflowf = 0;

        public override void OnUpdate()
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
                    resflow = resflowf.ToString("0.0000");
                }
            }
        }


        public override string getResourceManagerDisplayName()
        {
            return "Atmospheric Intake";
        }
    }
}
