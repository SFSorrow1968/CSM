using System;
using ThunderRoad;
using UnityEngine;

namespace CarnageReborn.Gore
{
	// Token: 0x0200002B RID: 43
	public class Eyeball : Deformable
	{
		// Token: 0x17000013 RID: 19
		// (get) Token: 0x0600011C RID: 284 RVA: 0x00006DEB File Offset: 0x00004FEB
		public override ManagedLoops EnabledManagedLoops
		{
			get
			{
				return 2;
			}
		}

		// Token: 0x0600011D RID: 285 RVA: 0x00006DF0 File Offset: 0x00004FF0
		private void Awake()
		{
			this.creature = base.GetComponentInParent<Creature>();
			this._inactiveTimer = Time.time + 10f;
			this._eyeBone = base.transform.GetChild(0);
			this.eye = ((this._eyeBone != null) ? this._eyeBone.GetComponent<CreatureEye>() : base.GetComponentInChildren<CreatureEye>());
		}

		// Token: 0x0600011E RID: 286 RVA: 0x00006E53 File Offset: 0x00005053
		private void Start()
		{
			this.creature.OnDespawnEvent -= new Creature.DespawnEvent(this.HandleDespawn);
			this.creature.OnDespawnEvent += new Creature.DespawnEvent(this.HandleDespawn);
		}

		// Token: 0x0600011F RID: 287 RVA: 0x00006E83 File Offset: 0x00005083
		protected override void ManagedOnDisable()
		{
			base.ManagedOnDisable();
			this.HandleDespawn(1);
		}

		// Token: 0x06000120 RID: 288 RVA: 0x00006E92 File Offset: 0x00005092
		private void HandleDespawn(EventTime eventTime)
		{
			if (eventTime != 1)
			{
				return;
			}
			this._inactiveTimer = Time.time + 10f;
			this.RestoreAll();
		}

		// Token: 0x06000121 RID: 289 RVA: 0x00006EB0 File Offset: 0x000050B0
		protected override void ManagedUpdate()
		{
			if (this._eyeBone == null || Time.time < this._inactiveTimer)
			{
				return;
			}
			if (this._isPopped)
			{
				this._eyeBone.parent.localScale = Vector3.zero;
				return;
			}
			if (this._poppedEyeball != null)
			{
				this.ClearPopped();
			}
			this._eyeBone.parent.localScale = Vector3.one;
			if (base.transform.IsWithinDeformationRange() && Physics.OverlapSphereNonAlloc(this._eyeBone.position, ModOptions.eyeballRadius, this._results, -1, 1) > 0)
			{
				for (int i = 0; i < this._results.Length; i++)
				{
					if (!(this._results[i] == null) && !(this._results[i].GetComponentInParent<Creature>() == this.creature))
					{
						Item item = this._results[i].GetComponentInParent<Item>();
						RagdollHand rh = this._results[i].GetComponentInParent<RagdollHand>();
						bool isHandDamager = rh != null;
						if ((item != null && (item.isPenetrating || item.physicBody.velocity.magnitude >= ModOptions.eyeballKnockoutVelocity)) || (isHandDamager && rh.physicBody.rigidBody.velocity.magnitude >= ModOptions.eyeballKnockoutVelocity))
						{
							this.Pop();
							return;
						}
					}
				}
			}
		}

		// Token: 0x06000122 RID: 290 RVA: 0x00007014 File Offset: 0x00005214
		public override void RestoreAll()
		{
			this._isPopped = false;
			this._eyeBone.parent.localScale = Vector3.one;
			if (this.eye != null)
			{
				this.eye.closeAmount = 0f;
			}
			this.ClearPopped();
			base.RestoreAll();
		}

		// Token: 0x06000123 RID: 291 RVA: 0x00007068 File Offset: 0x00005268
		public void Pop()
		{
			if (this._isPopped)
			{
				return;
			}
			this._isPopped = true;
			this._eyeBone.parent.localScale = Vector3.zero;
			this.ClearPopped();
			this.InvokeEyeballPopped();
			if (ModOptions.oldRemovableEyes)
			{
				Catalog.GetData<ItemData>("Eyeball", true).SpawnAsync(delegate(Item item)
				{
					this._poppedEyeball = item.gameObject;
				}, new Vector3?(this._eyeBone.position), new Quaternion?(this._eyeBone.rotation), null, true, null, 0);
			}
			else if (ModOptions.newRemovableEyes)
			{
				Catalog.InstantiateAsync("SD.Prefabs.PoachedEye", this._eyeBone.position, this._eyeBone.rotation * Quaternion.Euler(0f, -90f, 0f), this.creature.transform, delegate(GameObject eye)
				{
					this._poppedEyeball = eye;
					eye.transform.SetAsLastSibling();
					eye.transform.position = this._eyeBone.transform.position;
					eye.transform.localPosition += ((this.side == 1) ? new Vector3(0.01f, 0f, 0f) : new Vector3(-0.01f, 0f, 0f));
					eye.name = "CR-EYE";
					foreach (Collider col in eye.GetComponentsInChildren<Collider>())
					{
						foreach (Collider col2 in this.creature.ragdoll.GetPart(1).GetComponentsInChildren<Collider>())
						{
							Physics.IgnoreCollision(col, col2, true);
						}
					}
					eye.GetComponentInChildren<FixedJoint>().connectedBody = this.creature.ragdoll.GetPart(1).physicBody.rigidBody;
				}, "eye pop");
			}
			if (Random.value > 0.5f)
			{
				CreatureEye creatureEye = this.eye;
				if (creatureEye == null)
				{
					return;
				}
				creatureEye.SetClose();
			}
		}

		// Token: 0x06000124 RID: 292 RVA: 0x00007167 File Offset: 0x00005367
		private void ClearPopped()
		{
			Object.Destroy(this._poppedEyeball);
			this._poppedEyeball = null;
		}

		// Token: 0x040000B2 RID: 178
		public Creature creature;

		// Token: 0x040000B3 RID: 179
		public CreatureEye eye;

		// Token: 0x040000B4 RID: 180
		public Side side;

		// Token: 0x040000B5 RID: 181
		private Transform _eyeBone;

		// Token: 0x040000B6 RID: 182
		private readonly Collider[] _results = new Collider[5];

		// Token: 0x040000B7 RID: 183
		private bool _isPopped;

		// Token: 0x040000B8 RID: 184
		private float _inactiveTimer;

		// Token: 0x040000B9 RID: 185
		private GameObject _poppedEyeball;
	}
}
