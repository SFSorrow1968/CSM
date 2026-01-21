using System;
using UnityEngine;

namespace CarnageReborn
{
	// Token: 0x0200001C RID: 28
	public class FinisherSyringe : Syringe
	{
		// Token: 0x1700000C RID: 12
		// (get) Token: 0x060000CD RID: 205 RVA: 0x00005947 File Offset: 0x00003B47
		protected override Color Color
		{
			get
			{
				return new Color32(138, 52, 3, byte.MaxValue);
			}
		}

		// Token: 0x060000CE RID: 206 RVA: 0x00005960 File Offset: 0x00003B60
		protected override void TryApplyEffects(CarnageCreature creature)
		{
			creature.GetCreatureModule<CreatureAnimationModule>().PlayNonFatalFinisher();
		}
	}
}
