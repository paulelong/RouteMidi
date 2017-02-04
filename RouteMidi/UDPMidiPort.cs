using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace RouteMidi
{
    class UdpMidiPortList
    {
        List<UDPMidiPort> list = new List<UDPMidiPort>();

        ~UdpMidiPortList()
        {
        }

        public void StopListeners()
        {
            foreach (UDPMidiPort u in list)
            {
                u.StopListeners();
            }
        }

        public int Count
        {
            get { return list.Count; }
        }

        public void Add(int port, OutputMidi midiport)
        {
            UDPMidiPort u = new UDPMidiPort(port, midiport);
            list.Add(u);
        }

        public void ListRoutes()
        {
            foreach(UDPMidiPort ump in list)
            {
                Console.WriteLine("Port {0} -> {1}", ump.UDPInPort, ump.OutputUdp2Midi.Name);
            }
        }
    }

    class UDPMidiPort
    {
        // UDP Stuff
        UdpClient ipv4_listener;
        UdpClient ipv6_listener;
        AutoResetEvent waitHandle = new AutoResetEvent(false);
        public int UDPInPort = 9000;
        public OutputMidi OutputUdp2Midi;
        public bool debug = false;

        Thread ipv4_thread; 
        Thread ipv6_thread;

        public UDPMidiPort(int port, OutputMidi midiport)
        {
            UDPInPort = port;
            OutputUdp2Midi = midiport;

            ipv4_listener = new UdpClient(UDPInPort, AddressFamily.InterNetwork);
            ipv6_listener = new UdpClient(UDPInPort, AddressFamily.InterNetworkV6);

            ipv4_thread = new Thread(new ThreadStart(ipv4_udpListener));
            ipv6_thread = new Thread(new ThreadStart(ipv6_udpListener));

            ipv4_thread.Start();
            ipv6_thread.Start();
        }

        ~UDPMidiPort()
        {
        }

        public void StopListeners()
        {
            ipv4_thread.Abort();
            ipv6_thread.Abort();

            ipv4_listener.Close();
            ipv6_listener.Close();
            // http://stackoverflow.com/questions/1764898/how-do-i-safely-stop-a-c-sharp-net-thread-running-in-a-windows-service
            // http://stackoverflow.com/questions/14860613/how-to-use-asynchronous-receive-for-udpclient-in-a-loop
        }

        private void ipv4_udpListener()
        {
            //IPEndPoint object will allow us to read datagrams sent from any source.
            IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
            bool done = false;

            while (!done)
            {
                //listener.
                // Blocks until a message returns on this socket from a remote host.
                Byte[] receiveBytes = ipv4_listener.Receive(ref RemoteIpEndPoint);
                if (receiveBytes.Length >= 0)
                {
                    ProcessBytes(receiveBytes);
                }
            }
        }

        private void ipv6_udpListener()
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

        private void ProcessBytes(Byte[] receiveBytes)
        {
            if(debug)
            {
                Console.Write("Data: ");
                foreach (Byte b in receiveBytes)
                {
                    Console.Write("{0:X} ", b);
                }
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
    }
}
