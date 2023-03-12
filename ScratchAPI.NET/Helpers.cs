using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ScratchDL
{
    public static class Helpers
    {
        /// <summary>
        /// Waits until a file is avaiable for opening, then proceeds to open it
        /// </summary>
        /// <param name="fileName">The file to open</param>
        /// <param name="mode">Determines how the file should be opened</param>
        /// <param name="accessMode">Determines if the file has read or write acess</param>
        /// <param name="shareMode">Determines how the file can be shared with other processes</param>
        /// <returns>Returns a stream for reading or writing to the file</returns>
        public static async Task<FileStream> WaitTillFileAvailable(string fileName, FileMode mode, FileAccess accessMode = FileAccess.ReadWrite, FileShare shareMode = FileShare.None)
        {
            FileStream? stream;

            while (!TryOpenFile(fileName, out stream, mode, accessMode, shareMode))
            {
                await Task.Delay(50);
            }

            return stream!;
        }

        /// <summary>
        /// Tries to open a file.
        /// </summary>
        /// <param name="filename">The file to open</param>
        /// <param name="fileStream">The file stream if the file was opened</param>
        /// <param name="mode">The mode that determines how the file should be opened</param>
        /// <param name="accessMode">The access mode that determines how the file stream will be used</param>
        /// <param name="shareMode">Determines how other processes can use the file</param>
        /// <returns>Returns true if the file was opened</returns>
        public static bool TryOpenFile(string filename, out FileStream? fileStream, FileMode mode = FileMode.Open, FileAccess accessMode = FileAccess.ReadWrite, FileShare shareMode = FileShare.None)
        {
            try
            {
                var inputStream = File.Open(filename, mode, accessMode, shareMode);
                if (inputStream.Length > -1)
                {
                    fileStream = inputStream;
                    return true;
                }
                else
                {
                    fileStream = null;
                    return false;
                }
            }
            catch (Exception)
            {
                fileStream = null;
                return false;
            }
        }

        /// <summary>
        /// Checks if the span is the same as the char sequence
        /// </summary>
        /// <param name="span">The span to check</param>
        /// <param name="characters">The sequence of characters to check</param>
        /// <returns>Returns true if the span is the same as the sequence of characters</returns>
        public static bool IsSpanSameAs(ReadOnlySpan<byte> span, char[] characters)
        {
            for (int i = 0; i < characters.GetLength(0); i++)
            {
                if (span[i] != characters[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Finds a sequence of chars within a span
        /// </summary>
        /// <param name="span">The span to search</param>
        /// <param name="characters">The sequence of characters to find</param>
        /// <returns>Returns the start index where the sequence of characters was found, or -1 if the sequence wasn't found</returns>
        public static int FindPositionInSpan(ReadOnlySpan<byte> span, char[] characters)
        {
            int startPos = 0;
            int charactersFound = 0;
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] == characters[charactersFound])
                {
                    if (charactersFound == 0)
                    {
                        startPos = i;
                    }
                    charactersFound++;

                    if (charactersFound == characters.Length)
                    {
                        return startPos;
                    }
                }
                else
                {
                    startPos = 0;
                    charactersFound = 0;
                }
            }

            return -1;
        }

        /// <summary>
        /// Gets the value of an object's field, or null if the field doesn't exist
        /// </summary>
        /// <param name="obj">The object to get the field value from</param>
        /// <param name="fieldName">The name of the field</param>
        /// <returns>Returns the value of the field, or null if the field doesn't exist</returns>
        public static object? GetFieldValue(object? obj, string fieldName)
        {
            if (obj == null)
            {
                return null;
            }

            return obj.GetType().GetProperty(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(obj);
        }

        /// <summary>
        /// Removes any illegal charactesr from a path
        /// </summary>
        /// <param name="input">The input path</param>
        /// <returns>Returns the path with the illegal characters removed</returns>
        public static string RemoveIllegalCharacters(string input)
        {
            string invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());

            var filteredName = input;

            foreach (char c in invalidChars)
            {
                filteredName = filteredName.Replace(c.ToString(), "");
            }
            return filteredName;
        }

        /// <summary>
        /// Adds a backslash to the end of the path
        /// </summary>
        /// <param name="path">The input file path</param>
        /// <returns>Returns the path with a slash at the end</returns>
        public static string PathAddBackslash(string path)
        {
            path = path.TrimEnd();

            if (PathEndsWithDirectorySeparator())
                return path;

            return path + GetDirectorySeparatorUsedInPath();

            bool PathEndsWithDirectorySeparator()
            {
                if (path.Length == 0)
                    return false;

                char lastChar = path[path.Length - 1];
                return lastChar == Path.DirectorySeparatorChar
                    || lastChar == Path.AltDirectorySeparatorChar;
            }

            char GetDirectorySeparatorUsedInPath()
            {
                if (path.Contains(Path.AltDirectorySeparatorChar))
                    return Path.AltDirectorySeparatorChar;

                return Path.DirectorySeparatorChar;
            }
        }
    }
}
