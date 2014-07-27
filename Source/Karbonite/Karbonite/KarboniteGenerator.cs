using System;
using UnityEngine;
namespace Karbonite
{
	public class KarboniteGenerator : PartModule
	{
		[KSPField(isPersistant = true)]
		public bool running;

		// Current output in kW
		[KSPField(guiActive = true, guiName = "Current Output", guiUnits = "kW", guiFormat = "N")]
		public float currentOutput;

		// Maximum output in kW
		[KSPField(guiActive = true, guiName = "Maximum Output", guiUnits = "kW", guiFormat = "N")]
		public float maxOutput;

		// kJ per unit of Karbonite
		[KSPField()]
		public float conversionRatio;



		[KSPEvent(guiActive = true, guiName = "Start Generator")]
		public void EnableEvent()
		{
			Events["EnableEvent"].active = false;
			Events["DisableEvent"].active = true;
			running = true;
		}

		[KSPEvent(guiActive = true, guiName = "Stop Generator", active = false)]
		public void DisableEvent()
		{
			Events["EnableEvent"].active = true;
			Events["DisableEvent"].active = false;
			running = false;
			currentOutput = 0f;
		}

		public override void OnUpdate ()
		{
			if (running) {

				var dt = TimeWarp.fixedDeltaTime;
				var space = GetShipResourceSpace ("ElectricCharge");
				//output at either 1% of availiable space, or the max output value, whichever is lower
				var targetOutput = Math.Min (0.01f * space, maxOutput * dt);
				if (targetOutput > 0.01f) {
					var requiredKarbonite = targetOutput / conversionRatio;
					var usedKarbonite = part.RequestResource ("Karbonite", requiredKarbonite);
					var generatedElectricCharge = part.RequestResource ("ElectricCharge", -usedKarbonite * conversionRatio);
					currentOutput = (float)(-generatedElectricCharge / dt);
				} 
				else {
					currentOutput = 0f;
				}
			}
			base.OnUpdate ();
		}

		public override string GetInfo ()
		{
			return string.Format (
				"- Max Output: {0}kW\n" +
				"- Max Karbonite Use: {1}/s",
				maxOutput,
				maxOutput/conversionRatio
				);
		}

		// blatantly copied and pasted from KarboniteCoverter, should probably be put somewhere shared
		private double GetShipResourceSpace(string resName)
		{
			var space = 0d;
			if(vessel != null)
			{
				foreach(var p in vessel.parts)
				{
					if(p.Resources.Contains(resName))
					{
						var res = p.Resources[resName];
						space += (res.maxAmount - res.amount);
					}
				}
			}
			return space;
		}
	}
}

