using System;
using System.Collections;
using System.Collections.Generic;
using Coflnet;
using Coflnet.Server;
using MessagePack;

namespace Coflnet
{

    /// <summary>
    /// Handles Command (function) to slug (string) mapping.
    /// Needed in order to serialize commands.
    /// </summary>
    public class CommandController : ClassBasedDictionary<string,Command>{
		/// <summary>
		/// Contains all available commands that have been registered
		/// </summary>
		protected Dictionary<string, Command> commands 
		{
			get {return Items;}
			set {Items=value;}
		}
		/// <summary>
		/// This commandController will be searched for commands
		/// if a slug was not found in the current one
		/// </summary>
		public CommandController Fallback { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Coflnet.CommandController"/> class with no commands.
		/// </summary>
		public CommandController () {
			this.commands = new Dictionary<string, Command> ();
		}

		public CommandController (CommandController fallback) : this () {
			this.Fallback = fallback;
		}

		/// <summary>
		/// Adds additional <see cref="CommandController"> to search for commands.
		/// Will shift other fallback back.
		/// </summary>
		/// <param name="fallback">Controller to add</param>
		public void AddFallback (CommandController fallback) {
			if(Fallback == fallback)
			{
				// already added
				return;
			}
			if (Fallback != null) {
				fallback.AddFallback (Fallback);
			}
			Fallback = fallback;
		}

		public void RemoveAllCommands () {
			commands?.Clear ();
		}

		/// <summary>
		/// Registers a command
		/// </summary>
		/// <param name="command">Command.</param>
		[Obsolete ("Commands should now derive of Command")]
		public void RegisterCommand (CoflnetCommand command) {
			if (commands.ContainsKey (command.Slug)) {
				throw new Exception ("Command with the slug " + command.Slug + " already exists, it has to be unique");
			}
			RegisterCommand (new LegacyCommand (command));
			//  if (!commandIdentifiers.ContainsKey(command.GetCommand().))
			//  {
			//      // commands are unique
			//      commandIdentifiers.Add(command.GetCommand(), command.Slug);
			//  }
		}

		/// <summary>
		/// Registers a command class 
		/// </summary>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public void RegisterCommand<T> () where T : Command {
			RegisterCommand ((T) Activator.CreateInstance (typeof (T)));
		}

		/// <summary>
		/// Registers a command on the controller
		/// </summary>
		/// <param name="command">The Command wich to register</param>
		public void RegisterCommand (Command command) {
			if (commands.ContainsKey (command.Slug)) {
				throw new CoflnetException ("command_already_registered", $"The Command {command.Slug} is already registered on this controler. Use overwriteCommand if you want to change it.");
			}
			commands.Add (command.Slug, command);
		}

		/// <summary>
		/// Overwrites the command.
		/// </summary>
		/// <param name="slug">Slug.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public void OverwriteCommand<T> () where T : Command {
			var command = (T) Activator.CreateInstance (typeof (T));
			OverwriteCommand (command.Slug, command);
		}

		/// <summary>
		/// Overwrites the command.
		/// </summary>
		/// <param name="slug">Slug.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public void OverwriteCommand<T> (string slug) where T : Command {
			OverwriteCommand (slug, (T) Activator.CreateInstance (typeof (T)));
		}

		/// <summary>
		/// Overwrites or ads commands.
		/// </summary>
		/// <param name="slug">Slug.</param>
		/// <param name="command">Command.</param>
		public void OverwriteCommand (string slug, Command command) {
			commands[slug] = command;
		}

		/// <summary>
		/// Gets the command mapping for a command
		/// </summary>
		/// <returns>The command slug.</returns>
		/// <param name="command">The command to get the mapping for.</param>
		/// <param name="requireEncryption">If set to <c>true</c> command is checked if it is encrypted.</param>
		public string GetCommandMapping (Command command, bool requireEncryption = false) {
			return command.Slug;
		}

		/// <summary>
		/// Executes the command if permissions match
		/// </summary>
		/// <param name="command">Command.</param>
		/// <param name="data">Data.</param>
		/// <param name="target">Target.</param>
		public void ExecuteCommand (Command command, CommandData data, Entity target = null) {
			// test Permissions
			if (command.Settings.Permissions != null) {
				ValidatePermissions(command,data,target);
			}

			command.Execute (data);

		}

		private void ValidatePermissions(Command command, CommandData data, Entity target)
		{
			var settings = command.Settings;
			foreach (var permision in settings.Permissions) {
				if (!permision.CheckPermission (data, target)) {

					// the token may allow it
					if(TokenGrantsPermissionForCommand(command,data,target,permision))
					{
						continue;
					}


					throw new PermissionNotMetException(permision.Slug,data.Recipient,data.SenderId,command.Slug,data.MessageId);
				}
			}
		}

		private bool TokenGrantsPermissionForCommand(Command command, CommandData data, Entity target, Permission permission)
		{
			var header = data.Headers;
			if(header==null)
			{
				return false;
			}
			var token = header.Token;

			if(token != null && TokenManager.Instance.IsTokenValid(token,target,data.SenderId)
			&& (target as Core.IHasScopes).AvailableScopes
				.IsAllowedToExecute(token.Scopes,command.Slug))
			{
				// the token allows us to execute this, but is the token issuer authorized?
				var temp = new CommandData(data);
				temp.SenderId = token.Issuer;
				return permission.CheckPermission(data,target);
			}
			return false;
		}


		/// <summary>
		/// Executes a command.
		/// </summary>
		/// <param name="data">Decoded object sent from the server</param>
		public void ExecuteCommand (CommandData data, Entity target = null) {
			if(data == null || data.Type == null)
			{
				throw new CommandUnknownException("null");
			}
			var command = GetCommand (data.Type);
			ExecuteCommand (command, data, target);
		}

		/// <summary>
		/// Gets the a command.
		/// </summary>
		/// <returns>The command mapped to the slug.</returns>
		/// <param name="slug">The Slug to search for.</param>
		public Command GetCommand (string slug) {
			return GetCommand(slug,0);
		}

		protected Command GetCommand(string slug, int iteration)
		{
			if (!commands.ContainsKey (slug)) {
				// was this the last command controller or can we check another?
				if (Fallback != null && iteration < 100) {
					return Fallback.GetCommand (slug,iteration += 1);
				}
				if(iteration >= 100)
					Logger.Error("You have a loop dependency in your CommandController");

				throw new CommandUnknownException (slug);
			}

			return commands[slug];
		}

		/// <summary>
		/// Executes the command in current thread.
		/// Decrypts command if end-to-end encrypted
		/// </summary>
		/// <param name="command">The command object to execute</param>
		/// <param name="data">The data from the server which to execute the command with</param>
		public static void ExecuteCommandInCurrentThread (Command command, CommandData data) {
			if (command.Settings.Encrypted)
				EncryptionController.Instance.ReceiveEncryptedCommand (command, data);
			else
				command.Execute (data);
		}

	}


	public interface IHasSlug<T>
	{
		T Slug {get;}
	}

	public interface IHasSlug : IHasSlug<string>
	{
		
	}

	public interface ISlug {
		string Slug{get;}
	}

	/// <summary>
	/// Is thrown when a command that is only present on the serverside is attempted to be invoked on the client side
	/// </summary>
	public class CommandExistsOnServer : NotImplementedException {
		public CommandExistsOnServer (string message) : base (message) { }

		public CommandExistsOnServer () : base ("The command doesn't exist on the client only on the server") { }
	}

	/// <summary>
	/// A serverCommand with some default settings to avoid haveing to redevine them every time
	/// </summary>
	public abstract class DefaultServerCommand : ServerCommand {
		public override ServerCommand.ServerCommandSettings GetServerSettings () {
			return new ServerCommandSettings ();
		}
	}

	public abstract class AuthenticatedServerCommand : ServerCommand {
		public override ServerCommand.ServerCommandSettings GetServerSettings () {
			return new ServerCommandSettings (new OrPermission (IsUserPermission.Instance, IsAuthenticatedPermission.Instance));
		}
	}



	/// <summary>
	/// Represents a command that invokes a external rest api
	/// </summary>
	public class RestCommand : ServerCommand {
		RestSharp.RestClient client = new RestSharp.RestClient ();
		RestCommandRegisterRequest registerRequest;

		public override void Execute (CommandData data) {
			RestSharp.RestRequest request;
			request = new RestSharp.RestRequest (registerRequest.route, registerRequest.method);
			foreach (var item in registerRequest.defaultHeaders) {
				request.AddHeader (item.Key, item.Value);
			}
			var remoteResponse = client.Execute (request);

			//CommandData response = new CommandData();
			//response.m = remoteResponse.RawBytes;

			// assing the result
			SendBack (data, remoteResponse.RawBytes);
		}

		public override string Slug {
			get {

				throw new NotImplementedException ();
			}
		}

		protected override CommandSettings GetSettings () {
			throw new NotImplementedException ();
		}

		public override ServerCommandSettings GetServerSettings () {
			throw new NotImplementedException ();
		}

		public RestCommand (RestCommandRegisterRequest request) {
			registerRequest = request;
			client.BaseHost = request.BaseUrl;
		}
	}

	public class RestCommandRegisterRequest {
		public string BaseUrl;
		public string route;
		public Dictionary<string, string> defaultHeaders;
		public RestSharp.Method method;
	}

	public class StartRecover : ServerCommand {
		public override void Execute (CommandData data) {

			throw new NotImplementedException ();
		}

		public override ServerCommandSettings GetServerSettings () {
			return new ServerCommandSettings (false, false, 1, new IsMasterPermission ());
		}

		public override string Slug {
			get {

				throw new NotImplementedException ();
			}
		}
	}

	/// <summary>
	/// Command on another Server.
	/// Used to proxy commands to some third party server
	/// or oauth client backend.
	/// Needed for Dynamically adding commands
	/// </summary>
	public class ExternalCommand : ServerCommand {
		/// <summary>
		/// Commands to be executed and whose result should be sent in the body of this command
		/// </summary>
		protected Dictionary<Command, string> commands = new Dictionary<Command, string> ();
		protected string slug;
		protected ServerCommandSettings settings;

		public override void Execute (CommandData data) {
			//Dictionary<string, string> body = new Dictionary<string, string>();
			foreach (var item in commands) {
				string value = item.Value.Replace ("{sId}", data.SenderId.ToString ());
				CommandData newData = new CommandData (data.SenderId, 0, value, null);
				item.Key.Execute (newData);
			}

		}

		public override string Slug {
			get {

				return slug;
			}
		}

		public override ServerCommandSettings GetServerSettings () {
			return new ServerCommandSettings (false, false);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Coflnet.ExternalCommand"/> class.
		/// </summary>
		/// <param name="request">CommandRegisterRequest instance.</param>
		/// <param name="controller">CommandController which to take the commands from.</param>
		public ExternalCommand (CommandRegisterRequest request, CommandController controller) {
			foreach (var item in request.bodyCommands) {
				commands.Add (controller.GetCommand (item.Key), item.Value);
			}
			settings = request.settings;
			slug = request.Slug;
		}
	}

	public class CommandRegisterRequest {
		/// <summary>
		/// Local commands result to add to the body of the command
		/// Example command
		/// getUserValue with body "test"
		/// will result in the dictionary
		/// ["getUserValue" => "whatever value is stored under test"]
		/// being passed to the external server
		/// 
		/// Command bodys can contain placeholders like {userId}, {clientId}, {deviceId} and {serverId}
		/// </summary>
		public Dictionary<string, string> bodyCommands;

		/// <summary>
		/// Unique identifier for this command
		/// </summary>
		public string Slug;

		/// <summary>
		/// Settings for this command
		/// </summary>
		public ServerCommand.ServerCommandSettings settings;
		/// <summary>
		/// Servers which contain this command
		/// </summary>
		public List<long> CapableServers;
	}

}