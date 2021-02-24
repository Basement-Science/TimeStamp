using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using Pastel;

namespace TimeStamp {
    class Program {
        static void Main(string[] args) {
            Console.CancelKeyPress += new ConsoleCancelEventHandler(ExitHandler);

            bool TimeStampNewlines = false;
            bool UseLocalTimeZone = false;
            bool FullColor = false;
            bool NoColor = false;

            string[] OptionStrings_TimeStampNewlines = { "/n", "-n", "--stampNewLines" };
            string[] OptionStrings_LocalTimeZone = { "/l", "-l", "--localTime" };
            string[] OptionStrings_FullColor = { "/c", "-c", "--fullColor" };
            string[] OptionStrings_NoColor = { "/nc", "-nc", "--noColor" };

            // handle parameters
            foreach ( string arg in args ) {
                string argUpper = arg.ToUpper(CultureInfo.InvariantCulture);
                if ( ProcessCMDoption_bool(OptionStrings_TimeStampNewlines, argUpper) ) {
                    TimeStampNewlines = true;
                } else if ( ProcessCMDoption_bool(OptionStrings_LocalTimeZone, argUpper) ) {
                    UseLocalTimeZone = true;
                } else if ( ProcessCMDoption_bool(OptionStrings_FullColor, argUpper) ) {
                    FullColor = true;
                } else if ( ProcessCMDoption_bool(OptionStrings_NoColor, argUpper) ) {
                    NoColor = true;
                } else {
                    printHelpScreen();
                    CleanUp();
                    return;
                }
            }

            //string test1 = "test1\n";
            //string test2 = "test2";
            //Console.Write(test1.Pastel("#FF7800") + " extra");
            //Console.WriteLine(test2.Pastel("#FF3500") + " extra");
            //Console.WriteLine("not very colored?");


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

            /* Prints the Timestamp in UTC or local time, plus another string after it. */
            void PrintTimeStamp(string postFix) {
                DateTime time = UseLocalTimeZone ? DateTime.Now : DateTime.UtcNow;
                string[] stamp = time.ToString("u", System.Globalization.CultureInfo.InvariantCulture).TrimEnd('Z').Split(' ', 2);
                stamp[0] += " ";
                string end = UseLocalTimeZone ? "" : (FullColor ? "Z".Pastel("#808080") : "Z");
                StringBuilder builder;
                string output;

                Console.Write('[');
                if ( FullColor ) {
                    builder = new StringBuilder(stamp[0].Pastel("#FF7800")).Append(stamp[1].Pastel("#FF3500")).Append(end);
                    output = builder.ToString().PastelBg(Color.Black);
                    Console.Write(output);
                } else {
                    builder = new StringBuilder(stamp[0]).Append(stamp[1]).Append(end);
                    output = builder.ToString();
                    if ( ! NoColor ) {
                        // Use oldschool color
                        ConsoleColor oldBackground = Console.BackgroundColor;
                        Console.BackgroundColor = ConsoleColor.Black;
                        ConsoleColor oldForeground = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.Write(output);
                        Console.ForegroundColor = oldForeground;
                        Console.BackgroundColor = oldBackground;
                    } else {
                        // No color
                        Console.Write(output);
                    }
                }
                Console.Write(']' + postFix);
            }

            /* Compares argLower (a lowercase string) to all elements of the Array OptionStrings. 
             * If a match is found, returns true, else returns false.
             */
            static bool ProcessCMDoption_bool(string[] OptionStrings, string argLower) {
                foreach ( string possibleArg in OptionStrings ) {
                    if ( argLower == possibleArg.ToUpper(CultureInfo.InvariantCulture) ) {
                        return true;
                    }
                }
                return false;
            }

            void printHelpScreen() {
                string ProgramName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
                Console.WriteLine(ProgramName + " utility - Adds a Timestamp to the beginning of each line from the pipe |");

                // Prepare to print the help for option switches
                List<StringBuilder> SwitchesHelp = new List<StringBuilder> {
                        ConcatSwitchesHelp(OptionStrings_TimeStampNewlines),
                        ConcatSwitchesHelp(OptionStrings_LocalTimeZone),
                        ConcatSwitchesHelp(OptionStrings_FullColor)
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
                SwitchesHelp[0].Append("Will print a Timestamp with no text after it whenever an empty line arrives.");
                SwitchesHelp[1].Append("Will use your local Timezone to generate a Timestamp instead of UTC. \n" +
                    "Warning: Local timezones may be subject to adjustments due to \"Daylight Savings\" Time or similar.");
                SwitchesHelp[2].Append("Enable full ANSI code color support. May not be compatible with all other console applications, " +
                    "especially when piping " + ProgramName + "'s output into other applications. \n");

                // Actually print SwitchesHelp. 
                Console.WriteLine("Options:");
                foreach ( var HelpText in SwitchesHelp ) {
                    Console.WriteLine(HelpText);
                }

                // Print Example Usage
                Console.WriteLine("");
                string exampleCommand = "ping -n 3 localhost | ";
                string TS_exampleSwitches = " -c";
                Console.WriteLine("Example usage: '" + exampleCommand + System.Diagnostics.Process.GetCurrentProcess().ProcessName
                    + TS_exampleSwitches + "' ");
                Console.WriteLine("Running example ...");
                // actually run the example command
                Process p = new Process();
                p.StartInfo.FileName = "cmd";
                p.StartInfo.Arguments = "/C " + exampleCommand +
                    "\"" + System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName + "\"" + TS_exampleSwitches;
                p.Start();
                p.WaitForExit();
                p.Close();
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
            CleanUp();
        } // END MAIN()

        private static void CleanUp() {
            //Console.ResetColor();
        }

        protected static void ExitHandler(object sender, ConsoleCancelEventArgs e) {
            CleanUp();
            e.Cancel = true;
        }
    }
}
