using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PrintFileStructure
{
    class Program
    {
        private static List<string> commandHistory = new List<string> ( );
        private static int commandHistoryIndex = -1;

        static void Main ( string[] args )
        {
            string currentPath = Directory.GetCurrentDirectory ( );
            DisplayHelp ( );

            while ( true )
            {
                Console.Write ( $"\n{currentPath}> " );
                string command = ReadLineWithAutoComplete ( currentPath );
                if ( command == "exit" )
                {
                    break;
                }
                HandleCommand ( command,ref currentPath );
            }
        }

        static void DisplayHelp ( )
        {
            Console.WriteLine ( "Welcome! You can use the following commands:\n" +
                              "- 'pwd' to display the current directory\n" +
                              "- 'ls' to list the contents of the current directory\n" +
                              "- 'cd <path>' to change the directory\n" +
                              "- 'print' to print the directory structure\n" +
                              "- 'help' to display this help message again\n" +
                              "- 'exit' to close the program" );
        }

        static void HandleCommand ( string command,ref string currentPath )
        {
            if ( command == "pwd" )
            {
                Console.WriteLine ( $"Current directory: {currentPath}" );
            }
            else if ( command == "ls" )
            {
                Console.WriteLine ( "Listing contents:" );
                foreach ( var entry in Directory.GetFileSystemEntries ( currentPath ) )
                {
                    Console.WriteLine ( Path.GetFileName ( entry ) );
                }
            }
            else if ( command.StartsWith ( "cd " ) )
            {
                string newPath = command.Substring ( 3 ).Trim ( );
                if ( newPath == "" )
                {
                    Console.WriteLine ( "Please provide a directory to change to." );
                    return;
                }
                // Handling drive changes separately
                if ( newPath.Length == 2 && newPath[1] == ':' )
                {
                    if ( Directory.Exists ( newPath + "\\" ) )
                    {
                        currentPath = newPath + "\\";
                        Console.WriteLine ( $"Changed directory to: {currentPath}" );
                    }
                    else
                    {
                        Console.WriteLine ( $"The drive '{newPath}' does not exist." );
                    }
                }
                else
                {
                    newPath = Path.GetFullPath ( Path.Combine ( currentPath,newPath ) );

                    if ( Directory.Exists ( newPath ) )
                    {
                        currentPath = newPath;
                        Console.WriteLine ( $"Changed directory to: {currentPath}" );
                    }
                    else
                    {
                        Console.WriteLine ( $"The directory '{newPath}' does not exist." );
                    }
                }
            }
            else if ( command == "print" )
            {
                Console.WriteLine ( "Enter the names of directories or files to ignore, separated by commas:" );
                List<string> ignoreList = GetIgnoreListFromUser ( );
                PrintDirectoryStructure ( currentPath,"",ignoreList );
            }
            else if ( command == "help" )
            {
                DisplayHelp ( );
            }
            else
            {
                Console.WriteLine ( "Unknown command. Please use 'pwd', 'ls', 'cd <path>', 'print', 'help', or 'exit'." );
            }
        }

        static string ReadLineWithAutoComplete ( string currentPath )
        {
            var input = new List<char> ( );
            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey ( intercept: true );
                if ( key.Key == ConsoleKey.Tab )
                {
                    var currentInput = new string ( input.ToArray ( ) );
                    var isCdCommand = currentInput.StartsWith ( "cd " );
                    var partialPath = isCdCommand ? currentInput.Substring ( 3 ).Trim ( ) : currentInput;
                    var suggestions = GetSuggestions ( currentPath,partialPath ).ToList ( );

                    if ( suggestions.Count > 0 )
                    {
                        ClearCurrentConsoleLine ( );
                        input.Clear ( );

                        if ( isCdCommand )
                        {
                            input.AddRange ( "cd ".ToCharArray ( ) );
                        }

                        if ( suggestions.Count == 1 )
                        {
                            input.AddRange ( suggestions.First ( ).ToCharArray ( ) );
                        }
                        else
                        {
                            Console.WriteLine ( "Multiple matches found:" );
                            for ( int i = 0; i < suggestions.Count; i++ )
                            {
                                Console.WriteLine ( $"{i + 1}: {suggestions[i]}" );
                            }

                            Console.WriteLine ( "Enter the number of your choice:" );
                            int choice;
                            while ( true )
                            {
                                var choiceInput = Console.ReadLine ( );
                                if ( int.TryParse ( choiceInput,out choice ) && choice > 0 && choice <= suggestions.Count )
                                {
                                    input.AddRange ( suggestions[choice - 1].ToCharArray ( ) );
                                    break;
                                }
                                Console.WriteLine ( "Invalid choice. Please enter a valid number." );
                            }
                        }

                        Console.Write ( new string ( input.ToArray ( ) ) );
                    }
                }
                else if ( key.Key == ConsoleKey.Backspace && input.Count > 0 )
                {
                    input.RemoveAt ( input.Count - 1 );
                    Console.Write ( "\b \b" );
                }
                else if ( key.Key == ConsoleKey.UpArrow ) // Bläddra upp i historiken
                {
                    if ( commandHistoryIndex < commandHistory.Count - 1 )
                    {
                        commandHistoryIndex++;
                        ClearCurrentConsoleLine ( );
                        input.Clear ( );
                        input.AddRange ( commandHistory[commandHistory.Count - 1 - commandHistoryIndex].ToCharArray ( ) );
                        Console.Write ( new string ( input.ToArray ( ) ) );
                    }
                }
                else if ( key.Key == ConsoleKey.DownArrow ) // Bläddra ner i historiken
                {
                    if ( commandHistoryIndex > 0 )
                    {
                        commandHistoryIndex--;
                        ClearCurrentConsoleLine ( );
                        input.Clear ( );
                        input.AddRange ( commandHistory[commandHistory.Count - 1 - commandHistoryIndex].ToCharArray ( ) );
                        Console.Write ( new string ( input.ToArray ( ) ) );
                    }
                    else if ( commandHistoryIndex == 0 ) // Om vi är på det senaste kommandot
                    {
                        commandHistoryIndex--;
                        ClearCurrentConsoleLine ( );
                        input.Clear ( );
                        Console.Write ( new string ( input.ToArray ( ) ) ); // Återställ till tomt
                    }
                }
                else if ( !char.IsControl ( key.KeyChar ) )
                {
                    input.Add ( key.KeyChar );
                    Console.Write ( key.KeyChar );
                }
            } while ( key.Key != ConsoleKey.Enter );

            Console.WriteLine ( );
            string finalInput = new string ( input.ToArray ( ) );
            // Spara kommandot i historiken
            if ( !string.IsNullOrWhiteSpace ( finalInput ) )
            {
                commandHistory.Insert ( 0,finalInput );
                commandHistoryIndex = -1; // Återställ index för nästa input
            }

            return finalInput;
        }

        static IEnumerable<string> GetSuggestions ( string currentPath,string input )
        {
            var entries = Directory.GetFileSystemEntries ( currentPath )
                .Select ( Path.GetFileName )
                .Where ( name => name.StartsWith ( input,StringComparison.OrdinalIgnoreCase ) );
            return entries;
        }

        static void ClearCurrentConsoleLine ( )
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition ( 0,Console.CursorTop );
            Console.Write ( new string ( ' ',Console.WindowWidth ) );
            Console.SetCursorPosition ( 0,currentLineCursor );
        }

        static List<string> GetIgnoreListFromUser ( )
        {
            string ignoreInput = Console.ReadLine ( );
            return ignoreInput.Split ( ',' )
                .Select ( item => item.Trim ( ) )
                .Where ( item => !string.IsNullOrEmpty ( item ) )
                .ToList ( );
        }

        static void PrintDirectoryStructure ( string path,string indent,List<string> ignoreList )
        {
            foreach ( var directory in GetEntries ( path,ignoreList,true ) )
            {
                Console.WriteLine ( $"{indent}{directory}/" );
                PrintDirectoryStructure ( Path.Combine ( path,directory ),indent + "  ",ignoreList );
            }
            foreach ( var file in GetEntries ( path,ignoreList,false ) )
            {
                Console.WriteLine ( $"{indent}{file}" );
            }
        }

        static IEnumerable<string> GetEntries ( string path,List<string> ignoreList,bool getDirectories )
        {
            var entries = getDirectories ? Directory.EnumerateDirectories ( path ) : Directory.EnumerateFiles ( path );
            return entries.Select ( entry => Path.GetFileName ( entry ) )
                          .Where ( entry => !ignoreList.Contains ( entry ) );
        }
    }
}
