namespace YO.Modules
{
	/// <summary>
	/// Various string array processing extension methods.
	/// </summary>
	public static class StringExtensions
	{ 
		public static int[] ToIntArray(this string argument, char splitter)
		{
			string[] array = argument.Split(splitter);
			int[] output = new int[array.Length];
			for (int i = 0; i < array.Length; i++)
				output[i] = int.Parse(array[i]);
			return output;
		}

		public static int[] ToIntArray(this string[] argument)
		{
			int[] output = new int[argument.Length];
			for (int i = 0; i < argument.Length; i++)
				output[i] = int.Parse(argument[i]);
			return output;
		}

		public static double[] ToDoubleArray(this string[] argument)
		{
			double[] output = new double[argument.Length];
			for (int i = 0; i < argument.Length; i++)
				output[i] = double.Parse(argument[i]);
			return output;
		}

		public static double[] ToDoubleArray(this string argument, char splitter)
		{
			string[] array = argument.Split(splitter);
			double[] output = new double[array.Length];
			for (int i = 0; i < array.Length; i++)
				output[i] = double.Parse(array[i]);
			return output;
		}

		public static string[] DotChange(this string[] input)
		{
			for (int i = 0; i < input.Length; i++)
			{
				input[i] = input[i].Replace(',', '.');
			}
			return input;
		}

		public static string Merge(this string[] input, char splitter, char wrapper = '\0')
		{
			string output = "";
			foreach (var elem in input)
			{
				if (wrapper == '\0') output += elem + splitter;
				else output += wrapper + elem + wrapper + splitter;
			}
			return output.Remove(output.Length - 1, 1);
		}
	}
}