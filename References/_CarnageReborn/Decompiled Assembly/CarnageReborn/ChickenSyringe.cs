using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace CarnageReborn
{
	// Token: 0x0200001B RID: 27
	public class ChickenSyringe : Syringe
	{
		// Token: 0x1700000B RID: 11
		// (get) Token: 0x060000CA RID: 202 RVA: 0x000058DC File Offset: 0x00003ADC
		protected override Color Color
		{
			get
			{
				Color col;
				if (!ColorUtility.TryParseHtmlString("#c77416", ref col))
				{
					return Color.black;
				}
				return col;
			}
		}

		// Token: 0x060000CB RID: 203 RVA: 0x00005900 File Offset: 0x00003B00
		protected override void TryApplyEffects(CarnageCreature creature)
		{
			ChickenSyringe.<TryApplyEffects>d__2 <TryApplyEffects>d__;
			<TryApplyEffects>d__.<>t__builder = AsyncVoidMethodBuilder.Create();
			<TryApplyEffects>d__.<>4__this = this;
			<TryApplyEffects>d__.creature = creature;
			<TryApplyEffects>d__.<>1__state = -1;
			<TryApplyEffects>d__.<>t__builder.Start<ChickenSyringe.<TryApplyEffects>d__2>(ref <TryApplyEffects>d__);
		}
	}
}
