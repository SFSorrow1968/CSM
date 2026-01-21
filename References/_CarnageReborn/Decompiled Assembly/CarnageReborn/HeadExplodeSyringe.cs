using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace CarnageReborn
{
	// Token: 0x0200001D RID: 29
	public class HeadExplodeSyringe : Syringe
	{
		// Token: 0x1700000D RID: 13
		// (get) Token: 0x060000D0 RID: 208 RVA: 0x00005975 File Offset: 0x00003B75
		protected override Color Color
		{
			get
			{
				return Color.yellow;
			}
		}

		// Token: 0x060000D1 RID: 209 RVA: 0x0000597C File Offset: 0x00003B7C
		protected override void TryApplyEffects(CarnageCreature creature)
		{
			HeadExplodeSyringe.<TryApplyEffects>d__2 <TryApplyEffects>d__;
			<TryApplyEffects>d__.<>t__builder = AsyncVoidMethodBuilder.Create();
			<TryApplyEffects>d__.<>4__this = this;
			<TryApplyEffects>d__.creature = creature;
			<TryApplyEffects>d__.<>1__state = -1;
			<TryApplyEffects>d__.<>t__builder.Start<HeadExplodeSyringe.<TryApplyEffects>d__2>(ref <TryApplyEffects>d__);
		}
	}
}
