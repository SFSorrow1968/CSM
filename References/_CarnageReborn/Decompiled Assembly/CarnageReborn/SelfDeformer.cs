using System;
using UnityEngine;

namespace CarnageReborn
{
	// Token: 0x0200000F RID: 15
	public class SelfDeformer : Deformer
	{
		// Token: 0x06000085 RID: 133 RVA: 0x00004C2C File Offset: 0x00002E2C
		protected override void Awake()
		{
			base.Awake();
			if (base.GetComponent<Rigidbody>() == null && this.item != null && this.item.physicBody == null)
			{
				Debug.LogWarning("SelfDeformer on '" + base.gameObject.name + "' does not have a rigidbody.");
				Object.Destroy(this);
				return;
			}
		}

		// Token: 0x06000086 RID: 134 RVA: 0x00004C94 File Offset: 0x00002E94
		protected override void Start()
		{
			base.Start();
			this.deformables = ((this.item != null) ? this.item.GetComponentsInChildren<Deformable>(true) : base.GetComponentsInChildren<Deformable>(true));
		}

		// Token: 0x06000087 RID: 135 RVA: 0x00004CC8 File Offset: 0x00002EC8
		public void RestoreRequest()
		{
			Deformable[] array = this.deformables;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].RestoreAll();
			}
		}

		// Token: 0x06000088 RID: 136 RVA: 0x00004CF2 File Offset: 0x00002EF2
		protected override bool InvertVelocity()
		{
			return true;
		}

		// Token: 0x06000089 RID: 137 RVA: 0x00004CF5 File Offset: 0x00002EF5
		protected override bool ShouldDeformTarget(Collision collision, Collider target)
		{
			return collision != null && target != null && collision.collider.GetComponentInParent<Deformer>() == null && collision.collider.GetComponentInParent<CarnageCreature>() != null;
		}

		// Token: 0x0600008A RID: 138 RVA: 0x00004D29 File Offset: 0x00002F29
		protected override Collider GetColliderTargetFromCollision(Collision collision)
		{
			return collision.contacts[0].thisCollider;
		}

		// Token: 0x0400003D RID: 61
		private Deformable[] deformables = Array.Empty<Deformable>();
	}
}
