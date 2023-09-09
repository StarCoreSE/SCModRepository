using Draygo.BlockExtensionsAPI.DataStructure;
using ProtoBuf;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.Game.Gui;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Serialization;
using VRage.Utils;
using VRageMath;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;

namespace Draygo.BlockExtensionsAPI
{
	[MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
	public class DefinitionExtensionsAPICore : MySessionComponentBase
	{
		public const long MODID = 3033050686;
		public const ushort MODMESSAGEID = (ushort)(MODID % ushort.MaxValue);
		public DataRoot ModProperties = new DataRoot();

		public DefinitionExtensionsAPICore()
		{

		}
		Dictionary<Type, Delegate> ApiMethods = new Dictionary<Type, Delegate>();
		IReadOnlyDictionary<Type, Delegate> ReadApiMethods;

		bool _isServer = false;
		bool _isDedicated = false;
		bool _connectedToDedicated = false;

		public override void LoadData()
		{
			if (MyAPIGateway.Utilities == null)
				MyAPIGateway.Utilities = MyAPIUtilities.Static;

			Func<MyDefinitionId, MyStringId, MyStringId, MyTuple<bool, float>> _floatMethod = GetPropertyFloat;
			Func<MyDefinitionId, MyStringId, MyStringId, MyTuple<bool, double>> _doubleMethod = GetPropertyDouble;
			Func<MyDefinitionId, MyStringId, MyStringId, MyTuple<bool, int>> _intMethod = GetPropertyInt;
			Func<MyDefinitionId, MyStringId, MyStringId, MyTuple<bool, long>> _longMethod = GetPropertyLong;
			Func<MyDefinitionId, MyStringId, MyStringId, MyTuple<bool, string>> _textMethod = GetPropertyText;
			Func<MyDefinitionId, MyStringId, MyStringId, MyTuple<bool, Color>> _colorMethod = GetPropertyColor;
			Func<MyDefinitionId, MyStringId, MyStringId, MyTuple<bool, bool>> _booleanMethod = GetPropertyBoolean;
			Func<MyDefinitionId, MyStringId, MyStringId, MyTuple<bool, Vector2I>> _vector2IMethod = GetPropertyVector2I;
			Func<MyDefinitionId, MyStringId, MyStringId, MyTuple<bool, Vector2D>> _vector2DMethod = GetPropertyVector2D;
			Func<MyDefinitionId, MyStringId, MyStringId, MyTuple<bool, Vector3I>> _vector3IMethod = GetPropertyVector3I;
			Func<MyDefinitionId, MyStringId, MyStringId, MyTuple<bool, Vector3D>> _vector3DMethod = GetPropertyVector3D;
			Action<MyStringId, IMyModContext, Func<MyGameLogicComponent>> _RegisterModComponent = SetGameLogic;
			Func<int, Delegate> _GetDelegate = GetDelegate;
			ApiMethods.Clear();
			ApiMethods.Add(typeof(float), _floatMethod);
			ApiMethods.Add(typeof(double), _doubleMethod);
			ApiMethods.Add(typeof(int), _intMethod);
			ApiMethods.Add(typeof(long), _longMethod);
			ApiMethods.Add(typeof(string), _textMethod);
			ApiMethods.Add(typeof(Color), _colorMethod);
			ApiMethods.Add(typeof(bool), _booleanMethod);
			ApiMethods.Add(typeof(Vector2I), _vector2IMethod);
			ApiMethods.Add(typeof(Vector2D), _vector2DMethod);
			ApiMethods.Add(typeof(Vector3I), _vector3IMethod);
			ApiMethods.Add(typeof(Vector3D), _vector3DMethod);
			ApiMethods.Add(typeof(MyGameLogicComponent), _RegisterModComponent);
			ApiMethods.Add(typeof(Delegate), _GetDelegate);
			ReadApiMethods = new Dictionary<Type, Delegate>(ApiMethods);

			foreach (var mod in MyAPIGateway.Session.Mods)
			{

				if (MyAPIGateway.Utilities.FileExistsInModLocation("Data\\DefinitionExtensions.txt", mod))
				{
					using (var reader = MyAPIGateway.Utilities.ReadFileInModLocation("Data\\DefinitionExtensions.txt", mod))
					{
						while (reader.Peek() != -1)
						{
							ImportFile(reader.ReadLine(), mod);
						}
					}
				}
			}
			SendModHandler();

			_isServer = MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE || MyAPIGateway.Multiplayer.IsServer;
			_isDedicated = (MyAPIGateway.Utilities.IsDedicated && _isServer);
			if (_isDedicated)
			{
				MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(MODMESSAGEID, RecieveModMessage);
			}


			MyAPIGateway.Entities.OnEntityAdd += OnEntityAdded;

			MyAPIGateway.TerminalControls.CustomControlGetter += TerminalControls_CustomControlGetter;
			base.LoadData();
		}

		private void SetGameLogic(MyStringId componentName, IMyModContext mod, Func<MyGameLogicComponent> factory)
		{
			if (ModProperties.ModComponents.ContainsKey(componentName))
			{
				ModProperties.ModComponents[componentName].SetComponent(factory, mod);
			}
		}

		protected override void UnloadData()
		{
			MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdded;
			ModProperties = null;

		}

		List<IMySlimBlock> blocks = new List<IMySlimBlock>();
		private void OnEntityAdded(IMyEntity obj)
		{
			if (obj is IMyCubeGrid)
			{
				var grid = (IMyCubeGrid)obj;
				grid.OnBlockAdded += Grid_OnBlockAdded;
				grid.OnBlockRemoved += Grid_OnBlockRemoved;

				blocks.Clear();

				grid.GetBlocks(blocks);
				foreach (var block in blocks)
				{
					if (block.FatBlock != null)
						Grid_OnBlockAdded(block.FatBlock);
				}
			}
		}

		private void Grid_OnBlockRemoved(IMySlimBlock obj)
		{

		}

		private void Grid_OnBlockAdded(IMySlimBlock block)
		{
			if (block.FatBlock == null)
				return;
			Grid_OnBlockAdded(block.FatBlock);
		}

		private void Grid_OnBlockAdded(IMyCubeBlock block)
		{
			//MyLog.Default.WriteLineAndConsole($"{block} added");
			MyDefinitionId blockdef = block.BlockDefinition;
			if (blockdef == null)
				return;
			if (!ModProperties.ModDefinitionComponents.ContainsKey(blockdef))
			{
				return;
			}

			foreach (var comp in ModProperties.ModDefinitionComponents[blockdef])
			{
				if (comp.Value?.MyGLType == null)
					continue;
				if (block.GameLogic.Container.Contains(comp.Value.MyGLType) || block.GameLogic.GetType() == comp.Value.MyGLType)
					continue;

				var component = comp.Value.Factory();

				if (component == null)
				{
					continue;
				}
				
				if (!(block.GameLogic is MyCompositeGameLogicComponent))
				{
					List<MyGameLogicComponent> comps = new List<MyGameLogicComponent>();
					var oldcomp = block.GameLogic as MyGameLogicComponent;
					if(!(oldcomp is MyNullGameLogicComponent))
						comps.Add(oldcomp);
					comps.Add(component);
					block.GameLogic = null;
					block.GameLogic = MyCompositeGameLogicComponent.Create(comps, (MyEntity)block);

					if (!(oldcomp is MyNullGameLogicComponent))
					{
						if (oldcomp.Entity == null)
							oldcomp.SetContainer(block.Components);
					}
					else
					{
						oldcomp.Close();
					}

				}
				else
				{
					block.GameLogic.Container.Add(component);
					if(component.Container == null)
						component.SetContainer(block.Components);
				}
				component.Init(block.GetObjectBuilder());
			}
		}

		private void ImportFile(string v, MyObjectBuilder_Checkpoint.ModItem mod)
		{
			
			if (v.Length <= 0)
				return;
			MyLog.Default.WriteLineAndConsole($"DEAPI: Importing file {v} {mod}");
			if (MyAPIGateway.Utilities.FileExistsInModLocation($"Data\\{v}", mod))
			{
				Definitions readdefinition = null;
				using (var reader = MyAPIGateway.Utilities.ReadFileInModLocation($"Data\\{v}", mod))
				{
					//MyLog.Default.WriteLineAndConsole("opened file for read");
					StringBuilder sb = new StringBuilder();
					while (reader.Peek() != -1)
					{
						var line = reader.ReadLine();
						if (line.Contains("<Definition xsi:type="))
						{
							line = "<Definition>";
						}
						sb.AppendLine(line);
					}
					var data = sb.ToString();
					try
					{
						readdefinition = MyAPIGateway.Utilities.SerializeFromXML<Definitions>(data);
					}
					catch (Exception ex)
					{
						MyLog.Default.WriteLineAndConsole($"BlockExtensionsAPICore::ImportFile - Failed to serialize from XML - {v}");
						MyLog.Default.WriteLineAndConsole(ex.ToString());
					}
				}
				if (readdefinition != null)
				{
					ModProperties.Import(readdefinition);
				}
			}
		}

		private void SendModHandler()
		{
			MyAPIGateway.Utilities.SendModMessage(MODID, (object)ReadApiMethods);
		}

		private bool GroupExists(MyDefinitionId def, MyStringId group)
		{
			return ModProperties.Groups[def].ContainsKey(group);
		}

		private bool DefIDExists(MyDefinitionId def)
		{
			return (ModProperties?.Groups?.ContainsKey(def)) ?? false;
		}

		private void GetAllModifiedDefinitionIds(HashSet<MyDefinitionId> definitions)
		{
			if (definitions == null)
				definitions = new HashSet<MyDefinitionId>(MyDefinitionId.Comparer);
			definitions.Clear();
			foreach (var item in ModProperties.IndexedIds)
			{
				definitions.Add(item);
			}
		}

		public MyTuple<bool, long> GetPropertyLong(MyDefinitionId def, MyStringId group, MyStringId propertyname)
		{
			if (!DefIDExists(def) || !GroupExists(def, group) || !(ModProperties.Groups[def][group].Integer?.ContainsKey(propertyname) ?? false))
			{
				return new MyTuple<bool, long>(false, default(long));
			}
			else
			{
				return new MyTuple<bool, long>(true, ModProperties.Groups[def][group].Integer[propertyname]);
			}
		}

		public enum AdditionalMethods : int
		{
			None = 0,
			RegisterTSS = 1,
			UnRegisterTSS = 2,
			RegisterDataTSS = 3,
			GetDataTSS = 4,
			DefIDExists = 5,
			GetGroups = 6,
			GetProperties = 7,
			GetAllIndexedIds = 8
		}
		
		public Delegate GetDelegate(int delegateflag)
		{
			var methodrequested = (AdditionalMethods)delegateflag;
			switch(methodrequested)
			{
				case AdditionalMethods.RegisterTSS:
					Action<MyTSSCommon, IMyTextSurface, IMyTerminalBlock, Action<MyTSSCommon, IMyTerminalBlock, List<IMyTerminalControl>>> _RegisterTSS = RegisterTSS;
					return _RegisterTSS;
				case AdditionalMethods.UnRegisterTSS:
					Action<MyTSSCommon, IMyTextSurface, IMyTerminalBlock> _Unregister = UnRegisterTSS;
					return _Unregister;
				case AdditionalMethods.RegisterDataTSS:
					Action<Type, Type, IMyModContext, Func<MyEntityComponentBase>> _RegisterDataComponent = RegisterTSSDataComponent;
					return _RegisterDataComponent;
				case AdditionalMethods.GetDataTSS:
					Func<Type, IMyTerminalBlock, MyEntityComponentBase> _GetTSSDataComponent = GetTSSDataComponent;
					return _GetTSSDataComponent;
				case AdditionalMethods.DefIDExists:
					Func<MyDefinitionId, bool> _DefIDExists = DefIDExists;
					return _DefIDExists;
				case AdditionalMethods.GetGroups:
					Action<MyDefinitionId, List<MyStringId>> _GetGroups = GetGroups;
					return _GetGroups;
				case AdditionalMethods.GetProperties:
					Action<MyDefinitionId, MyStringId, List<MyTuple<MyStringId, Type>>> _GetProperties = GetProperties;
					return _GetProperties;
				case AdditionalMethods.GetAllIndexedIds:
					Action<HashSet<MyDefinitionId>> _GetAllModifiedIds = GetAllModifiedDefinitionIds;
					return _GetAllModifiedIds;
				case AdditionalMethods.None:
				default:
					return null;
			}
		}

		private void GetProperties(MyDefinitionId def, MyStringId group, List<MyTuple<MyStringId, Type>> propertylist)
		{
			if (!DefIDExists(def))
				return;
			if (!GroupExists(def, group))
				return;
			foreach (var item in ModProperties.Groups[def][group].Text)
			{
				propertylist.Add(new MyTuple<MyStringId, Type>(item.Key, typeof(string)));
			}
			foreach (var item in ModProperties.Groups[def][group].Integer)
			{
				propertylist.Add(new MyTuple<MyStringId, Type>(item.Key, typeof(long)));
			}
			foreach (var item in ModProperties.Groups[def][group].Decimal)
			{
				propertylist.Add(new MyTuple<MyStringId, Type>(item.Key, typeof(double)));
			}
			foreach (var item in ModProperties.Groups[def][group].Boolean)
			{
				propertylist.Add(new MyTuple<MyStringId, Type>(item.Key, typeof(bool)));
			}
			foreach (var item in ModProperties.Groups[def][group].Vector2D)
			{
				propertylist.Add(new MyTuple<MyStringId, Type>(item.Key, typeof(Vector2D)));
			}
			foreach (var item in ModProperties.Groups[def][group].Vector3D)
			{
				propertylist.Add(new MyTuple<MyStringId, Type>(item.Key, typeof(Vector2D)));
			}
			foreach (var item in ModProperties.Groups[def][group].Vector2I)
			{
				propertylist.Add(new MyTuple<MyStringId, Type>(item.Key, typeof(Vector2I)));
			}
			foreach (var item in ModProperties.Groups[def][group].Vector3I)
			{
				propertylist.Add(new MyTuple<MyStringId, Type>(item.Key, typeof(Vector3I)));
			}
			foreach (var item in ModProperties.Groups[def][group].Color)
			{
				propertylist.Add(new MyTuple<MyStringId, Type>(item.Key, typeof(Color)));
			}
		}

		private void GetGroups(MyDefinitionId def, List<MyStringId> grouplist)
		{
			if (!DefIDExists(def))
				return;
			foreach(var item in ModProperties.Groups[def])
			{
				grouplist.Add(item.Key);
			}
		}

		private MyEntityComponentBase GetTSSDataComponent(Type scripttype, IMyTerminalBlock block)
		{
			if (!_registeredTSSDataComponents.ContainsKey(scripttype))
			{
				return null;
			}
			TSSDataFactory factory = _registeredTSSDataComponents[scripttype];
			Type factorytype = factory.DataScryptType;
			MyComponentBase entityComponent;
			if(block.Components.TryGet(factorytype, out entityComponent))
			{
				return entityComponent as MyEntityComponentBase;
			}
			return null;
		}

		Dictionary<Type, TSSDataFactory> _registeredTSSDataComponents = new Dictionary<Type, TSSDataFactory>(); //needs to be syncronized
		Dictionary<MyStringId, Type> _registeredTSSSerializedType = new Dictionary<MyStringId, Type>(MyStringId.Comparer);
		public struct TSSDataFactory
		{
			public IMyModContext ModContext;
			public Type DataScryptType;
			public Func<MyComponentBase> factory;
			public MyStringId Lookup;
			
		}

		private void RegisterTSSDataComponent(Type tssscripttype, Type gamelogictype, IMyModContext modcontext, Func<MyEntityComponentBase> factory)
		{
			if(_registeredTSSDataComponents.ContainsKey(tssscripttype))
			{
				_registeredTSSSerializedType.Remove(_registeredTSSDataComponents[tssscripttype].Lookup);
				_registeredTSSDataComponents.Remove(tssscripttype);
			}
			MyStringId lookup = MyStringId.GetOrCompute($"{modcontext.ModId}:{tssscripttype.FullName}");//should be unique enough
			_registeredTSSDataComponents.Add(tssscripttype, new TSSDataFactory() { DataScryptType = gamelogictype, factory = factory, ModContext = modcontext, Lookup = lookup });
			_registeredTSSSerializedType.Add(lookup, tssscripttype);
		}
		public bool _checkIsConnectedToDedicated = false;
		
		[ProtoContract]
		public struct RequestDataComponent
		{
			[ProtoMember(1)]
			public long EntityId;
			[ProtoMember(2)]
			public string SerializedComponent;

			public RequestDataComponent(long entid, string sc)
			{
				EntityId = entid;
				SerializedComponent = sc;
			}
			public byte[] Serialize()
			{
				return MyAPIGateway.Utilities.SerializeToBinary(this);
			}
		}
		private void AddComponentIfNotExistsAndSync(IMyTerminalBlock block, Type scripttype)
		{
			if(_registeredTSSDataComponents.ContainsKey(scripttype))
			{
				TSSDataFactory factory = _registeredTSSDataComponents[scripttype];
				Type factorytype = factory.DataScryptType;
				
				if(!block.Components.Contains(factorytype))
				{
					var component = factory.factory.Invoke();
					block.Components.Add(factorytype, component);

					//might delay .Init on DS 1 tick. It might be fine though. 
					component.Init(null);
				}

				//if we are a client on a dedicated server we need to request the server to load the component. 
				//if(!_checkIsConnectedToDedicated)
				//{
				//	CheckIfConnectedToDedicated();
				//}
				//actually we need to request anyway because unless the host is in sync range the LCD script will not load i believe. Maybe check this later. 
				if(!_isServer)
				{
					var message = new RequestDataComponent(block.EntityId, factory.Lookup.String);
					MyAPIGateway.Multiplayer.SendMessageToServer(MODMESSAGEID, message.Serialize(), true);
				}
			}
		}
		private void RecieveModMessage(ushort messageid, byte[] payload, ulong fromSteamId, bool fromServer)
		{
			try
			{
				if (!_isServer)
					return;
				var data = MyAPIGateway.Utilities.SerializeFromBinary<RequestDataComponent>(payload);
				var block = MyAPIGateway.Entities.GetEntityById(data.EntityId) as IMyTerminalBlock;
				if (block == null)
					return;
				MyStringId componentLookupKey;
				if(MyStringId.TryGet(data.SerializedComponent, out componentLookupKey))
				{
					if(_registeredTSSSerializedType.ContainsKey(componentLookupKey))
					{
						AddComponentIfNotExistsAndSync(block, _registeredTSSSerializedType[componentLookupKey]);
					}
				}
			}
			catch
			{

			}
		}

		private void CheckIfConnectedToDedicated()
		{
			_checkIsConnectedToDedicated = true;
			if (!_isServer)
			{
				List<IMyPlayer> players = new List<IMyPlayer>();
				MyAPIGateway.Players.GetPlayers(players);
				_connectedToDedicated = true;
				foreach (var player in players)
				{
					if (player.IsBot)
						continue;

					if (MyAPIGateway.Multiplayer.IsServerPlayer(player.Client))
					{
						_connectedToDedicated = false;
						break;
					}
				}
			}
		}

		public struct scriptdata
		{
			public MyTSSCommon tsscript;
			public Action<MyTSSCommon, IMyTerminalBlock, List<IMyTerminalControl>> controlGetter;
		}
		Dictionary<IMyTextSurface, scriptdata?> _Scripts = new Dictionary<IMyTextSurface, scriptdata?>();
		public void RegisterTSS(MyTSSCommon script, IMyTextSurface surface, IMyTerminalBlock block, Action<MyTSSCommon, IMyTerminalBlock, List<IMyTerminalControl>> ControlGetter)
		{
			Type scripttype = script.GetType();

			AddComponentIfNotExistsAndSync(block, scripttype);

			scriptdata? sd = new scriptdata() { tsscript = script, controlGetter = ControlGetter };
			if (!_Scripts.ContainsKey(surface))
			{
				
				_Scripts.Add(surface, sd);
				return;
			}

			_Scripts[surface] = sd;
		}


		public void UnRegisterTSS(MyTSSCommon script, IMyTextSurface surface, IMyTerminalBlock block)
		{
			if (!_Scripts.ContainsKey(surface))
			{
				return;
			}
			_Scripts[surface] = null;
		}
		IMyTerminalControlSeparator _Spacer1, _Spacer2;
		IMyTerminalControlListbox _screenList;
		IMyTerminalControlOnOffSwitch refreshtoggle;
		int _selectedIndex = 0;
		private void TerminalControls_CustomControlGetter(IMyTerminalBlock block, List<IMyTerminalControl> controls)
		{
			if (_isDedicated)
				return;

			Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider provider = block as Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider;

			if (provider == null)
				return;
			if (_screenList == null)
				InitTermControl();
			if (refreshtoggle == null)
			{
				foreach (var item in controls)
				{
					if (item.Id == "ShowInToolbarConfig")
					{
						refreshtoggle = (IMyTerminalControlOnOffSwitch)item;
						break;
					}
				}
			}
			bool insertedspacer = false;
			if (provider.SurfaceCount == 0)
				return;
			if (provider.SurfaceCount > 1)
			{
				insertedspacer = true;
				controls.Add(_Spacer1);
				controls.Add(_screenList);
			}
			else
				_selectedIndex = 0;


			for (int i = 0; i < provider.SurfaceCount; i++)
			{
				var surface = provider.GetSurface(i);
				if (i != _selectedIndex)
					continue;
				if(_Scripts.ContainsKey(surface))
				{
					if(_Scripts[surface].HasValue)
					{
						if (_Scripts[surface].Value.controlGetter == null)
							continue;
						if(!insertedspacer)
						{
							insertedspacer = true;
							controls.Add(_Spacer1);
						}
						_Scripts[surface].Value.controlGetter.Invoke(_Scripts[surface].Value.tsscript, block, controls);
					}
				}
			}
			if (insertedspacer)
				controls.Add(_Spacer2);
		}

		private void InitTermControl()
		{
			if(_screenList == null)
			{
				_screenList = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyTerminalBlock>("ModExtensions.OptionScript");
				_screenList.ItemSelected = SelectedItem;
				_screenList.Enabled = (b) => true;
				_screenList.Multiselect = false;
				_screenList.VisibleRowsCount = 8;
				_screenList.Visible = (b) => true;
				_screenList.ListContent = ListContent;
				_screenList.Title = MyStringId.GetOrCompute("LCD Panels");
			}
			if(_Spacer1 == null)
			{
				_Spacer1 = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyTerminalBlock>("ModExtensions.OptionScriptSpacer1");
				_Spacer1.Enabled = (b) => true;
				_Spacer1.Visible = (b) => true;
			}
			if (_Spacer2 == null)
			{
				_Spacer2 = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyTerminalBlock>("ModExtensions.OptionScriptSpacer2");
				_Spacer2.Enabled = (b) => true;
				_Spacer2.Visible = (b) => true;
			}
		}

		private void ListContent(IMyTerminalBlock block, List<MyTerminalControlListBoxItem> contentlist, List<MyTerminalControlListBoxItem> selected)
		{
			contentlist.Clear();
			selected.Clear();
			Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider provider = block as Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider;
			if (provider == null)
				return;

			if (provider.SurfaceCount == 0)
				return;
			if (_selectedIndex > provider.SurfaceCount)
				_selectedIndex = 0;
			for(int i = 0; i < provider.SurfaceCount; i++)
			{
				var surface = provider.GetSurface(i);
				MyTerminalControlListBoxItem item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(surface.DisplayName), MyStringId.GetOrCompute(""), i);
				contentlist.Add(item);
				if (_selectedIndex == i)
					selected.Add(item);
			}

		}

		private void SelectedItem(IMyTerminalBlock block, List<MyTerminalControlListBoxItem> selected)
		{
			IMyTextSurfaceProvider provider = block as IMyTextSurfaceProvider;
			if (provider == null)
				return;

			if (provider.SurfaceCount == 0)
				return;
			int newindex = _selectedIndex;
			if (selected.Count > 0 )
			{
				newindex = (int)selected[0].UserData;
			}
			if(_selectedIndex != newindex)
			{
				_selectedIndex = newindex;
				if (refreshtoggle != null)
				{
					var originalSetting = refreshtoggle.Getter(block);
					refreshtoggle.Setter(block, !originalSetting);
					refreshtoggle.Setter(block, originalSetting);
				}
			}
		}

		public MyTuple<bool, int> GetPropertyInt(MyDefinitionId def, MyStringId group, MyStringId propertyname)
		{
			var ret = GetPropertyLong(def, group, propertyname);
			return new MyTuple<bool, int>(ret.Item1, (int)ret.Item2);
		}

		public MyTuple<bool, double> GetPropertyDouble(MyDefinitionId def, MyStringId group, MyStringId propertyname)
		{
			if (!DefIDExists(def) || !GroupExists(def, group) || !(ModProperties.Groups[def][group].Decimal?.ContainsKey(propertyname) ?? false))
				return new MyTuple<bool, double>(false, default(double));
			else
				return new MyTuple<bool, double>(true, ModProperties.Groups[def][group].Decimal[propertyname]);
		
		}
		public MyTuple<bool, float> GetPropertyFloat(MyDefinitionId def, MyStringId group, MyStringId propertyname)
		{
			var ret = GetPropertyDouble(def, group, propertyname);
			return new MyTuple<bool, float>(ret.Item1, (float)ret.Item2);
		}

		public MyTuple<bool, string> GetPropertyText(MyDefinitionId def, MyStringId group, MyStringId propertyname)
		{
			if (!DefIDExists(def) || !GroupExists(def, group) || !(ModProperties.Groups[def][group].Text?.ContainsKey(propertyname) ?? false))
				return new MyTuple<bool, string>(false, default(string));
			else
				return new MyTuple<bool, string>(true, ModProperties.Groups[def][group].Text[propertyname]);

		}

		public MyTuple<bool, Color> GetPropertyColor(MyDefinitionId def, MyStringId group, MyStringId propertyname)
		{
			if (!DefIDExists(def) || !GroupExists(def, group) || !(ModProperties.Groups[def][group].Color?.ContainsKey(propertyname) ?? false))
				return new MyTuple<bool, Color>(false, default(Color));
			else
				return new MyTuple<bool, Color>(true, ModProperties.Groups[def][group].Color[propertyname]);
		}

		public MyTuple<bool, bool> GetPropertyBoolean(MyDefinitionId def, MyStringId group, MyStringId propertyname)
		{
			if (!DefIDExists(def) || !GroupExists(def, group) || !(ModProperties.Groups[def][group].Boolean?.ContainsKey(propertyname) ?? false))
				return new MyTuple<bool, bool>(false, default(bool));
			else
				return new MyTuple<bool, bool>(true, ModProperties.Groups[def][group].Boolean[propertyname]);
		}

		public MyTuple<bool, Vector2I> GetPropertyVector2I(MyDefinitionId def, MyStringId group, MyStringId propertyname)
		{
			if (!DefIDExists(def) || !GroupExists(def, group) || !(ModProperties.Groups[def][group].Vector2I?.ContainsKey(propertyname) ?? false))
				return new MyTuple<bool, Vector2I>(false, default(Vector2I));
			else
				return new MyTuple<bool, Vector2I>(true, ModProperties.Groups[def][group].Vector2I[propertyname]);
		}

		public MyTuple<bool, Vector2D> GetPropertyVector2D(MyDefinitionId def, MyStringId group, MyStringId propertyname)
		{
			if (!DefIDExists(def) || !GroupExists(def, group) || !(ModProperties.Groups[def][group].Vector2D?.ContainsKey(propertyname) ?? false))
				return new MyTuple<bool, Vector2D>(false, default(Vector2D));
			else
				return new MyTuple<bool, Vector2D>(true, ModProperties.Groups[def][group].Vector2D[propertyname]);
		}

		public MyTuple<bool, Vector3I> GetPropertyVector3I(MyDefinitionId def, MyStringId group, MyStringId propertyname)
		{
			if (!DefIDExists(def) || !GroupExists(def, group) || !(ModProperties.Groups[def][group].Vector3I?.ContainsKey(propertyname) ?? false))
				return new MyTuple<bool, Vector3I>(false, default(Vector3I));
			else
				return new MyTuple<bool, Vector3I>(true, ModProperties.Groups[def][group].Vector3I[propertyname]);
		}

		public MyTuple<bool, Vector3D> GetPropertyVector3D(MyDefinitionId def, MyStringId group, MyStringId propertyname)
		{
			if (!DefIDExists(def) || !GroupExists(def, group) || !(ModProperties.Groups[def][group].Vector3D?.ContainsKey(propertyname) ?? false))
				return new MyTuple<bool, Vector3D>(false, default(Vector3D));
			else
				return new MyTuple<bool, Vector3D>(true, ModProperties.Groups[def][group].Vector3D[propertyname]);
		}

	}
}
