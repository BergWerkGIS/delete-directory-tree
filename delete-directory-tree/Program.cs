using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace delete_directory_tree {
	class Program {
		static int Main( string[] args ) {

			if (null == args || args.Length != 1) {
				Console.WriteLine( "Only one directory allowed!" );
				return 1;
			}

			string dir = args[0];
			if (!Directory.Exists( dir )) {
				Console.WriteLine( "Directory [{0}] does not exist!", dir );
				return 1;
			}

			return DeleteFilesAndFoldersRecursively( dir ) ? 0 : 1;
		}



		public static bool DeleteFilesAndFoldersRecursively( string target_dir ) {

			bool fileSuccess = true;
			foreach (string file in Directory.GetFiles( target_dir )) {
				bool singleFileSuccess = false;
				for (int i = 0; i < 3; i++) {
					try {
						File.Delete( file );
						singleFileSuccess = true;
						break;
					}
					catch (Exception e) {
						Console.WriteLine( "[{0}]: {1}", file, e.Message );
						singleFileSuccess = false;
						Thread.Sleep( 100 );
					}
				}

				if (!singleFileSuccess) { fileSuccess = false; }
			}

			bool dirSuccess = true;
			foreach (string subDir in Directory.GetDirectories( target_dir )) {
				bool tmpSuccess = DeleteFilesAndFoldersRecursively( subDir );
				if (!tmpSuccess) { dirSuccess = false; }
			}

			// This makes the difference between whether it works or not.
			Thread.Sleep( 10 );

			bool singleDirSuccess = false;
			for (int i = 0; i < 3; i++) {
				try {
					Directory.Delete( target_dir );
					singleDirSuccess = true;
					break;
				}
				catch (Exception e) {
					Console.WriteLine( "[{0}]: {1}", target_dir, e.Message );
					singleDirSuccess = false;
					Thread.Sleep( 100 );
				}

				if (!singleDirSuccess) { dirSuccess = false; }
			}

			return (fileSuccess && dirSuccess);
		}
	}
}
