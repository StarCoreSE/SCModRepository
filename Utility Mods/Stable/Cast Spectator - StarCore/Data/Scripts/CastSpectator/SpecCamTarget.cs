using System.Collections.Generic;
using Draygo.API;
using VRage.Input;
using VRageMath;

namespace KlimeDraygoMath.CastSpectator
{
	public class SpecCamTarget
	{
		Keybind ModifyKeybind = new Keybind(MyKeys.None, false, true, false);
		Keybind ActivateKeybind = new Keybind(MyKeys.None, false, false, false);

		private CameraState m_ObsCameraState = new CameraState()
		{
			localMatrix = MatrixD.Identity,
			lastOrientation = MatrixD.Identity,
			localVector = Vector3D.Zero,
			localDistance = 1f,

			lockmode = CameraMode.Free,
			lockEntity = null,
		};

		private bool m_hasstate = false;


		private void SetKey(MyKeys p)
		{
			ModifyKeybind.key = p;
			ActivateKeybind.key = p;
			SetMenuText();
		}
		public bool HasState
		{
			get
			{
				return m_hasstate;
			}
			private set
			{
				m_hasstate = value;
			}
		}
		public CameraState State
		{
			get
			{
				return m_ObsCameraState;
            }
			set
			{
				HasState = true;
				m_ObsCameraState = value;
			}
		}

		public bool IsActivateKeybindPressed()
		{
			return ActivateKeybind.IsKeybindPressed();
		}

		public bool IsModifyKeybindPressed()
		{
			return ModifyKeybind.IsKeybindPressed();
		}
		HudAPIv2.MenuKeybindInput KeybindMenu;
		CastSpectator m_parent;
		int menuitem = 0;

		internal void InitMenu(int i, HudAPIv2.MenuSubCategory saveTargetCat, CastSpectator parent)
		{
			menuitem = i;
			m_parent = parent;
            KeybindMenu = new HudAPIv2.MenuKeybindInput(getText(), saveTargetCat, "Keybind, press any key\nModifiers will be ignored.\nWill bind Ctrl + key to save camera state", this.MenuSubmitNewKey);
		}

		private string getText()
		{
			return string.Format("{0}: {1}", menuitem, ActivateKeybind);
        }
		internal void SetMenuText()
		{
			if(KeybindMenu != null)
			{
				KeybindMenu.Text = getText();
			}
		}
		public void MenuSubmitNewKey(MyKeys newkey, bool shift, bool ctrl, bool alt)
		{
			SetKey(newkey);
			SetMenuText();
            if (m_parent?.m_Pref != null)
			{
				m_parent.m_Pref.SaveTarget[menuitem - 1] = newkey;
				SpecCamPreferences.saveXML(m_parent.m_Pref);
			}
        }

		public static void LoadKeybinds(List<SpecCamTarget> TargetCache, List<MyKeys> key)
		{
			if (TargetCache.Count <= key.Count)
			{
				for (int i = 0; i < TargetCache.Count; i++)
				{
					TargetCache[i].SetKey(key[i]);

				}

			}
		}
	}
}
