using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace CarnageReborn
{
	// Token: 0x0200001F RID: 31
	public class RevivalSyringe : Syringe
	{
		// Token: 0x1700000F RID: 15
		// (get) Token: 0x060000D6 RID: 214 RVA: 0x000059EA File Offset: 0x00003BEA
		protected override Color Color
		{
			get
			{
				return Color.green;
			}
		}

		// Token: 0x060000D7 RID: 215 RVA: 0x000059F4 File Offset: 0x00003BF4
		protected override void TryApplyEffects(CarnageCreature creature)
		{
			RevivalSyringe.<TryApplyEffects>d__2 <TryApplyEffects>d__;
			<TryApplyEffects>d__.<>t__builder = AsyncVoidMethodBuilder.Create();
			<TryApplyEffects>d__.<>4__this = this;
			<TryApplyEffects>d__.creature = creature;
			<TryApplyEffects>d__.<>1__state = -1;
			<TryApplyEffects>d__.<>t__builder.Start<RevivalSyringe.<TryApplyEffects>d__2>(ref <TryApplyEffects>d__);
		}
	}
}
