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

        private List<USI_ResourceConverter> _converters;
 
        private bool _isConverting;
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
            CheckForConverting();
            base.OnUpdate();
        }

        private void FindGenerators()
        {
            if (vessel != null)
            {
                if (part.Modules.Contains("USI_ResourceConverter"))
                {
                    _converters = part.Modules.OfType<USI_ResourceConverter>().ToList();
                } 
            }
        }

        private void CheckForConverting()
        {
            if (_converters.Any(c => c.converterIsActive))
            {
                if (!ConvertAnimation.isPlaying)
                {
                    ConvertAnimation[convertAnimationName].speed = 1;
                    ConvertAnimation.Play(convertAnimationName);
                }
            }
        }
    }
}
