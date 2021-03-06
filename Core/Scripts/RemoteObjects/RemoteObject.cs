﻿using System;
using System.Collections.Generic;
using Coflnet;
using MessagePack;

namespace Coflnet.Core
{
    /// <summary>
    /// Base class for remote objects.
    /// Simplyfies the generation Process of commands to update the object.
    /// You may only use a <see cref="RemoteObject"/> if the content of it is frequently updated.
    /// </summary>
    /// <typeparam name="T">The type of the <see cref="RemoteObject<T>"/></typeparam>
    [MessagePackObject]
	public class RemoteObject<T>
	{
		/// <summary>
		/// A local copy representation of the Content of the Object.
		/// It may differ from the one on the managing server.
		/// Call <see cref="Commit()"/> if you want to commit the current local value.
		/// Be aware that some operations are commited automatically.
		/// </summary>
		[Key(0)]
		public T Value;

		private string parentAttributeName;

		private Entity parent;

		public RemoteObject()
		{

		}

		public RemoteObject(T value)
		{
			this.Value = value;
		}

		/// <summary>
		/// Clones a <see cref="RemoteObject<T>"/>
		/// </summary>
		/// <param name="remoteObject"></param>
		public RemoteObject(RemoteObject<T> remoteObject)
		{
			this.parent = remoteObject.parent;
			this.parentAttributeName = remoteObject.parentAttributeName;
		}

		public RemoteObject(string nameOfAttribute, Entity parent)
		{
			this.SetDetails(nameOfAttribute,parent);
		}


        /// <summary>
        /// Sets the Details for this remote Object
        /// </summary>
        /// <param name="nameOfAttribute">The value of nameof() of the Attribute this objects coresponds to</param>
        /// <param name="parent">The parent object</param>
        /// <param name="update">If true, a `set` command will be generated and sent for the object</param>
        public virtual void SetDetails(string nameOfAttribute, Entity parent, bool update = false)
		{
			this.parentAttributeName = nameOfAttribute;
			this.parent = parent;

			if(update)
				Set(Value);
		}

		/// <summary>
		/// Distributes the updated value
		/// </summary>
		/// <param name="newValue"></param>
		public void Set(T newValue)
		{
			Send("set",newValue);
		}

		/// <summary>
		/// Requests the current state of this object and replaces the local one with it
		/// </summary>
		/// <param name="onAfterUpdate">Invoked when update completed</param>
		public void GetUpdate(Action onAfterUpdate = null )
		{
			var data = new CommandData(parent.Id);
			data.Type = $"get{parentAttributeName}";
			CoflnetCore.Instance.SendGetCommand(data,m=>{
				Value = m.GetAs<T>();
				onAfterUpdate?.Invoke();
			});
		}

		/// <summary>
		/// Commits the current state of the object. 
		/// Only execute this when you made changes to <see cref="Value"/> directly
		/// </summary>
		public void Commit()
		{
			Set(Value);
		}

		/// <summary>
		/// Send a command that updates something
		/// </summary>
		/// <param name="commandName">The name of the command (will be suffixed with the attribute name</param>
		protected void Send(string commandName)
		{
			Send(commandName,0);
		}


		/// <summary>
		/// Sends a command that updates something
		/// </summary>
		/// <param name="commandName">The name of the command (will be suffixed with the attribute name</param>
		/// <param name="content">The data (if any) that should be transfered</param>
		/// <typeparam name="R">The type of the content</typeparam>
		protected void Send<R>(string commandName,R content)
		{
			var data = new CommandData(parent.Id);
			// don't serialize it if it is the default value
			if( !EqualityComparer<T>.Default.Equals(Value, default(T)))
				data.SerializeAndSet(content);
			data.Type = $"{commandName}{parentAttributeName}";
			CoflnetCore.Instance.SendCommand(data);
		}

		public override bool Equals(object obj)
        {
            var @object = obj as RemoteObject<T>;
            return @object != null &&
                   (EqualityComparer<T>.Default.Equals(Value, @object.Value) || @object.Value == null || Value == null) &&
                   parentAttributeName == @object.parentAttributeName &&
                   EqualityComparer<Entity>.Default.Equals(parent, @object.parent);
        }


        public override int GetHashCode()
        {
            var hashCode = 559626859;
			// do not use the content for the hash
            //hashCode = hashCode * -1521134295 + EqualityComparer<T>.Default.GetHashCode(Value);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(parentAttributeName);
            hashCode = hashCode * -1521134295 + EqualityComparer<EntityId>.Default.GetHashCode(parent.Id);
            return hashCode;
        }

        //public static implicit operator RemoteObject<T>(T value) => new RemoteObject<T>(value);
		/// <summary>
		/// Convert <see cref="RemoteObject<T>"/> to T aka get the Value of it.
		/// </summary>
		/// <param name="remObject">The RemoteObjet to convert</param>
        public static implicit operator T(RemoteObject<T> remObject) => remObject.Value;


		/// <summary>
		/// Adds the get and set commands to the object for a specific attribute.
		/// </summary>
		/// <param name="controller"></param>
		/// <param name="nameOfAttribute"></param>
		/// <param name="getter"></param>
		/// <param name="setter"></param>
		public static void AddCommands(CommandController controller,string nameOfAttribute, Func<CommandData,T> getter,Action<CommandData,T> setter)
		{
			controller.RegisterCommand(new SetCommand(nameOfAttribute,setter));
			controller.RegisterCommand(new GetCommand(nameOfAttribute,getter));
		}


		
		private class Test
		{
			RemoteInt d;

			void hi()
			{
				d+=2;
			}
		}


		public class SetCommand : Command
		{
			protected Action<CommandData,T> valueSetter;

			protected string nameOfAttribute;

			public SetCommand(string nameOfAttribute,Action<CommandData,T> valueSetter )
			{
				this.nameOfAttribute = nameOfAttribute;
				this.valueSetter = valueSetter;
			}

			/// <summary>
			/// Execute the command logic with specified data.
			/// </summary>
			/// <param name="data"><see cref="CommandData"/> passed over the network .</param>
			public override void Execute(CommandData data)
			{
				valueSetter.Invoke(data,data.GetAs<T>());
			}
	
			/// <summary>
			/// Special settings and Permissions for this <see cref="Command"/>
			/// </summary>
			/// <returns>The settings.</returns>
			protected override CommandSettings GetSettings()
			{
				return new CommandSettings(false,true,false,true,WritePermission.Instance);
			}
			/// <summary>
			/// The globally unique slug (short human readable id) for this command.
			/// </summary>
			/// <returns>The slug .</returns>
			public override string Slug => "set"+nameOfAttribute;
		}


		public class GetCommand : ReturnCommand
		{
			protected Func<CommandData,T> valueGetter;

			protected string nameOfAttribute;

			public GetCommand(string nameOfAttribute,Func<CommandData,T> valueGetter )
			{
				this.nameOfAttribute = nameOfAttribute;
				this.valueGetter = valueGetter;
			}

			/// <summary>
			/// Execute the command logic with specified data.
			/// </summary>
			/// <param name="data"><see cref="CommandData"/> passed over the network .</param>
			public override void Execute(CommandData data)
			{
				
			}
	
			/// <summary>
			/// Special settings and Permissions for this <see cref="Command"/>
			/// </summary>
			/// <returns>The settings.</returns>
			protected override CommandSettings GetSettings()
			{
				return new CommandSettings(false,true,false,true,ReadPermission.Instance);
			}

            public override CommandData ExecuteWithReturn(CommandData data)
            {
                return data.SerializeAndSet(valueGetter.Invoke(data));
            }

            /// <summary>
            /// The globally unique slug (short human readable id) for this command.
            /// </summary>
            /// <returns>The slug .</returns>
            public override string Slug => "get"+nameOfAttribute;
		}
		
	}


    /// <summary>
    /// String that can be updated over the network
    /// </summary>
    public class RemoteString : RemoteObject<String>
    {

        public RemoteString(string nameOfAttribute, Entity parent) : base(nameOfAttribute, parent)
        {
			
        }
        public RemoteString()
        {
        }

		
    }


}