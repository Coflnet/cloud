namespace Coflnet.Core
{
    public class RemoteInt : RemoteObject<int>
	{
		public RemoteInt(int value) : base(value) {}
		public RemoteInt(RemoteObject<int> remoteObject, int value) : base(remoteObject) {}


		public void Add(long value)
		{
			Send("add",value);
		}

		/// <summary>
		/// Subscracts value from the int
		/// </summary>
		/// <param name="value"></param>
		public void Substract(long value)
		{
			Send("subs",value);
		}

		/// <summary>
		/// Increments the value
		/// </summary>
		/// <param name="value"></param>
		public void Increment()
		{
			Send("inc");
		}

		/// <summary>
		/// Decrements the value
		/// </summary>
		/// <param name="value"></param>
		public void Decrement()
		{
			Send("dec");
		}

		
		/// <summary>
		/// Distributes the add command
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static RemoteInt operator +(RemoteInt a, long b)
		{
			a.Add(b);
			// the core has updated the value by now
			return a;
		}

		/// <summary>
		/// Distributes the substract command
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static RemoteInt operator -(RemoteInt a, long b)
		{
			a.Substract(b);
			// the core has updated the value by now
			return a;
		}


		/// <summary>
		/// Distributes the decrement command
		/// </summary>
		/// <param name="a"></param>
		/// <returns></returns>
		public static RemoteInt operator --(RemoteInt a)
		{
			a.Decrement();
			return new RemoteInt(a,a.Value--);
		}



		/// <summary>
		/// Distributes the decrement command
		/// </summary>
		/// <param name="a"></param>
		/// <returns></returns>
		public static RemoteInt operator ++(RemoteInt a)
		{
			a.Increment();
			return new RemoteInt(a,a.Value++);
		}
	}


}