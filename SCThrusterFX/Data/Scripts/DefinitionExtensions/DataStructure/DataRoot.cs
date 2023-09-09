using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Utils;

namespace Draygo.BlockExtensionsAPI.DataStructure
{
	public class DataRoot
	{
		public Dictionary<MyDefinitionId, Dictionary<MyStringId, PropertiesGroup>> Groups = new Dictionary<MyDefinitionId, Dictionary<MyStringId, PropertiesGroup>>(MyDefinitionId.Comparer);
		public Dictionary<MyDefinitionId, Dictionary<MyStringId, GameLogicManager>> ModDefinitionComponents = new Dictionary<MyDefinitionId, Dictionary<MyStringId, GameLogicManager>>(MyDefinitionId.Comparer);
		public Dictionary<MyStringId, GameLogicManager> ModComponents = new Dictionary<MyStringId, GameLogicManager>(MyStringId.Comparer);
		public HashSet<MyDefinitionId> IndexedIds = new HashSet<MyDefinitionId>(MyDefinitionId.Comparer);



		internal void Import(Definitions definition)
		{
			Import(definition?.CubeBlocks);
			Import(definition?.Components);
			Import(definition?.PhysicalItems);
			Import(definition?.PlanetGeneratorDefinitions);
			Import(definition?.Definition);
		}

		internal void Import(Definition[] defs)
		{
			if (defs == null)
				return;
			foreach (var def in defs)
			{
				Import(def);
			}
		}

		internal void Import(Definition def)
		{
			MyLog.Default.WriteLine($"Importing {def.Id}");

			if (!IndexedIds.Contains(def.Id))
			{
				IndexedIds.Add(def.Id);
			}
			ImportGroups(def);
			ImportModComponents(def);
		}

		private void ImportModComponents(Definition def)
		{
			if (def?.ModExtensions?.ModComponents == null)
				return;
			if (!ModDefinitionComponents.ContainsKey(def.Id))
			{
				ModDefinitionComponents.Add(def.Id, new Dictionary<MyStringId, GameLogicManager>(MyStringId.Comparer));
			}

			foreach (var modcomp in def.ModExtensions.ModComponents)
			{
				
				var strid = MyStringId.GetOrCompute(modcomp.Name);
				if(!ModComponents.ContainsKey(strid))
				{
					MyLog.Default.WriteLine($"Creating {modcomp.Name}");
					ModComponents[strid] = new GameLogicManager(strid);
				}

				if (modcomp.Enabled)
				{
					MyLog.Default.WriteLine($"Registering {modcomp.Name} {def.Id}");
					
					ModDefinitionComponents[def.Id][strid] = ModComponents[strid];

				}
				else
				{
					if(ModDefinitionComponents[def.Id].ContainsKey(strid))
						ModDefinitionComponents[def.Id].Remove(strid);
				}
			}
		}

		private void ImportGroups(Definition def)
		{

			if (def?.ModExtensions?.Group == null)
				return;
			if (!Groups.ContainsKey(def.Id))
			{
				Groups.Add(def.Id, new Dictionary<MyStringId, PropertiesGroup>());

			}
			//MyLog.Default.WriteLine($"Importing {def.Id}");
			foreach (var group in def.ModExtensions.Group)
			{
				var groupname = MyStringId.GetOrCompute(group.Name);
				if (!Groups[def.Id].ContainsKey(groupname))
				{
					Groups[def.Id].Add(groupname, new PropertiesGroup());
				}
				Groups[def.Id][groupname].Import(group);
			}
		}


	}
}
