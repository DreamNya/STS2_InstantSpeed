using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Nodes.Vfx
{
	// Token: 0x02000171 RID: 369
	[ScriptPath("res://src/Core/Nodes/Vfx/NCardFlyVfx.cs")]
	public class NCardFlyVfx : Node2D
	{
		// Token: 0x170005C6 RID: 1478
		// (get) Token: 0x060011E2 RID: 4578 RVA: 0x0006827C File Offset: 0x0006647C
		[Nullable(1)]
		public static IEnumerable<string> AssetPaths
		{
			[NullableContext(1)]
			get
			{
				return new <>z__ReadOnlySingleElementList<string>(NCardFlyVfx._scenePath);
			}
		}

		// Token: 0x170005C7 RID: 1479
		// (get) Token: 0x060011E3 RID: 4579 RVA: 0x00068288 File Offset: 0x00066488
		// (set) Token: 0x060011E4 RID: 4580 RVA: 0x00068290 File Offset: 0x00066490
		[Nullable(2)]
		public TaskCompletionSource SwooshAwayCompletion
		{
			[NullableContext(2)]
			get;
			[NullableContext(2)]
			private set;
		}

		// Token: 0x060011E5 RID: 4581 RVA: 0x0006829C File Offset: 0x0006649C
		[NullableContext(1)]
		[return: Nullable(2)]
		public static NCardFlyVfx Create(NCard card, Vector2 end, bool isAddingToPile, string trailPath)
		{
			if (TestMode.IsOn)
			{
				return null;
			}
			NCardFlyVfx ncardFlyVfx = PreloadManager.Cache.GetScene(NCardFlyVfx._scenePath).Instantiate<NCardFlyVfx>(PackedScene.GenEditState.Disabled);
			ncardFlyVfx._startPos = card.GlobalPosition;
			ncardFlyVfx._endPos = end;
			ncardFlyVfx._card = card;
			ncardFlyVfx._isAddingToPile = isAddingToPile;
			ncardFlyVfx._trailPath = trailPath;
			return ncardFlyVfx;
		}

		// Token: 0x060011E6 RID: 4582 RVA: 0x000682F4 File Offset: 0x000664F4
		public override void _Ready()
		{
			this._vfx = NCardTrailVfx.Create(this._card, this._trailPath);
			if (this._vfx != null)
			{
				base.GetParent().AddChildSafely(this._vfx);
			}
			this._controlPointOffset = Rng.Chaotic.NextFloat(100f, 400f);
			this._speed = Rng.Chaotic.NextFloat(1.1f, 1.25f);
			this._accel = Rng.Chaotic.NextFloat(2f, 2.5f);
			this._arcDir = ((this._endPos.Y < base.GetViewportRect().Size.Y * 0.5f) ? (-500f) : (500f + this._controlPointOffset));
			this._duration = Rng.Chaotic.NextFloat(1f, 1.75f);
			this._card.Connect(Node.SignalName.TreeExited, Callable.From(new Action(this.OnCardExitedTree)), 0U);
			if (NCombatUi.IsDebugHidingPlayContainer)
			{
				this._card.Modulate = Colors.Transparent;
				this._card.Visible = false;
				base.Visible = false;
			}
			TaskHelper.RunSafely(this.PlayAnim());
		}

		// Token: 0x060011E7 RID: 4583 RVA: 0x00068431 File Offset: 0x00066631
		public override void _ExitTree()
		{
			Tween fadeOutTween = this._fadeOutTween;
			if (fadeOutTween != null)
			{
				fadeOutTween.Kill();
			}
			this._cancelToken.Cancel();
			this._cancelToken.Dispose();
		}

		// Token: 0x060011E8 RID: 4584 RVA: 0x0006845C File Offset: 0x0006665C
		private void OnCardExitedTree()
		{
			try
			{
				NCardTrailVfx vfx = this._vfx;
				if (vfx != null)
				{
					vfx.QueueFreeSafely();
				}
			}
			catch (ObjectDisposedException)
			{
			}
			TaskCompletionSource swooshAwayCompletion = this.SwooshAwayCompletion;
			if (swooshAwayCompletion != null)
			{
				swooshAwayCompletion.TrySetResult();
			}
			this.QueueFreeSafely();
		}

		// Token: 0x060011E9 RID: 4585 RVA: 0x000684A8 File Offset: 0x000666A8
		[NullableContext(1)]
		private async Task PlayAnim()
		{
			this.SwooshAwayCompletion = new TaskCompletionSource();
			float time = 0f;
			while (time / this._duration <= 1f)
			{
				await base.ToSignal(base.GetTree(), SceneTree.SignalName.ProcessFrame);
				if (this._cancelToken.IsCancellationRequested)
				{
					TaskCompletionSource swooshAwayCompletion = this.SwooshAwayCompletion;
					if (swooshAwayCompletion != null)
					{
						swooshAwayCompletion.SetResult();
					}
					return;
				}
				float num = (float)base.GetProcessDeltaTime();
				time += this._speed * num;
				this._speed += this._accel * num;
				Vector2 vector = this._startPos + (this._endPos - this._startPos) * 0.5f;
				vector.Y -= this._arcDir;
				Vector2 vector2 = MathHelper.BezierCurve(this._startPos, this._endPos, vector, (time + 0.05f) / this._duration);
				this._card.GlobalPosition = MathHelper.BezierCurve(this._startPos, this._endPos, vector, time / this._duration);
				float num2 = (vector2 - this._card.GlobalPosition).Angle() + 1.57079637f;
				Node parent = this._card.GetParent();
				Control control = parent as Control;
				if (control != null)
				{
					num2 -= control.Rotation;
				}
				else
				{
					Node2D node2D = parent as Node2D;
					if (node2D != null)
					{
						num2 -= node2D.Rotation;
					}
				}
				this._card.Rotation = Mathf.LerpAngle(this._card.Rotation, num2, num * 12f);
				this._card.Body.Modulate = Colors.White.Lerp(Colors.Black, Mathf.Clamp(time * 3f / this._duration, 0f, 1f));
				this._card.Body.Scale = Vector2.One * Mathf.Lerp(1f, 0.1f, Mathf.Clamp(time * 3f / this._duration, 0f, 1f));
			}
			this._card.GlobalPosition = this._endPos;
			if (this._isAddingToPile)
			{
				CardPile pile = this._card.Model.Pile;
				if (pile != null)
				{
					pile.InvokeCardAddFinished();
				}
			}
			time = 0f;
			while (time / this._duration <= 1f)
			{
				await base.ToSignal(base.GetTree(), SceneTree.SignalName.ProcessFrame);
				if (this._cancelToken.IsCancellationRequested)
				{
					TaskCompletionSource swooshAwayCompletion2 = this.SwooshAwayCompletion;
					if (swooshAwayCompletion2 != null)
					{
						swooshAwayCompletion2.SetResult();
					}
					return;
				}
				float num3 = (float)base.GetProcessDeltaTime();
				time += this._speed * num3;
				if (time / this._duration > 0.25f && !this._vfxFading)
				{
					if (this._vfx != null)
					{
						TaskHelper.RunSafely(this._vfx.FadeOut());
					}
					this._vfxFading = true;
				}
				this._card.Body.Scale = Vector2.One * Mathf.Max(Mathf.Lerp(0.1f, -0.15f, time / this._duration), 0f);
			}
			TaskCompletionSource swooshAwayCompletion3 = this.SwooshAwayCompletion;
			if (swooshAwayCompletion3 != null)
			{
				swooshAwayCompletion3.SetResult();
			}
			this._card.QueueFreeSafely();
		}

		// Token: 0x060011EA RID: 4586 RVA: 0x000684EC File Offset: 0x000666EC
		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static List<MethodInfo> GetGodotMethodList()
		{
			return new List<MethodInfo>(4)
			{
				new MethodInfo(NCardFlyVfx.MethodName.Create, new PropertyInfo(Variant.Type.Object, "", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node2D"), false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
				{
					new PropertyInfo(Variant.Type.Object, "card", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Control"), false),
					new PropertyInfo(Variant.Type.Vector2, "end", PropertyHint.None, "", PropertyUsageFlags.Default, false),
					new PropertyInfo(Variant.Type.Bool, "isAddingToPile", PropertyHint.None, "", PropertyUsageFlags.Default, false),
					new PropertyInfo(Variant.Type.String, "trailPath", PropertyHint.None, "", PropertyUsageFlags.Default, false)
				}, null),
				new MethodInfo(NCardFlyVfx.MethodName._Ready, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, false), MethodFlags.Normal, null, null),
				new MethodInfo(NCardFlyVfx.MethodName._ExitTree, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, false), MethodFlags.Normal, null, null),
				new MethodInfo(NCardFlyVfx.MethodName.OnCardExitedTree, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, false), MethodFlags.Normal, null, null)
			};
		}

		// Token: 0x060011EB RID: 4587 RVA: 0x0006865C File Offset: 0x0006685C
		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
		{
			if ((in method) == NCardFlyVfx.MethodName.Create && args.Count == 4)
			{
				NCardFlyVfx ncardFlyVfx = NCardFlyVfx.Create(VariantUtils.ConvertTo<NCard>(args[0]), VariantUtils.ConvertTo<Vector2>(args[1]), VariantUtils.ConvertTo<bool>(args[2]), VariantUtils.ConvertTo<string>(args[3]));
				ret = VariantUtils.CreateFrom<NCardFlyVfx>(in ncardFlyVfx);
				return true;
			}
			if ((in method) == NCardFlyVfx.MethodName._Ready && args.Count == 0)
			{
				this._Ready();
				ret = default(godot_variant);
				return true;
			}
			if ((in method) == NCardFlyVfx.MethodName._ExitTree && args.Count == 0)
			{
				this._ExitTree();
				ret = default(godot_variant);
				return true;
			}
			if ((in method) == NCardFlyVfx.MethodName.OnCardExitedTree && args.Count == 0)
			{
				this.OnCardExitedTree();
				ret = default(godot_variant);
				return true;
			}
			return base.InvokeGodotClassMethod(in method, args, out ret);
		}

		// Token: 0x060011EC RID: 4588 RVA: 0x00068744 File Offset: 0x00066944
		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
		{
			if ((in method) == NCardFlyVfx.MethodName.Create && args.Count == 4)
			{
				NCardFlyVfx ncardFlyVfx = NCardFlyVfx.Create(VariantUtils.ConvertTo<NCard>(args[0]), VariantUtils.ConvertTo<Vector2>(args[1]), VariantUtils.ConvertTo<bool>(args[2]), VariantUtils.ConvertTo<string>(args[3]));
				ret = VariantUtils.CreateFrom<NCardFlyVfx>(in ncardFlyVfx);
				return true;
			}
			ret = default(godot_variant);
			return false;
		}

		// Token: 0x060011ED RID: 4589 RVA: 0x000687BC File Offset: 0x000669BC
		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool HasGodotClassMethod(in godot_string_name method)
		{
			return (in method) == NCardFlyVfx.MethodName.Create || (in method) == NCardFlyVfx.MethodName._Ready || (in method) == NCardFlyVfx.MethodName._ExitTree || (in method) == NCardFlyVfx.MethodName.OnCardExitedTree || base.HasGodotClassMethod(in method);
		}

		// Token: 0x060011EE RID: 4590 RVA: 0x0006880C File Offset: 0x00066A0C
		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
		{
			if ((in name) == NCardFlyVfx.PropertyName._card)
			{
				this._card = VariantUtils.ConvertTo<NCard>(in value);
				return true;
			}
			if ((in name) == NCardFlyVfx.PropertyName._trailPath)
			{
				this._trailPath = VariantUtils.ConvertTo<string>(in value);
				return true;
			}
			if ((in name) == NCardFlyVfx.PropertyName._vfx)
			{
				this._vfx = VariantUtils.ConvertTo<NCardTrailVfx>(in value);
				return true;
			}
			if ((in name) == NCardFlyVfx.PropertyName._fadeOutTween)
			{
				this._fadeOutTween = VariantUtils.ConvertTo<Tween>(in value);
				return true;
			}
			if ((in name) == NCardFlyVfx.PropertyName._vfxFading)
			{
				this._vfxFading = VariantUtils.ConvertTo<bool>(in value);
				return true;
			}
			if ((in name) == NCardFlyVfx.PropertyName._isAddingToPile)
			{
				this._isAddingToPile = VariantUtils.ConvertTo<bool>(in value);
				return true;
			}
			if ((in name) == NCardFlyVfx.PropertyName._startPos)
			{
				this._startPos = VariantUtils.ConvertTo<Vector2>(in value);
				return true;
			}
			if ((in name) == NCardFlyVfx.PropertyName._endPos)
			{
				this._endPos = VariantUtils.ConvertTo<Vector2>(in value);
				return true;
			}
			if ((in name) == NCardFlyVfx.PropertyName._controlPointOffset)
			{
				this._controlPointOffset = VariantUtils.ConvertTo<float>(in value);
				return true;
			}
			if ((in name) == NCardFlyVfx.PropertyName._duration)
			{
				this._duration = VariantUtils.ConvertTo<float>(in value);
				return true;
			}
			if ((in name) == NCardFlyVfx.PropertyName._speed)
			{
				this._speed = VariantUtils.ConvertTo<float>(in value);
				return true;
			}
			if ((in name) == NCardFlyVfx.PropertyName._accel)
			{
				this._accel = VariantUtils.ConvertTo<float>(in value);
				return true;
			}
			if ((in name) == NCardFlyVfx.PropertyName._arcDir)
			{
				this._arcDir = VariantUtils.ConvertTo<float>(in value);
				return true;
			}
			return base.SetGodotClassPropertyValue(in name, in value);
		}

		// Token: 0x060011EF RID: 4591 RVA: 0x00068980 File Offset: 0x00066B80
		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
		{
			if ((in name) == NCardFlyVfx.PropertyName._card)
			{
				value = VariantUtils.CreateFrom<NCard>(in this._card);
				return true;
			}
			if ((in name) == NCardFlyVfx.PropertyName._trailPath)
			{
				value = VariantUtils.CreateFrom<string>(in this._trailPath);
				return true;
			}
			if ((in name) == NCardFlyVfx.PropertyName._vfx)
			{
				value = VariantUtils.CreateFrom<NCardTrailVfx>(in this._vfx);
				return true;
			}
			if ((in name) == NCardFlyVfx.PropertyName._fadeOutTween)
			{
				value = VariantUtils.CreateFrom<Tween>(in this._fadeOutTween);
				return true;
			}
			if ((in name) == NCardFlyVfx.PropertyName._vfxFading)
			{
				value = VariantUtils.CreateFrom<bool>(in this._vfxFading);
				return true;
			}
			if ((in name) == NCardFlyVfx.PropertyName._isAddingToPile)
			{
				value = VariantUtils.CreateFrom<bool>(in this._isAddingToPile);
				return true;
			}
			if ((in name) == NCardFlyVfx.PropertyName._startPos)
			{
				value = VariantUtils.CreateFrom<Vector2>(in this._startPos);
				return true;
			}
			if ((in name) == NCardFlyVfx.PropertyName._endPos)
			{
				value = VariantUtils.CreateFrom<Vector2>(in this._endPos);
				return true;
			}
			if ((in name) == NCardFlyVfx.PropertyName._controlPointOffset)
			{
				value = VariantUtils.CreateFrom<float>(in this._controlPointOffset);
				return true;
			}
			if ((in name) == NCardFlyVfx.PropertyName._duration)
			{
				value = VariantUtils.CreateFrom<float>(in this._duration);
				return true;
			}
			if ((in name) == NCardFlyVfx.PropertyName._speed)
			{
				value = VariantUtils.CreateFrom<float>(in this._speed);
				return true;
			}
			if ((in name) == NCardFlyVfx.PropertyName._accel)
			{
				value = VariantUtils.CreateFrom<float>(in this._accel);
				return true;
			}
			if ((in name) == NCardFlyVfx.PropertyName._arcDir)
			{
				value = VariantUtils.CreateFrom<float>(in this._arcDir);
				return true;
			}
			return base.GetGodotClassPropertyValue(in name, out value);
		}

		// Token: 0x060011F0 RID: 4592 RVA: 0x00068B38 File Offset: 0x00066D38
		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static List<PropertyInfo> GetGodotPropertyList()
		{
			return new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, NCardFlyVfx.PropertyName._card, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, false),
				new PropertyInfo(Variant.Type.String, NCardFlyVfx.PropertyName._trailPath, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, false),
				new PropertyInfo(Variant.Type.Object, NCardFlyVfx.PropertyName._vfx, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, false),
				new PropertyInfo(Variant.Type.Object, NCardFlyVfx.PropertyName._fadeOutTween, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, false),
				new PropertyInfo(Variant.Type.Bool, NCardFlyVfx.PropertyName._vfxFading, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, false),
				new PropertyInfo(Variant.Type.Bool, NCardFlyVfx.PropertyName._isAddingToPile, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, false),
				new PropertyInfo(Variant.Type.Vector2, NCardFlyVfx.PropertyName._startPos, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, false),
				new PropertyInfo(Variant.Type.Vector2, NCardFlyVfx.PropertyName._endPos, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, false),
				new PropertyInfo(Variant.Type.Float, NCardFlyVfx.PropertyName._controlPointOffset, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, false),
				new PropertyInfo(Variant.Type.Float, NCardFlyVfx.PropertyName._duration, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, false),
				new PropertyInfo(Variant.Type.Float, NCardFlyVfx.PropertyName._speed, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, false),
				new PropertyInfo(Variant.Type.Float, NCardFlyVfx.PropertyName._accel, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, false),
				new PropertyInfo(Variant.Type.Float, NCardFlyVfx.PropertyName._arcDir, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, false)
			};
		}

		// Token: 0x060011F1 RID: 4593 RVA: 0x00068CF0 File Offset: 0x00066EF0
		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override void SaveGodotObjectData(GodotSerializationInfo info)
		{
			base.SaveGodotObjectData(info);
			info.AddProperty(NCardFlyVfx.PropertyName._card, Variant.From<NCard>(in this._card));
			info.AddProperty(NCardFlyVfx.PropertyName._trailPath, Variant.From<string>(in this._trailPath));
			info.AddProperty(NCardFlyVfx.PropertyName._vfx, Variant.From<NCardTrailVfx>(in this._vfx));
			info.AddProperty(NCardFlyVfx.PropertyName._fadeOutTween, Variant.From<Tween>(in this._fadeOutTween));
			info.AddProperty(NCardFlyVfx.PropertyName._vfxFading, Variant.From<bool>(in this._vfxFading));
			info.AddProperty(NCardFlyVfx.PropertyName._isAddingToPile, Variant.From<bool>(in this._isAddingToPile));
			info.AddProperty(NCardFlyVfx.PropertyName._startPos, Variant.From<Vector2>(in this._startPos));
			info.AddProperty(NCardFlyVfx.PropertyName._endPos, Variant.From<Vector2>(in this._endPos));
			info.AddProperty(NCardFlyVfx.PropertyName._controlPointOffset, Variant.From<float>(in this._controlPointOffset));
			info.AddProperty(NCardFlyVfx.PropertyName._duration, Variant.From<float>(in this._duration));
			info.AddProperty(NCardFlyVfx.PropertyName._speed, Variant.From<float>(in this._speed));
			info.AddProperty(NCardFlyVfx.PropertyName._accel, Variant.From<float>(in this._accel));
			info.AddProperty(NCardFlyVfx.PropertyName._arcDir, Variant.From<float>(in this._arcDir));
		}

		// Token: 0x060011F2 RID: 4594 RVA: 0x00068E24 File Offset: 0x00067024
		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override void RestoreGodotObjectData(GodotSerializationInfo info)
		{
			base.RestoreGodotObjectData(info);
			Variant variant;
			if (info.TryGetProperty(NCardFlyVfx.PropertyName._card, out variant))
			{
				this._card = variant.As<NCard>();
			}
			Variant variant2;
			if (info.TryGetProperty(NCardFlyVfx.PropertyName._trailPath, out variant2))
			{
				this._trailPath = variant2.As<string>();
			}
			Variant variant3;
			if (info.TryGetProperty(NCardFlyVfx.PropertyName._vfx, out variant3))
			{
				this._vfx = variant3.As<NCardTrailVfx>();
			}
			Variant variant4;
			if (info.TryGetProperty(NCardFlyVfx.PropertyName._fadeOutTween, out variant4))
			{
				this._fadeOutTween = variant4.As<Tween>();
			}
			Variant variant5;
			if (info.TryGetProperty(NCardFlyVfx.PropertyName._vfxFading, out variant5))
			{
				this._vfxFading = variant5.As<bool>();
			}
			Variant variant6;
			if (info.TryGetProperty(NCardFlyVfx.PropertyName._isAddingToPile, out variant6))
			{
				this._isAddingToPile = variant6.As<bool>();
			}
			Variant variant7;
			if (info.TryGetProperty(NCardFlyVfx.PropertyName._startPos, out variant7))
			{
				this._startPos = variant7.As<Vector2>();
			}
			Variant variant8;
			if (info.TryGetProperty(NCardFlyVfx.PropertyName._endPos, out variant8))
			{
				this._endPos = variant8.As<Vector2>();
			}
			Variant variant9;
			if (info.TryGetProperty(NCardFlyVfx.PropertyName._controlPointOffset, out variant9))
			{
				this._controlPointOffset = variant9.As<float>();
			}
			Variant variant10;
			if (info.TryGetProperty(NCardFlyVfx.PropertyName._duration, out variant10))
			{
				this._duration = variant10.As<float>();
			}
			Variant variant11;
			if (info.TryGetProperty(NCardFlyVfx.PropertyName._speed, out variant11))
			{
				this._speed = variant11.As<float>();
			}
			Variant variant12;
			if (info.TryGetProperty(NCardFlyVfx.PropertyName._accel, out variant12))
			{
				this._accel = variant12.As<float>();
			}
			Variant variant13;
			if (info.TryGetProperty(NCardFlyVfx.PropertyName._arcDir, out variant13))
			{
				this._arcDir = variant13.As<float>();
			}
		}

		// Token: 0x04000635 RID: 1589
		[Nullable(1)]
		private NCard _card;

		// Token: 0x04000636 RID: 1590
		[Nullable(1)]
		private string _trailPath;

		// Token: 0x04000637 RID: 1591
		[Nullable(2)]
		private NCardTrailVfx _vfx;

		// Token: 0x04000638 RID: 1592
		[Nullable(2)]
		private Tween _fadeOutTween;

		// Token: 0x04000639 RID: 1593
		private bool _vfxFading;

		// Token: 0x0400063A RID: 1594
		private bool _isAddingToPile;

		// Token: 0x0400063B RID: 1595
		private Vector2 _startPos;

		// Token: 0x0400063C RID: 1596
		private Vector2 _endPos;

		// Token: 0x0400063D RID: 1597
		private float _controlPointOffset;

		// Token: 0x0400063E RID: 1598
		private float _duration;

		// Token: 0x0400063F RID: 1599
		private float _speed;

		// Token: 0x04000640 RID: 1600
		private float _accel;

		// Token: 0x04000641 RID: 1601
		private float _arcDir;

		// Token: 0x04000642 RID: 1602
		[Nullable(1)]
		private readonly CancellationTokenSource _cancelToken = new CancellationTokenSource();

		// Token: 0x04000643 RID: 1603
		[Nullable(1)]
		private static readonly string _scenePath = SceneHelper.GetScenePath("vfx/vfx_card_fly");

		// Token: 0x02000ECA RID: 3786
		public new class MethodName : Node2D.MethodName
		{
			// Token: 0x04003986 RID: 14726
			public static readonly StringName Create = "Create";

			// Token: 0x04003987 RID: 14727
			public new static readonly StringName _Ready = "_Ready";

			// Token: 0x04003988 RID: 14728
			public new static readonly StringName _ExitTree = "_ExitTree";

			// Token: 0x04003989 RID: 14729
			public static readonly StringName OnCardExitedTree = "OnCardExitedTree";
		}

		// Token: 0x02000ECB RID: 3787
		public new class PropertyName : Node2D.PropertyName
		{
			// Token: 0x0400398A RID: 14730
			public static readonly StringName _card = "_card";

			// Token: 0x0400398B RID: 14731
			public static readonly StringName _trailPath = "_trailPath";

			// Token: 0x0400398C RID: 14732
			public static readonly StringName _vfx = "_vfx";

			// Token: 0x0400398D RID: 14733
			public static readonly StringName _fadeOutTween = "_fadeOutTween";

			// Token: 0x0400398E RID: 14734
			public static readonly StringName _vfxFading = "_vfxFading";

			// Token: 0x0400398F RID: 14735
			public static readonly StringName _isAddingToPile = "_isAddingToPile";

			// Token: 0x04003990 RID: 14736
			public static readonly StringName _startPos = "_startPos";

			// Token: 0x04003991 RID: 14737
			public static readonly StringName _endPos = "_endPos";

			// Token: 0x04003992 RID: 14738
			public static readonly StringName _controlPointOffset = "_controlPointOffset";

			// Token: 0x04003993 RID: 14739
			public static readonly StringName _duration = "_duration";

			// Token: 0x04003994 RID: 14740
			public static readonly StringName _speed = "_speed";

			// Token: 0x04003995 RID: 14741
			public static readonly StringName _accel = "_accel";

			// Token: 0x04003996 RID: 14742
			public static readonly StringName _arcDir = "_arcDir";
		}

		// Token: 0x02000ECC RID: 3788
		public new class SignalName : Node2D.SignalName
		{
		}
	}
}
