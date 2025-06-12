using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.ModAPI;

namespace KlimeDraygo.RelativeSpectator.API
{
	public class SpecCamAPI
	{

		public enum CameraMode : int
		{
			None,
			Free,
			Orbit,
			Follow,
			FirstPerson
			
		}

		/// <summary>
		/// Call Close() when done to unregister message handlers. Check Enabled to see if it is communicating with the Relative Spectator mod. 
		/// </summary>
		public SpecCamAPI()
		{
			MyAPIGateway.Utilities.RegisterMessageHandler(1914807557, RecieveMessage);
		}

		public void Close()
		{
			MyAPIGateway.Utilities.UnregisterMessageHandler(1914807557, RecieveMessage);
        }
		

		Action<IMyEntity> m_SetTarget;
		Action<int> m_SetMode;
		Func<IMyEntity> m_GetTarget;
		Func<int> m_GetMode;
		bool m_enabled = false;
		/// <summary>
		/// True when communicating with the Relative Spectator mod
		/// </summary>
		public bool Enabled
		{
			get
			{
				return m_enabled;
			}
			private set
			{
				m_enabled = value;
			}
		}
		private void RecieveMessage(object obj)
		{
			if(obj is MyTuple<Action<IMyEntity>, Action<int>, Func<IMyEntity>, Func<int>>)
			{
				var methods = (MyTuple<Action<IMyEntity>, Action<int>, Func<IMyEntity>, Func<int>>)obj;
				m_SetTarget = methods.Item1;
				m_SetMode = methods.Item2;
				m_GetTarget = methods.Item3;
				m_GetMode = methods.Item4;
				Enabled = true;
            }
		}

		/// <summary>
		/// Sets the follow target
		/// </summary>
		/// <param name="target">Entity camera is following</param>
		public void SetTarget(IMyEntity target)
		{
			if (m_SetTarget != null)
				m_SetTarget(target);
		}

		/// <summary>
		/// Gets the current locked target for the camera. Return value may be null if the camera is not locked on an entity.
		/// </summary>
		/// <returns></returns>
		public IMyEntity GetTarget()
		{
			if(m_GetTarget != null)
			{
				return m_GetTarget();
			}
			return null;
		}

		/// <summary>
		/// Sets camera mode
		/// </summary>
		/// <param name="mode">Free, Locked, or Follow</param>
		public void SetMode(CameraMode mode)
		{
			if(m_SetMode != null)
			{
				m_SetMode((int)mode);
			}
		}

		/// <summary>
		/// Gets the camera mode
		/// </summary>
		/// <returns>Enum value, Free, Locked, or Follow</returns>
		public CameraMode GetMode()
		{
			if (m_GetMode != null)
			{
				return (CameraMode)m_GetMode();
			}


			return CameraMode.None;

		}




	}
}
