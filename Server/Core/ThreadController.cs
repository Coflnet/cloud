using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System;
using System.Collections.Concurrent;
using RestSharp;
using System.Net;
using System.Text;
using Coflnet;
using Coflnet.Server;

/// <summary>
/// Controlls different (worker) threads, 
/// to run more efficient on multicore computers
/// </summary>
public class ThreadController : MonoBehaviour
{
	private static readonly int minWorkerCount = 1;

	private List<Thread> threads = new List<Thread>();
	private List<CoflnetThreadWorker> workers = new List<CoflnetThreadWorker>();
	static RestClient client = new RestClient("https://beta.coflnet.com/api/v1/captcha");
	private uint internalCommandIndex;
	public int sum = 0;
	private static ThreadController _instance;

	public static ThreadController Instance
	{
		get
		{
			if (_instance == null)
				_instance = new ThreadController();
			return _instance;
		}
	}

	public void ExecuteCommand(Command command, MessageData data)
	{
		CommandItem comandWithData = new CommandItem(command, data);

		// find worker
		CoflnetThreadWorker worker = GetWorkerForUser(data.sId);
		// stack commandItem on the worker
		// it will be executed asyncronously
		worker.queuedCommand.Enqueue(comandWithData);

		internalCommandIndex++;

		// check every 100 commands that workers aren't overloaded
		if (internalCommandIndex % 100 != 0)
			return;
		//CheckWorkerBalance();      
	}

	/// <summary>
	/// Executes a command instruction with a given CommandController.
	/// </summary>
	/// <param name="data">Data passed from a connected device.</param>
	/// <param name="controller">Controller to search for the command.</param>
	public void ExecuteCommand(ServerMessageData data, CommandController controller)
	{
		ExecuteCommand(controller.GetCommand(data.t), data);
	}


	void FixedUpdate()
	{
		CheckWorkerBalance();
	}

	/// <summary>
	/// Spawn or remove WorkerThreads from the pool
	/// </summary>
	protected void CheckWorkerBalance()
	{
		// only check every 100 fixed updates and/or commands
		// if there are more than 100 fixed updates between the next fixedUpdate
		internalCommandIndex++;
		if (internalCommandIndex % 100 != 0)
			return;

		int overloadedCount = 0;
		int underloadedCount = 0;
		CoflnetThreadWorker targetWorker = null;

		foreach (var item in workers)
		{
			if (item.queuedCommand.Count > 50)
			{
				overloadedCount++;
			}
			else if (item.queuedCommand.Count == 0)
			{
				if (targetWorker == null)
					targetWorker = item;
				underloadedCount++;
			}
		}

		// only create new workers when there are more than 50% overloaded
		// remember that this is an int so if there would be only one worker
		// it would start a second one (1 (workers.Count) / 2 = 0) if its >=
		if (overloadedCount > workers.Count / 2)
		{
			CreateWorkerThread();
			Debug.Log("Created new worker thread now " + workers.Count + " in total");
		}
		int minThreashold = workers.Count / 2;
		// make sure there are allways minWorkerCount worker threads
		if (minThreashold < minWorkerCount)
		{
			minThreashold = minWorkerCount;
		}
		else if (underloadedCount > workers.Count / 2 && workers.Count >= minThreashold)
		{
			DestroyWorkerThread(targetWorker);
			Debug.Log("Destroyed worker thread now " + workers.Count + " in total");
		}
	}


	/// <summary>
	/// Gets the thread for user.
	/// One user allways is in a specific thread to counter bad behavour that could block the whole program
	/// However an user could get to another thread if the total amount of threads in/decreases
	/// </summary>
	/// <param name="userId">User identifier.</param>
	public CoflnetThreadWorker GetWorkerForUser(SourceReference userId)
	{
		//ThreadPool.QueueUserWorkItem((object state) => { Debug.Log("hi"); });
		//ThreadStart threadStart = new ThreadStart(Work);
		//Thread thread = new Thread(threadStart, 1000);
		byte[] idSlice = BitConverter.GetBytes(userId.ResourceId);
		int id = Math.Abs(BitConverter.ToInt32(idSlice, 0)) % workers.Count;
		return workers[id];
	}

	void Awake()
	{
		CreateWorkerThread();
	}

	public void Start()
	{
		CreateWorkerThread();
	}


	/// <summary>
	/// Removes the worker from the workers List and
	/// destroies the worker thread.
	/// </summary>
	/// <param name="worker">Worker.</param>
	public void DestroyWorkerThread(CoflnetThreadWorker worker)
	{
		// remove them from the pool
		workers.Remove(worker);
		threads.Remove(worker.CurrentThread);

		worker.Stop();
	}

	public void CreateWorkerThread()
	{
		Debug.Log("Current thread count: " + threads.Count + " on " + SystemInfo.processorCount + " cores");
		// More threads than processors causes to much context switching
		// one core is reserved for the main thread
		if (SystemInfo.processorCount - 1 <= threads.Count)
			return;



		CoflnetThreadWorker worker = new CoflnetThreadWorker();

		ThreadStart threadStart = new ThreadStart(worker.ExecuteNextCommand);


		Thread thread = new Thread(threadStart, 1000000);
		worker.CurrentThread = thread;
		// also adds it to the pool
		StartThread(thread);
		// add the worker to the pool
		workers.Add(worker);

	}



	/// <summary>
	/// Starts a thread and adds it to the pool.
	/// </summary>
	/// <param name="thread">The thread to add.</param>
	private void StartThread(Thread thread)
	{
		threads.Add(thread);
		thread.Start();
	}

	public void OnApplicationQuit()
	{
		foreach (var item in threads)
		{
			item.Abort();
		}
	}



	public void Work(MessageData data)
	{
		int index = 0;
		//var request = new RestRequest(Method.GET);
		//var asyncHandle = client.ExecuteAsync(request, response =>
		//{
		//	Debug.Log("von rest sharp :D " + from + response.Content.ToString().Substring(0, index % 15));
		//});
		//var response = client.Execute(request);
		//Debug.Log("von rest sharp :D " + from + response.Content.ToString().Substring(0, 15));

		System.Random rnd = new System.Random();
		var result = rnd.Next(1, 13);

		while (true)
		{
			if (index > 1000000)
			{
				break;
			}
			index++;
			result += rnd.Next(1, 13);
		}
		sum += result;
		//Debug.Log("done work" + result);
	}

	public void Response(string data, int code)
	{
		UnityEngine.Debug.Log("received " + data);
	}
}


public class CoflnetThreadWorker
{
	public ConcurrentQueue<CommandItem> queuedCommand = new ConcurrentQueue<CommandItem>();
	private bool sleeping;
	private DateTime startTime;
	private bool isStopping;
	private Thread currentThread = null;

	public Thread CurrentThread
	{
		get
		{
			return currentThread;
		}
		set
		{
			if (currentThread == null)
			{
				currentThread = value;
			}
		}
	}

	public CoflnetThreadWorker()
	{
		this.startTime = DateTime.UtcNow;
	}


	public bool IsSleeping()
	{
		return sleeping;
	}

	public void ExecuteNextCommand()
	{
		if (queuedCommand.Count > 0)
		{
			sleeping = false;
			CommandItem item = NextCommandItem();
			try
			{
				CommandController.ExecuteCommandInCurrentThread(item.command, item.data);
			}
			catch (Exception ex)
			{
				Track.instance.Error(item.data.t, JsonUtility.ToJson(item), ex.ToString());
			}
		}
		else
		{
			if (isStopping)
			{
				return;
			}
			if (!sleeping)
			{
				long time = (DateTime.UtcNow - startTime).Ticks;
				Debug.Log("done in : " + time);
			}
			sleeping = true;
			Thread.Sleep(100);
		}
		ExecuteNextCommand();
	}

	private CommandItem NextCommandItem()
	{
		CommandItem item = null;
		queuedCommand.TryDequeue(out item);
		return item;
	}

	/// <summary>
	/// Signal to Stop this worker instance.
	/// Doesn't stop instantly.
	/// </summary>
	public void Stop()
	{
		isStopping = true;
	}
}

/// <summary>
/// Thread command result is produced when a command is invoked by an user.
/// Used for logging and billing purposes
/// </summary>
public class ThreadCommandResult
{
	public SourceReference executer;
	public string commandSlug;
	/// <summary>
	/// The execution time in ms.
	/// </summary>
	public int executionTime;
	/// <summary>
	/// The start time.
	/// </summary>
	public DateTime startTime;
}


[System.Serializable]
public class CommandItem
{
	public Command command;
	public MessageData data;

	public CommandItem(Command command, MessageData data)
	{
		this.command = command;
		this.data = data;
	}

	public CommandItem()
	{

	}
}