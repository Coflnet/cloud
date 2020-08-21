using System.Collections;
using System.Collections.Generic;



namespace Coflnet.Client
{
	public class UserService
	{

		public static UserService Instance { get; }

		public ClientCore ClientCoreInstance {get;set;}

		static UserService()
		{
			Instance = new UserService();
		}

		/// <summary>
		/// Gets the current user.
		/// </summary>
		/// <value>The current user if present.</value>
		public CoflnetUser CurrentUser
		{
			get
			{
				var userId = ConfigController.UserSettings.userId;
				if (userId == EntityId.Default)
				{
					throw new System.Exception("No user registered yet");
				}

				return EntityManager.Instance.GetEntity<CoflnetUser>(userId);
			}
		}

		/// <summary>
		/// Tries the get user. Will return false and null as the user if not found.
		/// </summary>
		/// <returns><c>true</c>, if get user was found, <c>false</c> otherwise.</returns>
		/// <param name="user">User.</param>
		public bool TryGetUser(out CoflnetUser user)
		{
			var userId = ConfigController.UserSettings.userId;
			return EntityManager.Instance.TryGetEntity<CoflnetUser>(userId, out user);
		}

		/// <summary>
		/// Creates a new user.
		/// </summary>
		/// <param name="privacyOptions">Privacy options.</param>
		public void CreateUser(Dictionary<string, bool> privacyOptions = null)
		{
			if (privacyOptions == null)
			{
				privacyOptions = new Dictionary<string, bool>();
			}

			var req = new CreateUser.CreateUserRequest();
			req.privacySettings = privacyOptions;

			var installId = ConfigController.InstallationId;
			
			// create the user locally and send the creation request to the server
			var tempUserId = ClientCoreInstance.CreateEntity<CreateUser,CreateUser.CreateUserRequest>(req,installId).Id;

			// Set the created user Active 
			ChangeCurrentUser(tempUserId);

		}

		/// <summary>
		/// Changes the current user.
		/// </summary>
		/// <param name="id">Identifier.</param>
		public void ChangeCurrentUser(EntityId id)
		{
			CurrentUserId = id;
		}


		/// <summary>
		/// Gets the available users.
		/// </summary>
		/// <value>The available users.</value>
		public List<UserSettings> AvailableUsers
		{
			get
			{
				return ConfigController.Users;
			}
		}

		/// <summary>
		/// Gets or sets the current user identifier.
		/// </summary>
		/// <value>The current user identifier.</value>
		public EntityId CurrentUserId
		{
			get
			{
				if(ConfigController.ActiveUserId.IsLocal)
				{
					// try to update the user id to the server generated one
					ConfigController.ActiveUserId = ClientCoreInstance.EntityManager.
													GetEntity<CoflnetUser>(ConfigController.ActiveUserId).Id;
				}
				return ConfigController.ActiveUserId;
			} 
			set
			{
				ConfigController.ActiveUserId = value;
			}
		}
	}

}

