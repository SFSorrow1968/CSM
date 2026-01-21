using System;
using System.Collections.Generic;

namespace CarnageReborn
{
	// Token: 0x02000027 RID: 39
	public static class MainThread
	{
		// Token: 0x060000F9 RID: 249 RVA: 0x00006501 File Offset: 0x00004701
		public static void Invoke(Action action)
		{
			MainThread._actions.Add(action);
		}

		// Token: 0x060000FA RID: 250 RVA: 0x00006510 File Offset: 0x00004710
		internal static void ThreadTick()
		{
			for (int i = MainThread._actions.Count - 1; i >= 0; i--)
			{
				Action action = MainThread._actions[i];
				if (action != null)
				{
					action();
				}
				MainThread._actions.RemoveAt(i);
			}
		}

		// Token: 0x040000B1 RID: 177
		private static readonly List<Action> _actions = new List<Action>();
	}
}
