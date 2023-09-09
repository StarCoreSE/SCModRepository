using System;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace Draygo.BlockExtensionsAPI.DataStructure
{
	public class GameLogicManager
	{

		//pooling?
		MyStringId Name;
		Func<MyGameLogicComponent> _factory;
		IMyModContext _context;
		public Type MyGLType;
		public GameLogicManager(MyStringId name)
		{
			Name = name;
		}
		public void SetComponent(Func<MyGameLogicComponent> factory, IMyModContext cont)
		{
			_factory = factory;
			_context = cont;
			if(factory != null)
			{
				MyGLType = factory().GetType();
			}
		}

		public MyGameLogicComponent Factory()
		{
		
			return _factory?.Invoke();
		}
	}
}
