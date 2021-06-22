using System;

namespace YO.Modules
{
	/// <summary>
	/// Helper struct for storing schedule and stats params.
	/// </summary>
	internal readonly struct MinMaxInfo : IComparable<MinMaxInfo>
	{
		public MinMaxInfo(int containsMin, int containsMax, DateTime addedDate)
		{
			ContainsMin = containsMin;
			ContainsMax = containsMax;
			AddedDate = addedDate;
		}

		public int ContainsMin { get; }

		public int ContainsMax { get; }

		public DateTime AddedDate { get; }

		public int CompareTo(MinMaxInfo other)
		{
			var containsMaxComparison = ContainsMax.CompareTo(other.ContainsMax);
			
			return containsMaxComparison != 0 
				? containsMaxComparison 
				: ContainsMin.CompareTo(other.ContainsMin);
		}
	}
}