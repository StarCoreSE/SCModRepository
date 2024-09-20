using Sandbox.ModAPI;
using VRage.Input;

namespace KlimeDraygoMath.CastSpectator
{
	public struct Keybind
	{

		public MyKeys key;
		public bool shift;
		public bool ctrl;
		public bool alt;
		public Keybind(MyKeys key, bool shift, bool ctrl, bool alt) : this()
		{
			this.key = key;
			this.shift = shift;
			this.ctrl = ctrl;
			this.alt = alt;
		}
		public bool IsKeybindPressed()
		{
 
			if (key == MyKeys.None)
				return false;
			return MyAPIGateway.Input.IsNewKeyPressed(key) && (shift == MyAPIGateway.Input.IsAnyShiftKeyPressed()) && (ctrl == MyAPIGateway.Input.IsAnyCtrlKeyPressed()) && (alt == MyAPIGateway.Input.IsAnyAltKeyPressed());
        }
		public override string ToString()
		{
			return string.Format("{0}{1}{2}{3}", (shift ? "Shift " : ""), (ctrl ? "Ctrl " : ""), (alt ? "Alt " : ""), key.ToString());
		}

	}
}
