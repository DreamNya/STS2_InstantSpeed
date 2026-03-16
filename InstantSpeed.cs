using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace InstantSpeed;

[ModInitializer("Initialize")]
public static class InstantSpeedMod
{
    public static void Initialize()
    {
        var harmony = new Harmony("DreamNya.InstantSpeed");
        harmony.PatchAll(typeof(InstantSpeedMod).Assembly);
    }
}

[HarmonyPatch(
    typeof(Cmd),
    // Cmd所有方法最终都汇入Wait方法
    nameof(Cmd.Wait),
    [typeof(float), typeof(CancellationToken), typeof(bool)]
)]
public static class Patch_Cmd_Wait
{
    [HarmonyPrefix]
    public static bool Prefix(float seconds, ref Task __result)
    {
        // 仅限战斗
        if (NCombatRoom.Instance != null && seconds > 0f)
        {
            // 终止等待任务
            __result = Task.CompletedTask;
            return false;
        }

        return true;
    }
}

// 播放速度加倍
[HarmonyPatch(typeof(NCardFlyVfx), "PlayAnim")]
public static class Patch_NCardFlyVfx
{
    [HarmonyPrefix]
    public static void Prefix(ref float ____speed, ref float ____accel)
    {
        if (NCombatRoom.Instance == null)
        {
            return;
        }

        ____speed *= 2f;
        ____accel *= 2f;
    }
}

[HarmonyPatch(typeof(NCardFlyShuffleVfx), "PlayAnim")]
public static class Patch_NCardFlyShuffleVfx
{
    [HarmonyPrefix]
    public static void Prefix(ref float ____speed, ref float ____accel)
    {
        if (NCombatRoom.Instance == null)
        {
            return;
        }

        ____speed *= 2f;
        ____accel *= 2f;
    }
}

// 持续时间减半
[HarmonyPatch(typeof(NCardFlyPowerVfx), "GetDurationInternal")]
public static class Patch_NCardFlyPowerVfx
{
    [HarmonyPostfix]
    public static void Postfix(ref float __result)
    {
        if (NCombatRoom.Instance == null)
        {
            return;
        }

        __result *= 0.5f;
    }
}
