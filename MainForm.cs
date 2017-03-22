﻿using DIMA_Sim.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Xml;
using System.Xml.Linq;

namespace DIMA_Sim
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            this.simulations = new List<Simulation>();
        }

        private List<Simulation> simulations;
        private Simulation averageSimulation;
        private Context simulationContext;

        private string LoadAgents(XDocument xmlReader)
        {
            try
            {
                var simulation = new Simulation();
                simulation.LoadFromXml(xmlReader);
                simulations.Add(simulation);
            }
            catch (Exception e)
            {
                return string.Format("Failed to process file.\n{0}", e);
            }

            return "Agents Loaded";
        }
        private string RunContext(XDocument xmlReader, int numSteps)
        {
            try
            {
                simulationContext = new Model.Context();
                simulationContext.LoadFromXml(xmlReader);
                simulationContext.AssignAgents(simulations.Last());

                for (int i = 0; i < numSteps; i++)
                {
                    foreach (var agent in simulationContext.contextAgents)
                    {
                        agent.CreateContextClusters(simulationContext, simulations.Last());
                        agent.CalculateDispersion(simulationContext, simulations.Last());
                        agent.CalculateNormativeFit(simulationContext, simulations.Last());
                        agent.CalculateComparativeFit(simulationContext, simulations.Last());
                        agent.UpdateExportData(simulationContext);
                        agent.UpdateAccessibility(simulationContext);
                    }

                    foreach (var agent in simulations.Last().agents)
                    {
                        if (!simulationContext.contextAgents.Contains(agent))
                            agent.NullExportData();
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error");
            }

            return "Finished";
        }

        delegate void ExportDelegate(string name, Func<Model.Agent.ExportData, string> dataExport);
        delegate void KbExportDelegate(Model.Agent agent, string name, Func<Model.Agent.KbExportData, string> dataExport);

        private void Export(string filePath)
        {
            CultureInfo ci = new CultureInfo("pt-PT", false);

            // Export to CVS
            var csv = new StringBuilder();

            csv.AppendFormat(ci, "sep=\t;\n");
            csv.AppendFormat(ci, "alfa\t{0}\n", simulations.Last().comparativeFitAlfa);
            csv.AppendFormat(ci, "beta\t{0}\n", simulations.Last().comparativeFitBeta);
            csv.AppendFormat(ci, "distance constraint\t{0}\n", simulations.Last().distanceConstraint);
            csv.AppendFormat(ci, "normative match distance\t{0}\n", simulations.Last().normativeMatchDistance);

            csv.AppendFormat(ci, "total agents\t{0}\n", simulations.Last().agents.Count);

            /*
            csv.Append("theme characteristics\t");

            foreach (var characteristic in simulationContext.relevantCharacteristcs)
                csv.AppendFormat(ci, "{0}\t", characteristic.name);

            csv.AppendLine();
            csv.AppendLine();
            */


            foreach (var characteristic in simulationContext.relevantCharacteristcs)
            {
                csv.AppendFormat(ci, "characteristic '{0}'\t", characteristic.name);
                csv.AppendFormat(ci, "{0}\t", characteristic.weight);

                csv.AppendLine();
            }

            csv.AppendLine();

            csv.Append("\t");
            foreach (var agent in simulations.Last().agents)
                csv.AppendFormat(ci, "{0}\t", agent.name);

            csv.AppendLine();

            foreach (var characteristic in simulationContext.relevantCharacteristcs)
            {
                csv.AppendFormat(ci, "characteristic '{0}'\t", characteristic.name);
                foreach (var agent in simulations.Last().agents)
                    csv.AppendFormat(ci, "{0}\t", agent.characteristics[characteristic]);

                csv.AppendLine();
            }

            csv.AppendLine();

            /*
            csv.Append("total characteristics\t");
            foreach (var agent in simulation.agents)
                csv.AppendFormat(ci, "{0}\t", agent.characteristics.Count);

            csv.AppendLine();
            
            csv.Append("group\t");
            foreach (var agent in simulation.agents)
                csv.AppendFormat(ci, "{0}\t", agent.GetSelfCluster());

            csv.AppendLine();
            */

            ExportDelegate varExport = (name, dataExport) =>
                {
                    csv.Append(name + "\t");
                    csv.AppendLine();

                    foreach (var agent in simulations.Last().agents)
                    {
                        csv.AppendFormat(ci, "{0}\t", agent.name);

                        foreach (var data in agent.exportData)
                        {
                            if (data.group == null)
                                csv.AppendFormat(ci, "N/A\t");
                            else
                                csv.AppendFormat(ci, "{0}\t", dataExport(data));
                        }

                        csv.AppendLine();
                    }

                    csv.AppendLine();
                };

            varExport("matched group name", dataExport => dataExport.group.name);
            varExport("agent-group distance", dataExport => dataExport.agentDistance.ToString());
            varExport("group dispersion", dataExport => dataExport.dispersion.ToString());
            varExport("group distance", dataExport => dataExport.groupDistance.ToString());
            varExport("comparative fit", dataExport => dataExport.comparativeFit.ToString());
            varExport("accessibility", dataExport => dataExport.accessibility.ToString());
            varExport("salience", dataExport => dataExport.salience.ToString());
            varExport("wealth", dataExport => dataExport.wealth.ToString());

            KbExportDelegate kbExport = (agent, name, dataExport) =>
            {
                csv.Append(name + "\t");
                csv.AppendLine();

                List<List<string>> orderedValues = new List<List<string>>();

                Int32 numDataValues = 0;
                foreach (var data in agent.exportData)
                {
                    Int32 numKb = data.kbData.Count;

                    for (Int32 i = 0; i < numKb; i++)
                    {
                        if (orderedValues.Count <= i)
                        {
                            orderedValues.Add(new List<string>());

                            for (Int32 ii = 0; ii < numDataValues; ii++)
                                orderedValues[i].Add("N/A");
                        }

                        orderedValues[i].Add(dataExport(data.kbData[i]));
                    }

                    for (Int32 i = numKb; i < orderedValues.Count; i++)
                        orderedValues[i].Add("N/A");

                    numDataValues++;
                }

                Int32 numValues = orderedValues.Count;

                for (Int32 i = 0; i < numValues; i++)
                {
                    if (i < agent.knowledgeBase.Count)
                        csv.Append(agent.knowledgeBase[i].name + "\t");
                    else
                        csv.Append("Unfamiliar_Group" + "\t");

                    foreach (var value in orderedValues[i])
                    {
                        csv.Append(value + "\t");
                    }

                    csv.AppendLine();
                }

                csv.AppendLine();
            };

            foreach (var agent in simulations.Last().agents)
            {
                csv.Append(agent.name + "\t");
                csv.AppendLine();

                kbExport(agent, "salience", dataExport => dataExport.salience.ToString());
                kbExport(agent, "accessibility", dataExport => dataExport.accessibility.ToString());

                csv.AppendLine();
            }

            csv.AppendLine();
            try
            {
                File.WriteAllText(filePath, csv.ToString());
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error");
            }

        }


        private void createComboBoxOptions(Simulation sim)
        {
            this.comboBox1.Items.Clear();
            this.comboBox1.Items.Add("Characteristics");
            this.comboBox1.Items.Add("Wealth");
            this.comboBox1.Items.Add("Population");
            for (int i = 0; i < simulations.Last().agents[0].clusterMeans.Count; i++)
            {
                this.comboBox1.Items.Add("Group " + (i + 1));
            }
        }

        private async void runButton_Click(object sender, EventArgs e)
        {
            textBoxOutputFile.Text = string.Empty;
            try
            {
                var xmlReader = XDocument.Load(textBoxAgentsSource.Text);
                for(int i = 0; i < numberOfRuns.Value; i++)
                {
                    await Task.Run(() => LoadAgents(xmlReader));
                    xmlReader = XDocument.Load(textBoxContextSource.Text);
                    await Task.Run(() => RunContext(xmlReader, (int)numberOfSteps.Value));
                }

                var filename = textBoxOutputFolder.Text + string.Format("\\output-{0:yyyy-MM-dd_hh-mm-ss}.csv", DateTime.Now);
                Export(filename);
                textBoxOutputFile.Text = "Output generated at '" + filename + "'";
                this.comboBox1_SelectedIndexChanged(sender, e);

                //add comboBox options
                createComboBoxOptions(simulations.Last());

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.textBoxAgentsSource.Text = Properties.Settings.Default.AgentsSource;
            this.textBoxContextSource.Text = Properties.Settings.Default.ContextSource;
            this.textBoxOutputFolder.Text = Properties.Settings.Default.OutputFolder;
            this.numberOfSteps.Value = Properties.Settings.Default.Steps;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.AgentsSource = textBoxAgentsSource.Text.Trim();
            Properties.Settings.Default.ContextSource = textBoxContextSource.Text.Trim();
            Properties.Settings.Default.OutputFolder = textBoxOutputFolder.Text.Trim();
            Properties.Settings.Default.Steps = numberOfSteps.Value;
            Properties.Settings.Default.Save();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var res = openFileDialog.ShowDialog();
            if (res == DialogResult.OK)
            {
                textBoxAgentsSource.Text = openFileDialog.FileName;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {

            var res = openFileDialog.ShowDialog();
            if (res == DialogResult.OK)
            {
                textBoxContextSource.Text = openFileDialog.FileName;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    textBoxOutputFolder.Text = fbd.SelectedPath;
                }
            }
        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void ClearChart()
        {
            this.chart1.Series.Clear();
            this.chart1.Legends.Clear();
            this.chart1.ChartAreas.Clear();
            this.chart1.Titles.Clear();
        }

        private void DisplayCharacteristicsChart()
        {
            ClearChart();
            var chartArea = new ChartArea
            {
                AxisX = createAxis(Consts.CHARACTERISTIC_MIN_VALUE, Consts.CHARACTERISTIC_MAX_VALUE,
                        10, simulationContext.relevantCharacteristcs[0].name),
                AxisY = createAxis(Consts.CHARACTERISTIC_MIN_VALUE, Consts.CHARACTERISTIC_MAX_VALUE,
                        10, simulationContext.relevantCharacteristcs[1].name)
            };

            var charTitle = new Title { Text = "Characteristics" };

            for (int i = 0; i < simulations.Last().agents.Count(); i++)
            {
                int clusterPos = simulations.Last().agents[i].GetSelfCluster();
                var series = new Series
                {
                    Name = simulations.Last().agents[i].name,
                    Color = Consts.COLORS[clusterPos],
                    BorderWidth = 5,
                    MarkerSize = 10,
                    IsVisibleInLegend = true,
                    ChartType = SeriesChartType.Point
                };
                series.Points.AddXY(
                    simulations.Last().agents[i].characteristics[simulationContext.relevantCharacteristcs[0]],
                    simulations.Last().agents[i].characteristics[simulationContext.relevantCharacteristcs[1]]);
                this.chart1.Series.Add(series);
            }

            //Clusters
            for (int i = 0; i < simulations.Last().agents[0].clusterMeans.Count(); i++)
            {
                var series = new Series
                {
                    Name = "Cluster " + (i + 1),
                    Color = Consts.COLORS[i],
                    BorderWidth = 5,
                    MarkerSize = 10,
                    IsVisibleInLegend = true,
                    ChartType = SeriesChartType.Point
                };
                var clusterMean = simulations.Last().agents[0].clusterMeans[i];
                series.Points.AddXY(clusterMean.mean[0], clusterMean.mean[1]);
                this.chart1.Series.Add(series);
            }

            /*
            //Groups
            for (int i = 0; i < simulation.agents[0].knowledgeBase.Count(); i++)
            {
                var series = new Series
                {
                    Name = "Group " + (i + 1),
                    Color = Consts.COLORS[i],
                    BorderWidth = 5,
                    MarkerSize = 10,
                    IsVisibleInLegend = true,
                    ChartType = SeriesChartType.Point
                };
                var kb = simulation.agents[0].knowledgeBase[i];
                var x = kb.characteristics[simulationContext.relevantCharacteristcs[0]];
                var y = kb.characteristics[simulationContext.relevantCharacteristcs[1]];
                series.Points.AddXY(x, y);
                this.chart1.Series.Add(series);
            }*/


            this.chart1.Legends.Add(new Legend { Title = "Agent" });
            this.chart1.ChartAreas.Add(chartArea);
            this.chart1.Titles.Add(charTitle);
        }



        private void DisplayGroupChart(int groupNumber)
        {
            ClearChart();

            var chartArea = new ChartArea
            {
                AxisX = createAxis(1, (int)numberOfSteps.Value, 1, "Step"),
                AxisY = createAxis(0, 1, 0.1, "Salience"),
            };

            var charTitle = new Title { Text = "Group " + groupNumber };

            for (int i = 0; i < simulations.Last().agents.Count(); i++)
            {
                var series = new Series
                {
                    Name = simulations.Last().agents[i].name,
                    Color = Consts.COLORS[i],
                    BorderWidth = 5,
                    MarkerSize = 10,
                    IsVisibleInLegend = true,
                    ChartType = SeriesChartType.Point
                };

                foreach (var data in simulations.Last().agents[i].exportData)
                {
                    if (groupNumber - 1 < data.kbData.Count)
                    {
                        series.Points.AddY(data.kbData[groupNumber - 1].salience);
                    }
                    else
                    {
                        series.Points.AddY(0);
                    }
                }

                this.chart1.Series.Add(series);
            }

            this.chart1.Legends.Add(new Legend { Title = "Agent" });
            this.chart1.ChartAreas.Add(chartArea);
            this.chart1.Titles.Add(charTitle);
        }


        private void DisplayWealthGroupChart()
        {
            ClearChart();

            var chartArea = new ChartArea
            {
                AxisX = new Axis { Title = "Step" },
                AxisY = new Axis { Title = "Wealth"},
            };

            var charTitle = new Title { Text = "Group Average Wealth"};

            //Groups
            for (int i = 0; i < simulations.Last().agents[0].knowledgeBase.Count(); i++)
            {
                var groupName = simulations.Last().agents[0].knowledgeBase[i].name;
                var series = this.CreateSeries(groupName, Consts.COLORS[i]);
                for(int s = 0; s < (int)numberOfSteps.Value; s++)
                {
                    var wealth = 0f;
                    int numOfAgents = 0;
                    foreach(var agent in simulations.Last().agents)
                    {
                        if(agent.exportData[s].group.name == groupName)
                        {
                            wealth += agent.exportData[s].wealth;
                            numOfAgents++;
                        }   
                    }
                    series.Points.Add(wealth / numOfAgents);
                }
                this.chart1.Series.Add(series);
            }

            this.chart1.Legends.Add(new Legend { Title = "Group" });
            this.chart1.ChartAreas.Add(chartArea);
            this.chart1.Titles.Add(charTitle);
        }

        private void DisplayPopulationChart()
        {
            ClearChart();

            var chartArea = new ChartArea
            {
                AxisX = new Axis { Title = "Step" },
                AxisY = new Axis { Title = "Population" },
            };

            var charTitle = new Title { Text = "Population Size Per Group" };

            //Groups
            for (int i = 0; i < simulations.Last().agents[0].knowledgeBase.Count(); i++)
            {
                var groupName = simulations.Last().agents[0].knowledgeBase[i].name;
                var series = this.CreateSeries(groupName, Consts.COLORS[i]);
                series.ChartType = SeriesChartType.Line;
                for (int s = 0; s < (int)numberOfSteps.Value; s++)
                {
                    int numOfAgents = 0;
                    foreach (var agent in simulations.Last().agents)
                    {
                        if (agent.exportData[s].group.name == groupName && agent.exportData[s].salience > agent.minimalSalienceThreshold)
                        {
                            numOfAgents++;
                        }
                    }
                    series.Points.Add(numOfAgents);
                }
                this.chart1.Series.Add(series);

            }
            //Add the no-group
            var noGroupseries = this.CreateSeries("No Group", Consts.COLORS[6]);
        //    var noGroupseries = this.CreateSeries("No Group", Consts.COLORS[6]);

            this.chart1.Legends.Add(new Legend { Title = "Group" });
            this.chart1.ChartAreas.Add(chartArea);
            this.chart1.Titles.Add(charTitle);
        }

        /*
        private void DisplayAgentChart(string agentName)
        {
            ClearChart();

            var agent = simulations.Last().agents.Where(a => a.name == agentName).FirstOrDefault();

            var chartArea = new ChartArea
            {
                AxisX = createAxis(1, (int)numberOfSteps.Value, 1, "Step"),
                AxisY = createAxis(0, agent.knowledgeBase.Count, 1, "Most Salient Groupr"),
            };

            var charTitle = new Title { Text = agentName };

            var series = this.CreateSeries(agent.name, Consts.COLORS[0]);

            foreach (var data in agent.exportData)
            {
                var groupName = data.group.name;
                var groupNumber = Int32.Parse(groupName.Split()[1]);
                series.Points.AddY(groupNumber);
            }

            this.chart1.Series.Add(series);
            this.chart1.ChartAreas.Add(chartArea);
            this.chart1.Titles.Add(charTitle);
        }
        */

        private Series CreateSeries(string name, Color color)
        {
            return new Series
            {
                Name = name,
                Color = color,
                BorderWidth = 5,
                MarkerSize = 10,
                IsVisibleInLegend = true,
                ChartType = SeriesChartType.Point
            };
        }
        private Axis createAxis(int min, int max, double interval, string title)
        {
            return new Axis
            {
                Interval = interval,
                Minimum = min,
                Maximum = max,
                Title = title
            };
        }
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (comboBox1.SelectedItem != null)
                {
                    if (comboBox1.SelectedItem.ToString().Contains("Characteristics"))
                        this.DisplayCharacteristicsChart();
                    if (comboBox1.SelectedItem.ToString().Contains("Group"))
                    {
                        var str = comboBox1.SelectedItem.ToString();
                        var words = str.Split();
                        DisplayGroupChart(Int32.Parse(words[1]));
                    }
                    if (comboBox1.SelectedItem.ToString().Contains("Population"))
                    {
                        this.DisplayPopulationChart();
                    }
                    if (comboBox1.SelectedItem.ToString().Contains("Wealth"))
                    {
                        DisplayWealthGroupChart();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }

        }

        

        private void chart1_Click(object sender, EventArgs e)
        {

        }
    }
}
