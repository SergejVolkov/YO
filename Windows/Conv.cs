using System;

namespace YO.Windows
{
	/// <summary>
	/// Day of week converter class.
	/// </summary>
	public static class Conv {
		/// <summary>
		/// Convert Sunday-Saturday format to Monday-Sunday format.
		/// </summary>
		/// <param name="international_week_day">Day of week in Sunday-Saturday format.</param>
		/// <returns>Day of week in Monday-Sunday format.</returns>
		public static int ToWeekDayRu(int international_week_day) {
			return (international_week_day - 1 + 7) % 7;
		}

		/// <summary>
		/// Convert Sunday-Saturday format to Monday-Sunday format.
		/// </summary>
		/// <param name="international_week_day">Day of week in Sunday-Saturday format.</param>
		/// <returns>Day of week in Monday-Sunday format.</returns>
		public static int ToWeekDayRu(DayOfWeek international_week_day) {
			return ToWeekDayRu(Convert.ToInt32(international_week_day));
		}
	}
}