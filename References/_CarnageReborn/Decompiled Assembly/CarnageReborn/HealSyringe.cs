using System;
using UnityEngine;

namespace CarnageReborn
{
	// Token: 0x0200001E RID: 30
	public class HealSyringe : Syringe
	{
		// Token: 0x1700000E RID: 14
		// (get) Token: 0x060000D3 RID: 211 RVA: 0x000059C3 File Offset: 0x00003BC3
		protected override Color Color
		{
			get
			{
				return Color.red;
			}
		}

		// Token: 0x060000D4 RID: 212 RVA: 0x000059CA File Offset: 0x00003BCA
		protected override void TryApplyEffects(CarnageCreature creature)
		{
			creature.creature.Heal(100f, creature.creature);
		}
	}
}
