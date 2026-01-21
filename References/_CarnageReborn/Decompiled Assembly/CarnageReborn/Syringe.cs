using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace CarnageReborn
{
	// Token: 0x02000022 RID: 34
	public class Syringe : ItemModule
	{
		// Token: 0x17000012 RID: 18
		// (get) Token: 0x060000DF RID: 223 RVA: 0x00005AF7 File Offset: 0x00003CF7
		protected virtual Color Color { get; }

		// Token: 0x060000E0 RID: 224 RVA: 0x00005B00 File Offset: 0x00003D00
		public override void OnItemLoaded(Item item)
		{
			Item.OnItemSpawn = (Action<Item>)Delegate.Combine(Item.OnItemSpawn, new Action<Item>(this.HandleItemSpawn));
			Item.OnItemDespawn = (Action<Item>)Delegate.Combine(Item.OnItemDespawn, new Action<Item>(this.HandleItemDespawn));
			base.OnItemLoaded(item);
		}

		// Token: 0x060000E1 RID: 225 RVA: 0x00005B54 File Offset: 0x00003D54
		protected virtual void CreatureImpaled(CarnageCreature creature)
		{
		}

		// Token: 0x060000E2 RID: 226 RVA: 0x00005B56 File Offset: 0x00003D56
		protected virtual void TryApplyEffects(CarnageCreature creature)
		{
		}

		// Token: 0x060000E3 RID: 227 RVA: 0x00005B58 File Offset: 0x00003D58
		protected Task WaitForTime(int minSecond, int maxSecond)
		{
			return Task.Delay(Random.Range(minSecond, maxSecond) * 1000);
		}

		// Token: 0x060000E4 RID: 228 RVA: 0x00005B6C File Offset: 0x00003D6C
		private void HandleItemDespawn(Item item)
		{
			item.RemoveCustomData<SyringeData>();
			this.instanced.Remove(item);
		}

		// Token: 0x060000E5 RID: 229 RVA: 0x00005B84 File Offset: 0x00003D84
		private void HandleItemSpawn(Item item)
		{
			if (item != this.item)
			{
				return;
			}
			item.colliderGroups[0].collisionHandler.OnCollisionStartEvent -= new CollisionHandler.CollisionEvent(this.HandleDamage);
			item.colliderGroups[0].collisionHandler.OnCollisionStartEvent += new CollisionHandler.CollisionEvent(this.HandleDamage);
			this.instanced.Add(item);
			this.SetVisualUseState(item, false);
		}

		// Token: 0x060000E6 RID: 230 RVA: 0x00005BF8 File Offset: 0x00003DF8
		private void HandleDamage(CollisionInstance collisionInstance)
		{
			if (collisionInstance == null || collisionInstance.damageStruct.damager == null)
			{
				return;
			}
			Item dealer = collisionInstance.damageStruct.damager.GetComponentInParent<Item>();
			SyringeData data;
			if (dealer == null || collisionInstance.damageStruct.hitRagdollPart == null || collisionInstance.damageStruct.hitRagdollPart.ragdoll == null || collisionInstance.damageStruct.hitRagdollPart.ragdoll.creature == null || collisionInstance.damageStruct.penetration == null || (dealer.TryGetCustomData<SyringeData>(ref data) && data != null && data.used))
			{
				return;
			}
			CarnageCreature creature = collisionInstance.damageStruct.hitRagdollPart.ragdoll.creature.GetComponent<CarnageCreature>();
			if (creature == null)
			{
				Debug.LogWarning("Tried to use syringe on an unsupported carnage creature.");
				return;
			}
			collisionInstance.damageStruct.damage = 0f;
			this.SetVisualUseState(dealer, true);
			this.CreatureImpaled(creature);
			dealer.AddCustomData<SyringeData>(new SyringeData
			{
				used = true
			});
			this.TryApplyEffects(creature);
		}

		// Token: 0x060000E7 RID: 231 RVA: 0x00005D08 File Offset: 0x00003F08
		private void SetColour(Item item, Color32 color)
		{
			Material material = item.GetCustomReference<MeshRenderer>("Glass", true).material;
			material.color = color;
			material.SetColor("_EmissionColor", color);
		}

		// Token: 0x060000E8 RID: 232 RVA: 0x00005D38 File Offset: 0x00003F38
		private void SetVisualUseState(Item item, bool state)
		{
			if (state)
			{
				item.GetCustomReference<MeshRenderer>("Glass", true).enabled = false;
				item.GetCustomReference("Plunger", true).localPosition = new Vector3(0f, 0f, -0.0477f);
				return;
			}
			item.GetCustomReference<MeshRenderer>("Glass", true).enabled = true;
			this.SetColour(item, this.Color);
			item.GetCustomReference("Plunger", true).localPosition = new Vector3(0f, 0f, -0.075f);
		}

		// Token: 0x0400005A RID: 90
		private readonly List<Item> instanced = new List<Item>();
	}
}
