using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using Sanford.Multimedia;
using Sanford.Multimedia.Midi;

namespace RouteMidi
{
    public class Routes
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
