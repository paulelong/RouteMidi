using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RouteMidi
{
    public class MidiConfig
    {
        Dictionary<string, List<string>> routes = new Dictionary<string, List<string>>();

        public void Save()
        {
            System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(this.GetType());
            using (StreamWriter writer = File.CreateText("RouteMidiConfig.cfg"))
            {
                xs.Serialize(writer, this);
            }
        }

        public void Load()
        {
            System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(this.GetType());
            using (StreamReader reader = File.OpenText("RouteMidiConfig.cfg"))
            {
                MidiConfig c = xs.Deserialize(reader) as MidiConfig;
                this.routes = c.routes;
            }
        }

        public void ReadRouteConfig(Routes[] outroutes, InputMidi[] im, OutputMidi[] om)
        {
            for(int n = 0; n < InputMidi.Count; n++)
            {
                outroutes[n].RemoveRoutes();
                foreach (string inportname in routes.Keys)
                {
                    int inport = InputMidi.MapName(im, inportname);
                    if (inport >= 0)
                    {
                        foreach (string outportname in routes[inportname])
                        {
                            int outport = OutputMidi.MapName(om, outportname);
                            if (outport >= 0)
                            {
                                outroutes[inport].AddRoute(outport, true);
                            }
                            else
                            {
                                Console.WriteLine("Could not find outport " + outportname);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Could not find inport " + inportname);
                    }
                }
            }
        }

        public void WriteRouteConfig(Routes[] inroutes, InputMidi[] im, OutputMidi[] om)
        {
            for (int n = 0; n < InputMidi.Count; n++)
            {
                foreach (int m in inroutes[n].GetRoutes())
                {
                    if(!routes.ContainsKey(im[n].Name))
                    {
                        List<string> l = new List<string>();
                        l.Add(om[m].Name);
                        routes[im[n].Name] = l;
                    }
                    else
                    {
                        routes[im[n].Name].Add(om[m].Name);
                    }
                }
            }
        }
    }
}
