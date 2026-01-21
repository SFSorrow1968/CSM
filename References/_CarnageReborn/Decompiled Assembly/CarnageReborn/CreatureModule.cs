using System;
using ThunderRoad;
using UnityEngine;

namespace CarnageReborn
{
	// Token: 0x02000004 RID: 4
	public class CreatureModule
	{
		// Token: 0x0600001A RID: 26 RVA: 0x000025F1 File Offset: 0x000007F1
		public virtual void Initialize(Creature creature)
		{
			this.creature = creature;
			this.transform = creature.transform;
		}

		// Token: 0x0600001B RID: 27 RVA: 0x00002606 File Offset: 0x00000806
		public virtual void Awake()
		{
		}

		// Token: 0x0600001C RID: 28 RVA: 0x00002608 File Offset: 0x00000808
		public virtual void Start()
		{
		}

		// Token: 0x0600001D RID: 29 RVA: 0x0000260A File Offset: 0x0000080A
		public virtual void Enabled()
		{
		}

		// Token: 0x0600001E RID: 30 RVA: 0x0000260C File Offset: 0x0000080C
		public virtual void Disabled()
		{
		}

		// Token: 0x0600001F RID: 31 RVA: 0x0000260E File Offset: 0x0000080E
		public virtual void Update()
		{
		}

		// Token: 0x06000020 RID: 32 RVA: 0x00002610 File Offset: 0x00000810
		public virtual void LateUpdate()
		{
		}

		// Token: 0x06000021 RID: 33 RVA: 0x00002612 File Offset: 0x00000812
		public virtual void FixedUpdate()
		{
		}

		// Token: 0x06000022 RID: 34 RVA: 0x00002614 File Offset: 0x00000814
		public virtual void Restore()
		{
		}

		// Token: 0x06000023 RID: 35 RVA: 0x00002616 File Offset: 0x00000816
		public virtual void HookEvents()
		{
		}

		// Token: 0x06000024 RID: 36 RVA: 0x00002618 File Offset: 0x00000818
		public virtual void UnHookEvents()
		{
		}

		// Token: 0x04000008 RID: 8
		protected Creature creature;

		// Token: 0x04000009 RID: 9
		protected Transform transform;
	}
}
