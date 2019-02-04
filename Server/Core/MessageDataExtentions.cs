using System;

namespace Coflnet.Server
{
	public static class MessageDataExtentions
	{
		/// <summary>
		/// Gets the resource target (receiver) of this <see cref="MessageData"/> if present on the server.
		/// This is the recomended way of implementing Serverside Commands
		/// </summary>
		/// <returns>The resource.</returns>
		/// <param name="data">Messagedata .</param>
		public static Referenceable GetResource(this MessageData data)
		{
			return ReferenceManager.Instance.GetResource(data.rId);
		}

		/// <summary>
		/// Gets the resource target (receiver) of this <see cref="MessageData"/> if present on the server.
		/// This is the recomended way of implementing Serverside Commands
		/// </summary>
		/// <returns>The resource.</returns>
		/// <param name="data">Data.</param>
		/// <typeparam name="T">What kind of <see cref="Referenceable"/> to get as.</typeparam>
		public static T GetResource<T>(this MessageData data) where T : Referenceable
		{
			return ReferenceManager.Instance.GetResource<T>(data.rId);
		}
	}
}
