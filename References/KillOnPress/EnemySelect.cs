using System;
using ThunderRoad;

namespace KillDef
{
	// Token: 0x02000003 RID: 3
	public class EnemySelect : ThunderScript
	{
		// Token: 0x06000003 RID: 3 RVA: 0x00002105 File Offset: 0x00000305
		public override void ScriptEnable()
		{
			base.ScriptEnable();
			EventManager.onCreatureHit += new EventManager.CreatureHitEvent(this.EventManager_onCreatureHit);
			EventManager.onPossess += new EventManager.PossessEvent(this.EventManager_onPossess);
		}

		// Token: 0x06000004 RID: 4 RVA: 0x00002134 File Offset: 0x00000334
		private void EventManager_onPossess(Creature creature, EventTime eventTime)
		{
			bool flag = eventTime == 0;
			if (!flag)
			{
				creature.gameObject.AddComponent<EnemyUpdate>();
			}
		}

		// Token: 0x06000005 RID: 5 RVA: 0x00002158 File Offset: 0x00000358
		private void EventManager_onCreatureHit(Creature creature, CollisionInstance collisionInstance, EventTime eventTime)
		{
			bool flag = creature != null && !creature.isPlayer && EnemySelect.enabledMod;
			if (flag)
			{
				bool flag2 = PlayerControl.GetHand(0).alternateUsePressed && creature.currentHealth > 0f;
				if (flag2)
				{
					creature.Kill();
				}
			}
		}

		// Token: 0x06000006 RID: 6 RVA: 0x000021B4 File Offset: 0x000003B4
		public override void ScriptDisable()
		{
			base.ScriptDisable();
			EventManager.onCreatureHit -= new EventManager.CreatureHitEvent(this.EventManager_onCreatureHit);
			EventManager.onPossess -= new EventManager.PossessEvent(this.EventManager_onPossess);
		}

		// Token: 0x04000001 RID: 1
		public static ModOptionBool[] booleanOptions = new ModOptionBool[]
		{
			new ModOptionBool("Enabled", EnemySelect.enabledMod),
			new ModOptionBool("Disabled", !EnemySelect.enabledMod)
		};

		// Token: 0x04000002 RID: 2
		[ModOptionTooltip("Enables or disables the mod depending on choice")]
		[ModOption(defaultValueIndex = 0)]
		public static bool enabledMod = false;
	}
}
