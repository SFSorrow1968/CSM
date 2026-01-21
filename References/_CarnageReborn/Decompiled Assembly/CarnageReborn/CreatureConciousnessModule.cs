using System;
using ThunderRoad;
using UnityEngine;

namespace CarnageReborn
{
	// Token: 0x02000008 RID: 8
	public class CreatureConciousnessModule : CreatureModule
	{
		// Token: 0x06000043 RID: 67 RVA: 0x0000374C File Offset: 0x0000194C
		public override void HookEvents()
		{
			this.creature.ragdoll.OnSliceEvent += new Ragdoll.SliceEvent(this.HandleRagdollSliced);
			foreach (RagdollPart part in this.creature.ragdoll.parts)
			{
				if (part.type != 1 && part.type != 4)
				{
					part.data.sliceForceKill = false;
				}
			}
			base.HookEvents();
		}

		// Token: 0x06000044 RID: 68 RVA: 0x000037E4 File Offset: 0x000019E4
		public override void UnHookEvents()
		{
			this.creature.ragdoll.OnSliceEvent -= new Ragdoll.SliceEvent(this.HandleRagdollSliced);
			base.UnHookEvents();
		}

		// Token: 0x06000045 RID: 69 RVA: 0x00003808 File Offset: 0x00001A08
		public override void Update()
		{
			if (Time.time < this.knockoutTime || this.isIncapacitated)
			{
				this.creature.ragdoll.SetState(1, this.isIncapacitated);
			}
			base.Update();
		}

		// Token: 0x06000046 RID: 70 RVA: 0x0000383C File Offset: 0x00001A3C
		public override void Disabled()
		{
			this.isIncapacitated = false;
			base.Disabled();
		}

		// Token: 0x06000047 RID: 71 RVA: 0x0000384B File Offset: 0x00001A4B
		public override void Restore()
		{
			this.isIncapacitated = false;
			if (!this.creature.isKilled)
			{
				this.creature.brain.instance.Start();
			}
			base.Restore();
		}

		// Token: 0x06000048 RID: 72 RVA: 0x0000387C File Offset: 0x00001A7C
		public void Electrify(float time, float power = 1f)
		{
			this.creature.TryElectrocute(power, time, true, false, null);
		}

		// Token: 0x06000049 RID: 73 RVA: 0x0000388E File Offset: 0x00001A8E
		public void Knockout(float time)
		{
			this.knockoutTime = Time.time + time;
		}

		// Token: 0x0600004A RID: 74 RVA: 0x0000389D File Offset: 0x00001A9D
		public void Stupify()
		{
			this.creature.GetComponent<CarnageCreature>().GetCreatureModule<CreatureAnimationModule>().Play("Idle");
		}

		// Token: 0x0600004B RID: 75 RVA: 0x000038BC File Offset: 0x00001ABC
		private void HandleRagdollSliced(RagdollPart part, EventTime eventTime)
		{
			if (eventTime != 1)
			{
				return;
			}
			if (!ModOptions.surviveDismemberment || part.type == 1 || part.type == 4)
			{
				part.ragdoll.creature.Kill();
			}
			if (part.type == 512 || part.type == 128 || part.type == 1024 || part.type == 256)
			{
				this.isIncapacitated = true;
			}
		}

		// Token: 0x0400001F RID: 31
		private float knockoutTime;

		// Token: 0x04000020 RID: 32
		private bool isIncapacitated;
	}
}
