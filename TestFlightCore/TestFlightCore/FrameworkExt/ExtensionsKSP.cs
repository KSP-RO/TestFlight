using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KSP;
using UnityEngine;

namespace KSPAlternateResourcePanel
{
    /// <summary>
    /// Class containing some extensions for KSP Classes
    /// </summary>
    public static class KSPExtensions
    {
        /// <summary>
        /// Returns the Stage number at which this part will be separated from the vehicle.
        /// </summary>
        /// <param name="p">Part to Check</param>
        /// <returns>Stage at which part will be decoupled. Returns -1 if the part will never be decoupled from the vessel</returns>
        internal static Int32 DecoupledAt(this Part p)
        {
            return CalcDecoupleStage(p);
        }

        /// <summary>
        /// Worker to find the decoupled at value
        /// </summary>
        /// <returns>Stage at which part will be decoupled. Returns -1 if the part will never be decoupled from the vessel</returns>
        private static Int32 CalcDecoupleStage(Part pTest)
        {
            Int32 stageOut = -1;

            //Is this part a decoupler
            if (pTest.Modules.OfType<ModuleDecouple>().Count() > 0 || pTest.Modules.OfType<ModuleAnchoredDecoupler>().Count() > 0)
            {
                stageOut = pTest.inverseStage;
            }
            //if not look further up the vessel tree
            else if (pTest.parent != null)
            {
                stageOut = CalcDecoupleStage(pTest.parent);
            }
            return stageOut;
        }

    }
}
