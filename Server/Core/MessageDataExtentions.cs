using System;

namespace Coflnet.Server
{
	public static class CommandDataExtentions
	{
		/// <summary>
		/// Gets the resource target (receiver) of this <see cref="CommandData"/> if present on the server.
		/// This is the recomended way of implementing Serverside Commands
		/// </summary>
		/// <returns>The resource.</returns>
		/// <param name="data"><see cref="CommandData"/> .</param>
		public static Entity GetResource(this CommandData data)
		{
			return EntityManager.Instance.GetResource(data.Recipient);
		}

		/// <summary>
		/// Gets the resource target (receiver) of this <see cref="CommandData"/> if present on the server.
		/// This is the recomended way of implementing Serverside Commands
		/// </summary>
		/// <returns>The resource.</returns>
		/// <param name="data">Data.</param>
		/// <typeparam name="T">What kind of <see cref="Entity"/> to get as.</typeparam>
		public static T GetEntity<T>(this CommandData data) where T : Entity
		{
			return EntityManager.Instance.GetEntity<T>(data.Recipient);
		}
	}
}
