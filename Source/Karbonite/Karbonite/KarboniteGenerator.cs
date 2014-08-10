using System;
using System.Collections;
using UnityEngine;
namespace Karbonite
{
	public class KarboniteGenerator : PartModule
	{
        [KSPField]
        public string startAnimationName = "";

        [KSPField]
        public string activeAnimationName = "";

        public Animation StartAnimation
        {
            get
            {
                if (startAnimationName == "") return null;
                return part.FindModelAnimators(startAnimationName)[0];
            }
        }
        public Animation ActiveAnimation
        {
            get
            {
                if (activeAnimationName == "") return null;
                return part.FindModelAnimators(activeAnimationName)[0];
            }
        }


		[KSPField(isPersistant = true)]
		public bool running;

		// Current output in MW 
		[KSPField(guiActive = true, guiName = "Current Output", guiUnits = "MW", guiFormat = "N")]
		public float currentOutput;

		// Maximum output in MW
		[KSPField(guiActive = true, guiName = "Maximum Output", guiUnits = "MW", guiFormat = "N")]
		public float maxOutput;

		// MJ per unit of Karbonite
		[KSPField()]
		public float conversionRatio;

		const double smoothingFactor = 0.2;

		double smoothedOutput;

		[KSPEvent(guiActive = true, guiName = "Start Generator")]
        public void StartGenerator()
		{
            Events["StartGenerator"].active = false;
            Events["StopGenerator"].active = true;
			running = true;
            PlayStartAnimation(1);
        }

		[KSPEvent(guiActive = true, guiName = "Stop Generator", active = false)]
		public void StopGenerator()
		{
            Events["StartGenerator"].active = true;
            Events["StopGenerator"].active = false;
            running = false;
            currentOutput = 0f;
            PlayStartAnimation(-1);
        }

        [KSPAction("Start Generator")]
        public void StartGeneratorAction(KSPActionParam param)
        {
            StartGenerator();
        }
        
        [KSPAction("Stop Generator")]
        public void StopGeneratorAction(KSPActionParam param)
        {
            StopGenerator();
        }


        [KSPAction("Toggle Generator")]
        public void ToggleGeneratorAction(KSPActionParam param)
        {
            if (running)
            {
                StopGenerator();
            }
            else
            {
                StartGenerator();
            }
        }


		public override void OnStart (StartState state)
		{
			if (state != StartState.Editor) {
				part.force_activate ();
                if (startAnimationName != "")
                {
                    StartAnimation[startAnimationName].layer = 3;
                }
                if (activeAnimationName != "")
                {
                    ActiveAnimation[activeAnimationName].layer = 4;
                }
                CheckAnimationState();
            }
			base.OnStart (state);
		}

	    public override void OnLoad(ConfigNode node)
	    {
	        CheckAnimationState();
            base.OnLoad(node);
	    }

	    private void CheckAnimationState()
	    {
	        if (running)
	        {
                PlayStartAnimation(1000);    
	        }
	        else
	        {
                PlayStartAnimation(-1000);    
	        }
	    }

	    public override void OnFixedUpdate ()
		{
		    try
		    {
                if (running)
                {
                    StartCoroutine(UpdateResources());
                    PlayActiveAnimation();
                }
            }
		    catch (Exception ex)
		    {
		        print("[KAR] Error in OnFixedUpdate of KarbointeGenerator - " + ex.Message);
		    }
			base.OnFixedUpdate ();
		}

	    private void PlayActiveAnimation()
	    {
	        if (activeAnimationName != "" && running)
	        {
                if (!ActiveAnimation.isPlaying && !StartAnimation.isPlaying)
                {
                    ActiveAnimation[activeAnimationName].speed = 1;
                    ActiveAnimation.Play(activeAnimationName);
                }
            }
	    }

        private void PlayStartAnimation(int speed)
        {
            if (startAnimationName != "")
            {
                if (speed < 0)
                {
                    StartAnimation[startAnimationName].time = StartAnimation[startAnimationName].length;
                }
                StartAnimation[startAnimationName].speed = speed;
                StartAnimation.Play(startAnimationName);
            }
        }

		public IEnumerator UpdateResources(){
			yield return new WaitForFixedUpdate();
			double dt = TimeWarp.fixedDeltaTime;
			var space = GetShipResourceSpace ("ElectricCharge");
			// output at either 50% of availiable space per seccond, or the max output value, whichever is lower.
			var targetOutput = Math.Min (0.5 * space, maxOutput);
			//exponetial moving average
			smoothedOutput = (targetOutput * smoothingFactor) + (1d - smoothingFactor) * smoothedOutput;
			var output = smoothedOutput > 0 ? smoothedOutput : 0;
			var requiredKarbonite = (output * dt) / conversionRatio;
			var usedKarbonite = part.RequestResource ("Karbonite", requiredKarbonite);
			var generatedElectricCharge = part.RequestResource ("ElectricCharge", -usedKarbonite * conversionRatio);
			currentOutput = (float)(-generatedElectricCharge / dt);
		}

		public override string GetInfo ()
		{
			return string.Format (
				"- Max Output: {0}MW\n" +
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

