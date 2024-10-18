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
        private static List<string> ignoreList = new List<string> ( ); // För att spara ignore-listan

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
                              "- 'ignore' to manage ignore list\n" +
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
                ignoreList = GetIgnoreListFromUser ( ); // Spara den nya ignore-listan

                // Visa ignore-listan och fråga om det är okej att fortsätta
                DisplayIgnoreList ( );

                Console.WriteLine ( "Are you okay with this list? (yes/no)" );
                string response = Console.ReadLine ( )?.Trim ( ).ToLower ( );

                if ( response == "yes" )
                {
                    PrintDirectoryStructure ( currentPath,"",ignoreList );
                }
                else
                {
                    Console.WriteLine ( "No changes made. You can add more items to the ignore list." );
                }
            }
            else if ( command == "ignore" )
            {
                ManageIgnoreList ( );
            }
            else if ( command == "help" )
            {
                DisplayHelp ( );
            }
            else
            {
                Console.WriteLine ( "Unknown command. Please use 'pwd', 'ls', 'cd <path>', 'print', 'ignore', 'help', or 'exit'." );
            }
        }

        static void DisplayIgnoreList ( )
        {
            Console.WriteLine ( "Current ignore list:" );
            if ( ignoreList.Count == 0 )
            {
                Console.WriteLine ( "No items in ignore list." );
            }
            else
            {
                foreach ( var item in ignoreList )
                {
                    Console.WriteLine ( $"- {item}" );
                }
            }
            Console.WriteLine ( ); // För extra radbrytning
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

        static void ManageIgnoreList ( )
        {
            Console.WriteLine ( "Current ignore list:" );
            if ( ignoreList.Count == 0 )
            {
                Console.WriteLine ( "No items in ignore list." );
            }
            else
            {
                foreach ( var item in ignoreList )
                {
                    Console.WriteLine ( $"- {item}" );
                }
            }

            Console.WriteLine ( "Do you want to add new items to the ignore list? (yes/no)" );
            string response = Console.ReadLine ( )?.Trim ( ).ToLower ( );

            if ( response == "yes" )
            {
                Console.WriteLine ( "Enter the names of directories or files to ignore, separated by commas:" );
                var newIgnoreList = GetIgnoreListFromUser ( );
                ignoreList.AddRange ( newIgnoreList );
                ignoreList = ignoreList.Distinct ( ).ToList ( ); // Ta bort duplicerade poster
            }
            else
            {
                Console.WriteLine ( "No changes made to the ignore list." );
            }
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
            Console.SetCursorPosition ( 0,currentLineCursor );
            Console.Write ( new string ( ' ',Console.WindowWidth ) );
            Console.SetCursorPosition ( 0,currentLineCursor );
        }

        static List<string> GetIgnoreListFromUser ( )
        {
            var input = Console.ReadLine ( );
            return input.Split ( new[] { ',' },StringSplitOptions.RemoveEmptyEntries )
                        .Select ( item => item.Trim ( ) )
                        .ToList ( );
        }

        static void PrintDirectoryStructure ( string currentPath,string indent,List<string> ignoreList )
        {
            try
            {
                foreach ( var directory in Directory.GetDirectories ( currentPath ) )
                {
                    string dirName = Path.GetFileName ( directory );
                    if ( !ignoreList.Contains ( dirName ) )
                    {
                        Console.WriteLine ( $"{indent}- {dirName}" );
                        PrintDirectoryStructure ( directory,indent + "  ",ignoreList );
                    }
                }

                foreach ( var file in Directory.GetFiles ( currentPath ) )
                {
                    string fileName = Path.GetFileName ( file );
                    if ( !ignoreList.Contains ( fileName ) )
                    {
                        Console.WriteLine ( $"{indent}- {fileName}" );
                    }
                }
            }
            catch ( UnauthorizedAccessException )
            {
                Console.WriteLine ( $"{indent}Access denied to {currentPath}" );
            }
        }
    }
}
