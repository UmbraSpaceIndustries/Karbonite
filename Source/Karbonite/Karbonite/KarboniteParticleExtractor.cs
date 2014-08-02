using System.Collections.Generic;
using System.Linq;
using OpenResourceSystem;
using UnityEngine;

namespace Karbonite
{
    public class KarboniteParticleExtractor : PartModule
    {
        [KSPField]
        public string deployAnimationName = "Deploy";
        
        [KSPField]
        public string drillAnimationName = "Filter";

        [KSPField(isPersistant = true)]
        private bool isDeployed = false;

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

        private List<ORSModuleParticleCollector> _extractors;

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
                if (_extractors == null) FindExtractors();
                if (!isDeployed)
                {
                    DisableExtractors();
                }
                else
                {
                    EnableExtractors();
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
                if (part.Modules.Contains("ORSModuleParticleCollector"))
                {
                    _extractors = part.Modules.OfType<ORSModuleParticleCollector>().ToList();
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
                e.DisableCollector();
                e.isDisabled = true;
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
    }
}