using System.Collections.Generic;
using System.Linq;
using ORSExtensions;
using UnityEngine;

namespace Karbonite
{
    public class KarboniteAtmoExtractor : PartModule
    {
        [KSPField]
        public string deployAnimationName = "Deploy";
        
        [KSPField]
        public string drillAnimationName = "Filter";

        [KSPField(isPersistant = true)]
        private bool isDeployed = false;

        [KSPField(isPersistant = true)]
        private bool _isDrilling;
        
        private StartState _state;

        [KSPEvent(guiName = "Deploy Extractor", guiActive = true, externalToEVAOnly = true, guiActiveEditor = true, active = true, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void DeployDrill()
        {
            SetDeployedState(1);
        }

        [KSPEvent(guiName = "Retract Extractor", guiActive = true, externalToEVAOnly = true, guiActiveEditor = true, active = true, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void RetractDrill()
        {
            SetRetractedState(-1);
        }

        [KSPAction("Deploy Extractor")]
        public void DeployDrillAction(KSPActionParam param)
        {
            if (!isDeployed)
            {
                DeployDrill();
            }
        }

        [KSPAction("Retract Extractor")]
        public void RetractDrillAction(KSPActionParam param)
        {
            if (isDeployed)
            {
                RetractDrill();
            }
        }


        [KSPAction("Toggle Extractor")]
        public void ToggleDrillAction(KSPActionParam param)
        {
            if (isDeployed)
            {
                RetractDrill();
            }
            else
            {
                DeployDrill();
            }
        }

        [KSPAction("Begin Extraction")]
        public void BeginExtractionAction(KSPActionParam param)
        {
            if (isDeployed && !_isDrilling)
            {
                ActivateExtractors();
            }
        }

        [KSPAction("Stop Extraction")]
        public void StopExtractionAction(KSPActionParam param)
        {
            if (isDeployed && _isDrilling)
            {
                DisableExtractors();
                EnableExtractors();
            }
        }

        [KSPAction("Toggle Extraction")]
        public void ToggleExtractionAction(KSPActionParam param)
        {
            if (isDeployed)
            {
                if (_isDrilling)
                {
                    DisableExtractors();
                    EnableExtractors();
                }
                else
                {
                    ActivateExtractors();
                }
            }
        }

        private List<ORSModuleAirScoop> _extractors;

        public Animation DeployAnimation
        {
            get
            {
                return part.FindModelAnimators(deployAnimationName)[0];
            }
        }
        public Animation DrillAnimation
        {
            get
            {
                if (drillAnimationName == "") return null;
                return part.FindModelAnimators(drillAnimationName)[0];
            }
        }

        public override void OnStart(StartState state)
        {
            _state = state;
            FindExtractors();
            CheckAnimationState();
            DeployAnimation[deployAnimationName].layer = 3;
            if (drillAnimationName != "")
            {
                DrillAnimation[drillAnimationName].layer = 4;
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            FindExtractors();
            CheckAnimationState();
        }

        public override void OnUpdate()
        {
            if (vessel != null)
            {
                if (!isDeployed)
                {
                    DisableExtractors();
                }
                else
                {
                    CheckForExtracting();
                }
            }
            base.OnUpdate();
        }


        private void CheckAnimationState()
        {
            if (isDeployed)
            {
                SetDeployedState(1000);
            }
            else
            {
                SetRetractedState(-1000);
            }
        }
        private void FindExtractors()
        {
            if (vessel != null)
            {
                if (part.Modules.Contains("ORSModuleAirScoop"))
                {
                    _extractors = part.Modules.OfType<ORSModuleAirScoop>().ToList();
                }
            }
        }

        private void CheckForExtracting()
        {
            if (_extractors.Any(e => e.scoopIsEnabled) && isDeployed)
            {
                if (!DrillAnimation.isPlaying)
                {
                    DrillAnimation[drillAnimationName].speed = 1;
                    DrillAnimation.Play(drillAnimationName);
                }
            }
        }

        private void SetRetractedState(int speed)
        {
            isDeployed = false;
            Events["RetractDrill"].active = false;
            Events["DeployDrill"].active = true;
            PlayDeployAnimation(speed);
            DisableExtractors();
        }

        private void SetDeployedState(int speed)
        {
            isDeployed = true;
            Events["DeployDrill"].active = false;
            Events["RetractDrill"].active = true;
            PlayDeployAnimation(speed);
            EnableExtractors();
        }

        private void PlayDeployAnimation(int speed)
        {
            if (speed < 0)
            {
                DeployAnimation[deployAnimationName].time = DeployAnimation[deployAnimationName].length;
            }
            DeployAnimation[deployAnimationName].speed = speed;
            DeployAnimation.Play(deployAnimationName);            
        }

        private void DisableExtractors()
        {
            if (vessel == null || _extractors == null) return;
            foreach (var e in _extractors)
            {
                e.isDisabled = true;
                e.DisableScoop();
            }
        }

        private void EnableExtractors()
        {
            if (vessel == null || _extractors == null) return; 
            foreach (var e in _extractors)
            {
                e.isDisabled = false;
            }
        }

        private void ActivateExtractors()
        {
            if (vessel == null || _extractors == null) return;
            foreach (var e in _extractors)
            {
                e.ActivateScoop();
            }
            _isDrilling = true;
        }
    }
}