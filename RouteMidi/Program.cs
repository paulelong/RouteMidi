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
        const uint MAXMIDIPORTS = 64;

        static private InputMidi[] im = new InputMidi[MAXMIDIPORTS];
        static private OutputMidi[] om = new OutputMidi[MAXMIDIPORTS];

        static private OutputMidi OutputUdp2Midi;

        static private Routes[] MidiRoutes = new Routes[MAXMIDIPORTS];

        static private List<int> outMidiList = new List<int>();
        static private List<int> inMidiList = new List<int>();

        static private SynchronizationContext context;

        // UDP Stuff
        static UdpClient ipv4_listener;
        static UdpClient ipv6_listener;
        static AutoResetEvent waitHandle = new AutoResetEvent(false);

        // Global stuff
        static private bool debug = false;
        private static int UDPInPort = 9000;
        static MidiConfig mc = new MidiConfig();

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
                    ipv4_listener = new UdpClient(UDPInPort, AddressFamily.InterNetwork);
                    ipv6_listener = new UdpClient(UDPInPort, AddressFamily.InterNetworkV6);

                    if (InitMidiPorts())
                    {
                        Thread ipv4_thread = new Thread(new ThreadStart(ipv4_udpListener));
                        Thread ipv6_thread = new Thread(new ThreadStart(ipv6_udpListener));

                        ipv4_thread.Start();
                        ipv6_thread.Start();

                        StartAllRecording();

                        ProcessCommands();

                        Console.WriteLine("Stopping Midi...");

                        StopAllRecording();

                        Console.WriteLine("Midi Stopped ...");

                        OutputUdp2Midi.Close();
                        CloseAllMidiPorts();

                        ipv4_thread.Abort();
                        ipv6_thread.Abort();

                        ipv4_listener.Close();
                        ipv6_listener.Close();
                        // http://stackoverflow.com/questions/1764898/how-do-i-safely-stop-a-c-sharp-net-thread-running-in-a-windows-service
                        // http://stackoverflow.com/questions/14860613/how-to-use-asynchronous-receive-for-udpclient-in-a-loop
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
                    case ConsoleKey.Q:
                        StillRunning = false;
                        Console.WriteLine("Exiting");
                        break;
                    default:
                        Console.WriteLine("Not a valid command.");
                        break;
                }
            } while (StillRunning);
        }

        private static void SaveConfig()
        {
            mc.WriteRouteConfig(MidiRoutes, im, om);
            mc.Save();
        }

        private static void LoadConfig()
        {
            mc.Load();
            mc.ReadRouteConfig(MidiRoutes, im, om);
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

                foreach (int i in inMidiList)
                {
                    im[i].InitInputDevice();
                    if (MidiRoutes[i].AllRoutes)
                    {
                        im[i].inMIDI.SysCommonMessageReceived += HandleSysCommonMessageReceived;
                        im[i].inMIDI.ChannelMessageReceived += HandleChannelMessageReceived;
                        im[i].inMIDI.SysExMessageReceived += HandleSysExMessageReceived;
                        im[i].inMIDI.SysRealtimeMessageReceived += HandleSysRealtimeMessageReceived;
                        im[i].inMIDI.Error += new EventHandler<Sanford.Multimedia.ErrorEventArgs>(inDevice_Error);
                    }
                }

                foreach (int i in outMidiList)
                {
                    om[i].InitOutputDevice();
                }
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

            if (!inMidiList.Contains(inport))
            {
                inMidiList.Add(inport);
            }

            if (!outMidiList.Contains(outport))
            {
                outMidiList.Add(outport);
            }

            if (MidiRoutes[inport] == null)
            {
                MidiRoutes[inport] = new Routes();
            }
            MidiRoutes[inport].AddRoute(outport, true);
        }

        private static void PrintRoutes()
        {
            if(inMidiList.Count <= 0)
            {
                Console.WriteLine("No routes defined, use A to add a route.");
                return;
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

                    MidiOutCaps caps = OutputDevice.GetDeviceCapabilities(OutPort);

                    OutputUdp2Midi = new OutputMidi(caps.name, OutPort);
                    OutputUdp2Midi.InitOutputDevice();
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
                    MidiRoutes[InPort].AddRoute(OutPort, AllMessages);
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
                                    UDPInPort = UdpPort;
                                    MidiOutCaps caps = OutputDevice.GetDeviceCapabilities(OutPort);

                                    OutputUdp2Midi = new OutputMidi(caps.name, OutPort);
                                    OutputUdp2Midi.InitOutputDevice();
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

        static private void ProcessBytes(Byte[] receiveBytes)
        {
            Console.Write("Data: ");
            foreach (Byte b in receiveBytes)
            {
                Console.Write("{0:X} ", b);
            }

            if (receiveBytes[0] < 0xF0)
            {
                if (debug)
                {
                    Console.WriteLine("Channel ");
                }
                OutputUdp2Midi.PlayNoteFromBuffer(receiveBytes, receiveBytes.Length);
            }
            else
            {
                if (receiveBytes[0] < 0xF8)
                {
                    if (receiveBytes[0] == 0xF0)
                    {
                        if (debug)
                        {
                            Console.WriteLine("SysEx ");
                        }
                        OutputUdp2Midi.PlaySysExFromBuffer(receiveBytes);
                    }
                    else
                    {
                        // Control Message
                        Console.WriteLine("Control Message ");
                    }
                }
                else
                {
                    // System Real-Time Message
                    Console.WriteLine("System Real-Time Message ");
                }
            }
        }

        static private void ipv4_udpListener()
        {
            //IPEndPoint object will allow us to read datagrams sent from any source.
            IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
            bool done = false;

            while (!done)
            {
                //listener.
                // Blocks until a message returns on this socket from a remote host.
                Byte[] receiveBytes = ipv4_listener.Receive(ref RemoteIpEndPoint);
                if(receiveBytes.Length >= 0)
                {
                    ProcessBytes(receiveBytes);
                }
            }
        }

        static private void ipv6_udpListener()
        {
            //IPEndPoint object will allow us to read datagrams sent from any source.
            IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
            bool done = false;

            while (!done)
            {
                //listener.
                // Blocks until a message returns on this socket from a remote host.
                Byte[] receiveBytes = ipv6_listener.Receive(ref RemoteIpEndPoint);
                if (receiveBytes.Length >= 0)
                {
                    ProcessBytes(receiveBytes);
                }
            }
        }
    }
}
