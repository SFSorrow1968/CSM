using System;
using CarnageReborn.Deformation;
using ThunderRoad;
using UnityEngine;

namespace CarnageReborn
{
	// Token: 0x0200000C RID: 12
	public class Deformer : ThunderBehaviour
	{
		// Token: 0x14000006 RID: 6
		// (add) Token: 0x06000074 RID: 116 RVA: 0x00004724 File Offset: 0x00002924
		// (remove) Token: 0x06000075 RID: 117 RVA: 0x0000475C File Offset: 0x0000295C
		public event Deform Deform;

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000076 RID: 118 RVA: 0x00004791 File Offset: 0x00002991
		public override ManagedLoops EnabledManagedLoops
		{
			get
			{
				return 2;
			}
		}

		// Token: 0x06000077 RID: 119 RVA: 0x00004794 File Offset: 0x00002994
		protected virtual void Awake()
		{
			this.item = base.GetComponent<Item>();
		}

		// Token: 0x06000078 RID: 120 RVA: 0x000047A2 File Offset: 0x000029A2
		protected virtual void Start()
		{
			this.currentVelocity = (base.transform.position - this.previousVelocity) / Time.deltaTime;
			this.previousVelocity = base.transform.position;
		}

		// Token: 0x06000079 RID: 121 RVA: 0x000047DB File Offset: 0x000029DB
		protected override void ManagedUpdate()
		{
			this.currentVelocity = (base.transform.position - this.previousVelocity) / Time.deltaTime;
			this.previousVelocity = base.transform.position;
		}

		// Token: 0x0600007A RID: 122 RVA: 0x00004814 File Offset: 0x00002A14
		public virtual float CalculateVelocityMultiplier()
		{
			float velocityMultiplier = 0.0025f;
			if (this.item != null && this.item.physicBody != null)
			{
				velocityMultiplier *= Mathf.Clamp(this.item.physicBody.mass, 0.1f, 1.25f);
				ItemData.Type type = this.item.data.type;
				float factor;
				if (type != 1)
				{
					if (type != 4)
					{
						factor = ModOptions.defaultDamageDeformationMultiplier;
					}
					else
					{
						factor = ModOptions.propDamageDeformationMultiplier;
					}
				}
				else
				{
					factor = ModOptions.weaponDamageDeformationMultiplier;
				}
				return velocityMultiplier * factor;
			}
			if (base.GetComponentInParent<RagdollHand>() != null)
			{
				return velocityMultiplier * ModOptions.playerHandsDamageDeformationMultiplier;
			}
			return velocityMultiplier;
		}

		// Token: 0x0600007B RID: 123 RVA: 0x000048B8 File Offset: 0x00002AB8
		public virtual void ProcessDeformation(Collision collision)
		{
			if (Time.time >= this.timeout && collision != null && !(collision.collider == null) && collision.relativeVelocity.magnitude > ModOptions.minDeformVelocity)
			{
				Creature c = collision.collider.GetComponentInParent<Creature>();
				if ((c == null || !c.isPlayer) && (!(this.item != null) || ModOptions.itemsCanDeform))
				{
					Collider target = this.GetColliderTargetFromCollision(collision);
					Deformable deformable = (target != null) ? target.GetComponentInParent<Deformable>() : null;
					if (target == null || deformable == null || !this.ShouldDeformTarget(collision, target) || (Player.local != null && ModOptions.preventDistantDeformations && Vector3.Distance(target.transform.position, Player.local.transform.position) >= ModOptions.maxDeformDistance))
					{
						return;
					}
					this.timeout = Time.time + ModOptions.deformationTimeout;
					Vector3 velocity = this.InvertVelocity() ? (-collision.relativeVelocity) : collision.relativeVelocity;
					float maxVelocity = this.GetMaxVelocity();
					velocity.x = Mathf.Clamp(velocity.x, -maxVelocity, maxVelocity);
					velocity.y = Mathf.Clamp(velocity.y, -maxVelocity, maxVelocity);
					velocity.z = Mathf.Clamp(velocity.z, -maxVelocity, maxVelocity);
					if (deformable.item != null && deformable.item.data != null)
					{
						ItemData.Type type = deformable.item.data.type;
						if (type != 1)
						{
							if (type != 4)
							{
								velocity *= ModOptions.deformationDamper;
							}
							else
							{
								velocity *= ModOptions.propDeformationDamper;
							}
						}
						else
						{
							velocity *= ModOptions.weaponDeformationDamper;
						}
					}
					else if (deformable.GetComponent<Creature>() != null)
					{
						velocity *= ModOptions.enemyDeformationDamper;
					}
					target.Deform(new DeformRequest(collision.contacts[0].point, velocity, target.transform, this.impactRadius, this.CalculateVelocityMultiplier()), delegate(Mesh mesh, Renderer renderer)
					{
						renderer.TrySetMeshToRender(mesh);
						deformable.InvokeDeformed(this, mesh, collision);
					});
					Deform deform = this.Deform;
					if (deform == null)
					{
						return;
					}
					deform(this, deformable, collision);
					return;
				}
			}
		}

		// Token: 0x0600007C RID: 124 RVA: 0x00004B40 File Offset: 0x00002D40
		protected virtual Collider GetColliderTargetFromCollision(Collision collision)
		{
			return collision.collider;
		}

		// Token: 0x0600007D RID: 125 RVA: 0x00004B48 File Offset: 0x00002D48
		protected virtual bool InvertVelocity()
		{
			return false;
		}

		// Token: 0x0600007E RID: 126 RVA: 0x00004B4B File Offset: 0x00002D4B
		protected virtual bool ShouldDeformTarget(Collision collision, Collider target)
		{
			return true;
		}

		// Token: 0x0600007F RID: 127 RVA: 0x00004B4E File Offset: 0x00002D4E
		protected virtual float GetMaxVelocity()
		{
			return ModOptions.maxVertexVelocity;
		}

		// Token: 0x06000080 RID: 128 RVA: 0x00004B55 File Offset: 0x00002D55
		protected void OnCollisionEnter(Collision collision)
		{
			this.ProcessDeformation(collision);
		}

		// Token: 0x0400002F RID: 47
		public float impactRadius = 0.08f;

		// Token: 0x04000030 RID: 48
		public Item item;

		// Token: 0x04000031 RID: 49
		protected float timeout;

		// Token: 0x04000032 RID: 50
		public Vector3 currentVelocity;

		// Token: 0x04000033 RID: 51
		protected Vector3 previousVelocity;
	}
}
