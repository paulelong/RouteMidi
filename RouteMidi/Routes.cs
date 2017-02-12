using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using Sanford.Multimedia;
using Sanford.Multimedia.Midi;
using System.Threading;

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
        static UdpMidiPortList umpl = new UdpMidiPortList();

        public Routes()
        {
            GetMidiInfo();
            Console.WriteLine("Route Midi initialized.");
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

        private void PrintRoutes()
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

        private void GetMidiInfo()
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

        private bool InitMidiPorts()
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

        public void RemoveRoutes(int routenum)
        {
            MidiRoutes[routenum].RemoveRoutes();
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

        static private void inDevice_Error(object sender, Sanford.Multimedia.ErrorEventArgs e)
        {
            Console.WriteLine("ERROR! " + e.Error.Message);
        }

        static private void HandleSysCommonMessageReceived(object sender, SysCommonMessageEventArgs e)
        {
            InputDevice id = (InputDevice)sender;
            MidiRoutes[id.DeviceID].SendMessages(om, e.Message);

            if (debug)
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
                for (int i = 0; i < e.Message.Length; i++)
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
