using System;
using ThunderRoad;
using UnityEngine;

namespace CarnageReborn
{
	// Token: 0x02000006 RID: 6
	public class BoneProfile
	{
		// Token: 0x06000035 RID: 53 RVA: 0x00002BBC File Offset: 0x00000DBC
		public BoneProfile(RagdollPart bone, Func<Transform, float> getAngle, Func<Vector3, float> getVelocity, float minimumAngle, float minimumForce, float idealAngle, float idealForce, bool paralasyis = false, bool partialParalasyis = false, bool causesDeath = false, bool allowMultipleBreaks = false, Action broken = null)
		{
			this.bone = bone;
			this.getAngle = getAngle;
			this.getVelocity = getVelocity;
			this.minimumAngle = minimumAngle;
			this.minimumForce = minimumForce;
			this.idealAngle = idealAngle;
			this.idealForce = idealForce;
			this.paralysis = paralasyis;
			this.partialParalysis = partialParalasyis;
			this.causesDeath = causesDeath;
			this.allowMultipleBreaks = allowMultipleBreaks;
			this.broken = broken;
		}

		// Token: 0x06000036 RID: 54 RVA: 0x00002C2C File Offset: 0x00000E2C
		public bool CanBreak()
		{
			if (this.bone == null || this.bone.physicBody == null)
			{
				return false;
			}
			float velocity = this.getVelocity(this.bone.physicBody.angularVelocity);
			if (this.bone.isGrabbed)
			{
				velocity *= 2f;
			}
			if (velocity < this.minimumForce)
			{
				return false;
			}
			float boneAngle = this.getAngle(this.bone.transform);
			return boneAngle >= this.minimumAngle && (velocity >= this.idealForce || boneAngle >= this.idealAngle);
		}

		// Token: 0x04000010 RID: 16
		public RagdollPart bone;

		// Token: 0x04000011 RID: 17
		public float minimumAngle;

		// Token: 0x04000012 RID: 18
		public float minimumForce;

		// Token: 0x04000013 RID: 19
		public float idealAngle;

		// Token: 0x04000014 RID: 20
		public float idealForce;

		// Token: 0x04000015 RID: 21
		public bool isBroken;

		// Token: 0x04000016 RID: 22
		public bool paralysis;

		// Token: 0x04000017 RID: 23
		public bool partialParalysis;

		// Token: 0x04000018 RID: 24
		public bool causesDeath;

		// Token: 0x04000019 RID: 25
		public bool allowMultipleBreaks;

		// Token: 0x0400001A RID: 26
		public Vector3 lastRotation;

		// Token: 0x0400001B RID: 27
		public Func<Transform, float> getAngle;

		// Token: 0x0400001C RID: 28
		public Func<Vector3, float> getVelocity;

		// Token: 0x0400001D RID: 29
		public Action broken;
	}
}
