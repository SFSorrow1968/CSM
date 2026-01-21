using System;
using ThunderRoad;
using UnityEngine;

namespace CarnageReborn
{
	// Token: 0x0200000A RID: 10
	public class CreatureTorsoGibModule : CreatureModule
	{
		// Token: 0x0600005E RID: 94 RVA: 0x00004170 File Offset: 0x00002370
		public override void HookEvents()
		{
			RagdollPart spine = this.creature.ragdoll.GetPart(4, 3);
			RagdollPart spine2 = this.creature.ragdoll.GetPart(4, 1);
			if (spine2 == null && spine == null)
			{
				Debug.LogError(string.Format("[Sliceable Torso] Spine is null! (Spine1: {0} / Spine: {1})", spine2, spine));
				return;
			}
			spine.SetAllowSlice(true);
			spine.sliceWidth = 0.08f;
			spine.sliceHeight = 0.05f;
			spine.sliceThreshold = ModOptions.torsoSliceThreshold;
			spine2.SetAllowSlice(true);
			spine2.sliceWidth = 0.08f;
			spine2.sliceHeight = 0.05f;
			spine2.sliceThreshold = ModOptions.torsoSliceThreshold;
			this.creature.ragdoll.OnSliceEvent += new Ragdoll.SliceEvent(this.OnSliced);
			base.HookEvents();
		}

		// Token: 0x0600005F RID: 95 RVA: 0x00004239 File Offset: 0x00002439
		public override void UnHookEvents()
		{
			this.creature.ragdoll.OnSliceEvent -= new Ragdoll.SliceEvent(this.OnSliced);
			base.UnHookEvents();
		}

		// Token: 0x06000060 RID: 96 RVA: 0x0000425D File Offset: 0x0000245D
		public override void Restore()
		{
			this._gutted = false;
			base.Restore();
		}

		// Token: 0x06000061 RID: 97 RVA: 0x0000426C File Offset: 0x0000246C
		private void OnSliced(RagdollPart ragdollPart, EventTime eventTime)
		{
			if (this._gutted || eventTime != 1 || !ragdollPart.name.Contains("Spine"))
			{
				return;
			}
			this._gutted = true;
			if (ModOptions.killOnTorsoSliced)
			{
				ragdollPart.ragdoll.creature.Kill();
			}
			if (ModOptions.spawnGuts)
			{
				ragdollPart.CreateGuts();
			}
		}

		// Token: 0x04000027 RID: 39
		private bool _gutted;
	}
}
