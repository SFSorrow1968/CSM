using System;
using CarnageReborn.Gore;
using ThunderRoad;
using UnityEngine;

namespace CarnageReborn
{
	// Token: 0x02000026 RID: 38
	public static class GoreFactory
	{
		// Token: 0x060000F4 RID: 244 RVA: 0x000062E4 File Offset: 0x000044E4
		internal static void CreateGuts(this RagdollPart part)
		{
			if (!ModOptions.spawnGuts)
			{
				return;
			}
			Catalog.InstantiateAsync(Definitions.gutsPrefabAddress, Vector3.zero, Quaternion.identity, null, delegate(GameObject guts)
			{
				guts.transform.position = part.transform.position;
				guts.transform.rotation = Quaternion.identity;
				Object.Destroy(guts, ModOptions.gutsDespawnTime);
			}, "Entry");
		}

		// Token: 0x060000F5 RID: 245 RVA: 0x0000632C File Offset: 0x0000452C
		internal static void CreateSkull(this Creature creature)
		{
			if (!ModOptions.spawnSkull)
			{
				return;
			}
			Transform headPart = creature.ragdoll.GetPart(1).transform;
			if (headPart.Find("SKULL") != null)
			{
				return;
			}
			Catalog.InstantiateAsync(Definitions.skullPrefabAddress, Vector3.zero, Quaternion.identity, headPart, delegate(GameObject skull)
			{
				skull.name = "SKULL";
				skull.transform.localPosition = Definitions.SkullOffset;
				skull.transform.localEulerAngles = Definitions.SkullRotation;
				skull.transform.localScale *= ((creature.data.gender == 1) ? Definitions.MaleSkullScaleMultiplier : Definitions.FemaleSkullScaleMultiplier);
				foreach (Collider collider in skull.GetComponentsInChildren<Collider>())
				{
					foreach (Collider collider2 in creature.GetComponentsInChildren<Collider>())
					{
						Physics.IgnoreCollision(collider, collider2, true);
					}
				}
			}, "Entry");
		}

		// Token: 0x060000F6 RID: 246 RVA: 0x000063A0 File Offset: 0x000045A0
		internal static void CreateExplodables(this Creature creature)
		{
			GameObject headPart = creature.ragdoll.GetPart(1).gameObject;
			if (headPart.GetComponent<Explodable>() == null)
			{
				Explodable explodable = headPart.AddComponent<Explodable>();
				explodable.Set(() => ModOptions.headSmashVelocity, "SD.FracturedSkull", true, (DamageType damageType) => ModOptions.headSmashing && (!ModOptions.bluntOnlySmashing || damageType == 3), delegate(GameObject go)
				{
					go.transform.GetChild(0).gameObject.SetActive(ModOptions.spawnSkullFragmentsOnSmash);
					if ((float)Random.Range(0, 100) >= ModOptions.brainSpawnChance)
					{
						go.transform.GetChild(3).gameObject.SetActive(ModOptions.spawnBrain);
					}
					Transform skull = explodable.limb.transform.Find("SKULL");
					if (skull != null)
					{
						skull.localScale = Vector3.zero;
					}
				});
			}
		}

		// Token: 0x060000F7 RID: 247 RVA: 0x0000643C File Offset: 0x0000463C
		internal static void RemoveExplodables(this Creature creature)
		{
			foreach (Explodable explodable in creature.GetComponentsInChildren<Explodable>())
			{
				explodable.transform.localScale = Vector3.one;
				Object.Destroy(explodable);
			}
		}

		// Token: 0x060000F8 RID: 248 RVA: 0x00006478 File Offset: 0x00004678
		internal static void CreateEyeball(this Creature creature, Side side)
		{
			if (!ModOptions.oldRemovableEyes && !ModOptions.newRemovableEyes)
			{
				return;
			}
			foreach (Transform child in creature.GetComponentsInChildren<Transform>())
			{
				if (!(child == null) && ((side == 1 && string.CompareOrdinal(child.name, "LeftEyeGlobe") == 0) || (side == null && string.CompareOrdinal(child.name, "RightEyeGlobe") == 0)) && child.GetComponent<Eyeball>() == null)
				{
					child.gameObject.AddComponent<Eyeball>().side = side;
					return;
				}
			}
		}
	}
}
