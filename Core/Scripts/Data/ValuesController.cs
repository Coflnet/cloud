using System.Text;
using System.IO;
using System;

namespace Coflnet
{

	/// <summary>
	/// Simple key value store, depends on FileController
	/// </summary>
	public class ValuesController
	{
		/// <summary>
		/// Hases the key.
		/// </summary>
		/// <returns><c>true</c>, if key exists, <c>false</c> otherwise.</returns>
		/// <param name="key">Key to search for.</param>
		public static bool HasKey(string key)
		{
			return FileController.Exists(SaveKeyWithPrefix(key));
		}


		/// <summary>
		/// Gets the string.
		/// </summary>
		/// <returns>The string found under that key.</returns>
		/// <param name="key">Key.</param>
		public static string GetString(string key)
		{
			try
			{
				return Encoding.UTF8.GetString(FileController.ReadAllBytes(SaveKeyWithPrefix(key)));
			}
			catch (FileNotFoundException)
			{
				return null;
			}
		}

		/// <summary>
		/// Gets an int.
		/// </summary>
		/// <returns>The int if found or -1 if an error occured.</returns>
		/// <param name="key">Key.</param>
		public static int GetInt(string key)
		{
			try
			{
				return BitConverter.ToInt32(FileController.ReadAllBytes("data" + key), 0);
			}
			catch (FileNotFoundException)
			{
				return -1;
			}
		}

		/// <summary>
		/// Saves a string to disc
		/// </summary>
		/// <param name="key">Key under which to save the string.</param>
		/// <param name="value">Value.</param>
		public static void SetString(string key, string value)
		{
			FileController.WriteAllBytes(SaveKeyWithPrefix(key), Encoding.UTF8.GetBytes(value));
		}

		/// <summary>
		/// Sets an int.
		/// </summary>
		/// <param name="key">Key under which to save the int.</param>
		/// <param name="value">The int which to save.</param>
		public static void SetInt(string key, int value)
		{
			FileController.WriteAllBytes(SaveKeyWithPrefix(key), BitConverter.GetBytes(value));
		}

		/// <summary>
		/// Deletes the key.
		/// </summary>
		/// <param name="key">Key.</param>
		public static void DeleteKey(string key)
		{
			FileController.Delete(SaveKeyWithPrefix(key));
		}


		private static string SaveKeyWithPrefix(string key)
		{
			return "data" + key;
		}
	}

}
