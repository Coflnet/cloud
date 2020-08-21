using System;
using MessagePack;
using System.Text;
using Coflnet;




public partial class CoflnetJsonEncoder
{
    /*
	public override void Send<T>(T data, CoflnetWebsocketServer socket)
	{
		socket.SendBack(MessagePackSerializer.ToJson<T>(data));
	}
*/
    [MessagePackObject]
	public class DevCommandData : ServerCommandData
	{
		byte[] ausgelagert;

		[Key("d")]
		public string data
		{
			get
			{
				return System.Convert.ToBase64String(message);
			}
			set
			{
				ausgelagert = Convert.FromBase64String(value);
			}
		}

		public override T GetAs<T>()
		{
			return Connection.Encoder.Deserialize<T>(this.ausgelagert);
		}


		public override string Data
		{
			get
			{
				return Encoding.UTF8.GetString(this.ausgelagert);

			}
		}


		public DevCommandData()
		{
		}

		public DevCommandData(CommandData data) : base(data)
		{
		}


	}
}






