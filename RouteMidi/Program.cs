using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
//using System.Threading.Tasks;
//using Sanford.Multimedia;
using Sanford.Multimedia.Midi;
using System.IO;

namespace RouteMidi
{
    class Program
    {
        //const int MINMIDIPORT = 1025;
        //const uint MAXMIDIPORTS = 64;

        //static private InputMidi[] im = new InputMidi[MAXMIDIPORTS];
        //static private OutputMidi[] om = new OutputMidi[MAXMIDIPORTS];

        //static private Route[] MidiRoutes = new Route[MAXMIDIPORTS];

        //static private List<int> outMidiList = new List<int>();
        //static private List<int> inMidiList = new List<int>();

        //static private SynchronizationContext context;

        //// UDP Stuff
        //static UdpMidiPortList umpl = new UdpMidiPortList();

        static Routes midiRoutes = new Routes();

        // Global stuff
        static string CurrentConfigurationName = "default";
        static private bool debug = false;
        static MidiConfig mc = new MidiConfig(CurrentConfigurationName);
        //private static bool dirty = false;

        static void Main(string[] args)
        {
            if (InputDevice.DeviceCount == 0 && !debug)
            {
                Console.WriteLine("No MIDI input devices available");
            }
            else
            {
                midiRoutes.GetMidiInfo();
                Console.WriteLine("Route Midi initialized.");
                ListMidiInfo();

                if (ParseCommandLine(args))
                {
                    mc.Load();
                    mc.ReadRouteConfigDefault(midiRoutes);

                    if (midiRoutes.InitMidiPorts())
                    {
                        midiRoutes.StartAllRecording();

                        ProcessCommands();

                        midiRoutes.Shutdown();
                    }
                    else
                    {
                        Console.WriteLine("Error initalizing midi ports");
                    }
                }
                else
                {
                    Console.WriteLine("Exiting due to command line syntax errors.");
                }
            }
        }

        private static void ProcessCommands()
        {
            ConsoleKeyInfo cki;
            bool StillRunning = true;

            do
            {
                Console.WriteLine();
                Console.Write("RouteMidi>> ");
                cki = Console.ReadKey();
                Console.WriteLine();
                Console.WriteLine();

                switch (cki.Key)
                {
                    case ConsoleKey.B:
                        ListMidiInfo();
                        break;
                    case ConsoleKey.I:
                        midiRoutes.DisplayInputMidiPorts();
                        break;
                    case ConsoleKey.O:
                        midiRoutes.DisplayOutputMidiPorts();
                        break;
                    case ConsoleKey.R:
                        midiRoutes.PrintRoutes();
                        break;
                    case ConsoleKey.A:
                        ManualAddRoute();
                        break;
                    case ConsoleKey.D:
                        ManualDeleteRoute();
                        break;
                    case ConsoleKey.S:
                        SaveConfig();
                        break;
                    case ConsoleKey.L:
                        LoadConfig();
                        break;
                    case ConsoleKey.N:
                        NewConfgiruation();
                        break;
                    case ConsoleKey.P:
                        PickConfiguration();
                        break;
                    case ConsoleKey.C:
                        mc.DisplayConfigurations();
                        break;
                    case ConsoleKey.U:
                        UpdateConfigurationName();
                        break;
                    case ConsoleKey.Q:
                        StillRunning = false;
                        Console.WriteLine("Exiting");
                        break;
                    case ConsoleKey.M:
                        SwapDebugMode();
                        break;
                    default:
                        switch(cki.KeyChar)
                        {
                        case '?':
                            ShowHelp();
                            break;
                        }
                        Console.WriteLine("Not a valid command.");
                        break;
                }
            } while (StillRunning);
        }

        private static void SwapDebugMode()
        {
            if (midiRoutes.Debug)
            {
                Console.WriteLine("Monitor debug mode off");
                midiRoutes.Debug = false;
            }
            else
            {
                Console.WriteLine("Monitor debug mode on");
                midiRoutes.Debug = true;
            }

//            umpl.Debug(debug);
        }

        private static void UpdateConfigurationName()
        {
            Console.WriteLine("Current name is {0}", CurrentConfigurationName);
            Console.Write("New name: ");
            mc.SetDefault(Console.ReadLine());
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Commands");
            Console.WriteLine("Midi Info:");
            Console.WriteLine(" I - Show Input Routes");
            Console.WriteLine(" O - Show Output Routes");
            Console.WriteLine(" B - Show Both In/Out Routes");
            Console.WriteLine("Routes:");
            Console.WriteLine(" A - Add Route");
            Console.WriteLine(" D - Delete Route");
            Console.WriteLine(" R - Show Route");
            Console.WriteLine("Configurations:");
            Console.WriteLine(" C - Show Configurations");
            Console.WriteLine(" N - New Configuration");
            Console.WriteLine(" P - Pick Configuration");
            Console.WriteLine(" S - Save all Configurations");
            Console.WriteLine(" L - Load all Configuration");
            Console.WriteLine(" U - Update Configuraiton Name");
            Console.WriteLine("Other:");
            Console.WriteLine(" M - Monitor Messages");
        }

        private static void DisplayConfigurations()
        {
            mc.DisplayConfigurations();
        }

        private static void PickConfiguration()
        {
            mc.DisplayConfigurations(); ;
            Console.Write("Pick which configuration: ");
            string in_str = Console.ReadLine();
            int confignum;

            if (!int.TryParse(in_str, out confignum) || confignum < 0 || !mc.ValidateConfigurationNumber(confignum))
            {
                Console.WriteLine("Port needs to be an integer and in the valid range of routes above.");
                return;
            }

            mc.ReadRouteConfigByNumber(confignum, midiRoutes);
        }

        private static void NewConfgiruation()
        {
            if(midiRoutes.IsDirty)
            {
                mc.WriteRouteConfig(CurrentConfigurationName, midiRoutes);
                midiRoutes.IsDirty = false;
            }

            Console.Write("Configuraiton Name: ");
            string in_str = Console.ReadLine();
            if (!mc.ConfigExists(in_str))
            {
                CurrentConfigurationName = in_str;
                midiRoutes.ResetRoutes();

                mc.WriteRouteConfig(in_str, midiRoutes);
            }
            else
            {
                Console.WriteLine("{0} is already defined, use another name.", in_str);
            }         
        }

        private static void SaveConfig()
        {
            if(mc.GetDefault() != "")
            {
                mc.WriteRouteConfig(CurrentConfigurationName, midiRoutes);
                mc.Save();
                midiRoutes.IsDirty = false;
            }
            else
            {
                Console.WriteLine("No configuraiton name defined.");
            }
        }

        private static void LoadConfig()
        {
            mc.Load();
            mc.ReadRouteConfigDefault(midiRoutes);
        }

        private static void ListMidiInfo()
        {
            midiRoutes.DisplayInputMidiPorts();
            Console.WriteLine();
            midiRoutes.DisplayOutputMidiPorts();
            Console.WriteLine();
        }

        private static void ManualDeleteRoute()
        {
            midiRoutes.PrintRoutes();
            Console.Write("Delete which route: ");
            string in_str = Console.ReadLine();

            int routenum;


            if(!int.TryParse(in_str, out routenum) || !midiRoutes.RemoveRoutes(routenum))
            {
                Console.WriteLine("Port needs to be an integer and in the valid range of routes above.");
            }
        }

        static private void ManualAddRoute()
        {
            Console.Write("Inport: ");
            string in_str = Console.ReadLine();
            Console.Write("Outport: ");
            string out_str = Console.ReadLine();

            int inport, outport;

            if(!int.TryParse(in_str, out inport))
            {
                Console.WriteLine("Inport needs to be an integer");
                return;
            }

            if (!int.TryParse(out_str, out outport))
            {
                Console.WriteLine("Inport needs to be an integer");
                return;
            }

            if (!midiRoutes.ValidateMidiInPort(inport))
            {
                Console.WriteLine("Not a valid in midi port");
                return;
            }

            if (!midiRoutes.ValidateMidiOutPort(outport))
            {
                Console.WriteLine("Not a valid out midi port");
                return;
            }

            midiRoutes.AddRoute(inport, outport);
        }

        private static bool ParseCommandLine(string[] args)
        {
            // A1-2 S3-4 S5-10
            foreach (string arg in args)
            {
                if (arg[0] == 'u' || arg[0] == 'U')
                {
                    string OutPortStr = arg.Substring(1);

                    int OutPort;


                    if (!int.TryParse(OutPortStr, out OutPort))
                    {
                        Console.WriteLine("Unable to convert source port " + OutPortStr);
                    }

                    midiRoutes.AddRoute(9000, OutPort);
                }
                else if(arg[0] == 'd' || arg[0] == 'D')
                {
                    Console.WriteLine("Debug mode enabled.");
                    debug = true;
                }
                else if(arg[0] == '!')
                {
                    // File is included afterwards, just use that as the args.
                    string file = arg.Substring(1);
                    if(!midiRoutes.ParseFile(file))
                    {
                        Console.WriteLine("Error loading argument file.");
                        return false;
                    }
                }
                else
                {
                    bool AllMessages;
                    if (arg[0] == 'a' || arg[0] == 'A')
                    {
                        AllMessages = true;
                    }
                    else if (arg[0] == 's' || arg[0] == 'S')
                    {
                        AllMessages = false;
                    }
                    else
                    {
                        Console.WriteLine("Error: First letter of each argumnet must be A (for ALL  messages) or S (for Sync messages)");
                        return false;
                    }

                    int sepindex = arg.IndexOf('-');
                    if (sepindex < 0)
                    {
                        Console.WriteLine("Error: Expect Port#-Port# syntax.");
                    }

                    string InPortStr = arg.Substring(1, sepindex - 1);
                    string OutPortStr = arg.Substring(sepindex + 1);

                    int InPort, OutPort;


                    if (!int.TryParse(InPortStr, out InPort))
                    {
                        Console.WriteLine("Unable to convert source port " + InPortStr);
                    }

                    if (!int.TryParse(OutPortStr, out OutPort))
                    {
                        Console.WriteLine("Unable to convert destination port " + OutPortStr);
                    }

                    midiRoutes.AddRoute(InPort, OutPort);
                }
            }

            return true;
        }

    }
}
