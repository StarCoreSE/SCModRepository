using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Utils;
using VRageMath;

namespace Draygo.BlockExtensionsAPI.DataStructure
{
	public class PropertiesGroup
	{
		
		public Dictionary<MyStringId, string> Text = new Dictionary<MyStringId, string>(MyStringId.Comparer);

		public Dictionary<MyStringId, long> Integer = new Dictionary<MyStringId, long>(MyStringId.Comparer);

		public Dictionary<MyStringId, bool> Boolean = new Dictionary<MyStringId, bool>(MyStringId.Comparer);

		public Dictionary<MyStringId, double> Decimal = new Dictionary<MyStringId, double>(MyStringId.Comparer);

		public Dictionary<MyStringId, Vector2I> Vector2I = new Dictionary<MyStringId, Vector2I>(MyStringId.Comparer);

		public Dictionary<MyStringId, Vector2D> Vector2D = new Dictionary<MyStringId, Vector2D>(MyStringId.Comparer);

		public Dictionary<MyStringId, Vector3I> Vector3I = new Dictionary<MyStringId, Vector3I>(MyStringId.Comparer);

		public Dictionary<MyStringId, Vector3D> Vector3D = new Dictionary<MyStringId, Vector3D>(MyStringId.Comparer);

		public Dictionary<MyStringId, Color> Color = new Dictionary<MyStringId, Color>(MyStringId.Comparer);

		internal void Import(Group group)
		{
			Import(group.Boolean);
			Import(group.Color);
			Import(group.Decimal);
			Import(group.Integer);
			Import(group.Text);
			Import(group.Vector2D);
			Import(group.Vector2I);
			Import(group.Vector3D);
			Import(group.Vector3I);
		}

		internal void Import(Vector3DAttribute[] vector3Dlist)
		{
			if (vector3Dlist == null)
				return;
			//MyLog.Default.WriteLine($"vectos {vector3Dlist.Length}");
			foreach (var item in vector3Dlist)
			{
				var id = MyStringId.GetOrCompute(item.Name);
				Vector3D vec = new Vector3D(item.X, item.Y, item.Z);
				//MyLog.Default.WriteLine($"adding {vec}");
				if (!Vector3D.ContainsKey(id))
				{
					Vector3D.Add(id, vec);
					continue;
				}
				Vector3D[id] = vec;
			}
		}

		internal void Import(Vector3IAttribute[] vector3Ilist)
		{
			if (vector3Ilist == null)
				return;
			foreach (var item in vector3Ilist)
			{
				var id = MyStringId.GetOrCompute(item.Name);
				Vector3I vec = new Vector3I(item.X, item.Y, item.Z);
				//MyLog.Default.WriteLine($"adding {vec}");
				if (!Vector3I.ContainsKey(id))
				{
					Vector3I.Add(id, vec);
					continue;
				}
				Vector3I[id] = vec;
			}
		}

		internal void Import(Vector2IAttribute[] vector2Ilist)
		{
			if (vector2Ilist == null)
				return;
			foreach (var item in vector2Ilist)
			{
				var id = MyStringId.GetOrCompute(item.Name);
				Vector2I vec = new Vector2I(item.X, item.Y);
				//MyLog.Default.WriteLine($"adding {vec}");
				if (!Vector2I.ContainsKey(id))
				{
					Vector2I.Add(id, vec);
					continue;
				}
				Vector2I[id] = vec;
			}
		}

		internal void Import(Vector2DAttribute[] vector2Dlist)
		{
			if (vector2Dlist == null)
				return;
			foreach (var item in vector2Dlist)
			{
				var id = MyStringId.GetOrCompute(item.Name);
				Vector2D vec = new Vector2D(item.X, item.Y);
				//MyLog.Default.WriteLine($"adding {vec}");
				if (!Vector2D.ContainsKey(id))
				{
					Vector2D.Add(id, vec);
					continue;
				}
				Vector2D[id] = vec;
			}
		}


		internal void Import(StringAttribute[] stringlist)
		{
			if (stringlist == null)
				return;
			foreach (var item in stringlist)
			{
				var id = MyStringId.GetOrCompute(item.Name);
				//MyLog.Default.WriteLine($"adding {item.Value}");
				if (!Text.ContainsKey(id))
				{
					Text.Add(id, item.Value);
					continue;
				}
				Text[id] = item.Value;
			}
		}

		internal void Import(LongAttribute[] longlist)
		{
			if (longlist == null)
				return;
			foreach (var item in longlist)
			{
				var id = MyStringId.GetOrCompute(item.Name);
				//MyLog.Default.WriteLine($"adding {item.Value}");
				if (!Integer.ContainsKey(id))
				{
					Integer.Add(id, item.Value);
					continue;
				}
				Integer[id] = item.Value;
			}
		}

		internal void Import(DecimalAttribute[] decimallist)
		{
			if (decimallist == null)
				return;
			foreach (var item in decimallist)
			{
				var id = MyStringId.GetOrCompute(item.Name);
				//MyLog.Default.WriteLine($"adding {item.Value}");
				if (!Decimal.ContainsKey(id))
				{
					Decimal.Add(id, item.Value);
					continue;
				}
				Decimal[id] = item.Value;
			}
		}


		internal void Import(ColorAttribute[] colorlist)
		{
			if (colorlist == null)
				return;
			foreach (var item in colorlist)
			{
				var id = MyStringId.GetOrCompute(item.Name);
				var col = new Color(item.R, item.G, item.B, item.A);
				//MyLog.Default.WriteLine($"adding {col}");
				if (!Color.ContainsKey(id))
				{
					Color.Add(id, col);
					continue;
				}
				Color[id] = col;
			}
		}

		internal void Import(BooleanAttribute[] boollist)
		{
			if(boollist == null)
				return;
			foreach(var item in boollist)
			{
				var id = MyStringId.GetOrCompute(item.Name);
				//MyLog.Default.WriteLine($"adding {item.Value}");
				if (!Boolean.ContainsKey(id))
				{
					Boolean.Add(id, item.Value);
					continue;
				}
				Boolean[id] = item.Value;
			}
		}
	}
}
