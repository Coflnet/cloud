using System.Collections.Generic;
using RestSharp;
using System;
using Coflnet;

/// <summary>
/// Some objects may not be in memory and have to be loaded from the DB
/// </summary>
public class DataBaseAdapter
{


}


// represents a not in memory List
public class DBList<T> where T : IDatabaseable
{
	protected static string sprunjeEndpoint = "";

	public T Get(string id)
	{
		RestRequest request = new RestRequest(sprunjeEndpoint, RestSharp.Method.GET);
		request.AddHeader("Authorization", "blabla");
		var client = new RestClient("http://example.com");

		var result = client.Execute(request);
		return MessagePack.MessagePackSerializer.Deserialize<T>(result.RawBytes);
	}

	public T GetT(DateTime start, DateTime end)
	{
		RestRequest request = new RestRequest(sprunjeEndpoint, RestSharp.Method.GET);
		request.AddHeader("Authorization", "blabla");
		var client = new RestClient("http://example.com");

		var result = client.Execute(request);
		return MessagePack.MessagePackSerializer.Deserialize<T>(result.RawBytes);
	}
}

/// <summary>
/// Represents messages for a specific user
/// </summary>
public class MessageList : Dictionary<long, MessageData>
{
	public CoflnetUser user;
	private static RestClient messageEndpoint = new RestClient("/localhost/api/v1/messages");

	public void Safe()
	{
		// TODO: send it to the DB
	}

	public MessageData Get(long id)
	{
		// search the cached first
		if (this.ContainsKey(id))
		{
			return this[id];
		}

		// wasn't found, search the DB over the rest API next      
		RestRequest request = new RestRequest(RestSharp.Method.GET);
		request.AddHeader("Authorization", user.AuthToken);
		request.AddHeader("messageId", id.ToString());
		var result = messageEndpoint.Execute(request);
		return MessagePack.MessagePackSerializer.Deserialize<MessageData>(result.RawBytes);
	}

	public void Add(MessageData data)
	{
		this.Add(data.mId, data);
	}
}


public interface IDatabaseable
{

}