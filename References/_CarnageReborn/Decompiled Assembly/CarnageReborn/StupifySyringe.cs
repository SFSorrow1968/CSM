using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace CarnageReborn
{
	// Token: 0x02000021 RID: 33
	public class StupifySyringe : Syringe
	{
		// Token: 0x17000011 RID: 17
		// (get) Token: 0x060000DC RID: 220 RVA: 0x00005A8C File Offset: 0x00003C8C
		protected override Color Color
		{
			get
			{
				Color col;
				if (!ColorUtility.TryParseHtmlString("#ab5505", ref col))
				{
					return Color.black;
				}
				return col;
			}
		}

		// Token: 0x060000DD RID: 221 RVA: 0x00005AB0 File Offset: 0x00003CB0
		protected override void TryApplyEffects(CarnageCreature creature)
		{
			StupifySyringe.<TryApplyEffects>d__2 <TryApplyEffects>d__;
			<TryApplyEffects>d__.<>t__builder = AsyncVoidMethodBuilder.Create();
			<TryApplyEffects>d__.<>4__this = this;
			<TryApplyEffects>d__.creature = creature;
			<TryApplyEffects>d__.<>1__state = -1;
			<TryApplyEffects>d__.<>t__builder.Start<StupifySyringe.<TryApplyEffects>d__2>(ref <TryApplyEffects>d__);
		}
	}
}
