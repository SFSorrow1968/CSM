using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace CarnageReborn
{
	// Token: 0x02000020 RID: 32
	public class SleepSyringe : Syringe
	{
		// Token: 0x17000010 RID: 16
		// (get) Token: 0x060000D9 RID: 217 RVA: 0x00005A3B File Offset: 0x00003C3B
		protected override Color Color
		{
			get
			{
				return Color.magenta;
			}
		}

		// Token: 0x060000DA RID: 218 RVA: 0x00005A44 File Offset: 0x00003C44
		protected override void TryApplyEffects(CarnageCreature creature)
		{
			SleepSyringe.<TryApplyEffects>d__2 <TryApplyEffects>d__;
			<TryApplyEffects>d__.<>t__builder = AsyncVoidMethodBuilder.Create();
			<TryApplyEffects>d__.<>4__this = this;
			<TryApplyEffects>d__.creature = creature;
			<TryApplyEffects>d__.<>1__state = -1;
			<TryApplyEffects>d__.<>t__builder.Start<SleepSyringe.<TryApplyEffects>d__2>(ref <TryApplyEffects>d__);
		}
	}
}
