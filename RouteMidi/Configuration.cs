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

        public void ReadRouteConfig(string Name, Routes midiRoutes)
        {
            MidiConfigurationsSchema record = Configurations.Find(r => r.ConfigurationName == Name);
            if (record != null)
            {
                ReadRouteRecord(record, midiRoutes);
            }
            else
            {
                Console.WriteLine("Unable to find config {0}", Name);
            }
        }

        public void ReadRouteConfigDefault(Routes midiRoutes)
        {
            MidiConfigurationsSchema record = Configurations.Find(r => r.ConfigurationName == rms.DefaultConfig);
            if(record != null)
            {
                ReadRouteRecord(record, midiRoutes);
            }
            else
            {
                Console.WriteLine("Unable to find config {0}", rms.DefaultConfig);
            }
        }

        public void ReadRouteConfigByNumber(int number, Routes midiRoutes)
        {
            if(number < Configurations.Count && number >= 1)
            {
                MidiConfigurationsSchema record = Configurations[number - 1];
                if (record != null)
                {
                    ReadRouteRecord(record, midiRoutes);
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

        private void ReadRouteRecord(MidiConfigurationsSchema record, Routes midiRoutes)
        {
            midiRoutes.ResetRoutes();

            for (int n = 0; n < record.InPorts.Count; n++)
            {
                // check if the input is available now
                int inport = midiRoutes.FindInputPort(record.InPorts[n]);
                if (inport >= 0)
                {
                    if(record.Routes.Count > 0)
                    {
                        foreach (int m in record.Routes[n])
                        {
                            int outport = midiRoutes.FindOutputPort(record.OutPorts[m]);
                            if (outport >= 0)
                            {
                                midiRoutes.AddRoute(inport, outport);
                            }
                            else
                            {
                                Console.WriteLine("Could not find outport {0} in current midi setup.", record.OutPorts[m]);
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

            // Configure UDP ports
            int i = 0;
            foreach(int inport in record.UdpPorts)
            {
                foreach(int op in record.UdpRoutes[i])
                {
                    int outport = midiRoutes.FindOutputPort(record.OutPorts[op]);
                    if (outport >= 0)
                    {
                        midiRoutes.AddRoute(inport, outport);
                    }
                    else
                    {
                        Console.WriteLine("Could not find outport {0} in current midi setup.", record.OutPorts[i]);
                    }
                }
                i++;
            }
        }

        public void WriteRouteConfig(string Name, Routes midiRoutes)
        {
            if(midiRoutes.Count <= 0)
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
                record.InPorts.Add(midiRoutes.GetInportName(n));
            }

            for (int n = 0; n < OutputMidi.Count; n++)
            {
                record.OutPorts.Add(midiRoutes.GetOutportName(n));
            }

            for (int n = 0; n < InputMidi.Count; n++)
            {
                record.Routes.Add(midiRoutes.GetRoute(n));
            }

            foreach(UDPMidiPort ump in midiRoutes.midiPortList.list)
            {
                List<int> opl = new List<int>();
                foreach(OutputMidi o in ump.oml)
                {
                    opl.Add(midiRoutes.FindOutputPort(o.Name));
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

        public bool ValidateConfigurationNumber(int num)
        {
            if(num > 0 && num <= Configurations.Count)
            {
                return true;
            }

            return false;
        }

        internal void ReadDefaultConfig(Routes midiRoutes)
        {
            MidiConfigurationsSchema record = Configurations[0];
            ReadRouteRecord(record, midiRoutes);
        }
    }
}
