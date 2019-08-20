using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Coflnet;
using Coflnet.Core;
using MessagePack;

namespace Coflnet
{
	


	/// <summary>
	/// The same as a normal list but capeable of updating itself
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class RemoteList<T> : RemoteObject<List<T>>,IEnumerable<T>,IEnumerable,IList<T>,ICollection<T>
	{
		public RemoteList(string element,Referenceable parent)
		{
			SetDetails(element,parent);
		}


		/// <summary>
		/// Sets the details needed by the list to operate
		/// </summary>
		/// <param name="nameOfAttribute"></param>
		/// <param name="parent"></param>
		/// <param name="update"></param>
		public override void SetDetails(string nameOfAttribute, Referenceable parent, bool update = false)
		{
			base.SetDetails(nameOfAttribute.Trim('s'),parent,update);
		}


		public void RemoteRemoveAt(int index)
		{
			Send("RemoveAt",index);
		}

		public void RemoteRemoveRange(int index,int count)
		{
			Send("RemoveRange",ValueTuple.Create<int,int>(index,count));
		}


		public void RemoteAddRange(T[] elements)
		{
			Send<T[]>("AddRange",elements);
		}


		public void RemoteClear()
		{
			Send("Clear",0);
		}


		public void RemoteInsertRange(int index, T[] items)
		{
			Send("InsertRange",new KeyValuePair<int,T[]>(index,items));
		}

		public int IndexOf(T item)
		{
			return Value.IndexOf(item);
		}

		public void Insert(int index, T item)
		{
			Send("Insert",new KeyValuePair<int,T>(index,item));
		}


		/// <summary>
		/// Removes the item at the specified index
		/// </summary>
		/// <param name="index">Index wich to remove</param>
        public void RemoveAt(int index)
        {
            Send("RemoveAt",index);
        }

		/// <summary>
		/// Add an item to the list
		/// </summary>
		/// <param name="item">The item to add</param>
        public void Add(T item)
        {
            Send("Add",item);
        }

        public void Clear()
        {
            Send("Clear");
        }

        public bool Contains(T item)
        {
            return Value.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Value.CopyTo(array,arrayIndex);
        }

		/// <summary>
		/// Removes the specified item from te list, Always returns true
		/// </summary>
		/// <param name="item">The item to remove</param>
		/// <returns>true</returns>
        public bool Remove(T item)
        {
            Send("Remove",item);

			return true;
        }

		public IEnumerator GetEnumerator()
        {
            return Value.GetEnumerator();
        }


        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return Value.GetEnumerator();
        }

        public RemoteList()
		{
			
		}


		public int Count => Value.Count;

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public T this[int index] 
		{ 
			get  {
				return Value[index];
			} 
			set {
				Send("setElement",new KeyValuePair<int,T>(index,value));
			}  
		}

		/// <summary>
		/// Adds Add,AddRange,Remove,RemoveAt,RemoveRange,Insert,InsertRange and Clear commands for any list on any referenceables
		/// </summary>
		/// <param name="controller">The controller of the <see cref="Referenceable"/> to add the commands to</param>
		/// <param name="prefix">The prefix for the commands, usually the attribute name</param>
		///  <param name="ListGetter">A function that returns the actual list to operate on, given the target <see cref="Referenceable"/> </param>
		/// <param name="Converter">A function that converts the data given in the <see cref="MessageData"/> content to the List type</param>
		/// <typeparam name="R">The target  <see cref="Referenceable"/> type</typeparam>
		/// <typeparam name="T">The type of the list elements</typeparam>
		public static void AddCommands(CommandController controller,string prefix, Func<MessageData,List<T>> ListGetter, Func<MessageData,T> Converter = null)
		{
			// commands have no plural s
			prefix = prefix.Trim('s');

			controller.RegisterCommand(new RemoteListAddCommand<T>(prefix,ListGetter,Converter));
			controller.RegisterCommand(new RemoteListRemoveCommand<T>(prefix,ListGetter,Converter));
			controller.RegisterCommand(new RemoteListRemoveAtCommand<T>(prefix,ListGetter,Converter));
			controller.RegisterCommand(new RemoteListClearCommand<T>(prefix,ListGetter,Converter));
			controller.RegisterCommand(new RemoteListAddRangeCommand<T>(prefix,ListGetter,null));
			controller.RegisterCommand(new RemoteListRemoveRangeCommand<T>(prefix,ListGetter,Converter));
			controller.RegisterCommand(new RemoteListInsertRangeCommand<T>(prefix,ListGetter,Converter));
			controller.RegisterCommand(new RemoteListInsertCommand<T>(prefix,ListGetter,Converter));
		}
    }


	
}


[DataContract]
public class ListResource<T> : Referenceable,IListResource<T>
{
	private static CommandController Commands = new CommandController();

	[DataMember]
	public List<T> Elements {get;set;}


    public override CommandController GetCommandController()
    {
        return Commands;
    }

	public ListResource(SourceReference owner) : base(owner) {
		Elements = new List<T>();
	}

	public ListResource(){}


	static ListResource()
	{
		Commands.RegisterCommand<ListAddCommand<T>>();
	}


	public void Add(T element)
	{
		CoflnetCore.Instance.SendCommand<RemoteListAddCommand<T>,T>(Access.Owner,element);
	}

	public void Remove(T element)
	{
		var data = new MessageData(Access.Owner);
		data.SerializeAndSet(MessageData.CreateMessageData<RemoteListRemoveCommand<T>,T>(this.Id,element));
		
		CoflnetCore.Instance.SendCommand(data);
	}

	public void RemoveAt(int index)
	{

	}

	public void Clear()
	{

	}
}




public class ListAddCommand<T> : Command
{
	/// <summary>
	/// Execute the command logic with specified data.
	/// </summary>
	/// <param name="data"><see cref="MessageData"/> passed over the network .</param>
	public override void Execute(MessageData data)
	{
		var obj = data.GetTargetAs<Referenceable>();
		var list = obj as IListResource<T>;
		list.Elements.Add(data.GetAs<T>());
	}

	/// <summary>
	/// Special settings and Permissions for this <see cref="Command"/>
	/// </summary>
	/// <returns>The settings.</returns>
	public override CommandSettings GetSettings()
	{
		return new CommandSettings(IsOwnerPermission.Instance );
	}
	/// <summary>
	/// The globally unique slug (short human readable id) for this command.
	/// </summary>
	/// <returns>The slug .</returns>
	public override string Slug => "Add";
}



public interface IListResource<T>
{
	List<T> Elements {get;set;}
}



public class RemoteListCommand : Command
{
	/// <summary>
	/// Execute the command logic with specified data.
	/// </summary>
	/// <param name="data"><see cref="MessageData"/> passed over the network .</param>
	public override void Execute(MessageData data)
	{
		//data.GetTargetAs<Referenceable>().ExecuteCommand()
		var innerData= data.GetAs<MessageData>();
		// set the owning resource as sender
		innerData.sId = data.rId;
		// execute the command
		data.CoreInstance.ReferenceManager.ExecuteForReference(innerData);
	}

	/// <summary>
	/// Special settings and Permissions for this <see cref="Command"/>
	/// </summary>
	/// <returns>The settings.</returns>
	public override CommandSettings GetSettings()
	{
		return new CommandSettings(true,true,false,false,WritePermission.Instance);
	}
	/// <summary>
	/// The globally unique slug (short human readable id) for this command.
	/// </summary>
	/// <returns>The slug .</returns>
	public override string Slug => "ListUpdate";
}



public abstract class RemoteListCommandBase<T> : Command
{
	protected Func<MessageData,List<T>> ListGetter;
	protected Func<MessageData,T> Converter;

	public RemoteListCommandBase(string Slug,Func<MessageData,List<T>> ListGetter,Func<MessageData,T> Converter)
	{
		this.ListGetter =ListGetter;
		this.Slug = Slug;
		this.Converter = Converter;
	}

	protected T GetElement(MessageData data)
	{
		if(Converter == null)
		{
			return data.GetAs<T>();
		}
		return Converter.Invoke(data);
	}

	/// <summary>
	/// Special settings and Permissions for this <see cref="Command"/>
	/// </summary>
	/// <returns>The settings.</returns>
	public override CommandSettings GetSettings()
	{
		return new CommandSettings(true,true,false,false,WritePermission.Instance);
	}

	/// <summary>
	/// The globally unique slug (short human readable id) for this command.
	/// </summary>
	/// <returns>The slug .</returns>
	public override string Slug {
		get;
	}
}


public class RemoteListAddCommand<T> : RemoteListCommandBase<T>
{
    public RemoteListAddCommand(string Slug, Func<MessageData, List<T>> ListGetter, Func<MessageData, T> Converter) 
	: base("Add" + Slug, ListGetter, Converter)
    {
    }

    /// <summary>
    /// Execute the command logic with specified data.
    /// </summary>
    /// <param name="data"><see cref="MessageData"/> passed over the network .</param>
    public override void Execute(MessageData data)
	{
		ListGetter.Invoke(data).Add(GetElement(data));
	}
}



public class RemoteListRemoveCommand<T> : RemoteListCommandBase<T>
{
    public RemoteListRemoveCommand(string Slug, Func<MessageData, List<T>> ListGetter, Func<MessageData, T> Converter) 
	: base("Remove" + Slug, ListGetter, Converter)
    {
    }

    /// <summary>
    /// Execute the command logic with specified data.
    /// </summary>
    /// <param name="data"><see cref="MessageData"/> passed over the network .</param>
    public override void Execute(MessageData data)
	{
		ListGetter.Invoke(data).Remove(GetElement(data));
	}
}


public class RemoteListClearCommand<T> : RemoteListCommandBase<T>
{
    public RemoteListClearCommand(string Slug, Func<MessageData, List<T>> ListGetter, Func<MessageData, T> Converter) 
	: base("Clear" + Slug, ListGetter, Converter)
    {
    }

    /// <summary>
    /// Execute the command logic with specified data.
    /// </summary>
    /// <param name="data"><see cref="MessageData"/> passed over the network .</param>
    public override void Execute(MessageData data)
	{
		ListGetter.Invoke(data).Clear();
	}
}

public class RemoteListAddRangeCommand<T> : RemoteListCommandBase<T>
{

	protected Func<MessageData,T[]> ConverterArray;

    public RemoteListAddRangeCommand(string Slug, Func<MessageData, List<T>> ListGetter, Func<MessageData, T[]> Converter) 
	: base("AddRange" + Slug, ListGetter, null)
    {
		ConverterArray = Converter;
    }

    /// <summary>
    /// Execute the command logic with specified data.
    /// </summary>
    /// <param name="data"><see cref="MessageData"/> passed over the network .</param>
    public override void Execute(MessageData data)
	{
		T[] values;
		if(ConverterArray != null)
		{
			values = ConverterArray(data);
		} else 
		{
			values = data.GetAs<T[]>();
		}

		ListGetter.Invoke(data).AddRange(values);
	}
}


public class RemoteListRemoveRangeCommand<T> : RemoteListCommandBase<T>
{

    public RemoteListRemoveRangeCommand(string Slug, Func<MessageData, List<T>> ListGetter, Func<MessageData, T> Converter) 
	: base("RemoveRange" + Slug, ListGetter, Converter)
    {
    }

    /// <summary>
    /// Execute the command logic with specified data.
    /// </summary>
    /// <param name="data"><see cref="MessageData"/> passed over the network .</param>
    public override void Execute(MessageData data)
	{
		var args = data.GetAs<ValueTuple<int,int>>();

		ListGetter.Invoke(data).RemoveRange(args.Item1,args.Item2);
	}
}

public class RemoteListInsertRangeCommand<T> : RemoteListCommandBase<T>
{

    public RemoteListInsertRangeCommand(string Slug, Func<MessageData, List<T>> ListGetter, Func<MessageData, T> Converter) 
	: base("InsertRange" + Slug, ListGetter, Converter)
    {
    }

    /// <summary>
    /// Execute the command logic with specified data.
    /// </summary>
    /// <param name="data"><see cref="MessageData"/> passed over the network .</param>
    public override void Execute(MessageData data)
	{
		var args = data.GetAs<KeyValuePair<int,T[]>>();

		ListGetter.Invoke(data).InsertRange(args.Key,args.Value);
	}
}


public class RemoteListInsertCommand<T> : RemoteListCommandBase<T>
{

    public RemoteListInsertCommand(string Slug, Func<MessageData, List<T>> ListGetter, Func<MessageData, T> Converter) 
	: base("Insert" + Slug, ListGetter, Converter)
    {
    }

    /// <summary>
    /// Execute the command logic with specified data.
    /// </summary>
    /// <param name="data"><see cref="MessageData"/> passed over the network .</param>
    public override void Execute(MessageData data)
	{
		var args = data.GetAs<KeyValuePair<int,T>>();

		ListGetter.Invoke(data).Insert(args.Key,args.Value);
	}
}


public class RemoteListRemoveAtCommand<T> : RemoteListCommandBase<T>
{

    public RemoteListRemoveAtCommand(string Slug, Func<MessageData, List<T>> ListGetter, Func<MessageData, T> Converter) 
	: base("RemoveAt" + Slug, ListGetter, Converter)
    {
    }

    /// <summary>
    /// Execute the command logic with specified data.
    /// </summary>
    /// <param name="data"><see cref="MessageData"/> passed over the network .</param>
    public override void Execute(MessageData data)
	{
		ListGetter.Invoke(data).RemoveAt(data.GetAs<int>());
	}
}

