﻿using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System;
using System.Collections.Concurrent;
using RestSharp;
using System.Net;
using System.Text;
using Coflnet;


namespace Coflnet
{

	/// <summary>
	/// Controlls different (worker) threads, 
	/// to run more efficient on multicore computers
	/// </summary>
	public class ThreadController
	{
		private static readonly int minWorkerCount = 1;

		private List<CoflnetThreadWorker> workers = new List<CoflnetThreadWorker>();

		static RestClient client = new RestClient("https://beta.coflnet.com/api/v1/captcha");
		private uint internalCommandIndex;
		public int sum = 0;
		/// <summary>
		/// The main thread queue.
		/// Things added to this queue will be executed in the main thread (either gui or unity3d world changing)
		/// </summary>
		private ConcurrentQueue<Action> mainThreadQueue = new ConcurrentQueue<Action>();

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


		/// <summary>
		/// Processes the mainthread queue.
		/// Eg. the wpf gui or unity main thread (fixedupdate)
		/// </summary>
		public void ProcessQueue()
		{
			// may add checking that we are actually in the main thread ...

			Action todo = null;
			while (mainThreadQueue.TryDequeue(out todo))
			{
				todo.Invoke();
			}
		}

		/// <summary>
		/// Shedules the action to be executed on the main thread
		/// </summary>
		/// <param name="action">Action.</param>
		public void OnMainThread(Action action)
		{
			mainThreadQueue.Enqueue(action);
		}


		public void ExecuteCommand(Command command, CommandData data)
		{
			CommandItem comandWithData = new CommandItem(command, data);

			// find worker
			CoflnetThreadWorker worker = GetWorkerForUser(data.SenderId);
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
		public void ExecuteCommand(CommandData data, CommandController controller)
		{
			ExecuteCommand(controller.GetCommand(data.Type), data);
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
			}
		}


		/// <summary>
		/// Gets the thread for user.
		/// One user allways is in a specific thread to counter bad behavour that could block the whole program
		/// However an user could get to another thread if the total amount of threads in/decreases
		/// </summary>
		/// <param name="userId">User identifier.</param>
		public CoflnetThreadWorker GetWorkerForUser(EntityId userId)
		{
			//ThreadPool.QueueUserWorkItem((object state) => { Logger.Log("hi"); });
			//ThreadStart threadStart = new ThreadStart(Work);
			//Thread thread = new Thread(threadStart, 1000);
			byte[] idSlice = BitConverter.GetBytes(userId.LocalId);
			int id = Math.Abs(BitConverter.ToInt32(idSlice, 0)) % workers.Count;
			return workers[id];
		}


		public ThreadController()
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

			worker.Stop();
		}

		public void CreateWorkerThread()
		{
			// More threads than processors causes to much context switching
			// one core is reserved for the main thread
			if (Environment.ProcessorCount - 1 <= workers.Count)
				return;



			CoflnetThreadWorker worker = new CoflnetThreadWorker();

			ThreadStart threadStart = new ThreadStart(worker.ExecuteNextCommand);


			Thread thread = new Thread(threadStart, 1000000);
			// tell the worker its thread
			worker.CurrentThread = thread;
			thread.Start();
			// add the worker to the pool
			workers.Add(worker);
		}




		public void OnApplicationQuit()
		{
			foreach (var item in workers)
			{
				item.CurrentThread.Abort();
			}
		}



		public void Work(CommandData data)
		{
			int index = 0;
			//var request = new RestRequest(Method.GET);
			//var asyncHandle = client.ExecuteAsync(request, response =>
			//{
			//	Logger.Log("von rest sharp :D " + from + response.Content.ToString().Substring(0, index % 15));
			//});
			//var response = client.Execute(request);
			//Logger.Log("von rest sharp :D " + from + response.Content.ToString().Substring(0, 15));

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
			//Logger.Log("done work" + result);
		}

		public void Response(string data, int code)
		{
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
					Track.instance.Error(item.data.Type, MessagePack.MessagePackSerializer.SerializeToJson(item), ex.ToString());
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
				}
				sleeping = true;
				ManualResetEvent resetEvent = new ManualResetEvent(false);
				resetEvent.WaitOne();


				resetEvent.Set();
				resetEvent.Reset();
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
		public EntityId executer;
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
		public CommandData data;

		public CommandItem(Command command, CommandData data)
		{
			this.command = command;
			this.data = data;
		}

		public CommandItem()
		{

		}
	}

}