using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RouteMidi
{
    // List
    public class MidiConfigurationsSchema
    {
        public string ConfigurationName;
        //public List<string> Configurations = new List<string>();
        public List<string> InPorts = new List<string>();
        public List<string> OutPorts = new List<string>();
        public List<List<int>> Routes = new List<List<int>>();
    }

    public class MidiConfig
    {
        public string defaultConfiguration;
        public List<MidiConfigurationsSchema> Configurations = new List<MidiConfigurationsSchema>();
         
        public void Save()
        {
            System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(List<MidiConfigurationsSchema>));
            using (StreamWriter writer = File.CreateText("RouteMidiConfig.cfg"))
            {
                xs.Serialize(writer, Configurations);
            }
        }

        public void Load()
        {

            System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(List<MidiConfigurationsSchema>));
            using (StreamReader reader = File.OpenText("RouteMidiConfig.cfg"))
            {
                try
                {
                    Configurations = xs.Deserialize(reader) as List<MidiConfigurationsSchema>;
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public void ReadRouteConfig(string Name, Routes[] outroutes, InputMidi[] im, OutputMidi[] om)
        {
            MidiConfigurationsSchema record = Configurations.Find(r => r.ConfigurationName == Name);
            ReadRouteRecord(record, outroutes, im, om);
        }

        public void ReadRouteConfigByNumber(int number, Routes[] outroutes, InputMidi[] im, OutputMidi[] om)
        {
            MidiConfigurationsSchema record = Configurations[number - 1];
            ReadRouteRecord(record, outroutes, im, om);
        }

        private void ReadRouteRecord(MidiConfigurationsSchema record, Routes[] outroutes, InputMidi[] im, OutputMidi[] om)
        {
            for (int n = 0; n < record.InPorts.Count; n++)
            {
                outroutes[n].RemoveRoutes();
                // check if the input is available now
                int inport = InputMidi.MapName(im, record.InPorts[n]);
                if (inport >= 0)
                {
                    foreach (int i in record.Routes[n])
                    {
                        int outport = OutputMidi.MapName(om, record.OutPorts[i]);
                        if (outport >= 0)
                        {
                            outroutes[inport].AddRoute(outport, true);
                        }
                        else
                        {
                            Console.WriteLine("Could not find outport {1} in current midi setup.", record.OutPorts[i]);
                        }

                    }
                }
                else
                {
                    Console.WriteLine("Unalble to load configuration because {1} doesn't exist in current MIDI setup.", record.InPorts[n]);
                }
            }
        }

        public void WriteRouteConfig(string Name, Routes[] inroutes, InputMidi[] im, OutputMidi[] om)
        {
            MidiConfigurationsSchema record = Configurations.Find(r => r.ConfigurationName == Name);
            if(record == null)
            {
                record = new MidiConfigurationsSchema();
                Configurations.Add(record);
            }

            for (int n = 0; n < InputMidi.Count; n++)
            {
                record.InPorts.Add(im[n].Name);
            }

            for (int n = 0; n < OutputMidi.Count; n++)
            {
                record.OutPorts.Add(om[n].Name);
            }

            for (int n = 0; n < InputMidi.Count; n++)
            {
                List<int> rts = new List<int>();
                foreach (int m in inroutes[n].GetRoutes())
                {
                    rts.Add(m);
                }
                record.Routes.Add(rts);
            }
        }

        internal bool ConfigExists(string Name)
        {
            MidiConfigurationsSchema record = Configurations.Find(r => r.ConfigurationName == Name);
            if (record == null)
                return false;
            else
                return true;

        }

        internal void DisplayConfigurations()
        {
            int n = 1;
            foreach(MidiConfigurationsSchema r in Configurations)
            {
                Console.WriteLine("{2} - {1}", r.ConfigurationName, n++);
            }
        }

        internal void ReadDefaultConfig(Routes[] outroutes, InputMidi[] im, OutputMidi[] om)
        {
            MidiConfigurationsSchema record = Configurations[0];
            ReadRouteRecord(record, outroutes, im, om);
        }
    }
}
