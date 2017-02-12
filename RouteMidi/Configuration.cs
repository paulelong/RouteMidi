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
        public List<string> InPorts = new List<string>();
        public List<string> OutPorts = new List<string>();
        public List<List<int>> Routes = new List<List<int>>();
        public List<int> UdpPorts = new List<int>();
        public List<List<int>> UdpRoutes = new List<List<int>>();
    }

    public class RouteMidiSettings
    {
        public string DefaultConfig;
    }

    public class MidiConfig
    {
        const string ConfigurationFile = "RouteMidiConfiguration.xml";
        const string SettingsFile = "RouteMidiSettings.xml";

        public List<MidiConfigurationsSchema> Configurations = new List<MidiConfigurationsSchema>();

        RouteMidiSettings rms = new RouteMidiSettings();

        public MidiConfig(string def)
        {
            rms.DefaultConfig = def;
        }

        public void SetDefault(string def)
        {
            rms.DefaultConfig = def;
        }

        public string GetDefault()
        {
            return rms.DefaultConfig;
        }

        public void Save()
        {
            System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(List<MidiConfigurationsSchema>));
            using (StreamWriter writer = File.CreateText(ConfigurationFile))
            {
                xs.Serialize(writer, Configurations);
            }

            System.Xml.Serialization.XmlSerializer xsdefaultConfig = new System.Xml.Serialization.XmlSerializer(typeof(RouteMidiSettings));
            using (StreamWriter writer = File.CreateText(SettingsFile))
            {
                xsdefaultConfig.Serialize(writer, rms);
            }
        }

        public void Load()
        {
            System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(List<MidiConfigurationsSchema>));
            try
            {
                using (StreamReader reader = File.OpenText(ConfigurationFile))
                {
                    Configurations = xs.Deserialize(reader) as List<MidiConfigurationsSchema>;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            try
            {
                System.Xml.Serialization.XmlSerializer xsdefaultConfig = new System.Xml.Serialization.XmlSerializer(typeof(RouteMidiSettings));
                using (StreamReader reader = File.OpenText(SettingsFile))
                {
                    rms = xsdefaultConfig.Deserialize(reader) as RouteMidiSettings;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void ReadRouteConfig(string Name, Route[] outroutes, InputMidi[] im, OutputMidi[] om)
        {
            MidiConfigurationsSchema record = Configurations.Find(r => r.ConfigurationName == Name);
            if (record != null)
            {
                ReadRouteRecord(record, outroutes, im, om);
            }
            else
            {
                Console.WriteLine("Unable to find config {0}", Name);
            }
        }

        public void ReadRouteConfigDefault(Route[] outroutes, InputMidi[] im, OutputMidi[] om)
        {
            MidiConfigurationsSchema record = Configurations.Find(r => r.ConfigurationName == rms.DefaultConfig);
            if(record != null)
            {
                ReadRouteRecord(record, outroutes, im, om);
            }
            else
            {
                Console.WriteLine("Unable to find config {0}", rms.DefaultConfig);
            }
        }

        public void ReadRouteConfigByNumber(int number, Route[] outroutes, InputMidi[] im, OutputMidi[] om)
        {
            if(number < Configurations.Count && number >= 1)
            {
                MidiConfigurationsSchema record = Configurations[number - 1];
                if (record != null)
                {
                    ReadRouteRecord(record, outroutes, im, om);
                }
                else
                {
                    Console.WriteLine("Unable to find config number {0}", number);
                }
            }
            else
            {
                Console.WriteLine("Configuraiton number out of range");
            }
        }

        private void ReadRouteRecord(MidiConfigurationsSchema record, Route[] outroutes, InputMidi[] im, OutputMidi[] om)
        {
            // Clear all the routes first
            for(int n = 0; n < outroutes.Length; n++)
            {
                if(outroutes[n] != null)
                {
                    outroutes[n].RemoveRoutes();
                }
            }
            for (int n = 0; n < record.InPorts.Count; n++)
            {
                // check if the input is available now
                int inport = InputMidi.MapName(im, record.InPorts[n]);
                if (inport >= 0)
                {
                    if(record.Routes.Count > 0)
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
                                Console.WriteLine("Could not find outport {0} in current midi setup.", record.OutPorts[i]);
                            }

                        }
                    }
                    else
                    {
                        Console.WriteLine("No routes in configuration");
                    }
                }
                else
                {
                    Console.WriteLine("Unalble to load configuration because {0} doesn't exist in current MIDI setup.", record.InPorts[n]);
                }
            }
        }

        public void WriteRouteConfig(string Name, Route[] inroutes, InputMidi[] im, OutputMidi[] om, UdpMidiPortList umpl)
        {
            if(inroutes.Length <= 0)
            {
                Console.WriteLine("Can't save configuration with out routes defined.");
                return;
            }

            MidiConfigurationsSchema record = Configurations.Find(r => r.ConfigurationName == Name);
            if(record == null)
            {
                record = new MidiConfigurationsSchema();
                record.ConfigurationName = Name;
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
                if(inroutes[n] != null)
                {
                    foreach (int m in inroutes[n].GetRoutes())
                    {
                        rts.Add(m);
                    }
                    record.Routes.Add(rts);
                }
            }

            foreach(UDPMidiPort ump in umpl.list)
            {
                List<int> opl = new List<int>();
                foreach(OutputMidi o in ump.oml)
                {
                    opl.Add(FindOutputName(om, o));
                }

                record.UdpPorts.Add(ump.UDPInPort);
                record.UdpRoutes.Add(opl);
            }
        }

        private int FindOutputName(OutputMidi[] om, OutputMidi o)
        {
            for(int i = 0; i < om.Length; i++)
            {
                if(om[i].Name == o.Name)
                {
                    return i;
                }
            }

            return -1;
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
                Console.WriteLine("{1} - {0}", r.ConfigurationName, n++);
            }
        }

        internal void ReadDefaultConfig(Route[] outroutes, InputMidi[] im, OutputMidi[] om)
        {
            MidiConfigurationsSchema record = Configurations[0];
            ReadRouteRecord(record, outroutes, im, om);
        }
    }
}
