using System;
using System.IO;
using System.Text;
using MessagePack;
using System.Collections.Generic;


namespace Coflnet
{
	public static class FileController
	{
		private static string dataPaht = "/var/lib/coflnet";
		private static string configPath = "/etc/coflnet";


		static FileController()
		{
			dataPaht = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/coflnet";
			UnityEngine.Debug.Log($"DataPath: {dataPaht}");

			Directory.CreateDirectory(dataPaht);
		}

		/// <summary>
		/// Writes all text.
		/// </summary>
		/// <param name="path">Path.</param>
		/// <param name="text">Text.</param>
		public static void WriteAllText(string path, string text)
		{
			File.WriteAllText(Path.Combine(dataPaht, path), text);
		}

		/// <summary>
		/// Writes all bytes.
		/// </summary>
		/// <param name="path">Path.</param>
		/// <param name="bytes">Bytes.</param>
		public static void WriteAllBytes(string path, byte[] bytes)
		{
			File.WriteAllBytes(Path.Combine(dataPaht, path), bytes);
		}


		/// <summary>
		/// Reads all bytes.
		/// </summary>
		/// <returns>The all bytes.</returns>
		/// <param name="relativePath">Path.</param>
		public static byte[] ReadAllBytes(string relativePath)
		{
			return File.ReadAllBytes(Path.Combine(dataPaht, relativePath));
		}

		/// <summary>
		/// Reads all config bytes.
		/// </summary>
		/// <returns>The all config bytes.</returns>
		/// <param name="relativePath">Relative path.</param>
		public static byte[] ReadAllConfigBytes(string relativePath)
		{
			return File.ReadAllBytes(Path.Combine(configPath, relativePath));
		}

		/// <summary>
		/// Determines whether the specified File exists relative to the settings direcotry
		/// </summary>
		/// <returns>The exists.</returns>
		/// <param name="path">Path.</param>
		public static bool SettingExits(string path)
		{
			return File.Exists(Path.Combine(configPath, path));
		}


		/// <summary>
		/// Determines whether the specified File exists relative to the settings direcotry
		/// </summary>
		/// <returns>The exists.</returns>
		/// <param name="path">Path.</param>
		public static bool Exists(string path)
		{
			return File.Exists(Path.Combine(dataPaht, path));
		}

		/// <summary>
		/// Reads Line by line and returns it as
		/// </summary>
		/// <returns>The lines as.</returns>
		/// <param name="relativePath">Relative path.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public static IEnumerable<T> ReadLinesAs<T>(string relativePath)
		{
			return ReadLinesAs<T>(relativePath, MessagePackSerializer.DefaultResolver);
		}


		/// <summary>
		/// Reads the lines and deserialize as specific object.
		/// </summary>
		/// <returns>The lines as.</returns>
		/// <param name="relativePath">Relative path.</param>
		/// <param name="resolver">Resolver.</param>
		/// <typeparam name="T">The type to deserialize to.</typeparam>
		public static IEnumerable<T> ReadLinesAs<T>(string relativePath, IFormatterResolver resolver)
		{
			using (var file = File.Open(Path.Combine(dataPaht, relativePath), FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read))
			{
				while (file.Position < file.Length)
				{
					yield return MessagePackSerializer.Deserialize<T>(file, resolver);
				}
			}
		}

		/// <summary>
		/// Appends an object to file after serializing it
		/// </summary>
		/// <param name="relativePath">Relative path.</param>
		/// <param name="data">The object to serialize.</param>
		/// <typeparam name="T">Type to serialize to.</typeparam>
		public static void AppendLineAs<T>(string relativePath, T data)
		{
			AppendLineAs<T>(relativePath, data, MessagePackSerializer.DefaultResolver);
		}

		/// <summary>
		/// Appends an object to a file after serializing it
		/// </summary>
		/// <param name="relativePath">Relative path.</param>
		/// <param name="data">Data.</param>
		/// <param name="resolver">Resolver.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public static void AppendLineAs<T>(string relativePath, T data, IFormatterResolver resolver)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(dataPaht, relativePath)));
			using (var file = File.Open(Path.Combine(dataPaht, relativePath), FileMode.Append, FileAccess.Write, FileShare.None))
			{
				MessagePackSerializer.Serialize<T>(file, data, resolver);
			}
		}


		/// <summary>
		/// Reads Line by line and returns it as
		/// </summary>
		/// <returns>The lines as.</returns>
		/// <param name="relativePath">Relative path.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public static void WriteLinesAs<T>(string relativePath, IEnumerable<T> data)
		{
			WriteLinesAs<T>(relativePath, data, MessagePackSerializer.DefaultResolver);
		}



		/// <summary>
		/// Serializes and writes objects to a file relative to the application data folder
		/// </summary>
		/// <param name="relativePath">Relative path to the data folder.</param>
		/// <param name="data">Data to write.</param>
		/// <param name="resolver">Resolver to use for serialization.</param>
		/// <typeparam name="T">What type to use for serialization.</typeparam>
		public static void WriteLinesAs<T>(string relativePath, IEnumerable<T> data, IFormatterResolver resolver)
		{
			using (var file = File.Open(Path.Combine(dataPaht, relativePath), FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
			{
				foreach (var item in data)
				{
					LZ4MessagePackSerializer.Serialize<T>(file, item, resolver);
				}
			}
		}


		/// <summary>
		/// Loads data and tries to serialize it into given type
		/// </summary>
		/// <returns>The as.</returns>
		/// <param name="relativePath">Relative path.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public static T LoadAs<T>(string relativePath)
		{
			return Deserialize<T>(ReadAllBytes(relativePath));
		}


		private static T Deserialize<T>(byte[] data)
		{
			return LZ4MessagePackSerializer.Deserialize<T>(data);
		}

		/// <summary>
		/// Serializes and saves some data
		/// </summary>
		/// <param name="relativePath">Relative path.</param>
		/// <param name="data">Data.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public static void SaveAs<T>(string relativePath, T data)
		{
			WriteAllBytes(relativePath, LZ4MessagePackSerializer.Serialize(data));
		}

		/// <summary>
		/// Delete the  file at specified relativePath.
		/// </summary>
		/// <param name="relativePath">Relative path.</param>
		public static void Delete(string relativePath)
		{
			File.Delete(Path.Combine(dataPaht, relativePath));
		}

		/// <summary>
		/// Move the specified relavtiveOrigin to relativeDestination.
		/// </summary>
		/// <param name="relavtiveOrigin">Relavtive origin.</param>
		/// <param name="relativeDestination">Relative destination.</param>
		public static void Move(string relavtiveOrigin, string relativeDestination)
		{
			File.Move(Path.Combine(dataPaht, relavtiveOrigin), Path.Combine(dataPaht, relativeDestination));
		}
	}
}
