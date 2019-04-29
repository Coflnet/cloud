using System.Collections;
using System.Collections.Generic;
using Coflnet;
using Coflnet;
using System;

namespace Coflnet
{
	/// <summary>
	/// Base class for Commands just returning a value
	/// </summary>
	public abstract class ReturnCommand : Command
	{
		public override void Execute(MessageData data)
		{
			var returnData = ExecuteWithReturn(data);
			// set headers
			returnData.t = "return";
			returnData.rId = data.sId;
			returnData.sId = data.rId;

			// wrap it into a container so receiver knows to what request it coresponds to
			returnData.message = IEncryption.ConcatBytes(BitConverter.GetBytes(data.mId), returnData.message);

			data.SendBack(returnData);
			//SendTo(data.sId, user.PublicId, "createdUser");
		}


		public abstract MessageData ExecuteWithReturn(MessageData data);

	}

	/// <summary>
	/// Special command for tunneling a response based flow (like http)
	/// <see cref="ReturnCommand"/> isn't persisted nor distributed.
	/// </summary>
	public class ReturnResponseCommand : Command
	{
		/// <summary>
		/// Execute the command logic with specified data.
		/// </summary>
		/// <param name="data"><see cref="MessageData"/> passed over the network .</param>
		public override void Execute(MessageData data)
		{
			// The command may not be present anymore
			long id = BitConverter.ToInt64(data.message, 0);
			byte[] dataWithoutId = new byte[data.message.Length - 8];
			data.message.CopyTo(dataWithoutId, 8);
			data.message = dataWithoutId;


			ReturnCommandService.Instance.ReceiveMessage(id, data);
		}

		/// <summary>
		/// Special settings and Permissions for this <see cref="Command"/>
		/// </summary>
		/// <returns>The settings.</returns>
		public override CommandSettings GetSettings()
		{
			return new CommandSettings();
		}
		/// <summary>
		/// Gets the globally unique slug (short human readable id).
		/// </summary>
		/// <returns>The slug .</returns>
		public override string GetSlug()
		{
			return "return";
		}
	}




}

