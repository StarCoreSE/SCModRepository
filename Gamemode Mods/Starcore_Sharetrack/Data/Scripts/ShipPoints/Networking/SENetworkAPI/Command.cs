using ProtoBuf;

namespace SENetworkAPI
{
	[ProtoContract]
	internal class Command
	{
		[ProtoMember(1)]
		public ulong SteamId { get; set; }
		[ProtoMember(2)]
		public string CommandString { get; set; }
		[ProtoMember(3)]
		public string Message { get; set; }
		[ProtoMember(4)]
		public byte[] Data { get; set; }
		[ProtoMember(5)]
		public long Timestamp { get; set; }
		[ProtoMember(6)]
		public bool IsProperty { get; set; }
		[ProtoMember(7)]
		public bool IsCompressed { get; set; }
	}
}
