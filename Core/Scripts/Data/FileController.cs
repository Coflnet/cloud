using System;
using System.IO;
using System.Text;
using MessagePack;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Buffers;

namespace Coflnet
{
	public static class FileController
	{
		static string _dataPath;
		public static string dataPath {
			get 
			{
				if(_dataPath == null)
				{
					_dataPath = DefaultDataPath;
				}
				return _dataPath;
			}
			set 
			{
				Logger.Log($"Datapath set to: {value}");
				Directory.CreateDirectory(value);
				_dataPath = value;
			}
		}
		public static string configPath = "/etc/coflnet";

		/// <summary>
		/// The subfolder within the <see cref="Environment.SpecialFolder.ApplicationData"/> where data will be saved
		/// </summary>
		public static readonly string dataPathPostFix = "coflnet";

		static string DefaultDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),dataPathPostFix);

		static FileController()
		{

		}

		/// <summary>
		/// Writes all text.
		/// </summary>
		/// <param name="path">Path.</param>
		/// <param name="text">Text.</param>
		public static void WriteAllText(string path, string text)
		{
			WriteAllBytes(Path.Combine(dataPath, path), System.Text.Encoding.UTF8.GetBytes(text));
		}

		/// <summary>
		/// Writes all bytes.
		/// Creates folder if it doesn't exist
		/// </summary>
		/// <param name="path">Path.</param>
		/// <param name="bytes">Bytes.</param>
		public static void WriteAllBytes(string path, byte[] bytes)
		{
			System.IO.FileInfo file = new FileInfo(Path.Combine(dataPath, path));
			file.Directory.Create();
			File.WriteAllBytes(file.FullName, bytes);
		}



		/// <summary>
		/// Reads all bytes.
		/// </summary>
		/// <returns>The all bytes.</returns>
		/// <param name="relativePath">Path.</param>
		public static byte[] ReadAllBytes(string relativePath)
		{
			return File.ReadAllBytes(Path.Combine(dataPath, relativePath));
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
			return File.Exists(Path.Combine(dataPath, path));
		}

		/// <summary>
		/// Reads Line by line and returns it as
		/// </summary>
		/// <returns>The lines as.</returns>
		/// <param name="relativePath">Relative path.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public static IEnumerable<T> ReadLinesAs<T>(string relativePath)
		{
			return ReadLinesAs<T>(relativePath, MessagePackSerializer.DefaultOptions);
		}


		/// <summary>
		/// Reads the lines and deserialize as specific object.
		/// </summary>
		/// <returns>The lines as.</returns>
		/// <param name="relativePath">Relative path.</param>
		/// <param name="options">Resolver.</param>
		/// <typeparam name="T">The type to deserialize to.</typeparam>
		public static IEnumerable<T> ReadLinesAs<T>(string relativePath, MessagePackSerializerOptions options)
		{
			var path = Path.Combine(dataPath, relativePath);
			if(!File.Exists(path)){
				// there is nothing to read
				yield break;
			}

			using (var file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				var t = DeserializeListFromStreamAsync<T>(file,new CancellationToken());
				t.Wait();
				foreach (var item in t.Result)
				{
					yield return item;
				}
			}
		}

		static async Task<List<T>> DeserializeListFromStreamAsync<T>(Stream stream, CancellationToken cancellationToken)
		{
			var dataStructures = new List<T>();
			using (var streamReader = new MessagePackStreamReader(stream))
			{
				while (await streamReader.ReadAsync(cancellationToken) is ReadOnlySequence<byte> msgpack)
				{
					dataStructures.Add(MessagePackSerializer.Deserialize<T>(msgpack, cancellationToken: cancellationToken));
				}
			}

			return dataStructures;
		}


		/// <summary>
		/// Appends an object to file after serializing it
		/// </summary>
		/// <param name="relativePath">Relative path.</param>
		/// <param name="data">The object to serialize.</param>
		/// <typeparam name="T">Type to serialize to.</typeparam>
		public static void AppendLineAs<T>(string relativePath, T data)
		{
			AppendLineAs<T>(relativePath, data, MessagePackSerializer.DefaultOptions);
		}

		/// <summary>
		/// Appends an object to a file after serializing it
		/// </summary>
		/// <param name="relativePath">Relative path.</param>
		/// <param name="data">Data.</param>
		/// <param name="options">Resolver.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public static void AppendLineAs<T>(string relativePath, T data, MessagePackSerializerOptions options)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(dataPath, relativePath)));
			using (var file = File.Open(Path.Combine(dataPath, relativePath), FileMode.Append, FileAccess.Write, FileShare.None))
			{
				MessagePackSerializer.Serialize<T>(file, data, options);
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
			WriteLinesAs<T>(relativePath, data, MessagePackSerializer.DefaultOptions);
		}



		/// <summary>
		/// Serializes and writes objects to a file relative to the application data folder
		/// </summary>
		/// <param name="relativePath">Relative path to the data folder.</param>
		/// <param name="data">Data to write.</param>
		/// <param name="resolver">Resolver to use for serialization.</param>
		/// <typeparam name="T">What type to use for serialization.</typeparam>
		public static void WriteLinesAs<T>(string relativePath, IEnumerable<T> data, MessagePackSerializerOptions resolver)
		{
			using (var file = File.Open(Path.Combine(dataPath, relativePath), FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
			{
				foreach (var item in data)
				{
					MessagePackSerializer.Serialize<T>(file, item, resolver);
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
			return MessagePackSerializer.Deserialize<T>(data);
		}

		/// <summary>
		/// Serializes and saves some data
		/// </summary>
		/// <param name="relativePath">Relative path.</param>
		/// <param name="data">Data.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public static void SaveAs<T>(string relativePath, T data)
		{
			WriteAllBytes(relativePath, MessagePackSerializer.Serialize(data));
		}

		/// <summary>
		/// Delete the  file at specified relativePath.
		/// </summary>
		/// <param name="relativePath">Relative path.</param>
		public static void Delete(string relativePath)
		{
			var path = Path.Combine(dataPath, relativePath);
			if (Directory.Exists(Path.GetDirectoryName(path)))
				File.Delete(path);
		}


		/// <summary>
		/// Deletes the relative folder with everything in it
		/// </summary>
		/// <param name="relativePath">Relative path to the folder</param>
		public static void DeleteFolder(string relativePath)
		{
			var path = Path.Combine(dataPath, relativePath);
			if(Directory.Exists(path)){
				Directory.Delete(path,true);
			}
		}

		/// <summary>
		/// Move the specified relavtiveOrigin to relativeDestination.
		/// </summary>
		/// <param name="relavtiveOrigin">Relavtive origin.</param>
		/// <param name="relativeDestination">Relative destination.</param>
		public static void Move(string relavtiveOrigin, string relativeDestination)
		{
			File.Move(Path.Combine(dataPath, relavtiveOrigin), Path.Combine(dataPath, relativeDestination));
		}
	}
}
