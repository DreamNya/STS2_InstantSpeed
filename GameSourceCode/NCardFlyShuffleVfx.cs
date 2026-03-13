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
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Nodes.Vfx
{
	// Token: 0x02000170 RID: 368
	[ScriptPath("res://src/Core/Nodes/Vfx/NCardFlyShuffleVfx.cs")]
	public class NCardFlyShuffleVfx : Control
	{
		// Token: 0x170005C5 RID: 1477
		// (get) Token: 0x060011D3 RID: 4563 RVA: 0x00067910 File Offset: 0x00065B10
		[Nullable(1)]
		public static IEnumerable<string> AssetPaths
		{
			[NullableContext(1)]
			get
			{
				return new <>z__ReadOnlySingleElementList<string>(NCardFlyShuffleVfx._scenePath);
			}
		}

		// Token: 0x060011D4 RID: 4564 RVA: 0x0006791C File Offset: 0x00065B1C
		[NullableContext(1)]
		[return: Nullable(2)]
		public static NCardFlyShuffleVfx Create(CardPile startPile, CardPile targetPile, string trailPath)
		{
			if (TestMode.IsOn)
			{
				return null;
			}
			NCardFlyShuffleVfx ncardFlyShuffleVfx = PreloadManager.Cache.GetScene(NCardFlyShuffleVfx._scenePath).Instantiate<NCardFlyShuffleVfx>(PackedScene.GenEditState.Disabled);
			ncardFlyShuffleVfx._startPos = startPile.Type.GetTargetPosition(null);
			ncardFlyShuffleVfx._endPos = targetPile.Type.GetTargetPosition(null);
			ncardFlyShuffleVfx._trailPath = trailPath;
			ncardFlyShuffleVfx._targetPile = targetPile;
			return ncardFlyShuffleVfx;
		}

		// Token: 0x060011D5 RID: 4565 RVA: 0x0006797C File Offset: 0x00065B7C
		public override void _Ready()
		{
			this._controlPointOffset = Rng.Chaotic.NextFloat(-300f, 400f);
			this._speed = Rng.Chaotic.NextFloat(1.1f, 1.25f);
			this._accel = Rng.Chaotic.NextFloat(2f, 2.5f);
			this._arcDir = ((this._endPos.Y < 540f) ? (-500f) : (500f + this._controlPointOffset));
			this._duration = Rng.Chaotic.NextFloat(1f, 1.75f);
			this._vfx = NCardTrailVfx.Create(this, this._trailPath);
			if (this._vfx != null)
			{
				NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(this._vfx);
			}
			Node parent = base.GetParent();
			parent.MoveChild(this, parent.GetChildCount(false) - 1);
			TaskHelper.RunSafely(this.PlayAnim());
		}

		// Token: 0x060011D6 RID: 4566 RVA: 0x00067A70 File Offset: 0x00065C70
		[NullableContext(1)]
		private async Task PlayAnim()
		{
			float time = 0f;
			while (time / this._duration <= 1f)
			{
				await base.ToSignal(base.GetTree(), SceneTree.SignalName.ProcessFrame);
				if (this._cancelToken.IsCancellationRequested)
				{
					return;
				}
				float num = (float)base.GetProcessDeltaTime();
				time += this._speed * num;
				this._speed += this._accel * num;
				Vector2 vector = this._startPos + (this._endPos - this._startPos) * 0.5f;
				vector.Y -= this._arcDir;
				base.GlobalPosition = MathHelper.BezierCurve(this._startPos, this._endPos, vector, time / this._duration);
				Vector2 vector2 = MathHelper.BezierCurve(this._startPos, this._endPos, vector, (time + 0.05f) / this._duration);
				base.Rotation = (vector2 - base.GlobalPosition).Angle() + 1.57079637f;
			}
			base.GlobalPosition = this._endPos;
			this._targetPile.InvokeCardAddFinished();
			time = 0f;
			while (time / this._duration <= 1f)
			{
				await base.ToSignal(base.GetTree(), SceneTree.SignalName.ProcessFrame);
				if (this._cancelToken.IsCancellationRequested)
				{
					return;
				}
				float num2 = (float)base.GetProcessDeltaTime();
				time += this._speed * num2;
				if (time / this._duration > 0.25f && !this._vfxFading)
				{
					if (this._vfx != null)
					{
						TaskHelper.RunSafely(this._vfx.FadeOut());
					}
					this._vfxFading = true;
				}
				base.Scale = Vector2.One * Mathf.Max(Mathf.Lerp(0.1f, -0.1f, time / this._duration), 0f);
			}
			this._fadeOutTween = base.CreateTween();
			this._fadeOutTween.TweenProperty(this, "modulate:a", 0f, 0.800000011920929);
			await Task.Delay(800);
			this.QueueFreeSafely();
		}

		// Token: 0x060011D7 RID: 4567 RVA: 0x00067AB3 File Offset: 0x00065CB3
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

		// Token: 0x060011D8 RID: 4568 RVA: 0x00067ADC File Offset: 0x00065CDC
		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static List<MethodInfo> GetGodotMethodList()
		{
			return new List<MethodInfo>(2)
			{
				new MethodInfo(NCardFlyShuffleVfx.MethodName._Ready, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, false), MethodFlags.Normal, null, null),
				new MethodInfo(NCardFlyShuffleVfx.MethodName._ExitTree, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, false), MethodFlags.Normal, null, null)
			};
		}

		// Token: 0x060011D9 RID: 4569 RVA: 0x00067B50 File Offset: 0x00065D50
		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
		{
			if ((in method) == NCardFlyShuffleVfx.MethodName._Ready && args.Count == 0)
			{
				this._Ready();
				ret = default(godot_variant);
				return true;
			}
			if ((in method) == NCardFlyShuffleVfx.MethodName._ExitTree && args.Count == 0)
			{
				this._ExitTree();
				ret = default(godot_variant);
				return true;
			}
			return base.InvokeGodotClassMethod(in method, args, out ret);
		}

		// Token: 0x060011DA RID: 4570 RVA: 0x00067BB0 File Offset: 0x00065DB0
		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool HasGodotClassMethod(in godot_string_name method)
		{
			return (in method) == NCardFlyShuffleVfx.MethodName._Ready || (in method) == NCardFlyShuffleVfx.MethodName._ExitTree || base.HasGodotClassMethod(in method);
		}

		// Token: 0x060011DB RID: 4571 RVA: 0x00067BD8 File Offset: 0x00065DD8
		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
		{
			if ((in name) == NCardFlyShuffleVfx.PropertyName._vfx)
			{
				this._vfx = VariantUtils.ConvertTo<NCardTrailVfx>(in value);
				return true;
			}
			if ((in name) == NCardFlyShuffleVfx.PropertyName._fadeOutTween)
			{
				this._fadeOutTween = VariantUtils.ConvertTo<Tween>(in value);
				return true;
			}
			if ((in name) == NCardFlyShuffleVfx.PropertyName._vfxFading)
			{
				this._vfxFading = VariantUtils.ConvertTo<bool>(in value);
				return true;
			}
			if ((in name) == NCardFlyShuffleVfx.PropertyName._startPos)
			{
				this._startPos = VariantUtils.ConvertTo<Vector2>(in value);
				return true;
			}
			if ((in name) == NCardFlyShuffleVfx.PropertyName._endPos)
			{
				this._endPos = VariantUtils.ConvertTo<Vector2>(in value);
				return true;
			}
			if ((in name) == NCardFlyShuffleVfx.PropertyName._controlPointOffset)
			{
				this._controlPointOffset = VariantUtils.ConvertTo<float>(in value);
				return true;
			}
			if ((in name) == NCardFlyShuffleVfx.PropertyName._duration)
			{
				this._duration = VariantUtils.ConvertTo<float>(in value);
				return true;
			}
			if ((in name) == NCardFlyShuffleVfx.PropertyName._speed)
			{
				this._speed = VariantUtils.ConvertTo<float>(in value);
				return true;
			}
			if ((in name) == NCardFlyShuffleVfx.PropertyName._accel)
			{
				this._accel = VariantUtils.ConvertTo<float>(in value);
				return true;
			}
			if ((in name) == NCardFlyShuffleVfx.PropertyName._arcDir)
			{
				this._arcDir = VariantUtils.ConvertTo<float>(in value);
				return true;
			}
			if ((in name) == NCardFlyShuffleVfx.PropertyName._trailPath)
			{
				this._trailPath = VariantUtils.ConvertTo<string>(in value);
				return true;
			}
			return base.SetGodotClassPropertyValue(in name, in value);
		}

		// Token: 0x060011DC RID: 4572 RVA: 0x00067D18 File Offset: 0x00065F18
		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
		{
			if ((in name) == NCardFlyShuffleVfx.PropertyName._vfx)
			{
				value = VariantUtils.CreateFrom<NCardTrailVfx>(in this._vfx);
				return true;
			}
			if ((in name) == NCardFlyShuffleVfx.PropertyName._fadeOutTween)
			{
				value = VariantUtils.CreateFrom<Tween>(in this._fadeOutTween);
				return true;
			}
			if ((in name) == NCardFlyShuffleVfx.PropertyName._vfxFading)
			{
				value = VariantUtils.CreateFrom<bool>(in this._vfxFading);
				return true;
			}
			if ((in name) == NCardFlyShuffleVfx.PropertyName._startPos)
			{
				value = VariantUtils.CreateFrom<Vector2>(in this._startPos);
				return true;
			}
			if ((in name) == NCardFlyShuffleVfx.PropertyName._endPos)
			{
				value = VariantUtils.CreateFrom<Vector2>(in this._endPos);
				return true;
			}
			if ((in name) == NCardFlyShuffleVfx.PropertyName._controlPointOffset)
			{
				value = VariantUtils.CreateFrom<float>(in this._controlPointOffset);
				return true;
			}
			if ((in name) == NCardFlyShuffleVfx.PropertyName._duration)
			{
				value = VariantUtils.CreateFrom<float>(in this._duration);
				return true;
			}
			if ((in name) == NCardFlyShuffleVfx.PropertyName._speed)
			{
				value = VariantUtils.CreateFrom<float>(in this._speed);
				return true;
			}
			if ((in name) == NCardFlyShuffleVfx.PropertyName._accel)
			{
				value = VariantUtils.CreateFrom<float>(in this._accel);
				return true;
			}
			if ((in name) == NCardFlyShuffleVfx.PropertyName._arcDir)
			{
				value = VariantUtils.CreateFrom<float>(in this._arcDir);
				return true;
			}
			if ((in name) == NCardFlyShuffleVfx.PropertyName._trailPath)
			{
				value = VariantUtils.CreateFrom<string>(in this._trailPath);
				return true;
			}
			return base.GetGodotClassPropertyValue(in name, out value);
		}

		// Token: 0x060011DD RID: 4573 RVA: 0x00067E90 File Offset: 0x00066090
		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static List<PropertyInfo> GetGodotPropertyList()
		{
			return new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, NCardFlyShuffleVfx.PropertyName._vfx, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, false),
				new PropertyInfo(Variant.Type.Object, NCardFlyShuffleVfx.PropertyName._fadeOutTween, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, false),
				new PropertyInfo(Variant.Type.Bool, NCardFlyShuffleVfx.PropertyName._vfxFading, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, false),
				new PropertyInfo(Variant.Type.Vector2, NCardFlyShuffleVfx.PropertyName._startPos, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, false),
				new PropertyInfo(Variant.Type.Vector2, NCardFlyShuffleVfx.PropertyName._endPos, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, false),
				new PropertyInfo(Variant.Type.Float, NCardFlyShuffleVfx.PropertyName._controlPointOffset, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, false),
				new PropertyInfo(Variant.Type.Float, NCardFlyShuffleVfx.PropertyName._duration, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, false),
				new PropertyInfo(Variant.Type.Float, NCardFlyShuffleVfx.PropertyName._speed, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, false),
				new PropertyInfo(Variant.Type.Float, NCardFlyShuffleVfx.PropertyName._accel, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, false),
				new PropertyInfo(Variant.Type.Float, NCardFlyShuffleVfx.PropertyName._arcDir, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, false),
				new PropertyInfo(Variant.Type.String, NCardFlyShuffleVfx.PropertyName._trailPath, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, false)
			};
		}

		// Token: 0x060011DE RID: 4574 RVA: 0x00068008 File Offset: 0x00066208
		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override void SaveGodotObjectData(GodotSerializationInfo info)
		{
			base.SaveGodotObjectData(info);
			info.AddProperty(NCardFlyShuffleVfx.PropertyName._vfx, Variant.From<NCardTrailVfx>(in this._vfx));
			info.AddProperty(NCardFlyShuffleVfx.PropertyName._fadeOutTween, Variant.From<Tween>(in this._fadeOutTween));
			info.AddProperty(NCardFlyShuffleVfx.PropertyName._vfxFading, Variant.From<bool>(in this._vfxFading));
			info.AddProperty(NCardFlyShuffleVfx.PropertyName._startPos, Variant.From<Vector2>(in this._startPos));
			info.AddProperty(NCardFlyShuffleVfx.PropertyName._endPos, Variant.From<Vector2>(in this._endPos));
			info.AddProperty(NCardFlyShuffleVfx.PropertyName._controlPointOffset, Variant.From<float>(in this._controlPointOffset));
			info.AddProperty(NCardFlyShuffleVfx.PropertyName._duration, Variant.From<float>(in this._duration));
			info.AddProperty(NCardFlyShuffleVfx.PropertyName._speed, Variant.From<float>(in this._speed));
			info.AddProperty(NCardFlyShuffleVfx.PropertyName._accel, Variant.From<float>(in this._accel));
			info.AddProperty(NCardFlyShuffleVfx.PropertyName._arcDir, Variant.From<float>(in this._arcDir));
			info.AddProperty(NCardFlyShuffleVfx.PropertyName._trailPath, Variant.From<string>(in this._trailPath));
		}

		// Token: 0x060011DF RID: 4575 RVA: 0x00068110 File Offset: 0x00066310
		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override void RestoreGodotObjectData(GodotSerializationInfo info)
		{
			base.RestoreGodotObjectData(info);
			Variant variant;
			if (info.TryGetProperty(NCardFlyShuffleVfx.PropertyName._vfx, out variant))
			{
				this._vfx = variant.As<NCardTrailVfx>();
			}
			Variant variant2;
			if (info.TryGetProperty(NCardFlyShuffleVfx.PropertyName._fadeOutTween, out variant2))
			{
				this._fadeOutTween = variant2.As<Tween>();
			}
			Variant variant3;
			if (info.TryGetProperty(NCardFlyShuffleVfx.PropertyName._vfxFading, out variant3))
			{
				this._vfxFading = variant3.As<bool>();
			}
			Variant variant4;
			if (info.TryGetProperty(NCardFlyShuffleVfx.PropertyName._startPos, out variant4))
			{
				this._startPos = variant4.As<Vector2>();
			}
			Variant variant5;
			if (info.TryGetProperty(NCardFlyShuffleVfx.PropertyName._endPos, out variant5))
			{
				this._endPos = variant5.As<Vector2>();
			}
			Variant variant6;
			if (info.TryGetProperty(NCardFlyShuffleVfx.PropertyName._controlPointOffset, out variant6))
			{
				this._controlPointOffset = variant6.As<float>();
			}
			Variant variant7;
			if (info.TryGetProperty(NCardFlyShuffleVfx.PropertyName._duration, out variant7))
			{
				this._duration = variant7.As<float>();
			}
			Variant variant8;
			if (info.TryGetProperty(NCardFlyShuffleVfx.PropertyName._speed, out variant8))
			{
				this._speed = variant8.As<float>();
			}
			Variant variant9;
			if (info.TryGetProperty(NCardFlyShuffleVfx.PropertyName._accel, out variant9))
			{
				this._accel = variant9.As<float>();
			}
			Variant variant10;
			if (info.TryGetProperty(NCardFlyShuffleVfx.PropertyName._arcDir, out variant10))
			{
				this._arcDir = variant10.As<float>();
			}
			Variant variant11;
			if (info.TryGetProperty(NCardFlyShuffleVfx.PropertyName._trailPath, out variant11))
			{
				this._trailPath = variant11.As<string>();
			}
		}

		// Token: 0x04000627 RID: 1575
		[Nullable(2)]
		private NCardTrailVfx _vfx;

		// Token: 0x04000628 RID: 1576
		[Nullable(2)]
		private Tween _fadeOutTween;

		// Token: 0x04000629 RID: 1577
		private bool _vfxFading;

		// Token: 0x0400062A RID: 1578
		private Vector2 _startPos;

		// Token: 0x0400062B RID: 1579
		private Vector2 _endPos;

		// Token: 0x0400062C RID: 1580
		private float _controlPointOffset;

		// Token: 0x0400062D RID: 1581
		private float _duration;

		// Token: 0x0400062E RID: 1582
		private float _speed;

		// Token: 0x0400062F RID: 1583
		private float _accel;

		// Token: 0x04000630 RID: 1584
		private float _arcDir;

		// Token: 0x04000631 RID: 1585
		[Nullable(1)]
		private string _trailPath;

		// Token: 0x04000632 RID: 1586
		[Nullable(1)]
		private CardPile _targetPile;

		// Token: 0x04000633 RID: 1587
		[Nullable(1)]
		private readonly CancellationTokenSource _cancelToken = new CancellationTokenSource();

		// Token: 0x04000634 RID: 1588
		[Nullable(1)]
		private static readonly string _scenePath = SceneHelper.GetScenePath("vfx/vfx_card_shuffle_fly");

		// Token: 0x02000EC6 RID: 3782
		public new class MethodName : Control.MethodName
		{
			// Token: 0x04003973 RID: 14707
			public new static readonly StringName _Ready = "_Ready";

			// Token: 0x04003974 RID: 14708
			public new static readonly StringName _ExitTree = "_ExitTree";
		}

		// Token: 0x02000EC7 RID: 3783
		public new class PropertyName : Control.PropertyName
		{
			// Token: 0x04003975 RID: 14709
			public static readonly StringName _vfx = "_vfx";

			// Token: 0x04003976 RID: 14710
			public static readonly StringName _fadeOutTween = "_fadeOutTween";

			// Token: 0x04003977 RID: 14711
			public static readonly StringName _vfxFading = "_vfxFading";

			// Token: 0x04003978 RID: 14712
			public static readonly StringName _startPos = "_startPos";

			// Token: 0x04003979 RID: 14713
			public static readonly StringName _endPos = "_endPos";

			// Token: 0x0400397A RID: 14714
			public static readonly StringName _controlPointOffset = "_controlPointOffset";

			// Token: 0x0400397B RID: 14715
			public static readonly StringName _duration = "_duration";

			// Token: 0x0400397C RID: 14716
			public static readonly StringName _speed = "_speed";

			// Token: 0x0400397D RID: 14717
			public static readonly StringName _accel = "_accel";

			// Token: 0x0400397E RID: 14718
			public static readonly StringName _arcDir = "_arcDir";

			// Token: 0x0400397F RID: 14719
			public static readonly StringName _trailPath = "_trailPath";
		}

		// Token: 0x02000EC8 RID: 3784
		public new class SignalName : Control.SignalName
		{
		}
	}
}
