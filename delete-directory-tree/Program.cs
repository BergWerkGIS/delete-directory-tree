using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace delete_directory_tree {

	class Program {


		private static bool _Quiet = false;
		private static long _CntAllFiles = 0;
		private static long _CntErrorFiles = 0;
		private static long _CntAllDirs = 0;
		private static long _CntErrorDirs = 0;

		static int Main(string[] args) {

			if (null == args || args.Length < 1 || args.Length > 2) {
				writeError("delete-directory-tree [/Q] <directory>");
				return 1;
			}

			string dir = string.Empty;

			if (2 == args.Length && !"/Q".Equals(args[0], StringComparison.OrdinalIgnoreCase)) {
				writeError("[{0}]: unknown parameter", args[0]);
				return 1;
			} else if (2 == args.Length && "/Q".Equals(args[0], StringComparison.OrdinalIgnoreCase)) {
				_Quiet = true;
				dir = args[1];
			} else {
				dir = args[0];
			}

			string fullPath = new DirectoryInfo(dir).FullName;

			if (!Directory.Exists(dir)) {
				writeError("[{0}] does not exist!", fullPath);
				return 0;
			}

			try {
				if (_Quiet) { Console.WriteLine( "[{0}]", fullPath ); }
				return DeleteFilesAndFoldersRecursively(dir) ? 0 : 1;
			}
			catch (Exception e) {
				writeError(e.Message);
				return 1;
			}
			finally {
				//show summary, repeat base dir name if not quiet
				if (!_Quiet) { Console.WriteLine( "[{0}]", fullPath ); }
				string msg = string.Format(
					//"{0,7:#######} folders - {1,7:#######} with error, {2,7:#######} files - {3,7:#######} with error"
					"{0,7} folders {1,7} with error, {2,7} files {3,7} with error"
					, _CntAllDirs
					, _CntErrorDirs
					, _CntAllFiles
					, _CntErrorFiles
				);
				if (_CntErrorDirs > 0 || _CntErrorFiles > 0) {
					writeError(msg);
				} else {
					Console.WriteLine(msg);
				}
			}
		}



		public static bool DeleteFilesAndFoldersRecursively(string target_dir) {

			bool fileSuccess = true;
			foreach (string file in Directory.GetFiles(target_dir)) {
				_CntAllFiles++;
				if (!deleteFile(file)) {
					fileSuccess = false;
					_CntErrorFiles++;
				}
			}

			bool dirSuccess = true;
			foreach (string subDir in Directory.GetDirectories(target_dir)) {
				bool tmpSuccess = DeleteFilesAndFoldersRecursively(subDir);
				if (!tmpSuccess) { dirSuccess = false; }
			}

			// This makes the difference between whether it works or not.
			Thread.Sleep(10);

			_CntAllDirs++;
			if (!deleteDir(target_dir)) {
				dirSuccess = false;
				_CntErrorDirs++;
			}

			return (fileSuccess && dirSuccess);
		}


		public static bool deleteFile(string file) {

			for (int i = 0; i < 3; i++) {
				try {
					//another way to reset attributes:
					//FileSystemInfo fsi = new FileSystemInfo(pathToFile);
					//fsi.Attributes = FileAttributes.Normal;
					//or
					//File.SetAttributes(pathToFile, FileAttributes.Normal);
					//File.Delete(pathToFile);
					FileInfo f = new FileInfo(file);
					f.Attributes = f.Attributes & ~(FileAttributes.Archive | FileAttributes.ReadOnly | FileAttributes.Hidden);
					f.Delete();
					return true;
				}
				catch (Exception e) {
					writeError("[{0}]: {1}", file, e.Message);
					Thread.Sleep(100);
				}
			}

			return false;
		}


		public static bool deleteDir(string dir) {

			for (int i = 0; i < 3; i++) {
				try {
					if (!_Quiet) { Console.WriteLine("[{0}] deleting ...", dir); }
					//Directory.Delete( dir );
					DirectoryInfo d = new DirectoryInfo(dir);
					d.Attributes = d.Attributes & ~(FileAttributes.Archive | FileAttributes.ReadOnly | FileAttributes.Hidden);
					d.Delete();
					return true;
				}
				catch (Exception e) {
					writeError("[{0}]: {1}", dir, e.Message.Trim());
					Thread.Sleep(100);
				}
			}

			return false;
		}

		public static void writeError(string msg, params object[] args) {
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(msg, args);
			Console.ResetColor();
		}



	}
}
