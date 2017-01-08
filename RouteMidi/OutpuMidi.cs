using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sanford.Multimedia;
using Sanford.Multimedia.Midi;

namespace RouteMidi
{
    class OutputMidi
    {
        public OutputDevice outMIDI;
        public int id;
        private string name;

        public OutputMidi(string Name, int ID)
        {
            name = Name;
            id = ID;
        }

        ~OutputMidi()
        {
            if (outMIDI != null)
            {
                outMIDI.Close();
            }
        }

        public string Name
        {
            set
            {
                this.name = value;
            }
            get
            {
                return this.name;
            }
        }

        public void PrintInfo()
        {
            Console.WriteLine(id.ToString() + " - " + name);
        }

        public string GetNameId()
        {
            return name;
        }

        static public void PrintMidiList(OutputMidi[] om)
        {
            Console.WriteLine("Output Devices: ");
            for (int i = 0; i < OutputDevice.DeviceCount; i++)
            {
                Console.Write("  ");
                om[i].PrintInfo();
            }
        }

        static public int MapName(OutputMidi[] om, string name)
        {
            for (int i = 0; i < OutputDevice.DeviceCount; i++)
            {
                if(name == om[i].GetNameId())
                {
                    return i;
                }
            }

            return -1;
        }

        internal void InitOutputDevice()
        {
            outMIDI = new OutputDevice(id);
        }

        public void PlayNoteFromBuffer(Byte[] buf, int length)
        {
            if(length < 3 || buf[0] == 0)
            {
                return;
            }

            ChannelMessageBuilder cmb = new ChannelMessageBuilder();
            cmb.Command = (ChannelCommand)buf[0];
            cmb.Data1 = buf[1];
            cmb.Data2 = buf[2];
            cmb.Build();

            outMIDI.Send(cmb.Result);
        }

        public void PlaySysExFromBuffer(Byte[] buf)
        {
            SysExMessage cmb = new SysExMessage(buf);

            outMIDI.Send(cmb);
        }

        internal void Close()
        {
            if (outMIDI != null)
            {
                outMIDI.Close();
            }
        }
    }
}
