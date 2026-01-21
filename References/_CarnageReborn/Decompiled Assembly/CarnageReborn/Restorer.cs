using System;
using UnityEngine;

namespace CarnageReborn
{
	// Token: 0x0200000E RID: 14
	public class Restorer : MonoBehaviour
	{
		// Token: 0x06000083 RID: 131 RVA: 0x00004BCC File Offset: 0x00002DCC
		private void OnCollisionEnter(Collision collision)
		{
			if (Time.time > this.lastRestore)
			{
				Deformable[] restorables = collision.collider.GetComponentsInParent<Deformable>();
				if (restorables != null && restorables.Length != 0)
				{
					this.lastRestore = Time.time + 0.1f;
					Deformable[] array = restorables;
					for (int i = 0; i < array.Length; i++)
					{
						array[i].RestoreAll();
					}
				}
			}
		}

		// Token: 0x0400003C RID: 60
		private float lastRestore;
	}
}
