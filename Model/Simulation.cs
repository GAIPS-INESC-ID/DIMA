using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DIMA_Sim.Model
{
    class Simulation
    {
        public List<Agent> agents;

        public float normativeMatchDistance;
        public float distanceConstraint;
        public float comparativeFitAlfa;
        public float comparativeFitBeta;

        public void LoadFromXml(XDocument xmlReader)
        {
            var propertiesElement = xmlReader.Root.Element("properties");
            normativeMatchDistance = float.Parse(propertiesElement.Element("normative_match_distance").Value.ToString());
            distanceConstraint = float.Parse(propertiesElement.Element("distance_constraint").Value.ToString());
            comparativeFitAlfa = float.Parse(propertiesElement.Element("comparative_fit_alfa").Value.ToString());
            comparativeFitBeta = float.Parse(propertiesElement.Element("comparative_fit_beta").Value.ToString());
            //
            agents = new List<Agent>();

            foreach (var element in xmlReader.Root.Element("agents").Elements("agent"))
            {
                var agent = new Agent();

                agent.characteristics = new Dictionary<Characteristic, float>();
                agent.knowledgeBase = new List<SocialGroup>();

                agent.name = element.Attribute("name").Value;

                foreach (var subElement in element.Elements("characteristic"))
                {
                    var characteristic = new Characteristic();
                    characteristic.name = subElement.Attribute("name").Value;

                    agent.characteristics.Add(characteristic, float.Parse(subElement.Value));
                }

                foreach (var subElement in element.Element("knowledge_base").Elements("social_group"))
                {
                    var socialGroup = new SocialGroup();
                    socialGroup.characteristics = new Dictionary<Characteristic, float>();

                    socialGroup.name = subElement.Attribute("name").Value;

                    foreach (var subEubElement in subElement.Elements("characteristic"))
                    {
                        var characteristic = new Characteristic();
                        characteristic.name = subEubElement.Attribute("name").Value;

                        socialGroup.characteristics.Add(characteristic, float.Parse(subEubElement.Value));
                    }

                    socialGroup.CalculateAccessibility(agent);

                    agent.knowledgeBase.Add(socialGroup);
                }

               // agent.NormalizeAccessibility();

                agents.Add(agent);
            }
        }
    }
}
