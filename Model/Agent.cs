using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIMA_Sim.Model
{
    public class Agent
    {
        public string name;
        public float wealth;
        public Dictionary<Characteristic, float> characteristics;
        public List<SocialGroup> knowledgeBase;

        private int numAdhocGroups;

        public struct ClusterMean
        {
            public ClusterMean(int numDimensions)
            {
                mean = new float[numDimensions];

                numAgents = 0;
                dispersion = 0.0f;
                matchedGroup = null;
            }

            public int numAgents;
            public float[] mean;

            public float dispersion;
            public SocialGroup matchedGroup;
        }

        public struct AgentCluster
        {
            public AgentCluster(Agent agent)
            {
                this.agent = agent;
                cluster = 0;
            }

            public Agent agent;
            public int cluster;
        
        }

        public List<AgentCluster> agentsCluster;
        public List<ClusterMean> clusterMeans;

        public struct KbExportData
        {
            public float accessibility;
            public float salience;
        }

        public struct ExportData
        {
            public SocialGroup group;
            public float agentDistance;
            public float groupDistance;
            public float dispersion;
            public float comparativeFit;
            public float accessibility;
            public float salience;
            public float wealth;
            public List<KbExportData> kbData;
        }

        public List<ExportData> exportData = new List<ExportData>();

        public SocialGroup selfGroup;

        public float comparativeFit;
        public float salience;
        public float groupMeanDistance;

        public void UpdateExportData(Context currentContext)
        {
            ExportData data;
            data.group = selfGroup;
            data.agentDistance = GetClusterDistance(currentContext);
            data.groupDistance = groupMeanDistance;
            data.dispersion = GetClusterDispersion();
            data.comparativeFit = comparativeFit;
            data.salience = salience;
            data.wealth = wealth;
            data.accessibility = (selfGroup != null) ? selfGroup.accessibility : 0.0f;

            data.kbData = new List<KbExportData>();

            foreach (var group in knowledgeBase)
            {
                KbExportData kbData;
                kbData.accessibility = group.accessibility;
                kbData.salience = group.salience;

                data.kbData.Add(kbData);
            }

            exportData.Add(data);
        }

        public void NullExportData()
        {
            ExportData data;
            data.group = null;
            data.agentDistance = 0.0f;
            data.groupDistance = 0.0f;
            data.dispersion = 0.0f;
            data.comparativeFit = 0.0f;
            data.salience = 0.0f;
            data.accessibility = 0.0f;
            data.wealth = 0.0f;
            data.kbData = new List<KbExportData>();

            foreach (var group in knowledgeBase)
            {
                KbExportData kbData;
                kbData.accessibility = group.accessibility;
                kbData.salience = group.salience;

                data.kbData.Add(kbData);
            }

            exportData.Add(data);
        }

        public int GetSelfCluster()
        {
            int selfCluster = 0;
            foreach (var agentCluster in agentsCluster)
            {
                if (agentCluster.agent != this)
                    continue;

                selfCluster = agentCluster.cluster;
                break;
            }

            return selfCluster;
        }

        public float GetClusterDispersion()
        {
            return clusterMeans[GetSelfCluster()].dispersion;
        }

        public float GetClusterDistance(Context currentContext)
        {
            int numDimensions = currentContext.relevantCharacteristcs.Count;

            int selfCluster = GetSelfCluster();

            float distance = 0.0f;

            for (int i = 0; i < numDimensions; i++)
            {
                float agentValue = 0.0f;
                characteristics.TryGetValue(currentContext.relevantCharacteristcs[i], out agentValue);

                float weight = currentContext.relevantCharacteristcs[i].weight;

                float difference = (clusterMeans[selfCluster].mean[i] - agentValue) * weight;
                distance += difference * difference;
            }

            return (float)Math.Sqrt((double)distance) / currentContext.GetMaxDistance();
        }

        public float GetAccessibility()
        {
            return selfGroup.accessibility;
        }

        

        public void UpdateAccessibility(Context currentContext)
        {
            if (selfGroup == null)
                return;

            var updateFactor = Interaction.MinimalGroupResourceTask(this, currentContext);
            //  var updateFactor = Interaction.DictatorGameTask(this, currentContext);

            float accessibility = 0.1f * (float)Math.Log(-selfGroup.accessibility / (selfGroup.accessibility - 1.0001)) + 0.5f;

            float newAccessibility = accessibility + salience * updateFactor;
            
            selfGroup.accessibility = Math.Min(1.0f, 1.0f / (1.0f + (float)Math.Pow(Math.E, (-newAccessibility + 0.5) / 0.1)));
            
            if (float.IsNaN(selfGroup.accessibility))
                selfGroup.accessibility = 0.0f;
        }


        /*
        public void NormalizeAccessibility()
        {
            float totalAccessibility = 0.0f;

            foreach (var socialGroup in knowledgeBase)
            {
                totalAccessibility += socialGroup.accessibility;
            }

            if (totalAccessibility == 0.0f)
            {
                foreach (var socialGroup in knowledgeBase)
                {
                    socialGroup.accessibility = 1.0f / knowledgeBase.Count;
                }
            }
            else
            {
                foreach (var socialGroup in knowledgeBase)
                {
                    socialGroup.accessibility /= totalAccessibility;
                }
            }
        }*/

        public void CalculateComparativeFit(Context currentContext, Simulation simulation)
        {
            salience = 0.0f;

            foreach (var socialGroup in knowledgeBase)
            {
                float groupComparativeFit;

                int numDimensions = currentContext.relevantCharacteristcs.Count;

                int selfCluster = GetSelfCluster();

                float meanDistance = 0.0f, meanDispersion = 0.0f;

                bool validGroup = true;

                for (int c = 0; c < clusterMeans.Count; c++)
                {
                    if (c == selfCluster)
                        continue;

                    var clusterMean = clusterMeans[c];
                    
                    float distance = 0.0f;

                    for (int i = 0; i < numDimensions; i++)
                    {
                        // Comparative distance with visible groups or matched groups
                        float groupValue = 0.0f, selfGroupValue = 0.0f;
                        clusterMean.matchedGroup.characteristics.TryGetValue(currentContext.relevantCharacteristcs[i], out groupValue);

                        if (!socialGroup.characteristics.TryGetValue(currentContext.relevantCharacteristcs[i], out selfGroupValue))
                        {
                            validGroup = false;
                            break;
                        }

                        float weight = currentContext.relevantCharacteristcs[i].weight;

                        float difference = (groupValue - selfGroupValue) * weight;
                        distance += difference * difference;
                    }

                    if (!validGroup)
                        break;

                    meanDistance += (float)Math.Sqrt((double)distance) / currentContext.GetMaxDistance();

                    meanDispersion += clusterMean.dispersion;
                }

                if (!validGroup)
                    continue;

                if (clusterMeans.Count > 1)
                {
                    meanDistance /= clusterMeans.Count - 1;
                    meanDispersion /= clusterMeans.Count - 1;
                    
                    groupComparativeFit = simulation.comparativeFitAlfa * meanDistance + (1.0f - simulation.comparativeFitAlfa) * 
                        (simulation.comparativeFitBeta * (1.0f - clusterMeans[selfCluster].dispersion) + (1.0f - simulation.comparativeFitBeta) * meanDispersion);
                }
                else
                {
                    meanDistance = 0.0f;

                    groupComparativeFit = 0.0f;
                }

                float groupSalience = groupComparativeFit * socialGroup.accessibility;

                socialGroup.salience = groupSalience;

                if (groupSalience > salience)
                {
                    selfGroup = socialGroup;
                    salience = groupSalience;
                    comparativeFit = groupComparativeFit;

                    groupMeanDistance = meanDistance;
                }
            }
        }

        public void CalculateNormativeFit(Context currentContext, Simulation simulation)
        {
            int numDimensions = currentContext.relevantCharacteristcs.Count;

            for (int c = 0; c < clusterMeans.Count; c++)
            {
                var clusterMean = clusterMeans[c];

                float bestDistance = float.MaxValue;

                foreach (var socialGroup in knowledgeBase)
                {
                    bool validGroup = true;

                    float distance = 0.0f;

                    for (int i = 0; i < numDimensions; i++)
                    {
                        float groupValue = 0.0f;
                        if (!socialGroup.characteristics.TryGetValue(currentContext.relevantCharacteristcs[i], out groupValue))
                        {
                            validGroup = false;
                            break;
                        }

                        float weight = currentContext.relevantCharacteristcs[i].weight;

                        float difference = (clusterMean.mean[i] - groupValue) * weight;
                        distance += difference * difference;
                    }

                    if (!validGroup)
                        continue;

                    distance = (float)Math.Sqrt((double)distance);

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        clusterMean.matchedGroup = socialGroup;
                    }
                }

                // ad-hoc group
                if (bestDistance > simulation.normativeMatchDistance * currentContext.GetMaxDistance())
                {
                    numAdhocGroups++;

                    SocialGroup adhocGroup = new SocialGroup();
                    adhocGroup.isAdHoc = true;

                    adhocGroup.characteristics = new Dictionary<Characteristic, float>();

                    adhocGroup.name = "Group " + numAdhocGroups;

                    for (int i = 0; i < numDimensions; i++)
                    {
                        adhocGroup.characteristics.Add(currentContext.relevantCharacteristcs[i], clusterMean.mean[i]);
                    }

                    adhocGroup.CalculateAccessibility(this);

                    knowledgeBase.Add(adhocGroup);
                    
                    clusterMean.matchedGroup = adhocGroup;
                }

                clusterMeans[c] = clusterMean;
            }
        }

        public void CalculateDispersion(Context currentContext, Simulation simulation)
        {
            int numDimensions = currentContext.relevantCharacteristcs.Count;

            for (int c = 0; c < clusterMeans.Count; c++)
            {
                var clusterMean = clusterMeans[c];

                // Calculate dispersion
                clusterMean.dispersion = 0.0f;

                foreach (var agentCluster in agentsCluster)
                {
                    if (agentCluster.cluster != c)
                        continue;

                    float distance = 0.0f;

                    for (int i = 0; i < numDimensions; i++)
                    {
                        float agentValue = 0.0f, groupValue = 0.0f;
                        agentCluster.agent.characteristics.TryGetValue(currentContext.relevantCharacteristcs[i], out agentValue);

                        // Dispersion with visible group...
                        groupValue = clusterMean.mean[i];
                        // ...instead of matched group
                        //clusterMean.matchedGroup.characteristics.TryGetValue(currentContext.relevantCharacteristcs[i], out groupValue);

                        float weight = currentContext.relevantCharacteristcs[i].weight;

                        float difference = (groupValue - agentValue) * weight;
                        distance += difference * difference;
                    }

                    distance = (float)Math.Sqrt((double)distance);

                    clusterMean.dispersion += distance;
                }

                clusterMean.dispersion /= clusterMean.numAgents * (simulation.distanceConstraint * currentContext.GetMaxDistance());

                clusterMeans[c] = clusterMean;
            }
        }

        public void CreateContextClusters(Context currentContext, Simulation simulation)
        {
            // K-means clustering
            agentsCluster = new List<AgentCluster>(currentContext.contextAgents.Count);

            // Add agents 
            foreach (Agent contextAgent in currentContext.contextAgents)
            {
                agentsCluster.Add(new AgentCluster(contextAgent));
            }

            int numDimensions = currentContext.relevantCharacteristcs.Count;

            int numClusters = 0;

            // Todo: possibly change to fixed seed
            Random randomCluster = new Random(0);

            for (; ; )
            {
                numClusters++;

                clusterMeans = new List<ClusterMean>(numClusters);

                for (int i = 0; i < numClusters; i++)
                {
                    clusterMeans.Add(new ClusterMean(numDimensions));
                }

                bool needExtraCluster = false;
                int constraintIterations = 0;

                // Iterate clusters
                for (int it = 0; ; it++)
                {
                    bool changedClustering = false;

                    float maxDistance = 0.0f;

                    if (it == 0)
                    {
                        // Assign random cluster
                        changedClustering = true;

                        var clusterNumberList = new List<int>(agentsCluster.Count);
                        for (int i = 0; i < agentsCluster.Count; i++)
                        {
                            clusterNumberList.Add(i % numClusters);
                        }

                        for (int i = 0; i < agentsCluster.Count; i++)
                        {
                            int clusterNumber = randomCluster.Next(0, clusterNumberList.Count);

                            var agentCluster = agentsCluster[i];
                            agentCluster.cluster = clusterNumberList[clusterNumber];
                            agentsCluster[i] = agentCluster;

                            clusterNumberList.RemoveAt(clusterNumber);
                        }
                    }
                    else
                    {
                        // Find closest cluster
                        for (int i = 0; i < agentsCluster.Count; i++)
                        {
                            var agentCluster = agentsCluster[i];

                            float bestDistance = float.MaxValue;
                            int bestCluster = 0;

                            for (int c = 0; c < clusterMeans.Count; c++)
                            {
                                var clusterMean = clusterMeans[c];

                                float distance = 0.0f;

                                for (int Idx = 0; Idx < numDimensions; Idx++)
                                {
                                    float agentValue = 0.0f;
                                    agentCluster.agent.characteristics.TryGetValue(currentContext.relevantCharacteristcs[Idx], out agentValue);

                                    float weight = currentContext.relevantCharacteristcs[Idx].weight;

                                    float difference = (clusterMean.mean[Idx] - agentValue) * weight;
                                    distance += difference * difference;
                                }

                                distance = (float)Math.Sqrt((double)distance);

                                if (distance < bestDistance)
                                {
                                    bestDistance = distance;
                                    bestCluster = c;
                                }
                            }

                            if (agentCluster.cluster != bestCluster)
                            {
                                changedClustering = true;
                                agentCluster.cluster = bestCluster;
                            }

                            maxDistance = Math.Max(maxDistance, bestDistance);

                            agentsCluster[i] = agentCluster;
                        }
                    }

                    if (!changedClustering)
                    {
                        if (maxDistance < simulation.distanceConstraint * currentContext.GetMaxDistance())
                            break;

                        constraintIterations++;

                        if (constraintIterations > 10)
                        {
                            needExtraCluster = true;
                            break;
                        }
                    }

                    // Calculate means
                    for (int c = 0; c < clusterMeans.Count; c++)
                    {
                        var clusterMean = clusterMeans[c];

                        clusterMean.numAgents = 0;

                        for (int idx = 0; idx < numDimensions; idx++)
                        {
                            clusterMean.mean[idx] = 0.0f;
                        }

                        clusterMeans[c] = clusterMean;
                    }

                    foreach (var agentCluster in agentsCluster)
                    {
                        var clusterMean = clusterMeans[agentCluster.cluster];
                        clusterMean.numAgents++;

                        for (int idx = 0; idx < numDimensions; idx++)
                        {
                            float value = 0.0f;
                            agentCluster.agent.characteristics.TryGetValue(currentContext.relevantCharacteristcs[idx], out value);
                            
                            clusterMean.mean[idx] += value;
                        }

                        clusterMeans[agentCluster.cluster] = clusterMean;
                    }

                    bool restartCluster = false;

                    foreach (var clusterMean in clusterMeans)
                    {
                        if (clusterMean.numAgents == 0)
                        {
                            restartCluster = true;
                            break;
                        }
                        
                        for (int idx = 0; idx < numDimensions; idx++)
                        {
                            clusterMean.mean[idx] /= clusterMean.numAgents;
                        }
                    }

                    if (restartCluster)
                        it = 0;
                }

                if (!needExtraCluster)
                    break;
            }
        }
    }
}
