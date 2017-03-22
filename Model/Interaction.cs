using System;

namespace DIMA_Sim.Model
{
    public class Interaction
    {


        public static float DictatorGameTask(Agent agent, Context currentContext)
        {
            //Dividing resource interaction
            var randomGen = new Random(Guid.NewGuid().GetHashCode());
            var numOfAgents = currentContext.contextAgents.Count;

            Agent otherRandomAgent;
            int attempts = 0;
            do
            {
                otherRandomAgent = currentContext.contextAgents[randomGen.Next(numOfAgents)];
                attempts++; //just a fail-safe
            } while (attempts < 1000 || otherRandomAgent.name == agent.name);

            var updateFactor = DetermineInteractionEffect(agent.selfGroup?.name, agent.salience, otherRandomAgent.selfGroup?.name);

            //The average amount given, under these standard conditions, is found by Forsythe et al. to be around 20 % of the allocated money.[7]
            float fairOffer = currentContext.wealthIncrement * 0.20f;

            //no group
            if(agent.salience < agent.minimalSalienceThreshold)
            {
                otherRandomAgent.wealth += fairOffer;
            }

            float groupBias = (int)Math.Ceiling(fairOffer * agent.salience);
            if (otherRandomAgent.selfGroup?.name == agent.selfGroup?.name)
            {
                var otherOffer = fairOffer + groupBias;
                otherRandomAgent.wealth += otherOffer;
                //agent.wealth += currentContext.wealthIncrement - otherOffer;
            }
            else if (otherRandomAgent.selfGroup?.name != agent.selfGroup?.name)
            {
                var otherOffer = fairOffer - groupBias;
                otherRandomAgent.wealth += otherOffer;
                //agent.wealth += currentContext.wealthIncrement - otherOffer;
            }
            return updateFactor;
        }


        public static float MinimalGroupResourceTask(Agent agent, Context currentContext)
        {
            //Dividing resource interaction
            var randomGen = new Random(Guid.NewGuid().GetHashCode());
            var numOfAgents = currentContext.contextAgents.Count;

            Agent otherRandomAgent1;
            Agent otherRandomAgent2;
            int attempts = 0;
            do
            {
                otherRandomAgent1 = currentContext.contextAgents[randomGen.Next(numOfAgents)];
                otherRandomAgent2 = currentContext.contextAgents[randomGen.Next(numOfAgents)];
                attempts++; //just a fail-safe
            } while (attempts < 1000 || otherRandomAgent1.name == agent.name
                || otherRandomAgent2.name == agent.name
                || otherRandomAgent1.name == otherRandomAgent2.name);

            var updateFactor = 0.0f;
            updateFactor += DetermineInteractionEffect(agent.selfGroup?.name, agent.salience, otherRandomAgent1.selfGroup?.name);
            updateFactor += DetermineInteractionEffect(agent.selfGroup?.name, agent.salience, otherRandomAgent2.selfGroup?.name);

            //giving resources
            float fairOffer = currentContext.wealthIncrement / 2;

            if (otherRandomAgent1.selfGroup?.name != otherRandomAgent2.selfGroup?.name)
            {
                float ingroupBias = (int)Math.Ceiling(fairOffer * agent.salience);
                if (otherRandomAgent1.selfGroup?.name == agent.selfGroup?.name)
                {
                    otherRandomAgent1.wealth += fairOffer + ingroupBias;
                    otherRandomAgent2.wealth += fairOffer - ingroupBias;
                }
                else if (otherRandomAgent2.selfGroup?.name == agent.selfGroup?.name)
                {
                    otherRandomAgent2.wealth += fairOffer + ingroupBias;
                    otherRandomAgent1.wealth += fairOffer - ingroupBias;
                }
            }
            else
            {
                otherRandomAgent1.wealth += fairOffer;
                otherRandomAgent2.wealth += fairOffer;
            }
            return updateFactor;
        }



        private static float DetermineInteractionEffect(string selfGroupName, float selfGroupSalience, string otherAgentGroup)
        {
            if (otherAgentGroup == null) return 0.0f;

            if (otherAgentGroup != selfGroupName)
            {
                return selfGroupSalience * 0.1f; //we divide by 10 to attenuate the impact
            }
            else
            {
                return selfGroupSalience * -0.1f;
                //return selfGroupSalience;
            }
        }
    }
}
