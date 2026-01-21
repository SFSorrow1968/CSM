using System;
using ThunderRoad;
using UnityEngine;

namespace CarnageReborn
{
	// Token: 0x0200001A RID: 26
	public class AllySyringe : Syringe
	{
		// Token: 0x1700000A RID: 10
		// (get) Token: 0x060000C7 RID: 199 RVA: 0x00005897 File Offset: 0x00003A97
		protected override Color Color
		{
			get
			{
				return Color.blue;
			}
		}

		// Token: 0x060000C8 RID: 200 RVA: 0x0000589E File Offset: 0x00003A9E
		protected override void TryApplyEffects(CarnageCreature creature)
		{
			creature.GetCreatureModule<CreatureConciousnessModule>().Electrify(0.1f, 1f);
			creature.creature.SetFaction(Player.local.creature.faction.id);
		}
	}
}
