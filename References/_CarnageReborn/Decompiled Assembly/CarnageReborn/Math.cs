using System;

namespace CarnageReborn
{
	// Token: 0x02000028 RID: 40
	public static class Math
	{
		// Token: 0x060000FC RID: 252 RVA: 0x00006561 File Offset: 0x00004761
		public static float Normalize(this float value, float min, float max)
		{
			return (value - min) / (max - min);
		}

		// Token: 0x060000FD RID: 253 RVA: 0x0000656A File Offset: 0x0000476A
		public static float GetPercentage(this float value, float percent)
		{
			return value / 100f * percent;
		}
	}
}
