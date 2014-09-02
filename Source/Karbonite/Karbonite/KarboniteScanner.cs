using System.Collections.Generic;
using System.Linq;
using ORSExtensions;
using UnityEngine;

namespace Karbonite
{
    public class KarboniteScanner : PartModule
    {
        [KSPField]
        public string deployAnimationName = "Deploy";
        
        [KSPField]
        public string scanAnimationName = "Scan";

        [KSPField(isPersistant = true)]
        private bool isDeployed = false;
       
        [KSPField(isPersistant = true)]
        private bool _isScanning;
        
        [KSPEvent(guiName = "Deploy Scanner", guiActive = true, externalToEVAOnly = true, guiActiveEditor = true, active = true, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void DeployScanner()
        {
            SetDeployedState(1);
        }

        [KSPEvent(guiName = "Retract Scanner", guiActive = true, externalToEVAOnly = true, guiActiveEditor = true, active = true, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void RetractScanner()
        {
            SetRetractedState(-1);
        }

        [KSPAction("Deploy Scanner")]
        public void DeployScannerAction(KSPActionParam param)
        {
            if (!isDeployed)
            {
                DeployScanner();
            }
        }

        [KSPAction("Retract Scanner")]
        public void RetractScannerAction(KSPActionParam param)
        {
            if (isDeployed)
            {
                RetractScanner();
            }
        }


        [KSPAction("Toggle Scanner")]
        public void ToggleScannerAction(KSPActionParam param)
        {
            if (isDeployed)
            {
                RetractScanner();
            }
            else
            {
                DeployScanner();
            }
        }

        [KSPAction("Begin Scanning")]
        public void BeginScanningAction(KSPActionParam param)
        {
            if (isDeployed && !_isScanning)
            {
                ActivateScanners();
            }
        }

        [KSPAction("Stop Scanning")]
        public void StopScanningAction(KSPActionParam param)
        {
            if (isDeployed && _isScanning)
            {
                DisableScanners();
                EnableScanners();
            }
        }

        [KSPAction("Toggle Scanning")]
        public void ToggleExtractionAction(KSPActionParam param)
        {
            if (isDeployed)
            {
                if (_isScanning)
                {
                    DisableScanners();
                    EnableScanners();
                }
                else
                {
                    ActivateScanners();
                }
            }
        }

        private List<ORSSurfaceScanner> _scanners;

        public Animation DeployAnimation
        {
            get
            {
                return part.FindModelAnimators(deployAnimationName)[0];
            }
        }
        public Animation ScanAnimation
        {
            get
            {
                if (scanAnimationName == "") return null;
                return part.FindModelAnimators(scanAnimationName)[0];
            }
        }

        public override void OnStart(StartState state)
        {
            FindScanners();
            CheckAnimationState();
            DeployAnimation[deployAnimationName].layer = 3;
            if (scanAnimationName != "")
            {
                ScanAnimation[scanAnimationName].layer = 4;
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            FindScanners();
            CheckAnimationState();
        }

        public override void OnUpdate()
        {
            if (vessel != null)
            {
                if (!isDeployed)
                {
                    DisableScanners();
                }
                else
                {
                    CheckForScanning();
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
        private void FindScanners()
        {
            if (vessel != null)
            {
                if (part.Modules.Contains("ORSSurfaceScanner"))
                {
                    _scanners = part.Modules.OfType<ORSSurfaceScanner>().ToList();
                }
            }
        }

        private void CheckForScanning()
        {
            if (_scanners.Any(e => e.isActive) && isDeployed)
            {
                if (!ScanAnimation.isPlaying)
                {
                    ScanAnimation[scanAnimationName].speed = 1;
                    ScanAnimation.Play(scanAnimationName);
                }
            }
        }


        private void SetRetractedState(int speed)
        {
            isDeployed = false;
            Events["RetractScanner"].active = false;
            Events["DeployScanner"].active = true;
            PlayDeployAnimation(speed);
            DisableScanners();
        }

        private void SetDeployedState(int speed)
        {
            isDeployed = true;
            Events["DeployScanner"].active = false;
            Events["RetractScanner"].active = true;
            PlayDeployAnimation(speed);
            EnableScanners();
        }

        private void PlayDeployAnimation(int speed)
        {
            if (speed < 0)
            {
                if (scanAnimationName != "")
                {
                    ScanAnimation.Stop(scanAnimationName);
                }

                DeployAnimation[deployAnimationName].time = DeployAnimation[deployAnimationName].length;
            }
            DeployAnimation[deployAnimationName].speed = speed;
            DeployAnimation.Play(deployAnimationName);            
        }

        private void DisableScanners()
        {
            if (vessel == null || _scanners == null) return; 
            foreach (var e in _scanners)
            {
                e.isActive = false;
            }
            _isScanning = false;
        }

        private void EnableScanners()
        {
            if (vessel == null || _scanners == null) return;
            foreach (var e in _scanners)
            {
                e.isActive = true;
            }
        }

        private void ActivateScanners()
        {
            if (vessel == null || _scanners == null) return;
            foreach (var e in _scanners)
            {
                e.isActive = true;
            }
            _isScanning = true;
        }
    }
}