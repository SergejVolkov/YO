using System;

namespace YO.Windows
{
	/// <summary>
	/// Day of week converter class.
	/// </summary>
	public static class WeekDayConverter {
		/// <summary>
		/// Convert Sunday-Saturday format to Monday-Sunday format.
		/// </summary>
		/// <param name="internationalWeekDay">Day of week in Sunday-Saturday format.</param>
		/// <returns>Day of week in Monday-Sunday format.</returns>
		public static int ToWeekDayRu(int internationalWeekDay) {
			return (internationalWeekDay - 1 + 7) % 7;
		}

		/// <summary>
		/// Convert Sunday-Saturday format to Monday-Sunday format.
		/// </summary>
		/// <param name="internationalWeekDay">Day of week in Sunday-Saturday format.</param>
		/// <returns>Day of week in Monday-Sunday format.</returns>
		public static int ToWeekDayRu(DayOfWeek internationalWeekDay) {
			return ToWeekDayRu((int) internationalWeekDay);
		}
	}
}