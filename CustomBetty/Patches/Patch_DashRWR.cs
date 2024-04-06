using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using UnityEngine;

[HarmonyPatch(typeof(DashRWR), "Start")]
class Patch_DashRWR_Start
{
    [HarmonyPostfix]
    static void Postfix(DashRWR __instance)
    {
        if (CustomBetty2.instance.currentProfile != null)
        {
            CustomBetty2.Profile voiceProfile = CustomBetty2.instance.currentProfile;

            Debug.Log("Replacing SARH"); 
            if (voiceProfile.collisionWarning != null)
            {
               __instance.sarhLockBlip = voiceProfile.collisionWarning;
            }
        }
    }
}