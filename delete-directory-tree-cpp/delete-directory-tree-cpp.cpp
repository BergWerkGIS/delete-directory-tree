// delete-directory-tree-cpp.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <iostream>
#include <string>
#include <Windows.h>


using std::cout;

#if defined(UNICODE)
#define _tcout std::wcout
#else
#define _tcout std::cout
#endif


bool _Quiet = false;
long _CntAllFiles = 0;
long _CntErrorFiles = 0;
long _CntAllDirs = 0;
long _CntErrorDirs = 0;


//based on:
//How to Delete Directories Recursively with Win32
//http://blog.nuclex-games.com/2012/06/how-to-delete-directories-recursively-with-win32/


class SearchHandleScope {

	/// <summary>Initializes a new search handle closer</summary>
	/// <param name="searchHandle">Search handle that will be closed on destruction</param>
public: SearchHandleScope(HANDLE searchHandle) :
	searchHandle(searchHandle) {
}

		/// <summary>Closes the search handle</summary>
public: ~SearchHandleScope() {
	::FindClose(this->searchHandle);
}

		/// <summary>Search handle that will be closed when the instance is destroyed</summary>
private: HANDLE searchHandle;

};


/// <summary>Recursively deletes the specified directory and all its contents</summary>
/// <param name="path">Absolute path of the directory that will be deleted</param>
/// <remarks>
///   The path must not be terminated with a path separator.
/// </remarks>
bool recursiveDeleteDirectory(const std::wstring &path) {

	static const std::wstring allFilesMask(L"\\*");

	WIN32_FIND_DATAW findData;
	bool dirSuccess = true;
	bool fileSuccess = true;
	bool returnValue = true;

	// First, delete the contents of the directory, recursively for subdirectories
	std::wstring searchMask = path + allFilesMask;
	HANDLE searchHandle = ::FindFirstFileExW(
		searchMask.c_str()
		, FindExInfoBasic
		, &findData
		, FindExSearchNameMatch
		, nullptr
		, 0
		);

	if (searchHandle == INVALID_HANDLE_VALUE) {
		DWORD lastError = ::GetLastError();
		if (lastError != ERROR_FILE_NOT_FOUND) { // or ERROR_NO_MORE_FILES, ERROR_NOT_FOUND?
			_tcout << _T("Directory [") << path << _T("] does not exist!") << std::endl;
			return 0;
		}
	}

	// Did this directory have any contents? If so, delete them first
	if (searchHandle != INVALID_HANDLE_VALUE) {
		SearchHandleScope scope(searchHandle);
		for (;;) {

			// Do not process the obligatory '.' and '..' directories
			//if (findData.cFileName[0] != '.') { //also exlcudes .files e.g. .gitignore
			if (0 != _wcsicmp(findData.cFileName, _T(".")) && 0 != _wcsicmp(findData.cFileName, _T(".."))) {
				bool isDirectory =
					((findData.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) != 0) ||
					((findData.dwFileAttributes & FILE_ATTRIBUTE_REPARSE_POINT) != 0);

				// Subdirectories need to be handled by deleting their contents first
				std::wstring filePath = path + L'\\' + findData.cFileName;
				if (isDirectory) {
					returnValue = recursiveDeleteDirectory(filePath);
				} else {
					_CntAllFiles++;
					bool tmpFileSuccess = true;
					for (int i = 0; i < 3; i++) {
						BOOL result = ::DeleteFileW(filePath.c_str());
						if (result == TRUE) {
							tmpFileSuccess = true;
							break;
						} else {
							tmpFileSuccess = false;
							if (0 == i) { _CntErrorFiles++; }
							_tcout << filePath << _T(": Could not delete file") << std::endl;
							Sleep(100);
						}
					}
					if (!tmpFileSuccess) { fileSuccess = false; }
				}
			}

			// Advance to the next file in the directory
			BOOL result = ::FindNextFileW(searchHandle, &findData);
			if (result == FALSE) {
				DWORD lastError = ::GetLastError();
				if (lastError != ERROR_NO_MORE_FILES) {
					_tcout << _T("Error enumerating directory") << std::endl;
				}
				break; // All directory contents enumerated and deleted
			}
		} // for
	}

	Sleep(100);

	if (!_Quiet) { _tcout << path << " deleting ..." << std::endl; }
	_CntAllDirs++;

	// The directory is empty, we can now safely remove it
	for (int i = 0; i < 3; i++) {
		BOOL result = ::RemoveDirectory(path.c_str());
		_tcout << result;
		if (result == TRUE) {
			dirSuccess = true;
			break;
		} else {
			dirSuccess = false;
			_tcout << path << _T(" Could not remove directory") << std::endl;
			Sleep(100);
		}
	}

	return (fileSuccess && dirSuccess && returnValue) ? true : false;
}





int _tmain(int argc, _TCHAR* argv[]) {

	//_tcout << _T("There are ") << argc << _T(" arguments:") << std::endl;
	//for (int i = 0; i < argc; i++) { _tcout << i << _T(" ") << argv[i] << std::endl; }


	if (argc < 2 || argc > 3) {
		_tcout << _T("delete-directory-tree [/Q] <directory>") << std::endl;
		return 1;
	}

	std::wstring dir;

	if (3 == argc && 0 != _wcsicmp(_T("/Q"), (argv[1]))) {
		_tcout << (argv[1]) << _T(" unknown parameter") << std::endl;
		return 1;
	} else if (3 == argc && 0 == _wcsicmp(_T("/Q"), (argv[1]))) {
		//_tcout << _T("QUIET TRUE") << std::endl;
		_Quiet = true;
		dir = argv[2];
	} else {
		dir = argv[1];
	}

	try {
		bool returnValue = recursiveDeleteDirectory(dir);

		_tcout << _CntAllDirs << _T(" directories") << std::endl;
		_tcout << _CntAllFiles << _T(" files") << std::endl;
		_tcout << _CntErrorDirs << _T(" directories with error") << std::endl;
		_tcout << _CntErrorFiles << _T(" files with error") << std::endl;

		return returnValue ? 0 : 1;
	}
	catch (...) {
		_tcout << "unknown EXCEPTION" << std::endl;
		return 1;
	}
}

