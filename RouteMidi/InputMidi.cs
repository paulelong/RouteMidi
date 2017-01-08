using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sanford.Multimedia;
using Sanford.Multimedia.Midi;

namespace RouteMidi
{
    class InputMidi
    {
        public InputDevice inMIDI = null;
        public int id;
        private string name;

        public InputMidi(string Name, int ID)
        {
            name = Name;
            id = ID;
        }

        ~InputMidi()
        {
            if (inMIDI != null)
            {
                inMIDI.Close();
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

        static public void PrintMidiList(InputMidi[] im)
        {
            Console.WriteLine("Input Devices: ");
            for (int i = 0; i < InputDevice.DeviceCount; i++)
            {
                Console.Write("  ");
                im[i].PrintInfo();
            }
        }
        public string GetNameId()
        {
            return name;
        }

        static public int MapName(InputMidi[] im, string name)
        {
            for (int i = 0; i < InputDevice.DeviceCount; i++)
            {
                if (name == im[i].GetNameId())
                {
                    return i;
                }
            }

            return -1;
        }

        internal void InitInputDevice()
        {
            inMIDI = new InputDevice(id);
        }

        internal void StartRecording()
        {
            inMIDI.StartRecording() ;
        }

        internal void StopRecording()
        {
            inMIDI.StopRecording();
            inMIDI.Reset();
        }

        internal void Close()
        {
            if (inMIDI != null)
            {
                inMIDI.Close();
            }
        }
    }
}
