using System;
using ThunderRoad;
using UnityEngine;

namespace KillDef
{
	// Token: 0x02000002 RID: 2
	public class EnemyUpdate : MonoBehaviour
	{
		// Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
		public void Update()
		{
			bool flag = Player.local != null;
			if (flag)
			{
				bool enabledMod = EnemySelect.enabledMod;
				if (enabledMod)
				{
					bool flag2 = !Player.local.creature.handRight.caster.allowSpellWheel;
					if (!flag2)
					{
						Player.local.creature.handRight.caster.DisableSpellWheel(this);
					}
				}
				else
				{
					bool allowSpellWheel = Player.local.creature.handRight.caster.allowSpellWheel;
					if (!allowSpellWheel)
					{
						Player.local.creature.handRight.caster.AllowSpellWheel(this);
					}
				}
			}
		}
	}
}
