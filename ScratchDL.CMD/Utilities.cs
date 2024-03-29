﻿using ScratchDL;
using ScratchDL.CMD.Options.Base;
using ScratchDL.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public static class Utilities
{
	static bool doneReadingArguments = false;
	static int currentArgumentIndex = -1;
	public static string[] StartingArguments = Array.Empty<string>();

	public const int DEFAULT_SCAN_DEPTH = 400;

	public static EnumType PickEnumOption<EnumType>(string message, IEnumerable<string>? descriptions = null) where EnumType : Enum
	{
		return PickEnumOption(message, (EnumType)(object)-1, descriptions);
	}

	public static EnumType PickEnumOption<EnumType>(string message, EnumType defaultValue, IEnumerable<string>? descriptions = null) where EnumType : Enum
	{
		bool useDefault = ((int)(object)defaultValue) != -1;

		if (descriptions == null)
		{
			descriptions = Enumerable.Empty<string>();
		}
		Console.WriteLine(message);
		Console.WriteLine("--------------------------------");
		var options = (EnumType[])Enum.GetValues(typeof(EnumType));

		var descArray = descriptions.ToArray();

		int index = 0;

		foreach (var val in options)
		{
			string defaultText;
			if (useDefault && val.Equals(defaultValue))
			{
				defaultText = "(Default)";
			}
			else
			{
				defaultText = "";
			}


			if (index < descArray.Length)
			{
				Console.WriteLine($"{(int)(object)val} - {Helpers.Prettify(val.ToString())} {defaultText} : {descArray[index]}");
			}
			else
			{
				Console.WriteLine($"{(int)(object)val} - {Helpers.Prettify(val.ToString())} {defaultText}");
			}


			index++;
		}

		Console.WriteLine("--------------------------------");
		while (true)
		{
			Console.WriteLine("Enter number to select:");
			var inputLine = ReadLineFromConsole();
			if (int.TryParse(inputLine, out var input) && Enum.IsDefined(typeof(EnumType), (EnumType)(object)input))
			{
				var option = (EnumType)Enum.ToObject(typeof(EnumType), input);
				if (options.Contains(option))
				{
					return option;
				}
				else
				{
					Console.WriteLine("Invalid Option - Try Again");
				}
			}
			else if ((string.IsNullOrEmpty(inputLine) || string.IsNullOrWhiteSpace(inputLine)) && useDefault)
			{
				return defaultValue;
			}
			else
			{
				Console.WriteLine("Invalid Number - Try Again");
			}
		}
	}

    internal static ProgramOption_Base PickProgramOption(IEnumerable<ProgramOption_Base> options, ProgramOption_Base? defaultOption = null)
    {
		Console.WriteLine();
		ProgramOption_Base[] optionsArray = options.ToArray();

        for (int i = 0; i < optionsArray.Length; i++)
        {
			var option = optionsArray[i];
			string defaultText = option == defaultOption ? "(Default)" : "";

			Console.WriteLine($"{i + 1} - {option.Title} {defaultText} : {option.Description}");
		}

		Console.WriteLine("--------------------------------");
		while (true)
		{
			Console.WriteLine("Enter number to select:");
			var inputLine = ReadLineFromConsole();

			if (int.TryParse(inputLine, out var input))
            {
                if (input > 0 && input <= optionsArray.Length)
                {
					return optionsArray[input - 1];
                }
				else
                {
					Console.WriteLine("Invalid Option - Try Again");
				}
            }
			else if ((string.IsNullOrEmpty(inputLine) || string.IsNullOrWhiteSpace(inputLine)) && defaultOption != null)
            {
				return defaultOption;
            }
			else
            {
				Console.WriteLine("Invalid Number - Try Again");
			}
		}
	}

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

	public static ScanType GetScanType() => PickEnumOption<ScanType>("How should the user's projects be scanned?", new string[]
	{
		"Will only retrieve the published projects on the user's profile page",
		"Will do a deep scan for any published AND unpublished projects made by the user"
	});

	public static bool GetCommentDownloadOption()
    {
		return PickEnumOption("Download Project Comments?", DownloadComments.Yes) == DownloadComments.Yes;
	}

	public static uint GetScanDepth(uint defaultValue = DEFAULT_SCAN_DEPTH)
    {
		return GetUIntFromConsole("Enter the scan depth. The higher the number, the more projects that could be found, but the longer the scan will take", defaultValue);
	}

	public static DirectoryInfo GetDirectory()
	{
		while (true)
		{
			Console.Write("Specify output directory: ");
			var directory = ReadLineFromConsole();
			try
			{
				return Directory.CreateDirectory(directory);
			}
			catch (Exception)
			{
				Console.WriteLine("Invalid Directory. Try Again");
			}
		}
	}

	public static string GetStringFromConsole(string message)
	{
		while (true)
		{
			Console.WriteLine($"{message}: ");
			var result = ReadLineFromConsole();
			if (string.IsNullOrEmpty(result) || string.IsNullOrWhiteSpace(result))
			{
				continue;
			}
			else
			{
				return result;
			}
		}
	}

	public static uint GetUIntFromConsole(string message, uint? defaultValue)
	{
		while (true)
		{
			if (defaultValue == null)
			{
				Console.WriteLine($"{message}: ");
			}
			else
			{
				Console.WriteLine($"{message} (Default Value - {defaultValue.Value}): ");
			}
			var result = ReadLineFromConsole();
			if (string.IsNullOrEmpty(result) || string.IsNullOrWhiteSpace(result))
			{
				if (defaultValue != null)
				{
					return defaultValue.Value;
				}
			}
			else
			{
				if (uint.TryParse(result, out var number))
				{
					return number;
				}
				else
				{
					Console.WriteLine("Invalid Number. Try Again");
				}
			}
		}
	}

	public static string GetPasswordInput(string message)
	{
		Console.WriteLine(message);
        if (HasNextLinePrepared())
        {
			return ReadLineFromConsole();
		}

		var pass = string.Empty;
		ConsoleKey key;
		do
		{
			var keyInfo = Console.ReadKey(intercept: true);
			key = keyInfo.Key;

			if (key == ConsoleKey.Backspace && pass.Length > 0)
			{
				Console.Write("\b \b");
				pass = pass[0..^1];
			}
			else if (!char.IsControl(keyInfo.KeyChar))
			{
				Console.Write("*");
				pass += keyInfo.KeyChar;
			}
		} while (key != ConsoleKey.Enter);
		Console.WriteLine();
		return pass;
	}

	static string ReadLineFromConsole()
    {
        if (!doneReadingArguments)
        {
			currentArgumentIndex++;
            if (currentArgumentIndex >= StartingArguments.Length || StartingArguments == null)
            {
				doneReadingArguments = true;
            }
			else
            {
				return StartingArguments[currentArgumentIndex];
            }
		}

		string? result = null;

        while (result == null)
        {
			result = Console.ReadLine();
		}

		return result;
    }

	static bool HasNextLinePrepared()
    {
		if (!doneReadingArguments)
		{
			if (currentArgumentIndex + 1 < StartingArguments.Length && StartingArguments != null)
			{
				return true;
			}
		}

		return false;
	}
}