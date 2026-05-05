using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace NOLF.Patches;

[HarmonyPatch(typeof(OpticalSeeker))]
public class MissileFixes
{

    public static void computeTTT(OpticalSeeker seeker)
    {
        var hdir = (seeker.knownPos - seeker.missile.GlobalPosition()) with
        {
            y = 0.0f
        };
        seeker.targetDist = hdir.magnitude;
        var hvel = Vector3.Dot(hdir.normalized, seeker.missile.rb.velocity);
        var spd = Mathf.Max(hvel, seeker.selfDestructAtSpeed);
        var result = seeker.targetDist / spd;
        seeker.timeToTarget = result;
    }

    [HarmonyPatch(nameof(OpticalSeeker.Initialize))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> InitializeTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var missileNetworkSeekerModeSetter =
            AccessTools.DeclaredPropertySetter(typeof(Missile), nameof(Missile.NetworkseekerMode));
        var tttComputeMethod = AccessTools.Method(typeof(MissileFixes), nameof(computeTTT));
        
        CodeInstruction? prev = null;
        foreach (var instr in instructions)
        {
            yield return instr;
            if (instr.Calls(missileNetworkSeekerModeSetter))
            {
                yield return  new CodeInstruction(OpCodes.Ldarg_0);
                yield return  new CodeInstruction(OpCodes.Call, tttComputeMethod);
            }
        }
    }
}
