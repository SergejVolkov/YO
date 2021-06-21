using System;

namespace YO.Modules
{
	/// <summary>
	/// Helper struct for storing schedule and stats params.
	/// </summary>
	struct MinMaxInfo {
		private int contains_min, contains_max;
		DateTime added_date;

		public MinMaxInfo(int contains_min, int contains_max, DateTime added_date) {
			this.contains_min = contains_min;
			this.contains_max = contains_max;
			this.added_date = added_date;
		}

		public int ContainsMin => contains_min;
		public int ContainsMax => contains_max;
		public DateTime AddedDate => added_date;
	}
}