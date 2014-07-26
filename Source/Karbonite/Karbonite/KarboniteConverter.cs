using OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Karbonite
{
    public class KarboniteConverter : PartModule
    {
        [KSPField] 
        public string convertAnimationName = "Convert";

        private bool _isConverting;
        private List<ModuleGenerator> _generators;

        public Animation ConvertAnimation
        {
            get
            {
                return part.FindModelAnimators(convertAnimationName)[0];
            }
        }

        public override void OnStart(PartModule.StartState state)
        {
            FindGenerators();
            ConvertAnimation[convertAnimationName].layer = 3;
        }

        public override void OnLoad(ConfigNode node)
        {
            FindGenerators();
        }

        public override void OnAwake()
        {
            FindGenerators();
        }

        public override void OnUpdate()
        {
            CheckForSpace();
            CheckForConverting();
            base.OnUpdate();
        }

        private void CheckForSpace()
        {
            foreach (var g in _generators)
            {
                foreach(var op in g.outputList)
                {
                    var spaceAvailable = GetShipResourceSpace(op.name);
                    if(spaceAvailable == 0)
                    {
                        g.Shutdown();
                    }
                }
            }
        }


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

        private void FindGenerators()
        {
            if (vessel != null)
            {
                if (part.Modules.Contains("ModuleGenerator"))
                {
                    _generators = part.Modules.OfType<ModuleGenerator>().ToList();
                } 
            }
        }

        private void CheckForConverting()
        {
            if(_generators.Any(g=>g.generatorIsActive))
            {
                if (!ConvertAnimation.isPlaying)
                {
                    ConvertAnimation[convertAnimationName].speed = 1;
                    ConvertAnimation.Play(convertAnimationName);
                }
            }
        }
    }

    public class KarboniteDrill : PartModule
    {
        [KSPField]
        public string deployAnimationName = "Deploy";
        public string drillAnimationName = "Drill";

        [KSPField]
        [KSPEvent(guiActive = true, guiName = "Deploy", active = true)]
        public void DeployDrill()
        {
            _isDeployed = true;
            Events["RetractDrill"].active = false;
        }

        public void RetractDrill()
        {
            _isDeployed = true;
            Events["RetractDrill"].active = false;
        }
        
        private bool _isDeployed;
        private bool _isDrilling;

        private List<ORSModuleResourceExtraction> _extractors;

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
                return part.FindModelAnimators(drillAnimationName)[0];
            }
        }

        public override void OnStart(PartModule.StartState state)
        {
            FindExtractors();
            DeployAnimation[deployAnimationName].layer = 3;
            DrillAnimation[drillAnimationName].layer = 3;
        }

        public override void OnLoad(ConfigNode node)
        {
            FindExtractors();
        }

        public override void OnAwake()
        {
            FindExtractors();
        }

        public override void OnUpdate()
        {
            CheckForDrilling();
            base.OnUpdate();
        }

        private void FindExtractors()
        {
            if (vessel != null)
            {
                if (part.Modules.Contains("ORSModuleResourceExtraction"))
                {
                    _extractors = part.Modules.OfType<ORSModuleResourceExtraction>().ToList();
                }
            }
        }

        private void CheckForDrilling()
        {
            if (_extractors.Any(e => e.IsEnabled))
            {
                if (!DrillAnimation.isPlaying)
                {
                    DrillAnimation[drillAnimationName].speed = 1;
                    DrillAnimation.Play(drillAnimationName);
                }
            }
        }
    }
}
