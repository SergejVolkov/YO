using System;

namespace YO.Internals.Extensions
{
	public static class DateTimeExtensions
	{
		public static DayOfWeek AddDays(this DayOfWeek current, int offset) 
			=> (DayOfWeek) (((int) current + offset) % 7);
	}
}