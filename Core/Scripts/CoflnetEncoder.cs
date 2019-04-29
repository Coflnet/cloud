
using MessagePack;

namespace Coflnet
{
	public class CoflnetEncoder
	{


		public virtual T Deserialize<T>(byte[] args)
		{
			return MessagePackSerializer.Deserialize<T>(args);
		}

		public virtual byte[] Serialize<T>(T target)
		{
			return MessagePackSerializer.Serialize<T>(target);
		}

		public static CoflnetEncoder Instance { get; }


		static CoflnetEncoder()
		{
			Instance = new CoflnetEncoder();
		}
	}
}