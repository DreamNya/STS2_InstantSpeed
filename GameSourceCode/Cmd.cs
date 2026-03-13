using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;

namespace MegaCrit.Sts2.Core.Commands
{
	// Token: 0x02000C6F RID: 3183
	[NullableContext(1)]
	[Nullable(0)]
	public static class Cmd
	{
		// Token: 0x06007F4A RID: 32586 RVA: 0x002A3794 File Offset: 0x002A1994
		public static Task Wait(float seconds, bool ignoreCombatEnd = false)
		{
			return Cmd.Wait(seconds, default(CancellationToken), ignoreCombatEnd);
		}

		// Token: 0x06007F4B RID: 32587 RVA: 0x002A37B4 File Offset: 0x002A19B4
		public static async Task Wait(float seconds, CancellationToken cancelToken, bool ignoreCombatEnd = false)
		{
			if (!NonInteractiveMode.IsActive)
			{
				if (seconds > 0f)
				{
					if (NGame.Instance != null)
					{
						if (SaveManager.Instance.PrefsSave.FastMode == FastModeType.Instant)
						{
							return;
						}
						if (!ignoreCombatEnd && CombatManager.Instance.IsEnding)
						{
							return;
						}
					}
					SceneTree sceneTree = (SceneTree)Engine.GetMainLoop();
					SceneTreeTimer sceneTreeTimer = sceneTree.CreateTimer((double)seconds, true, false, false);
					await Cmd.WaitInternal(sceneTreeTimer, cancelToken);
				}
			}
		}

		// Token: 0x06007F4C RID: 32588 RVA: 0x002A3808 File Offset: 0x002A1A08
		private static Task WaitInternal(SceneTreeTimer timer, CancellationToken cancellationToken)
		{
			Cmd.<>c__DisplayClass2_0 CS$<>8__locals1 = new Cmd.<>c__DisplayClass2_0();
			CS$<>8__locals1.timer = timer;
			CS$<>8__locals1.cancellationToken = cancellationToken;
			CS$<>8__locals1.tcs = new TaskCompletionSource();
			CS$<>8__locals1.timer.Timeout += CS$<>8__locals1.<WaitInternal>g__Receive|0;
			if (CS$<>8__locals1.cancellationToken.CanBeCanceled)
			{
				CS$<>8__locals1.cancellationToken.Register(delegate
				{
					CS$<>8__locals1.tcs.TrySetCanceled(CS$<>8__locals1.cancellationToken);
				});
			}
			return CS$<>8__locals1.tcs.Task;
		}

		// Token: 0x06007F4D RID: 32589 RVA: 0x002A387C File Offset: 0x002A1A7C
		public static async Task CustomScaledWait(float fastSeconds, float standardSeconds, bool ignoreCombatEnd = false, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!NonInteractiveMode.IsActive)
			{
				if (SaveManager.Instance.PrefsSave.FastMode != FastModeType.Instant)
				{
					if (ignoreCombatEnd || !CombatManager.Instance.IsEnding)
					{
						switch (SaveManager.Instance.PrefsSave.FastMode)
						{
						case FastModeType.Normal:
							await Cmd.Wait(standardSeconds, cancellationToken, ignoreCombatEnd);
							goto IL_015A;
						case FastModeType.Fast:
							await Cmd.Wait(fastSeconds, cancellationToken, ignoreCombatEnd);
							goto IL_015A;
						case FastModeType.Instant:
							goto IL_015A;
						}
						throw new ArgumentOutOfRangeException();
						IL_015A:;
					}
				}
			}
		}
	}
}
