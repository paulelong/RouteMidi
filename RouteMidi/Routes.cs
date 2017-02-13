using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using Sanford.Multimedia;
using Sanford.Multimedia.Midi;
using System.Threading;
using System.IO;

namespace RouteMidi
{
    public class Routes
    {
        const int MINMIDIPORT = 1025;
        const uint MAXMIDIPORTS = 64;

        private InputMidi[] im = new InputMidi[MAXMIDIPORTS];
        private OutputMidi[] om = new OutputMidi[MAXMIDIPORTS];

        private Route[] MidiRoutes = new Route[MAXMIDIPORTS];

        private List<int> outMidiList = new List<int>();
        private List<int> inMidiList = new List<int>();

        private SynchronizationContext context;

        // UDP Stuff
        private UdpMidiPortList umpl = new UdpMidiPortList();

        public UdpMidiPortList midiPortList
        {
            get { return umpl;  }
        }

        private bool debug = false;

        public bool Debug
        {
            get { return debug; }
            set
            {
                debug = value;
                umpl.Debug(debug);
            }
        }

        private bool dirty = false;
        public bool IsDirty
        {
            get { return dirty; }
            set { dirty = value;  }
        }

        public int Count
        {
            get
            {
                int n = 0;
                for(int i = 0; i < MAXMIDIPORTS; i++)
                {
                    if(MidiRoutes[i] != null)
                    {
                        n++;
                    }
                }

                return n;
            }
        }

        public Routes()
        {
            GetMidiInfo();
            Console.WriteLine("Route Midi initialized.");
        }

        public List<int> GetRoute(int n)
        {
            return MidiRoutes[n].GetRoutes();
        }

        public List<int> UDPPortList(UDPMidiPort ump)
        {
            List<int> l = new List<int>();
            foreach(OutputMidi om in ump.oml)
            {
                l.Add(FindOutputPort(om.Name));
            }

            return l;

        }

        //public List<int> GetUDPRoutes()

        public void ResetRoutes()
        {
            foreach (Route r in MidiRoutes)
            {
                if (r != null)
                {
                    r.RemoveRoutes();
                }
            }

            // clear up udp routes too.
            umpl.StopListeners();
            umpl.list.Clear();
        }

        public void Shutdown()
        {
            Console.WriteLine("Stopping Midi...");

            StopAllRecording();

            umpl.StopListeners();

            Console.WriteLine("Midi Stopped ...");

            CloseAllMidiPorts();
        }

        public void DisplayInputMidiPorts()
        {
            InputMidi.PrintMidiList(im);
            Console.WriteLine();
        }

        public void DisplayOutputMidiPorts()
        {
            OutputMidi.PrintMidiList(om);
            Console.WriteLine();
        }

        public int FindInputPort(string name)
        {
            return InputMidi.MapName(im, name);
        }

        public int FindOutputPort(string name)
        {
            return OutputMidi.MapName(om, name);
        }

        public string GetInportName(int portnum)
        {
            return im[portnum].Name;
        }

        public string GetOutportName(int portnum)
        {
            return om[portnum].Name;
        }

        public void PrintRoutes()
        {
            if (inMidiList.Count <= 0 && umpl.Count <= 0)
            {
                Console.WriteLine("No routes defined, use A to add a route.");
                return;
            }

            if (umpl.Count > 0)
            {
                umpl.ListRoutes();
            }

            foreach (int i in inMidiList)
            {
                List<int> outs = MidiRoutes[i].GetRoutes();
                if (outs.Count > 0)
                {
                    Console.Write(i.ToString() + " - " + im[i].Name + " -> ");
                    foreach (int o in outs)
                    {
                        Console.Write(om[o].Name + ",");
                    }
                    Console.WriteLine();
                }
            }
        }

        public bool ValidateMidiInPort(int port)
        {
            if (port >= InputMidi.Count && port < MINMIDIPORT)
            {
                return false;
            }

            return true;
        }

        public bool ValidateMidiOutPort(int port)
        {
            if (port >= OutputMidi.Count)
            {
                return false;
            }

            return true;
        }

        public void GetMidiInfo()
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


        public void CloseAllMidiPorts()
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

        public void StopAllRecording()
        {
            foreach (int i in inMidiList)
            {
                im[i].StopRecording();
            }
        }

        public void StartAllRecording()
        {
            foreach (int i in inMidiList)
            {
                Console.WriteLine("Starting {0}", i);
                im[i].StartRecording();
            }
        }

        public bool InitMidiPorts()
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

        public bool RemoveRoutes(int routenum)
        {
            if (routenum < 0 || routenum >= MidiRoutes.Length)
            {
                return false;
            }

            MidiRoutes[routenum].RemoveRoutes();

            return true;
        }

        public void AddRoute(int inport, int outport)
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
                    MidiRoutes[inport] = new Route();
                }
                MidiRoutes[inport].AddRoute(outport, true);
            }

            dirty = true;
        }

        // Event handlers for Midi messages

        /// <summary>
        /// Event Handlers
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void inDevice_Error(object sender, Sanford.Multimedia.ErrorEventArgs e)
        {
            Console.WriteLine("ERROR! " + e.Error.Message);
        }

        private void HandleSysCommonMessageReceived(object sender, SysCommonMessageEventArgs e)
        {
            InputDevice id = (InputDevice)sender;
            MidiRoutes[id.DeviceID].SendMessages(om, e.Message);

            if (debug)
            {
                Console.WriteLine("SysCom: " + e.Message.Message + " " + e.Message.Data1 + " " + e.Message.Data2);
            }
        }

        private void HandleChannelMessageReceived(object sender, ChannelMessageEventArgs e)
        {
            InputDevice id = (InputDevice)sender;
            MidiRoutes[id.DeviceID].SendMessages(om, e.Message);
            if (debug)
            {
                Console.WriteLine("Channel: " + e.Message.Message + " c:" + e.Message.Command + " d1:" + e.Message.Data1 + " d2:" + e.Message.Data2 + " mc:" + e.Message.MidiChannel + " mt:" + e.Message.MessageType);
            }
        }

        private void HandleSysExMessageReceived(object sender, SysExMessageEventArgs e)
        {
            InputDevice id = (InputDevice)sender;
            MidiRoutes[id.DeviceID].SendMessages(om, e.Message);
            if (debug)
            {
                Console.Write("SysEx: " + e.Message.SysExType + " ");
                for (int i = 0; i < e.Message.Length; i++)
                {
                    Console.Write("{0:X} ", e.Message[i]);
                }
                Console.WriteLine();
            }
        }

        private void HandleSysRealtimeMessageReceived(object sender, SysRealtimeMessageEventArgs e)
        {
            InputDevice id = (InputDevice)sender;
            MidiRoutes[id.DeviceID].SendMessages(om, e.Message);
            if (debug)
            {
                Console.WriteLine("SysRealTime: " + e.Message.Message + " " + e.Message.SysRealtimeType + " " + e.Message.MessageType);
            }
        }

        public bool ParseFile(string file)
        {
            if (!File.Exists(file))
            {
                Console.WriteLine("Couldn't find input file " + file);
                return false;
            }

            using (StreamReader fs = File.OpenText(file))
            {
                while (fs.Peek() >= 0)
                {
                    string line = fs.ReadLine();
                    int n = line.IndexOf(',');
                    if (n > 0)
                    {
                        string leftside = line.Substring(0, n);
                        string rightside = line.Substring(n + 1);

                        if (leftside[0] == '#')
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
                                    if (UdpPort > MAXMIDIPORTS)
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

                            if (InPort == -1 || InPort >= im.Length || OutPort == -1 || OutPort >= om.Length)
                            {
                                Console.Write("Port range, " + InPort.ToString() + " to " + OutPort.ToString() + " is invalid for ");
                            }
                            else
                            {
                                Console.Write("Port range, " + InPort.ToString() + " to " + OutPort.ToString() + " router for ");
                                AddRoute(InPort, OutPort);
                            }
                        }
                    }
                }
            }

            return true;
        }
    }

    public class Route
    {
        private bool all;
        private List<int> outMidiList = new List<int>();

        public void AddRoute(int outport, bool All)
        {
            if (!outMidiList.Contains(outport))
            {
                outMidiList.Add(outport);
            }

            all = All;
        }

        public bool RemoveRoutes()
        {
            outMidiList.Clear();
            return true;
        }

        public bool AllRoutes
        {
            get
            {
                return all;
            }
        }

        public List<int> GetRoutes()
        {
            return outMidiList;
        }

        internal void SendMessages(OutputMidi[] om, SysCommonMessage CommonMessage)
        {
            foreach (int i in outMidiList)
            {
                om[i].outMIDI.Send(CommonMessage);
            }
        }

        internal void SendMessages(OutputMidi[] om, ChannelMessage CommonMessage)
        {
            foreach (int i in outMidiList)
            {
                om[i].outMIDI.Send(CommonMessage);
            }
        }

        internal void SendMessages(OutputMidi[] om, SysRealtimeMessage CommonMessage)
        {
            foreach (int i in outMidiList)
            {
                om[i].outMIDI.Send(CommonMessage);
            }
        }

        internal void SendMessages(OutputMidi[] om, SysExMessage CommonMessage)
        {
            foreach (int i in outMidiList)
            {
                om[i].outMIDI.Send(CommonMessage);
            }
        }
    }
}
