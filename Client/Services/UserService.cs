using System.Collections;
using System.Collections.Generic;
using UnityEngine;



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
				if (userId == SourceReference.Default)
				{
					throw new System.Exception("No user registered yet");
				}

				return ReferenceManager.Instance.GetResource<CoflnetUser>(userId);
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
			return ReferenceManager.Instance.TryGetResource<CoflnetUser>(userId, out user);
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
			var request = new RegisterUserRequest();
			request.clientId = ConfigController.ApplicationSettings.id;
			request.privacySettings = privacyOptions;

			


			var user = new CoflnetUser(ConfigController.ApplicationSettings.id);
			user.AssignId(ClientCoreInstance.ReferenceManager);

			// assign temporary local id to request so we can have multiple requests at the same time 
			// and are able to continue with setup locally and update it with the server asyncronously
			var requestData = MessageData.CreateMessageData<RegisterUser, RegisterUserRequest>(request.clientId,request);
			requestData.sId = user.Id;

			UnityEngine.Debug.Log($"creating user {user.Id} {CoflnetCore.Instance.GetType().Name} in {ClientCoreInstance.ReferenceManager.RelativeStorageFolder}");

			user.SendCommand(requestData);
			//CoflnetCore.Instance.SendCommand(requestData);
		}

		/// <summary>
		/// Changes the current user.
		/// </summary>
		/// <param name="id">Identifier.</param>
		public void ChangeCurrentUser(SourceReference id)
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
		public SourceReference CurrentUserId
		{
			get
			{
				return ConfigController.ActiveUserId;
			}
			set
			{
				ConfigController.ActiveUserId = value;
			}
		}
	}

}

