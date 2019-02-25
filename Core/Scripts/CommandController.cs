using System.Collections;
using System.Collections.Generic;
using System;
using Coflnet.Server;
using MessagePack;
using Coflnet;

namespace Coflnet
{

	/// <summary>
	/// Handles Command (function) to slug (string) mapping.
	/// Needed in order to serialize commands.
	/// </summary>
	public class CommandController
	{
		/// <summary>
		/// Contains all available commands that have been registered
		/// </summary>
		protected Dictionary<string, Command> commands;
		protected Dictionary<Type, string> commandIdentifiers;
		/// <summary>
		/// This commandController will be searched for a command 
		/// if a command was not found in the current one
		/// </summary>
		protected CommandController backfall;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Coflnet.CommandController"/> class with no commands.
		/// </summary>
		public CommandController()
		{
			this.commands = new Dictionary<string, Command>();
		}

		public CommandController(CommandController backfall) : this()
		{
			this.backfall = backfall;
		}



		/// <summary>
		/// Registers a command
		/// </summary>
		/// <param name="command">Command.</param>
		[Obsolete("Commands should now derive of Command")]
		public void RegisterCommand(CoflnetCommand command)
		{
			if (commands.ContainsKey(command.GetSlug()))
			{
				throw new Exception("Command with the slug " + command.GetSlug() + " already exists, it has to be unique");
			}
			RegisterCommand(new LegacyCommand(command));
			//  if (!commandIdentifiers.ContainsKey(command.GetCommand().))
			//  {
			//      // commands are unique
			//      commandIdentifiers.Add(command.GetCommand(), command.GetSlug());
			//  }
		}

		/// <summary>
		/// Registers a command class 
		/// </summary>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public void RegisterCommand<T>() where T : Command
		{
			RegisterCommand((T)Activator.CreateInstance(typeof(T)));
		}

		/// <summary>
		/// Registers a command on the controller
		/// </summary>
		/// <param name="command">The Command wich to register</param>
		public void RegisterCommand(Command command)
		{
			if (commands.ContainsKey(command.GetSlug()))
			{
				throw new CoflnetException("command_already_registered", $"The Command {command.GetSlug()} is already registered on this controler. Use overwriteCommand if you want to change it.");
			}
			commands.Add(command.GetSlug(), command);
		}

		/// <summary>
		/// Overwrites the command.
		/// </summary>
		/// <param name="slug">Slug.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public void OverwriteCommand<T>(string slug) where T : Command
		{
			OverwriteCommand(slug, (T)Activator.CreateInstance(typeof(T)));
		}

		/// <summary>
		/// Overwrites or ads commands.
		/// </summary>
		/// <param name="slug">Slug.</param>
		/// <param name="command">Command.</param>
		public void OverwriteCommand(string slug, Command command)
		{
			commands[slug] = command;
		}


		/// <summary>
		/// Gets the command mapping for a command
		/// </summary>
		/// <returns>The command slug.</returns>
		/// <param name="command">The command to get the mapping for.</param>
		/// <param name="requireEncryption">If set to <c>true</c> command is checked if it is encrypted.</param>
		public string GetCommandMapping(Command command, bool requireEncryption = false)
		{
			return command.GetSlug();
		}



		/// <summary>
		/// Executes the command if permissions match
		/// </summary>
		/// <param name="command">Command.</param>
		/// <param name="data">Data.</param>
		/// <param name="target">Target.</param>
		public void ExecuteCommand(Command command, MessageData data, Referenceable target = null)
		{
			// test Permissions
			var settings = command.GetSettings();
			if (settings.Permissions != null)
			{
				foreach (var item in command.GetSettings().Permissions)
				{
					if (!item.CheckPermission(data, target))
					{
						UnityEngine.Debug.Log(MessagePackSerializer.ToJson(data));
						UnityEngine.Debug.Log(MessagePackSerializer.ToJson<CoflnetUser>(target as CoflnetUser));
						UnityEngine.Debug.Log("concludes to : " + item.CheckPermission(data, target));
						throw new CoflnetException("permission_not_met", $"The permission {item.GetSlug()} required for executing this command wasn't met");
					}
				}
			}

			command.Execute(data);
		}

		/// <summary>
		/// Executes a command.
		/// </summary>
		/// <param name="data">Decoded object sent from the server</param>
		public void ExecuteCommand(MessageData data, Referenceable target = null)
		{
			var command = GetCommand(data.t);
			ExecuteCommand(command, data, target);
		}

		/// <summary>
		/// Gets the a command.
		/// </summary>
		/// <returns>The command mapped to the slug.</returns>
		/// <param name="slug">The Slug to search for.</param>
		public Command GetCommand(string slug)
		{
			if (!commands.ContainsKey(slug))
			{
				// was this the last command controller or can we check another?
				if (backfall != null)
				{
					return backfall.GetCommand(slug);
				}

				throw new CommandUnknownException(slug);
			}
			return commands[slug];
		}

		/// <summary>
		/// Executes the command in current thread.
		/// Decrypts command if end-to-end encrypted
		/// </summary>
		/// <param name="command">The command object to execute</param>
		/// <param name="data">The data from the server which to execute the command with</param>
		public static void ExecuteCommandInCurrentThread(Command command, MessageData data)
		{
			if (command.Settings.Encrypted)
				EncryptionController.instance.ReceiveEncryptedCommand(command, data);
			else
				command.Execute(data);
		}

	}

	public class CommandUnknownException : CoflnetException
	{
		public CommandUnknownException(string slug, long msgId = -1) : base("unknown_command", $"The command `{slug}` is unknown", null, 404, null, msgId)
		{
		}
	}

	/// <summary>
	/// Insuficient permission exception.
	/// Thrown when at least one Permission required is not fullfilled
	/// </summary>
	public class InsuficientPermissionException : CoflnetException
	{
		public InsuficientPermissionException(long msgId = -1, string message = "You are currently not allowed to execute this command. Upgrade your connection with authentication and try again.", string userMessage = "No permission", string info = null) : base("insuficient_permission", message, userMessage, 403, null, msgId)
		{
		}
	}

	public interface IRegisterCommands
	{
		/// <summary>
		/// Should call controller.RegisterCommand
		/// </summary>
		void RegisterCommands(CommandController controller);
	}



	public abstract class Command
	{
		private CommandSettings _settings;

		/// <summary>
		/// Gets the command settings.
		/// </summary>
		/// <value>The settings.</value>
		public CommandSettings Settings
		{
			get
			{
				if (_settings == null)
					_settings = GetSettings();
				return _settings;
			}
		}

		/// <summary>
		/// Invokes the actual code to do some work
		/// </summary>
		/// <param name="data">Data.</param>
		public abstract void Execute(MessageData data);
		/// <summary>
		/// Should return an unique identifier for this command.
		/// Usually the namespace + the class name if not too long.
		/// try to stay under 16 characters
		/// </summary>
		/// <returns>The slug.</returns>
		public abstract string GetSlug();
		/// <summary>
		/// Gets the command settings for this command.
		/// Will generate a new Settings Object. 
		/// Use the attribute Settings to get the caches settings.
		/// </summary>
		/// <returns>The settings.</returns>
		public abstract CommandSettings GetSettings();

		public void SendTo(SourceReference rId, object data, string type)
		{

		}

		public void SendTo(SourceReference rId, byte[] data, string type)
		{

		}

		/// <summary>
		/// Sends a command to the server
		/// </summary>
		/// <param name="data">Data.</param>
		/// <param name="type">Type.</param>
		public void SendToServer(byte[] data, string type)
		{
			//ServerController.Instance.SendCommandToServer(new MessageData(type,data), server);
		}

		/// <summary>
		/// Sends a command to a specific server
		/// </summary>
		/// <param name="serverId">Server identifier.</param>
		/// <param name="data">Data.</param>
		/// <param name="type">Type.</param>
		public void SendToServer(long serverId, byte[] data, string type)
		{
			ServerController.Instance.SendCommandToServer(new MessageData(type, data), serverId);
		}

		/// <summary>
		/// Sends a command to another user
		/// </summary>
		/// <param name="uId">U identifier.</param>
		/// <param name="data">Data.</param>
		/// <param name="type">Type.</param>
		public void SendToUser(string uId, byte[] data, string type)
		{
			//UserController.instance.SendToUser(data);
		}


		/// <summary>
		/// Response to some previous command.
		/// </summary>
		[MessagePackObject]
		public class Response
		{
			[Key(0)]
			public long Id;
			[Key(1)]
			public byte[] data;

			public Response(long id, byte[] data)
			{
				Id = id;
				this.data = data;
			}
		}

		/// <summary>
		/// Custom Settings for command
		/// </summary>
		public class CommandSettings
		{
			/// <summary>
			/// Wherether this command can be run in multiple threads at the same time
			/// or if that would create race conditions.
			/// </summary>
			public bool ThreadSave
			{
				get;
				private set;
			}

			/// <summary>
			/// Gets or sets a value indicating whether the target resource will be changed by this command.
			/// </summary>
			/// <value><c>true</c> if command updates the resource and has to be distributed; otherwise, <c>false</c>.</value>
			public bool IsChaning
			{
				get;
				protected set;
			}

			/// <summary>
			/// Wherether or not this command is end-to-end encrypted
			/// </summary>
			public bool Encrypted
			{
				get;
				private set;
			}

			/// <summary>
			/// Gets or sets a value indicating whether this <see cref="T:Coflnet.Command.CommandSettings"/> should be executed locally (and on eg other devices from the same user) if resource is present locally.
			/// Eg username update -> should also update local username instantly.
			/// </summary>
			/// <value><c>true</c> if command should be executed locally before sending or not; otherwise, <c>false</c>.</value>
			public bool LocalPropagation
			{
				get;
				protected set;
			}


			/// <summary>
			/// Gets the permissions needed for this command
			/// </summary>
			/// <value>The permissions which need to execute as true for this command to be executed.</value>
			public Permission[] Permissions
			{
				get;
				private set;
			}

			/// <summary>
			/// Initializes a new instance of the <see cref="T:Coflnet.Command.CommandSettings"/> class.
			/// </summary>
			/// <param name="threadSave">Set to <c>true</c> if command can be run in multiple threads at the same time.</param>
			/// <param name="encrypted">Set to <c>true</c> if command is end-to-end encrypted and should be decrypted prior to execution.</param>
			/// <param name="localPropagation">Set to <c>true</c>if command should also be executed locally if resource is present. Should be true if update to the ui is required.</param>
			public CommandSettings(bool threadSave = false, bool encrypted = false, bool localPropagation = false)
			{
				ThreadSave = threadSave;
				Encrypted = encrypted;
				LocalPropagation = localPropagation;
			}

			/// <summary>
			/// Initializes a new instance of the <see cref="T:Coflnet.Command.CommandSettings"/> class.
			/// </summary>
			/// <param name="threadSave">If set to <c>true</c> thread save.</param>
			/// <param name="encrypted">If set to <c>true</c> encrypted.</param>
			/// <param name="localPropagation">If set to <c>true</c> local propagation.</param>
			/// <param name="permissions">Permissions.</param>
			public CommandSettings(bool threadSave, bool encrypted, bool localPropagation, params Permission[] permissions)
			{
				ThreadSave = threadSave;
				Encrypted = encrypted;
				LocalPropagation = localPropagation;
				Permissions = permissions;
			}


			public CommandSettings(bool threadSave, bool isUpdating, bool encrypted, bool localPropagation, params Permission[] permissions)
			{
				ThreadSave = threadSave;
				IsChaning = isUpdating;
				Encrypted = encrypted;
				LocalPropagation = localPropagation;
				Permissions = permissions;
			}


			public CommandSettings(params Permission[] permissions)
			{
				Permissions = permissions;
			}

		}
	}


	public interface IContainsSlug
	{
		string GetSlug();
	}


	public class CommandExistsOnServer : NotImplementedException
	{
		public CommandExistsOnServer(string message) : base(message)
		{
		}

		public CommandExistsOnServer() : base("The command doesn't exist on the client only on the server")
		{
		}
	}


	public abstract class ServerCommand : Command
	{
		public override CommandSettings GetSettings()
		{
			return GetServerSettings();
		}

		public abstract ServerCommandSettings GetServerSettings();




		public new void SendToServer(long serverId, byte[] data, string type)
		{
			ServerController.Instance.SendCommandToServer(new MessageData(type, data), serverId);
		}



		/// <summary>
		/// Sends some data back to the user calling the command
		/// </summary>
		/// <param name="original">Original message data pointer.</param>
		/// <param name="data">Data.</param>
		public void SendBack(MessageData original, byte[] data)
		{
			byte[] withId = MessagePackSerializer.Serialize<Response>(new Response(original.mId, data));
			original.sId.ExecuteForResource(new MessageData("response", withId));
			//SendToUser(original.sId, withId, "response");
		}

		/// <summary>
		/// Sends an integer back to the user calling the command
		/// </summary>
		/// <param name="original">Original message data.</param>
		/// <param name="data">Data.</param>
		public void SendBack(MessageData original, int data)
		{
			SendBack(original, BitConverter.GetBytes(data));
		}

		public class ServerCommandSettings : CommandSettings
		{
			private short _cost;

			/// <summary>
			/// How much this command costs, is at least one.
			/// </summary>
			/// <value>The cost.</value>
			public short Cost
			{
				get
				{
					return _cost;
				}
				private set
				{
					_cost = value < (short)0 ? (short)1 : value;
				}
			}



			/// <summary>
			/// Initializes a new instance of the <see cref="T:Coflnet.ServerCommand.ServerCommandSettings"/> class.
			/// </summary>
			/// <param name="threadSave">If set to <c>true</c> the command can be executed simotaniously in multiple threads.</param>
			/// <param name="encrypted">If set to <c>true</c> the command is end to end encrypted.</param>
			/// <param name="cost">How many usages are used by this command.</param>
			/// <param name="permissions">Permissions needed to execute the command.</param>
			public ServerCommandSettings(bool threadSave = false, bool encrypted = false, short cost = 1, params Permission[] permissions) : base(threadSave, encrypted, false, permissions)
			{
				Cost = cost;
			}


			/// <summary>
			/// Initializes a new instance of the <see cref="T:Coflnet.ServerCommand.ServerCommandSettings"/> class.
			/// </summary>
			/// <param name="threadSave">If set to <c>true</c> the command is thread save.</param>
			/// <param name="cost">Cost of the command.</param>
			/// <param name="permissions">Permissions.</param>
			public ServerCommandSettings(bool threadSave = false, short cost = 1, params Permission[] permissions) : base(threadSave, false, false, permissions)
			{
				Cost = cost;
			}

			/// <summary>
			/// Initializes a new instance of the <see cref="T:Coflnet.ServerCommand.ServerCommandSettings"/> class.
			/// </summary>
			/// <param name="permissions">Permissions.</param>
			public ServerCommandSettings(params Permission[] permissions) : this(false, false, 1, permissions) { }
		}
	}

	/// <summary>
	/// A serverCommand with some default settings to avoid haveing to redevine them every time
	/// </summary>
	public abstract class DefaultServerCommand : ServerCommand
	{
		public override ServerCommand.ServerCommandSettings GetServerSettings()
		{
			return new ServerCommandSettings();
		}
	}


	public abstract class AuthenticatedServerCommand : ServerCommand
	{
		public override ServerCommand.ServerCommandSettings GetServerSettings()
		{
			return new ServerCommandSettings(new OrPermission(IsUserPermission.Instance, IsAuthenticatedPermission.Instance));
		}
	}




	public class RegisterDevice : ServerCommand
	{
		public override void Execute(MessageData data)
		{
			Device device = new Device();
			device.Users.Add(new Reference<CoflnetUser>(data.User.PublicId));
			SendBack(data, device.PublicKey);
		}

		public override ServerCommandSettings GetServerSettings()
		{
			// Only users can register devices
			return new ServerCommandSettings(new IsUserPermission());
		}

		public override string GetSlug()
		{
			return "registerDevice";
		}
	}



	/// <summary>
	/// Represents a command that invokes a external rest api
	/// </summary>
	public class RestCommand : ServerCommand
	{
		RestSharp.RestClient client = new RestSharp.RestClient();
		RestCommandRegisterRequest registerRequest;

		public override void Execute(MessageData data)
		{
			RestSharp.RestRequest request;
			request = new RestSharp.RestRequest(registerRequest.route, registerRequest.method);
			foreach (var item in registerRequest.defaultHeaders)
			{
				request.AddHeader(item.Key, item.Value);
			}
			var remoteResponse = client.Execute(request);

			//MessageData response = new MessageData();
			//response.m = remoteResponse.RawBytes;

			// assing the result
			SendBack(data, remoteResponse.RawBytes);
		}

		public override string GetSlug()
		{
			throw new NotImplementedException();
		}

		public override CommandSettings GetSettings()
		{
			throw new NotImplementedException();
		}

		public override ServerCommandSettings GetServerSettings()
		{
			throw new NotImplementedException();
		}

		public RestCommand(RestCommandRegisterRequest request)
		{
			registerRequest = request;
			client.BaseHost = request.BaseUrl;
		}
	}

	public class RestCommandRegisterRequest
	{
		public string BaseUrl;
		public string route;
		public Dictionary<string, string> defaultHeaders;
		public RestSharp.Method method;
	}


	public class StartRecover : ServerCommand
	{
		public override void Execute(MessageData data)
		{

			throw new NotImplementedException();
		}

		public override ServerCommandSettings GetServerSettings()
		{
			return new ServerCommandSettings(false, false, 1, new IsMasterPermission());
		}

		public override string GetSlug()
		{
			throw new NotImplementedException();
		}
	}

	/// <summary>
	/// Command on another Server.
	/// Used to proxy commands to some third party server
	/// or oauth client backend.
	/// Needed for Dynamically adding commands
	/// </summary>
	public class ExternalCommand : ServerCommand
	{
		/// <summary>
		/// Commands to be executed and whose result should be sent in the body of this command
		/// </summary>
		protected Dictionary<Command, string> commands = new Dictionary<Command, string>();
		protected string slug;
		protected ServerCommandSettings settings;

		public override void Execute(MessageData data)
		{
			//Dictionary<string, string> body = new Dictionary<string, string>();
			foreach (var item in commands)
			{
				string value = item.Value.Replace("{sId}", data.sId.ToString());
				MessageData newData = new MessageData(data.sId, 0, value, null);
				item.Key.Execute(newData);
			}

		}

		public override string GetSlug()
		{
			return slug;
		}

		public override ServerCommandSettings GetServerSettings()
		{
			return new ServerCommandSettings(false, false);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Coflnet.ExternalCommand"/> class.
		/// </summary>
		/// <param name="request">CommandRegisterRequest instance.</param>
		/// <param name="controller">CommandController which to take the commands from.</param>
		public ExternalCommand(CommandRegisterRequest request, CommandController controller)
		{
			foreach (var item in request.bodyCommands)
			{
				commands.Add(controller.GetCommand(item.Key), item.Value);
			}
			settings = request.settings;
			slug = request.Slug;
		}
	}


	public class CommandRegisterRequest
	{
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

	public class LegacyCommand : Command
	{
		CoflnetCommand oldCommand;

		public override void Execute(MessageData data)
		{
			oldCommand.GetCommand().Invoke(data);
		}

		public override string GetSlug()
		{
			return oldCommand.GetSlug();
		}

		public override CommandSettings GetSettings()
		{
			return new CommandSettings(oldCommand.IsThreadAble(), oldCommand.IsEncrypted());
		}

		public LegacyCommand(CoflnetCommand legacyCommand)
		{
			this.oldCommand = legacyCommand;
		}

		public LegacyCommand(string slug, CoflnetCommand.Command command, bool threadable = false, bool encrypted = false)
		{
			this.oldCommand = new CoflnetCommand(slug, command, threadable, encrypted);
		}
	}



	[Obsolete("Yous should now derive from the abstract class 'Command'")]
	public class CoflnetCommand
	{
		public delegate void Command(MessageData messageData);
		private string slug;
		private Command command;
		private bool threadAble;
		private bool encrypted;



		public CoflnetCommand(string slug, Command command, bool threadAble, bool encrypted)
		{
			this.slug = slug;
			this.command = command;
			this.threadAble = threadAble;
			this.encrypted = encrypted;
		}



		/// <summary>
		/// Is this command encrypted while in trasmit?
		/// </summary>
		public bool IsEncrypted()
		{
			return encrypted;
		}

		/// <summary>
		/// Is this command able to be executed in another thread
		/// </summary>
		public bool IsThreadAble()
		{
			return threadAble;
		}

		/// <summary>
		/// Gets the command actual function behind this command.
		/// </summary>
		/// <returns>The command.</returns>
		public Command GetCommand()
		{
			return command;
		}

		public string GetSlug()
		{
			return slug;
		}
	}

}