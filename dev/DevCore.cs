using System;
using Coflnet.Client;
using Coflnet.Server;

namespace Coflnet.Dev {
	/// <summary>
	/// Development Core for testing purposses.
	/// It simulates A client alongside with a server.
	/// </summary>
	public class DevCore : CoflnetCore {
		private ClientCore clientCore;
		private ServerCore serverCore;

		/// <summary>
		/// This is the user and the serverid simulated
		/// </summary>
		private SourceReference userId;

		static DevCore () {

		}

		public static void Init()
		{
			ClientCore.Init();
			ServerCore.Init();
			CoflnetCore.Instance = new DevCore();
		}

		public override CommandController GetCommandController () {
			throw new System.NotImplementedException ();
		}

		/// <summary>
		/// Will execute commands directly as the server, except the receipient is the <see cref="userId">
		/// </summary>
		/// <param name="data">Data to send</param>
		/// <param name="serverId">optional serverId, ignored in this implementation</param>
		public override void SendCommand (MessageData data, long serverId = 0) {
			// set correct sender
			if (data.rId == userId) {

				data.sId = new SourceReference (data.rId.ServerId, 0);
			} else {
				data.sId = userId;
			}
			ReferenceManager.Instance.ExecuteForReference (data);
		}

		public override void SendCommand<C, T> (SourceReference receipient, T data, long id = 0) {
			var commandInstance = ((C) Activator.CreateInstance (typeof (C)));

			var messageData = MessageData.SerializeMessageData<T> (data, commandInstance.GetSlug (), id);

			if (receipient.ServerId == this.Id.ServerId && commandInstance.Settings.LocalPropagation) {

				ThreadController.Instance.ExecuteCommand (commandInstance, messageData);
			}

			SendCommand (messageData);
		}

		public override void SendCommand<C> (SourceReference receipient, byte[] data) {
			var commandInstance = ((C) Activator.CreateInstance (typeof (C)));
			var messageData = new MessageData (receipient, data, commandInstance.GetSlug ());

			SendCommand (messageData);
		}
	}
}