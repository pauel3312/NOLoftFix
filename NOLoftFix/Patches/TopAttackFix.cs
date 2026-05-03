using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace NOLF.Patches;

[HarmonyPatch(typeof(OpticalSeeker))]
public class MissileFixes
{

    public static float computeTTT(OpticalSeeker seeker)
    {
        Vector3 vector3 = (seeker.knownPos - seeker.missile.GlobalPosition()) with
        {
            y = 0.0f
        };
        return seeker.targetDist / Mathf.Max(Vector3.Dot(vector3.normalized, seeker.missile.rb.velocity), seeker.selfDestructAtSpeed);
    }

    [HarmonyPatch(nameof(OpticalSeeker.Initialize))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> InitializeTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var tttField = AccessTools.Field(typeof(OpticalSeeker), nameof(OpticalSeeker.timeToTarget));
        var tttComputeMethod = AccessTools.Method(typeof(MissileFixes), nameof(computeTTT));
        
        CodeInstruction? prev = null;
        foreach (var instr in instructions)
        {
            if (prev is null)
            {
                prev = instr;
                continue;
            }

            if (prev.opcode == OpCodes.Ldc_R4 &&
                Mathf.Approximately((float)prev.operand, 100f) &&
                instr.opcode == OpCodes.Stfld &&
                (FieldInfo)instr.operand == tttField)
            {
                yield return  new CodeInstruction(OpCodes.Ldarg_0);
                yield return  new CodeInstruction(OpCodes.Call, tttComputeMethod);
            } else
            {
                yield return prev;
            }        
            prev = instr;
        }
        if (prev is not null)
            yield return prev;
    }
}
