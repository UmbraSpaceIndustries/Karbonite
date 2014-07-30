using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Karbonite
{
    class KarboniteResourceConverter : PartModule
    {
        //Adapted from ModuleResourceConverter by okbillybunnyface and released as free to use/edit to the KSP community.
        private double efficiency = 1.00;

        [KSPField(isPersistant = true, guiActive = true, guiName = "Activated")]
        public bool converterIsActive = false;

        [KSPField(isPersistant = true)]
        public bool deactivateIfFull = true;

        [KSPField(isPersistant = true)]
        public bool deactivateIfEmpty = false;

        [KSPField(isPersistant = true)]
        public string displayName = "Converter";


        public override string GetInfo()
        {
            String info = "Deactivates if full: " + deactivateIfFull + "\n";
            info = String.Concat(info, "Deactivates if empty: " + deactivateIfEmpty + "\n");

            info = String.Concat(info, "\nInput Resources:\n");
            foreach (ModuleGenerator.GeneratorResource resource in inputList)
            {
                info = String.Concat(info, resource.name + " (" + resource.rate + " units/second)\n");
            }


            info = String.Concat(info, "\nOutput Resources:\n");
            foreach (ModuleGenerator.GeneratorResource resource in outputList)
            {
                info = String.Concat(info, resource.name + " (" + resource.rate + " units/second)\n");
            }

            return info;
        }

        private ResourceTracker[] inputResources;
        public List<ModuleGenerator.GeneratorResource> inputList = new List<ModuleGenerator.GeneratorResource>();
        public List<ModuleGenerator.GeneratorResource> outputList = new List<ModuleGenerator.GeneratorResource>();

        public override void OnLoad(ConfigNode node)
        {
            inputList.Clear();
            outputList.Clear();

            if (node.HasValue("deactivateIfFull"))
            {
                deactivateIfFull = Boolean.Parse(node.GetValue("deactivateIfFull"));
            }
            if (node.HasValue("deactivateIfEmpty"))
            {
                deactivateIfEmpty = Boolean.Parse(node.GetValue("deactivateIfEmpty"));
            }

            foreach (ConfigNode subNode in node.nodes)
            {
                ModuleGenerator.GeneratorResource stuff;
                switch (subNode.name)
                {
                    case "INPUT_RESOURCE":
                        stuff = new ModuleGenerator.GeneratorResource();
                        stuff.Load(subNode);
                        inputList.Add(stuff);
                        break;
                    case "OUTPUT_RESOURCE":
                        stuff = new ModuleGenerator.GeneratorResource();
                        stuff.Load(subNode);
                        outputList.Add(stuff);
                        break;
                }
            }

            inputResources = new ResourceTracker[inputList.Count];
            int temp = 0;
            foreach (ModuleGenerator.GeneratorResource resource in inputList)
            {
                inputResources[temp] = new ResourceTracker(resource.name, resource.id, resource.rate);
                temp++;
            }

            //Set up name on UI
            Actions["ActivateAction"].guiName = "Activate " + displayName;
            Actions["ShutdownAction"].guiName = "Shutdown " + displayName;
            Actions["ToggleAction"].guiName = "Toggle " + displayName;
            Events["Activate"].guiName = "Activate " + displayName;
            Events["Deactivate"].guiName = "Deactivate " + displayName;
        }

        public override void OnSave(ConfigNode node)
        {
            node.AddValue("deactivateIfFull", deactivateIfFull);
            node.AddValue("deactivateIfEmpty", deactivateIfEmpty);
            foreach (ModuleGenerator.GeneratorResource resource in inputList)
            {
                resource.Save(node.AddNode("INPUT_RESOURCE"));
            }
            foreach (ModuleGenerator.GeneratorResource resource in outputList)
            {
                resource.Save(node.AddNode("OUTPUT_RESOURCE"));
            }
        }

        public override void OnStart(StartState state)
        {
            Events["Activate"].active = true;
            Events["Deactivate"].active = false;
        }

        public override void OnAwake()
        {
            if (inputList == null)
            {
                inputList = new List<ModuleGenerator.GeneratorResource>();
            }
            if (outputList == null)
            {
                outputList = new List<ModuleGenerator.GeneratorResource>();
            }
        }

        public override void OnUpdate()
        {
            if (converterIsActive)
            {
                efficiency = 1.00;

                //Gather resources
                for (int i = 0; i < inputResources.Length; i++)
                {
                    inputResources[i].request = inputResources[i].rate * (double)Time.deltaTime * (double)TimeWarp.CurrentRate;
                    inputResources[i].amount += part.RequestResource(inputResources[i].id, inputResources[i].request - inputResources[i].amount);

                    double ratio = inputResources[i].amount / inputResources[i].request;
                    if (ratio < efficiency) { efficiency = ratio; }
                }

                if (efficiency == 0 && deactivateIfEmpty) Deactivate();//Out of something! Get more! (The converter stops running if there is none of a reagent anyway)

                //Subtract required resources
                for (int i = 0; i < inputResources.Length; i++)
                {
                    inputResources[i].amount -= inputResources[i].request * efficiency;
                }

                //Deliver output resources
                foreach (ModuleGenerator.GeneratorResource resource in outputList)
                {
                    double output = part.RequestResource(resource.id, -resource.rate * efficiency * (double)Time.deltaTime * (double)TimeWarp.CurrentRate);
                    if (output >= 0 && deactivateIfFull) Deactivate();//Too full of something. Make room!
                }
            }
        }

        [KSPAction("Activate Converter")]
        public void ActivateAction(KSPActionParam param)
        {
            Activate();
        }

        [KSPAction("Shutdown Converter")]
        public void ShutdownAction(KSPActionParam param)
        {
            Deactivate();
        }

        [KSPAction("Toggle Converter")]
        public void ToggleAction(KSPActionParam param)
        {
            if (converterIsActive)
            {
                Deactivate();
            }
            else
            {
                Activate();
            }
        }

        [KSPEvent(guiActive = true, guiName = "Activate Converter", active = true, guiActiveUnfocused = false, guiActiveEditor = false, externalToEVAOnly = true)]
        public void Activate()
        {
            Events["Activate"].active = false;
            Events["Deactivate"].active = true;

            converterIsActive = true;
        }

        [KSPEvent(guiActive = true, guiName = "Deactivate Converter", active = false, guiActiveUnfocused = false, guiActiveEditor = false, externalToEVAOnly = true)]
        public void Deactivate()
        {
            Events["Activate"].active = true;
            Events["Deactivate"].active = false;

            converterIsActive = false;
        }

        private class ResourceTracker
        {
            public int id;
            public double rate, amount, request;

            public ResourceTracker(string name, int id, double rate)
            {
                this.id = id;
                this.rate = rate;
                this.amount = 0;
                this.request = 0;
            }
        }
    }

}
