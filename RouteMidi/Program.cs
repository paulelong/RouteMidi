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
        const int MINMIDIPORT = 1025;
        const uint MAXMIDIPORTS = 64;

        static private InputMidi[] im = new InputMidi[MAXMIDIPORTS];
        static private OutputMidi[] om = new OutputMidi[MAXMIDIPORTS];

        static private Routes[] MidiRoutes = new Routes[MAXMIDIPORTS];

        static private List<int> outMidiList = new List<int>();
        static private List<int> inMidiList = new List<int>();

        static private SynchronizationContext context;

        // UDP Stuff
        static UdpMidiPortList umpl = new UdpMidiPortList();

 //       private static int UDPInPort = 9000;

        // Global stuff
        static private bool debug = false;
        static MidiConfig mc = new MidiConfig();
        static string CurrentConfigurationName = "default";
        private static bool dirty = false;

        static void Main(string[] args)
        {
            if (InputDevice.DeviceCount == 0 && !debug)
            {
                Console.WriteLine("No MIDI input devices available");
            }
            else
            {
                GetMidiInfo();
                Console.WriteLine("Route Midi initialized.");
                ListMidiInfo();

                if (ParseCommandLine(args))
                {
                    mc.Load();
                    mc.ReadRouteConfigDefault(MidiRoutes, im, om);

                    if (InitMidiPorts())
                    {
                        StartAllRecording();

                        ProcessCommands();

                        Console.WriteLine("Stopping Midi...");

                        StopAllRecording();

                        umpl.StopListeners();

                        Console.WriteLine("Midi Stopped ...");

                        CloseAllMidiPorts();
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
                        InputMidi.PrintMidiList(im);
                        break;
                    case ConsoleKey.O:
                        OutputMidi.PrintMidiList(om);
                        break;
                    case ConsoleKey.R:
                        PrintRoutes();
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
                        DisplayConfigurations();
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
            if (debug)
            {
                Console.WriteLine("Monitor debug mode off");
                debug = false;
            }
            else
            {
                Console.WriteLine("Monitor debug mode on");
                debug = true;
            }
        }

        private static void UpdateConfigurationName()
        {
            Console.WriteLine("Current name is {0}", CurrentConfigurationName);
            Console.Write("New name: ");
            CurrentConfigurationName = Console.ReadLine();
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
            DisplayConfigurations();
            Console.Write("Pick which configuration: ");
            string in_str = Console.ReadLine();
            int confignum;

            if (!int.TryParse(in_str, out confignum) || confignum < 0 || confignum >= MidiRoutes.Length)
            {
                Console.WriteLine("Port needs to be an integer and in the valid range of routes above.");
                return;
            }

            mc.ReadRouteConfigByNumber(confignum, MidiRoutes, im, om);
        }

        private static void NewConfgiruation()
        {
            if(dirty)
            {
                mc.WriteRouteConfig(CurrentConfigurationName, MidiRoutes, im, om);
                dirty = false;
            }

            Console.Write("Configuraiton Name: ");
            string in_str = Console.ReadLine();
            if (!mc.ConfigExists(in_str))
            {
                CurrentConfigurationName = in_str;
                foreach(Routes r in MidiRoutes)
                {
                    if(r != null)
                    {
                        r.RemoveRoutes();
                    }
                }

                mc.WriteRouteConfig(in_str, MidiRoutes, im, om);
            }
            else
            {
                Console.WriteLine("{0} is already defined, use another name.", in_str);
            }         
        }

        private static void SaveConfig()
        {
            if(CurrentConfigurationName != "")
            {
                mc.WriteRouteConfig(CurrentConfigurationName, MidiRoutes, im, om);
                mc.Save();
                dirty = false;
            }
            else
            {
                Console.WriteLine("No configuraiton name defined.");
            }
        }

        private static void LoadConfig()
        {
            mc.Load();
            mc.ReadDefaultConfig(MidiRoutes, im, om);
        }

        private static void ListMidiInfo()
        {
            InputMidi.PrintMidiList(im);
            Console.WriteLine();
            OutputMidi.PrintMidiList(om);
            Console.WriteLine();
        }

        private static void ManualDeleteRoute()
        {
            PrintRoutes();
            Console.Write("Delete which route: ");
            string in_str = Console.ReadLine();

            int routenum;

            if (!int.TryParse(in_str, out routenum) || routenum < 0 || routenum >= MidiRoutes.Length)
            {
                Console.WriteLine("Port needs to be an integer and in the valid range of routes above.");
                return;
            }

            MidiRoutes[routenum].RemoveRoutes();
        }

        private static void CloseAllMidiPorts()
        {
            foreach (int i in inMidiList)
            {
                im[i].Close();
            }

            foreach (int i in outMidiList)
            {
                om[i].Close();
            }
        }

        private static void StopAllRecording()
        {
            foreach (int i in inMidiList)
            {
                im[i].StopRecording();
            }
        }

        private static void StartAllRecording()
        {
            foreach (int i in inMidiList)
            {
                im[i].StartRecording();
            }
        }

        private static bool InitMidiPorts()
        {
            try
            {
                context = SynchronizationContext.Current;
                /*
                //foreach (int i in inMidiList)
                for(int i = 0; i < InputMidi.Count; i++)
                {
                    im[i].InitInputDevice();
                    //if (MidiRoutes[i].AllRoutes)
                    {
                        im[i].inMIDI.SysCommonMessageReceived += HandleSysCommonMessageReceived;
                        im[i].inMIDI.ChannelMessageReceived += HandleChannelMessageReceived;
                        im[i].inMIDI.SysExMessageReceived += HandleSysExMessageReceived;
                        im[i].inMIDI.SysRealtimeMessageReceived += HandleSysRealtimeMessageReceived;
                        im[i].inMIDI.Error += new EventHandler<Sanford.Multimedia.ErrorEventArgs>(inDevice_Error);
                    }
                }

                //                foreach (int i in outMidiList)
                for (int i = 0; i < OutputMidi.Count; i++)
                {
                    om[i].InitOutputDevice();
                }
                */
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return false;
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

            if(inport >= InputMidi.Count && inport < MINMIDIPORT)
            {
                Console.WriteLine("No a valid in midi port");
                return;
            }

            if (outport >= OutputMidi.Count)
            {
                Console.WriteLine("No a valid out midi port");
                return;
            }

            AddRoute(inport, outport);
        }

        private static void AddRoute(int inport, int outport)
        {
            if (!outMidiList.Contains(outport))
            {
                outMidiList.Add(outport);
                om[outport].InitOutputDevice();
            }

            if (inport > MINMIDIPORT)
            {
                MidiOutCaps caps = OutputDevice.GetDeviceCapabilities(outport);                

                umpl.Add(inport, om[outport]);
            }
            else
            {
                if (!inMidiList.Contains(inport))
                {
                    inMidiList.Add(inport);
                    im[inport].InitInputDevice();
                    //if (MidiRoutes[i].AllRoutes)
                    {
                        im[inport].inMIDI.SysCommonMessageReceived += HandleSysCommonMessageReceived;
                        im[inport].inMIDI.ChannelMessageReceived += HandleChannelMessageReceived;
                        im[inport].inMIDI.SysExMessageReceived += HandleSysExMessageReceived;
                        im[inport].inMIDI.SysRealtimeMessageReceived += HandleSysRealtimeMessageReceived;
                        im[inport].inMIDI.Error += new EventHandler<Sanford.Multimedia.ErrorEventArgs>(inDevice_Error);
                    }
                }

                if (MidiRoutes[inport] == null)
                {
                    MidiRoutes[inport] = new Routes();
                }
                MidiRoutes[inport].AddRoute(outport, true);
            }

            dirty = true;
        }

        private static void PrintRoutes()
        {
            if(inMidiList.Count <= 0 && umpl.Count <= 0)
            {
                Console.WriteLine("No routes defined, use A to add a route.");
                return;
            }

            if(umpl.Count > 0)
            {
                umpl.ListRoutes();
            }

            foreach(int i in inMidiList)
            {
                List<int> outs = MidiRoutes[i].GetRoutes();
                if(outs.Count > 0)
                {
                    Console.Write(i.ToString() + " - " + im[i].Name + " -> ");
                    foreach(int o in outs)
                    {
                        Console.Write(om[o].Name + ",");
                    }
                    Console.WriteLine();
                }
            }
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

                    AddRoute(-1, OutPort);
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
                    if(!ParseFile(file))
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

                    AddRoute(InPort, OutPort);
                }
            }

            return true;
        }

        private static bool ParseFile(string file)
        {
            if(!File.Exists(file))
            {
                Console.WriteLine("Couldn't find input file " + file);
                return false;
            }

            using (StreamReader fs = File.OpenText(file))
            {
                while(fs.Peek() >= 0)
                {
                    string line = fs.ReadLine();
                    int n = line.IndexOf(',');
                    if(n > 0)
                    {
                        string leftside = line.Substring(0, n);
                        string rightside = line.Substring(n + 1);

                        if(leftside[0] == '#')
                        {
                            int OutPort = OutputMidi.MapName(om, rightside);
                            if (OutPort == -1 || OutPort >= om.Length)
                            {
                                Console.Write("Outport for UDP " + OutPort.ToString() + " is invalid for ");
                            }
                            else
                            {
                                int UdpPort;
                                if (!int.TryParse(leftside.Substring(1), out UdpPort))
                                {
                                    Console.WriteLine("Unable to convert destination port " + leftside.Substring(1));
                                }
                                else
                                {
                                    if(UdpPort > MAXMIDIPORTS)
                                    {
                                        AddRoute(UdpPort, OutPort);
                                    }
                                    else
                                    {
                                        Console.WriteLine("UDP Port must be greater than {0}", MAXMIDIPORTS);
                                    }
                                };
                            }
                        }
                        else
                        {
                            int InPort = InputMidi.MapName(im, leftside);
                            int OutPort = OutputMidi.MapName(om, rightside);

                            if(InPort == -1 || InPort >= im.Length || OutPort == -1 || OutPort >= om.Length)
                            {
                                Console.Write("Port range, " + InPort.ToString() + " to " + OutPort.ToString() + " is invalid for ");
                            }
                            else
                            {
                                Console.Write("Port range, " + InPort.ToString() + " to " + OutPort.ToString() + " router for ");

                                if (!inMidiList.Contains(InPort))
                                {
                                    inMidiList.Add(InPort);
                                }

                                if (!outMidiList.Contains(OutPort))
                                {
                                    outMidiList.Add(OutPort);
                                }

                                if (MidiRoutes[InPort] == null)
                                {
                                    MidiRoutes[InPort] = new Routes();
                                }
                                MidiRoutes[InPort].AddRoute(OutPort, true);
                            }
                        }
                    }
                }
            }

            return true;
        }

        private static void GetMidiInfo()
        {
            for (int i = 0; i < InputDevice.DeviceCount; i++)
            {
                MidiInCaps caps = InputDevice.GetDeviceCapabilities(i);
                im[i] = new InputMidi(caps.name, i);
            }

            for (int i = 0; i < OutputDevice.DeviceCount; i++)
            {
                MidiOutCaps caps = OutputDevice.GetDeviceCapabilities(i);
                om[i] = new OutputMidi(caps.name, i);
            }
        }

        // Event handlers for Midi messages

        /// <summary>
        /// Event Handlers
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        static private void inDevice_Error(object sender, Sanford.Multimedia.ErrorEventArgs e)
        {
            Console.WriteLine("ERROR! " + e.Error.Message);
        }

        static private void HandleSysCommonMessageReceived(object sender, SysCommonMessageEventArgs e)
        {
            InputDevice id = (InputDevice)sender;
            MidiRoutes[id.DeviceID].SendMessages(om, e.Message);

            if(debug)
            {
                Console.WriteLine("SysCom: " + e.Message.Message + " " + e.Message.Data1 + " " + e.Message.Data2);
            }
        }

        static private void HandleChannelMessageReceived(object sender, ChannelMessageEventArgs e)
        {
            InputDevice id = (InputDevice)sender;
            MidiRoutes[id.DeviceID].SendMessages(om, e.Message);
            if (debug)
            {
                Console.WriteLine("Channel: " + e.Message.Message + " c:" + e.Message.Command + " d1:" + e.Message.Data1 + " d2:" + e.Message.Data2 + " mc:" + e.Message.MidiChannel + " mt:" + e.Message.MessageType);
            }
        }

        static private void HandleSysExMessageReceived(object sender, SysExMessageEventArgs e)
        {
            InputDevice id = (InputDevice)sender;
            MidiRoutes[id.DeviceID].SendMessages(om, e.Message);
            if (debug)
            {
                Console.Write("SysEx: " + e.Message.SysExType + " ");
                for(int i = 0; i < e.Message.Length; i++)
                {
                    Console.Write("{0:X} ", e.Message[i]);
                }
                Console.WriteLine();
            }
        }

        static private void HandleSysRealtimeMessageReceived(object sender, SysRealtimeMessageEventArgs e)
        {
            InputDevice id = (InputDevice)sender;
            MidiRoutes[id.DeviceID].SendMessages(om, e.Message);
            if (debug)
            {
                Console.WriteLine("SysRealTime: " + e.Message.Message + " " + e.Message.SysRealtimeType + " " + e.Message.MessageType);
            }
        }
    }
}
