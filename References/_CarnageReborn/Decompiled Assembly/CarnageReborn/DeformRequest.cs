using System;
using UnityEngine;

namespace CarnageReborn
{
	// Token: 0x0200000D RID: 13
	public readonly struct DeformRequest
	{
		// Token: 0x06000082 RID: 130 RVA: 0x00004B74 File Offset: 0x00002D74
		public DeformRequest(Vector3 worldPoint, Vector3 velocity, Transform target, float radius = 0.08f, float velocityMultiplier = 0.0025f)
		{
			this.worldPoint = worldPoint;
			this.velocity = velocity;
			this.localToWorldMatrix = target.localToWorldMatrix;
			this.worldToLocalMatrix = target.worldToLocalMatrix;
			this.velocityOffset = target.InverseTransformVector(velocity * velocityMultiplier);
			this.radius = radius;
			this.velocityMultiplier = velocityMultiplier;
		}

		// Token: 0x04000035 RID: 53
		public readonly Vector3 worldPoint;

		// Token: 0x04000036 RID: 54
		public readonly Vector3 velocity;

		// Token: 0x04000037 RID: 55
		public readonly Vector3 velocityOffset;

		// Token: 0x04000038 RID: 56
		public readonly Matrix4x4 localToWorldMatrix;

		// Token: 0x04000039 RID: 57
		public readonly Matrix4x4 worldToLocalMatrix;

		// Token: 0x0400003A RID: 58
		public readonly float radius;

		// Token: 0x0400003B RID: 59
		public readonly float velocityMultiplier;
	}
}
