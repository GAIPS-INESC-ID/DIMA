using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIMA_Sim.Model
{
    class SocialGroup
    {
        public String name;
        public Dictionary<Characteristic, float> characteristics;

        public float accessibility;
        public float salience;

        public bool isAdHoc = false;

        public void CalculateAccessibility(Agent agent)
        {
            int numDimensions = characteristics.Count;

            float distance = 0.0f;

            int validCharacteristics = 0;

            foreach (var characteristic in characteristics)
            {
                float agentValue = 0.0f;
                if (agent.characteristics.TryGetValue(characteristic.Key, out agentValue))
                {
                    validCharacteristics++;

                    float difference = agentValue - characteristic.Value;

                    distance += difference * difference;
                }
            }

            distance = (float)(Math.Sqrt((double)distance) / (Math.Sqrt(validCharacteristics) * 100.0f));

            //accessibility = 1.0f - distance;
            accessibility = 0.5f;

        }
    }
}
