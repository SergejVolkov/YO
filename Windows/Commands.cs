using System.Windows.Input;

namespace YO.Windows
{
	/// <summary>
	/// Keyboard shortcuts.
	/// </summary>
	public static class Commands {
		public static RoutedCommand F5 = new RoutedCommand();
		public static RoutedCommand Esc = new RoutedCommand();
		public static RoutedCommand CtrlO = new RoutedCommand();
		public static RoutedCommand CtrlM = new RoutedCommand();
		public static RoutedCommand CtrlQ = new RoutedCommand();

		/// <summary>
		/// Initialize shortcut class.
		/// </summary>
		public static void Init() {
			F5.InputGestures.Add(new KeyGesture(Key.F5));
			Esc.InputGestures.Add(new KeyGesture(Key.Escape));
			CtrlO.InputGestures.Add(new KeyGesture(Key.O, ModifierKeys.Control));
			CtrlM.InputGestures.Add(new KeyGesture(Key.M, ModifierKeys.Control));
			CtrlQ.InputGestures.Add(new KeyGesture(Key.Q, ModifierKeys.Control));
		}
	}
}