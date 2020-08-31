using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace TimeStamp {
    class Program {
        static void Main(string[] args) {
            Console.CancelKeyPress += new ConsoleCancelEventHandler(ExitHandler);

            bool TimeStampNewlines = false;
            bool UseLocalTimeZone = false;

            string[] OptionStrings_TimeStampNewlines = { "/n", "-n", "--stampNewLines" };
            string[] OptionStrings_LocalTimeZone = { "/l", "-l", "--localTime" };

            // handle parameters
            foreach ( string arg in args ) {
                string argLower = arg.ToUpper(CultureInfo.InvariantCulture);
                if ( ProcessCMDoption_bool(OptionStrings_TimeStampNewlines, argLower) ) {
                    TimeStampNewlines = true;
                } else if ( ProcessCMDoption_bool(OptionStrings_LocalTimeZone, argLower) ) {
                    UseLocalTimeZone = true;
                } else {
                    // print help
                    string ProgramName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
                    Console.WriteLine(ProgramName + " utility - Adds a Timestamp to the beginning of each line from the pipe |");

                    // Prepare to print the help for option switches
                    List<StringBuilder> SwitchesHelp = new List<StringBuilder> {
                        ConcatSwitchesHelp(OptionStrings_TimeStampNewlines),
                        ConcatSwitchesHelp(OptionStrings_LocalTimeZone)
                    };

                    // calculate maximum length of switches
                    int max = 0;
                    foreach ( var switches in SwitchesHelp ) {
                        max = switches.Length > max ? switches.Length : max;
                    }
                    foreach ( var switches in SwitchesHelp ) {
                        string temp = new string(' ', max - switches.Length + 3);
                        switches.Append(temp);
                    }
                    SwitchesHelp[0].Append("Will print a Timestamp with no text after it whenever an empt line arrives.");
                    SwitchesHelp[1].Append("Will use your local Timezone to generate a Timestamp instead of UTC. \n" +
                        "Warning: Local timezones may be subject to adjustments due to \"Daylight Savings\" Time or similar.");

                    Console.WriteLine("Options:");
                    foreach ( var HelpText in SwitchesHelp ) {
                        Console.WriteLine(HelpText);
                    }

                    // Print Example Usage
                    Console.WriteLine("");
                    string exampleCommand = "ping -n 3 localhost | ";
                    Console.WriteLine("Example usage: '" + exampleCommand + System.Diagnostics.Process.GetCurrentProcess().ProcessName + "' ");
                    Console.WriteLine("Running example ...");
                    // actually run the example command
                    Process p = new Process();
                    p.StartInfo.FileName = "cmd";
                    p.StartInfo.Arguments = "/C " + exampleCommand +
                        "\"" + System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName + "\"";
                    p.Start();
                    p.WaitForExit();
                    p.Close();
                    return;
                }

            }

            // handle the input
            if ( Console.IsInputRedirected ) {
                // Program was called with a pipe input
                string s;
                while ( (s = Console.ReadLine()) != null ) {
                    if ( TimeStampNewlines || s.Length != 0 ) {
                        PrintTimeStamp(" - ");
                    }
                    Console.WriteLine(s);
                }
            } else {
                // Program was called directly
                PrintTimeStamp("\n");
            }

            /* Prints the Timestamp in UTC or local time, plus another string after it.
             * Does not add a newline.
             */
            void PrintTimeStamp(string postFix) {
                if ( UseLocalTimeZone ) {
                    Console.Write(DateTime.Now.ToString("u", System.Globalization.CultureInfo.InvariantCulture) + postFix);
                } else {
                    Console.Write(DateTime.UtcNow.ToString("u", System.Globalization.CultureInfo.InvariantCulture) + postFix);
                }
            }

            /* Concats the strings from an array EXCEPT the first one, separated by a ", "
             * Returns a StringBuilder to allow further efficient Concatenations
             */
            static StringBuilder ConcatSwitchesHelp(string[] OptionStrings) {
                StringBuilder optionHelp = new StringBuilder("");
                for ( int i = 1; i < OptionStrings.Length; i++ ) {
                    if ( i > 1 ) {
                        optionHelp.Append(", ");
                    }
                    optionHelp.Append(OptionStrings[i]);
                }
                return optionHelp;
            }
        }

        protected static void ExitHandler(object sender, ConsoleCancelEventArgs e) {
            e.Cancel = true;
        }

        /* Compares argLower (a lowercase string) to all elements of the Array OptionStrings. 
         * If a match is found, returns true, else returns false.
         */
        private static bool ProcessCMDoption_bool(string[] OptionStrings, string argLower) {
            foreach ( string possibleArg in OptionStrings ) {
                if ( argLower == possibleArg.ToUpper(CultureInfo.InvariantCulture) ) {
                    return true;
                }
            }
            return false;
        }
    }
}
