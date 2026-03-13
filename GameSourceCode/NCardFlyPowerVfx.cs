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
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Nodes.Vfx
{
	// Token: 0x0200016F RID: 367
	[NullableContext(1)]
	[Nullable(0)]
	[ScriptPath("res://src/Core/Nodes/Vfx/NCardFlyPowerVfx.cs")]
	public class NCardFlyPowerVfx : Node2D
	{
		// Token: 0x170005C3 RID: 1475
		// (get) Token: 0x060011BF RID: 4543 RVA: 0x000671BC File Offset: 0x000653BC
		// (set) Token: 0x060011C0 RID: 4544 RVA: 0x000671C4 File Offset: 0x000653C4
		public NCard CardNode { get; private set; }

		// Token: 0x170005C4 RID: 1476
		// (get) Token: 0x060011C1 RID: 4545 RVA: 0x000671CD File Offset: 0x000653CD
		public static IEnumerable<string> AssetPaths
		{
			get
			{
				return new <>z__ReadOnlySingleElementList<string>(NCardFlyPowerVfx._scenePath);
			}
		}

		// Token: 0x060011C2 RID: 4546 RVA: 0x000671DC File Offset: 0x000653DC
		[return: Nullable(2)]
		public static NCardFlyPowerVfx Create(NCard card)
		{
			if (TestMode.IsOn)
			{
				return null;
			}
			NCardFlyPowerVfx ncardFlyPowerVfx = PreloadManager.Cache.GetScene(NCardFlyPowerVfx._scenePath).Instantiate<NCardFlyPowerVfx>(PackedScene.GenEditState.Disabled);
			ncardFlyPowerVfx.CardNode = card;
			return ncardFlyPowerVfx;
		}

		// Token: 0x060011C3 RID: 4547 RVA: 0x00067214 File Offset: 0x00065414
		public override void _Ready()
		{
			base.GlobalPosition = this.CardNode.GlobalPosition;
			Player owner = this.CardNode.Model.Owner;
			this._cardOwnerNode = NCombatRoom.Instance.GetCreatureNode(owner.Creature);
			this._vfx = NCardTrailVfx.Create(this.CardNode, owner.Character.TrailPath);
			if (this._vfx != null)
			{
				this.AddChildSafely(this._vfx);
			}
			Vector2 vfxSpawnPosition = this._cardOwnerNode.VfxSpawnPosition;
			Vector2 vector = vfxSpawnPosition - base.GlobalPosition;
			this._swooshPath = base.GetNode<Path2D>("SwooshPath");
			this._swooshPath.Curve.SetPointPosition(this._swooshPath.Curve.PointCount - 1, vector);
		}

		// Token: 0x060011C4 RID: 4548 RVA: 0x000672DB File Offset: 0x000654DB
		public override void _ExitTree()
		{
			base._ExitTree();
			this._cancelToken.Cancel();
			this._cancelToken.Dispose();
		}

		// Token: 0x060011C5 RID: 4549 RVA: 0x000672F9 File Offset: 0x000654F9
		public float GetDuration()
		{
			return this.GetDurationInternal() + 0.05f;
		}

		// Token: 0x060011C6 RID: 4550 RVA: 0x00067307 File Offset: 0x00065507
		private float GetDurationInternal()
		{
			return this._swooshPath.Curve.GetBakedLength() / 3000f;
		}

		// Token: 0x060011C7 RID: 4551 RVA: 0x00067320 File Offset: 0x00065520
		public async Task PlayAnim()
		{
			base.CreateTween().TweenProperty(this.CardNode, "scale", Vector2.One * 0.1f, 0.30000001192092896);
			float length = this._swooshPath.Curve.GetBakedLength();
			double timeAccumulator = 0.0;
			float duration = this.GetDurationInternal();
			while (timeAccumulator < (double)duration)
			{
				await base.ToSignal(base.GetTree(), SceneTree.SignalName.ProcessFrame);
				if (this._cancelToken.IsCancellationRequested)
				{
					break;
				}
				double processDeltaTime = base.GetProcessDeltaTime();
				timeAccumulator += processDeltaTime;
				float num = (float)(timeAccumulator / (double)duration);
				float num2 = Ease.QuadIn(num);
				Transform2D transform2D = this._swooshPath.Curve.SampleBakedWithRotation(num2 * length, false);
				this.CardNode.GlobalPosition = base.GlobalPosition + transform2D.Origin;
				float num3 = transform2D.Rotation - this.CardNode.Rotation;
				float num4 = Mathf.Lerp(3.14159274f, 157.079636f, num);
				this.CardNode.Rotation += (float)Mathf.Sign(num3) * Mathf.Min(Mathf.Abs(num3), (float)((double)num4 * processDeltaTime));
				if (num >= 0.9f)
				{
					base.CreateTween().TweenProperty(this.CardNode, "scale", Vector2.Zero, (double)duration - timeAccumulator);
				}
			}
			NGame.Instance.ScreenShake(ShakeStrength.Medium, ShakeDuration.Short, -1f);
			if (this._vfx != null)
			{
				await this._vfx.FadeOut();
			}
			this.CardNode.QueueFreeSafely();
			this.QueueFreeSafely();
		}

		// Token: 0x060011C8 RID: 4552 RVA: 0x00067364 File Offset: 0x00065564
		[NullableContext(0)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static List<MethodInfo> GetGodotMethodList()
		{
			return new List<MethodInfo>(5)
			{
				new MethodInfo(NCardFlyPowerVfx.MethodName.Create, new PropertyInfo(Variant.Type.Object, "", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node2D"), false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
				{
					new PropertyInfo(Variant.Type.Object, "card", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Control"), false)
				}, null),
				new MethodInfo(NCardFlyPowerVfx.MethodName._Ready, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, false), MethodFlags.Normal, null, null),
				new MethodInfo(NCardFlyPowerVfx.MethodName._ExitTree, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, false), MethodFlags.Normal, null, null),
				new MethodInfo(NCardFlyPowerVfx.MethodName.GetDuration, new PropertyInfo(Variant.Type.Float, "", PropertyHint.None, "", PropertyUsageFlags.Default, false), MethodFlags.Normal, null, null),
				new MethodInfo(NCardFlyPowerVfx.MethodName.GetDurationInternal, new PropertyInfo(Variant.Type.Float, "", PropertyHint.None, "", PropertyUsageFlags.Default, false), MethodFlags.Normal, null, null)
			};
		}

		// Token: 0x060011C9 RID: 4553 RVA: 0x000674A0 File Offset: 0x000656A0
		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
		{
			if ((in method) == NCardFlyPowerVfx.MethodName.Create && args.Count == 1)
			{
				NCardFlyPowerVfx ncardFlyPowerVfx = NCardFlyPowerVfx.Create(VariantUtils.ConvertTo<NCard>(args[0]));
				ret = VariantUtils.CreateFrom<NCardFlyPowerVfx>(in ncardFlyPowerVfx);
				return true;
			}
			if ((in method) == NCardFlyPowerVfx.MethodName._Ready && args.Count == 0)
			{
				this._Ready();
				ret = default(godot_variant);
				return true;
			}
			if ((in method) == NCardFlyPowerVfx.MethodName._ExitTree && args.Count == 0)
			{
				this._ExitTree();
				ret = default(godot_variant);
				return true;
			}
			if ((in method) == NCardFlyPowerVfx.MethodName.GetDuration && args.Count == 0)
			{
				float duration = this.GetDuration();
				ret = VariantUtils.CreateFrom<float>(in duration);
				return true;
			}
			if ((in method) == NCardFlyPowerVfx.MethodName.GetDurationInternal && args.Count == 0)
			{
				float durationInternal = this.GetDurationInternal();
				ret = VariantUtils.CreateFrom<float>(in durationInternal);
				return true;
			}
			return base.InvokeGodotClassMethod(in method, args, out ret);
		}

		// Token: 0x060011CA RID: 4554 RVA: 0x00067594 File Offset: 0x00065794
		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
		{
			if ((in method) == NCardFlyPowerVfx.MethodName.Create && args.Count == 1)
			{
				NCardFlyPowerVfx ncardFlyPowerVfx = NCardFlyPowerVfx.Create(VariantUtils.ConvertTo<NCard>(args[0]));
				ret = VariantUtils.CreateFrom<NCardFlyPowerVfx>(in ncardFlyPowerVfx);
				return true;
			}
			ret = default(godot_variant);
			return false;
		}

		// Token: 0x060011CB RID: 4555 RVA: 0x000675E4 File Offset: 0x000657E4
		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool HasGodotClassMethod(in godot_string_name method)
		{
			return (in method) == NCardFlyPowerVfx.MethodName.Create || (in method) == NCardFlyPowerVfx.MethodName._Ready || (in method) == NCardFlyPowerVfx.MethodName._ExitTree || (in method) == NCardFlyPowerVfx.MethodName.GetDuration || (in method) == NCardFlyPowerVfx.MethodName.GetDurationInternal || base.HasGodotClassMethod(in method);
		}

		// Token: 0x060011CC RID: 4556 RVA: 0x00067644 File Offset: 0x00065844
		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
		{
			if ((in name) == NCardFlyPowerVfx.PropertyName.CardNode)
			{
				this.CardNode = VariantUtils.ConvertTo<NCard>(in value);
				return true;
			}
			if ((in name) == NCardFlyPowerVfx.PropertyName._cardOwnerNode)
			{
				this._cardOwnerNode = VariantUtils.ConvertTo<NCreature>(in value);
				return true;
			}
			if ((in name) == NCardFlyPowerVfx.PropertyName._vfx)
			{
				this._vfx = VariantUtils.ConvertTo<NCardTrailVfx>(in value);
				return true;
			}
			if ((in name) == NCardFlyPowerVfx.PropertyName._swooshPath)
			{
				this._swooshPath = VariantUtils.ConvertTo<Path2D>(in value);
				return true;
			}
			return base.SetGodotClassPropertyValue(in name, in value);
		}

		// Token: 0x060011CD RID: 4557 RVA: 0x000676C8 File Offset: 0x000658C8
		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
		{
			if ((in name) == NCardFlyPowerVfx.PropertyName.CardNode)
			{
				NCard cardNode = this.CardNode;
				value = VariantUtils.CreateFrom<NCard>(in cardNode);
				return true;
			}
			if ((in name) == NCardFlyPowerVfx.PropertyName._cardOwnerNode)
			{
				value = VariantUtils.CreateFrom<NCreature>(in this._cardOwnerNode);
				return true;
			}
			if ((in name) == NCardFlyPowerVfx.PropertyName._vfx)
			{
				value = VariantUtils.CreateFrom<NCardTrailVfx>(in this._vfx);
				return true;
			}
			if ((in name) == NCardFlyPowerVfx.PropertyName._swooshPath)
			{
				value = VariantUtils.CreateFrom<Path2D>(in this._swooshPath);
				return true;
			}
			return base.GetGodotClassPropertyValue(in name, out value);
		}

		// Token: 0x060011CE RID: 4558 RVA: 0x00067760 File Offset: 0x00065960
		[NullableContext(0)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static List<PropertyInfo> GetGodotPropertyList()
		{
			return new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, NCardFlyPowerVfx.PropertyName.CardNode, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, false),
				new PropertyInfo(Variant.Type.Object, NCardFlyPowerVfx.PropertyName._cardOwnerNode, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, false),
				new PropertyInfo(Variant.Type.Object, NCardFlyPowerVfx.PropertyName._vfx, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, false),
				new PropertyInfo(Variant.Type.Object, NCardFlyPowerVfx.PropertyName._swooshPath, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, false)
			};
		}

		// Token: 0x060011CF RID: 4559 RVA: 0x000677F8 File Offset: 0x000659F8
		[NullableContext(0)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override void SaveGodotObjectData(GodotSerializationInfo info)
		{
			base.SaveGodotObjectData(info);
			StringName cardNode = NCardFlyPowerVfx.PropertyName.CardNode;
			NCard cardNode2 = this.CardNode;
			info.AddProperty(cardNode, Variant.From<NCard>(in cardNode2));
			info.AddProperty(NCardFlyPowerVfx.PropertyName._cardOwnerNode, Variant.From<NCreature>(in this._cardOwnerNode));
			info.AddProperty(NCardFlyPowerVfx.PropertyName._vfx, Variant.From<NCardTrailVfx>(in this._vfx));
			info.AddProperty(NCardFlyPowerVfx.PropertyName._swooshPath, Variant.From<Path2D>(in this._swooshPath));
		}

		// Token: 0x060011D0 RID: 4560 RVA: 0x00067868 File Offset: 0x00065A68
		[NullableContext(0)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override void RestoreGodotObjectData(GodotSerializationInfo info)
		{
			base.RestoreGodotObjectData(info);
			Variant variant;
			if (info.TryGetProperty(NCardFlyPowerVfx.PropertyName.CardNode, out variant))
			{
				this.CardNode = variant.As<NCard>();
			}
			Variant variant2;
			if (info.TryGetProperty(NCardFlyPowerVfx.PropertyName._cardOwnerNode, out variant2))
			{
				this._cardOwnerNode = variant2.As<NCreature>();
			}
			Variant variant3;
			if (info.TryGetProperty(NCardFlyPowerVfx.PropertyName._vfx, out variant3))
			{
				this._vfx = variant3.As<NCardTrailVfx>();
			}
			Variant variant4;
			if (info.TryGetProperty(NCardFlyPowerVfx.PropertyName._swooshPath, out variant4))
			{
				this._swooshPath = variant4.As<Path2D>();
			}
		}

		// Token: 0x0400061D RID: 1565
		private const float _speed = 3000f;

		// Token: 0x0400061E RID: 1566
		private const float _scaleOutProportion = 0.9f;

		// Token: 0x0400061F RID: 1567
		private const float _initialRotationSpeed = 3.14159274f;

		// Token: 0x04000620 RID: 1568
		private const float _maxRotationSpeed = 157.079636f;

		// Token: 0x04000622 RID: 1570
		private NCreature _cardOwnerNode;

		// Token: 0x04000623 RID: 1571
		[Nullable(2)]
		private NCardTrailVfx _vfx;

		// Token: 0x04000624 RID: 1572
		private Path2D _swooshPath;

		// Token: 0x04000625 RID: 1573
		private readonly CancellationTokenSource _cancelToken = new CancellationTokenSource();

		// Token: 0x04000626 RID: 1574
		private static readonly string _scenePath = SceneHelper.GetScenePath("vfx/vfx_card_power_fly");

		// Token: 0x02000EC2 RID: 3778
		[NullableContext(0)]
		public new class MethodName : Node2D.MethodName
		{
			// Token: 0x04003962 RID: 14690
			public static readonly StringName Create = "Create";

			// Token: 0x04003963 RID: 14691
			public new static readonly StringName _Ready = "_Ready";

			// Token: 0x04003964 RID: 14692
			public new static readonly StringName _ExitTree = "_ExitTree";

			// Token: 0x04003965 RID: 14693
			public static readonly StringName GetDuration = "GetDuration";

			// Token: 0x04003966 RID: 14694
			public static readonly StringName GetDurationInternal = "GetDurationInternal";
		}

		// Token: 0x02000EC3 RID: 3779
		[NullableContext(0)]
		public new class PropertyName : Node2D.PropertyName
		{
			// Token: 0x04003967 RID: 14695
			public static readonly StringName CardNode = "CardNode";

			// Token: 0x04003968 RID: 14696
			public static readonly StringName _cardOwnerNode = "_cardOwnerNode";

			// Token: 0x04003969 RID: 14697
			public static readonly StringName _vfx = "_vfx";

			// Token: 0x0400396A RID: 14698
			public static readonly StringName _swooshPath = "_swooshPath";
		}

		// Token: 0x02000EC4 RID: 3780
		[NullableContext(0)]
		public new class SignalName : Node2D.SignalName
		{
		}
	}
}
