using System;
using System.IO;
using ThunderRoad;
using UnityEngine;

namespace CarnageReborn
{
	// Token: 0x02000025 RID: 37
	public static class GeneralUtilities
	{
		// Token: 0x060000F1 RID: 241 RVA: 0x0000623C File Offset: 0x0000443C
		public static string GetRandomFirstName(CreatureData.Gender gender)
		{
			string[] lines = File.ReadAllLines(Path.Combine(Entry.Location, "Data", (gender == 1) ? "names_male.txt" : "names_female.txt"));
			return lines[Random.Range(0, lines.Length)];
		}

		// Token: 0x060000F2 RID: 242 RVA: 0x0000627C File Offset: 0x0000447C
		public static string GetRandomLastName()
		{
			string[] lines = File.ReadAllLines(Path.Combine(Entry.Location, "Data", "last_names.txt"));
			return lines[Random.Range(0, lines.Length)];
		}

		// Token: 0x060000F3 RID: 243 RVA: 0x000062B0 File Offset: 0x000044B0
		public static string GetRandomCrime()
		{
			string[] lines = File.ReadAllLines(Path.Combine(Entry.Location, "Data", "crimes.txt"));
			return lines[Random.Range(0, lines.Length)];
		}
	}
}
