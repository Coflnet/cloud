using WebSocketSharp;
using Coflnet;
/// <summary>
/// Coflnet encoder extention methods.
/// </summary>
public static class CoflnetEncoderExtention
{
	public static T Deserialize<T>(this CoflnetEncoder encoder, MessageEventArgs args)
	{
		return encoder.Deserialize<T>(args.RawData);
	}

	public static ServerCommandData Deserialize(this CoflnetEncoder encoder, MessageEventArgs args)
	{
		return encoder.Deserialize<ServerCommandData>(args.RawData);
	}

	/// <summary>
	/// Send the specified data after encoding with the given socket.
	/// </summary>
	/// <param name="encoder">Encoder.</param>
	/// <param name="data">Data.</param>
	/// <param name="socket">Socket.</param>
	/// <typeparam name="T">The 1st type parameter.</typeparam>
	public static void Send<T>(this CoflnetEncoder encoder, T data, CoflnetWebsocketServer socket)
	{
		socket.SendBack(encoder.Serialize<T>(data));
	}
}






