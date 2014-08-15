/**
 * Umbra Space Industries Resource Converter
 * 
 * This is a derivative work of Thunder Aerospace Corporation's library for  
 * the Kerbal Space Program, which is (c) 2013, Taranis Elsu, who retains the copyright for 
 * all unmodified portions of this work.  Enhancements and extensions are (c) 2014 Bob Palmer.  
 *  
 * Kerbal Space Program is Copyright (C) 2013 Squad. See http://kerbalspaceprogram.com/. This
 * project is in no way associated with nor endorsed by Squad.
 * 
 * This code is licensed under the Attribution-NonCommercial-ShareAlike 3.0 (CC BY-NC-SA 3.0)
 * creative commons license. See <http://creativecommons.org/licenses/by-nc-sa/3.0/legalcode>
 * for full details.
 * 
 * Attribution — You are free to modify this code, so long as you mention that the resulting
 * work is based upon or adapted from this code.
 * 
 * Non-commercial - You may not use this work for commercial purposes.
 * 
 * Share Alike — If you alter, transform, or build upon this work, you may distribute the
 * resulting work only under the same or similar license to the CC BY-NC-SA 3.0 license.
 * 
 * Note that Thunder Aerospace Corporation and Umbra Space Industries are ficticious entities 
 * created for entertainment purposes. It is in no way meant to represent a real entity.
 *  Any similarity to a real entity is purely coincidental.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace USI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using UnityEngine;

    namespace USI
    {
        public class UsiResourceConverter : PartModule
        {
            private static char[] delimiters = { ' ', ',', '\t', ';' };
            private const int SECONDS_PER_DAY = 24 * 60 * 60;

            [KSPField]
            public string converterName = "USI Converter";

            [KSPField(guiActive = true, guiName = "Converter Status")]
            public string converterStatus = "Unknown";

            [KSPField(isPersistant = true)]
            public bool converterEnabled = false;

            [KSPField]
            public float conversionRate = 1.0f;

            [KSPField]
            public bool requiresOxygenAtmo = false;

            private double lastUpdateTime = 0.0f;

            private List<ResourceRatio> inputResourceList;
            private List<ResourceRatio> outputResourceList;
            private ConfigNode _node;

            public override void OnAwake()
            {
                this.Log("OnAwake");
                base.OnAwake();
                UpdateResourceLists();
            }

            public override void OnStart(PartModule.StartState state)
            {
                this.Log("OnStart: " + state);
                base.OnStart(state);

                if (state != StartState.Editor)
                {
                    part.force_activate();
                }
            }

            public override void OnFixedUpdate()
            {
                base.OnFixedUpdate();

                if (Time.timeSinceLevelLoad < 1.0f || !FlightGlobals.ready)
                {
                    return;
                }

                if (lastUpdateTime == 0.0f)
                {
                    // Just started running
                    lastUpdateTime = Planetarium.GetUniversalTime();
                    return;
                }

                double deltaTime = Math.Min(Planetarium.GetUniversalTime() - lastUpdateTime, Utilities.MaxDeltaTime);
                lastUpdateTime += deltaTime;

                if (converterEnabled)
                {
                    if (requiresOxygenAtmo && !vessel.mainBody.atmosphereContainsOxygen)
                    {
                        converterStatus = "Atmo lacks oxygen.";
                        return;
                    }

                    double desiredAmount = conversionRate / SECONDS_PER_DAY * deltaTime;
                    double maxElectricityDesired = Math.Min(desiredAmount, conversionRate / SECONDS_PER_DAY * Math.Max(Utilities.ElectricityMaxDeltaTime, Time.fixedDeltaTime)); // Limit the max electricity consumed when reloading a vessel

                    // Limit the resource amounts so that we do not produce more than we have room for, nor consume more than is available
                    foreach (ResourceRatio output in outputResourceList)
                    {
                        if (!output.allowExtra)
                        {
                            if (output.resource.id == Utilities.ElectricityId && desiredAmount > maxElectricityDesired)
                            {
                                // Special handling for electricity
                                double desiredElectricity = maxElectricityDesired * output.ratio;
                                double availableSpace = -part.IsResourceAvailable(output.resource, -desiredElectricity);
                                desiredAmount = desiredAmount * (availableSpace / desiredElectricity);
                            }
                            else
                            {
                                double availableSpace = -part.IsResourceAvailable(output.resource, -desiredAmount * output.ratio);
                                desiredAmount = availableSpace / output.ratio;
                            }

                            if (desiredAmount <= 0.000000001)
                            {
                                // Out of space, so no need to run
                                converterStatus = "No space for more " + output.resource.name;
                                return;
                            }
                        }
                    }

                    foreach (ResourceRatio input in inputResourceList)
                    {
                        if (input.resource.id == Utilities.ElectricityId && desiredAmount > maxElectricityDesired)
                        {
                            // Special handling for electricity
                            double desiredElectricity = maxElectricityDesired * input.ratio;
                            double amountAvailable = part.IsResourceAvailable(input.resource, desiredElectricity);
                            desiredAmount = desiredAmount * (amountAvailable / desiredElectricity);
                        }
                        else
                        {
                            double amountAvailable = part.IsResourceAvailable(input.resource, desiredAmount * input.ratio);
                            desiredAmount = amountAvailable / input.ratio;
                        }

                        if (desiredAmount <= 0.000000001)
                        {
                            // Not enough input resources
                            converterStatus = "Not enough " + input.resource.name;
                            return;
                        }
                    }

                    foreach (ResourceRatio input in inputResourceList)
                    {
                        double desired;
                        if (input.resource.id == Utilities.ElectricityId)
                        {
                            desired = Math.Min(desiredAmount, maxElectricityDesired) * input.ratio;
                        }
                        else
                        {
                            desired = desiredAmount * input.ratio;
                        }

                        double actual = part.TakeResource(input.resource, desired);

                        if (actual < (desired * 0.999))
                        {
                            this.LogWarning("OnFixedUpdate: obtained less " + input.resource.name + " than expected: " + desired.ToString("0.000000000") + "/" + actual.ToString("0.000000000"));
                        }
                    }

                    foreach (ResourceRatio output in outputResourceList)
                    {
                        double desired;
                        if (output.resource.id == Utilities.ElectricityId)
                        {
                            desired = Math.Min(desiredAmount, maxElectricityDesired) * output.ratio;
                        }
                        else
                        {
                            desired = desiredAmount * output.ratio;
                        }

                        double actual = -part.TakeResource(output.resource.id, -desired);

                        if (actual < (desired * 0.999) && !output.allowExtra)
                        {
                            this.LogWarning("OnFixedUpdate: put less " + output.resource.name + " than expected: " + desired.ToString("0.000000000") + "/" + actual.ToString("0.000000000"));
                        }
                    }

                    converterStatus = "Running";
                }
            }

            public override void OnLoad(ConfigNode node)
            {
                this.Log("OnLoad: " + node);
                base.OnLoad(node);
                lastUpdateTime = Utilities.GetValue(node, "lastUpdateTime", lastUpdateTime);

                _node = node;
                UpdateResourceLists();
                UpdateEvents();
            }

            public override void OnSave(ConfigNode node)
            {
                node.AddValue("lastUpdateTime", lastUpdateTime);
                this.Log("OnSave: " + node);
            }

            public override string GetInfo()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(base.GetInfo());
                sb.Append("\nContains the ");
                sb.Append(converterName);
                sb.Append(" module\n  Inputs: ");
                sb.Append(String.Join(", ", inputResourceList.Select(value => value.resource.name + ", " + value.ratio).ToArray()));
                sb.Append("\n  Outputs: ");
                sb.Append(String.Join(", ", outputResourceList.Select(value => value.resource.name + ", " + value.ratio).ToArray()));
                sb.Append("\n  Conversion Rate: ");
                sb.Append(conversionRate);
                if (requiresOxygenAtmo)
                {
                    sb.Append("\nRequires an atmosphere containing Oxygen.");
                }
                sb.Append("\n");

                return sb.ToString();
            }

            [KSPEvent(active = false, guiActive = true, guiName = "Activate Converter")]
            public void ActivateConverter()
            {
                converterEnabled = true;
                UpdateEvents();
            }

            [KSPEvent(active = false, guiActive = true, guiName = "Deactivate Converter")]
            public void DeactivateConverter()
            {
                converterEnabled = false;
                UpdateEvents();
            }

            [KSPAction("Toggle Converter")]
            public void ToggleConverter(KSPActionParam param)
            {
                converterEnabled = !converterEnabled;
                UpdateEvents();
            }

            private void UpdateEvents()
            {
                Events["ActivateConverter"].active = !converterEnabled;
                Events["DeactivateConverter"].active = converterEnabled;

                if (!converterEnabled)
                {
                    converterStatus = "Inactive";
                }
            }

            private void UpdateResourceLists()
            {
                if (_node != null)
                {
                    inputResourceList = new List<ResourceRatio>();
                    outputResourceList = new List<ResourceRatio>();

                    foreach (ConfigNode subNode in _node.nodes)
                    {
                        var res = new ResourceRatio();
                        switch (subNode.name)
                        {
                            case "INPUT_RESOURCE":
                                res = GetResourceFromNode(subNode);
                                inputResourceList.Add(res);
                                break;
                            case "OUTPUT_RESOURCE":
                                res = GetResourceFromNode(subNode);
                                outputResourceList.Add(res);
                                break;
                        }
                    }
                }
                Events["ActivateConverter"].guiName = "Activate " + converterName;
                Events["DeactivateConverter"].guiName = "Deactivate " + converterName;
                Actions["ToggleConverter"].guiName = "Toggle " + converterName;
                Fields["converterStatus"].guiName = converterName;
            }

            private ResourceRatio GetResourceFromNode(ConfigNode node)
            {
                var rr = new ResourceRatio();
                var resName = node.GetValue("resource");
                rr.resource = PartResourceLibrary.Instance.GetDefinition(resName);
                rr.ratio = Double.Parse(node.GetValue("ratio"));
                rr.allowExtra = bool.Parse(node.GetValue("dumpExcess"));

                return rr;
            }
        }
    }
}
