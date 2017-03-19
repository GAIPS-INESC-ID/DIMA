using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DIMA_Sim.Model
{
    public class Context
    {
        public List<Characteristic> relevantCharacteristcs;

        public float defaultValence;
        public float wealthIncrement;
        public Dictionary<string, Dictionary<string, float>> agentsValences;
        public List<Agent> contextAgents;

                public float GetMaxDistance()
        {
            double totalWeight = 0.0;
            foreach (var characteristic in relevantCharacteristcs)
                totalWeight += Math.Pow(characteristic.weight * 100.0, 2.0);

            return (float)Math.Sqrt(totalWeight);
        }

        public void LoadFromXml(XDocument xmlReader)
        {
            relevantCharacteristcs = new List<Characteristic>();

            foreach (var element in xmlReader.Root.Element("context").Elements("characteristic"))
            {
                var characteristic = new Characteristic();
                characteristic.name = element.Value;
                characteristic.weight = float.Parse(element.Attribute("weight").Value, CultureInfo.InvariantCulture);

                relevantCharacteristcs.Add(characteristic);
            }

            defaultValence = float.Parse(xmlReader.Root.Element("default_emotional_valence").Attribute("value").Value, CultureInfo.InvariantCulture);
            wealthIncrement = float.Parse(xmlReader.Root.Element("wealth_increment").Attribute("value").Value, CultureInfo.InvariantCulture);
            agentsValences = new Dictionary<string, Dictionary<string, float>>();

            foreach (var element in xmlReader.Root.Element("agents").Elements("agent"))
            {
                string name = element.Attribute("name").Value;
                float emotionalValence = float.Parse(element.Attribute("emotional_valence").Value, CultureInfo.InvariantCulture);
                string socialGroup = element.Attribute("social_group").Value;

                if (!agentsValences.ContainsKey(name))
                    agentsValences.Add(name, new Dictionary<string, float>());

                agentsValences[name].Add(socialGroup, emotionalValence);
            }
        }

        public void AssignAgents(Simulation simulation)
        {
            contextAgents = new List<Agent>();

            foreach (Agent agent in simulation.agents)
            {
                if (agentsValences.ContainsKey(agent.name))
                    contextAgents.Add(agent);
            }
        }
    }
}
