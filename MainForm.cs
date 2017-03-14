using DIMA_Sim.Model;
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
using System.Xml;
using System.Xml.Linq;

namespace DIMA_Sim
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private Model.Simulation simulation;
        private Model.Context simulationContext;




        private string LoadAgents(XDocument xmlReader)
        {
            try
            {
                simulation = new Model.Simulation();
                simulation.LoadFromXml(xmlReader);
            }
            catch (Exception e)
            {
                return string.Format("Failed to process file.\n{0}", e);
            }

            return "Agents Loaded";
        }
        private string RunContext(XDocument xmlReader, int numRuns)
        {
            try
            {
                simulationContext = new Model.Context();
                simulationContext.LoadFromXml(xmlReader);
                simulationContext.AssignAgents(simulation);

                for (int i = 0; i < numRuns; i++)
                {
                    foreach (var agent in simulationContext.contextAgents)
                    {
                        agent.CreateContextClusters(simulationContext, simulation);
                        agent.CalculateDispersion(simulationContext, simulation);
                        agent.CalculateNormativeFit(simulationContext, simulation);
                        agent.CalculateComparativeFit(simulationContext, simulation);
                        agent.UpdateExportData(simulationContext);
                        agent.UpdateAccessibility(simulationContext);
                    }

                    foreach (var agent in simulation.agents)
                    {
                        if (!simulationContext.contextAgents.Contains(agent))
                            agent.NullExportData();
                    }
                }
            }
            catch (Exception e)
            {
                return string.Format("Failed to process file.\n{0}", e);
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
            csv.AppendFormat(ci, "alfa\t{0}\n", simulation.comparativeFitAlfa);
            csv.AppendFormat(ci, "beta\t{0}\n", simulation.comparativeFitBeta);
            csv.AppendFormat(ci, "distance constraint\t{0}\n", simulation.distanceConstraint);
            csv.AppendFormat(ci, "normative match distance\t{0}\n", simulation.normativeMatchDistance);

            csv.AppendFormat(ci, "total agents\t{0}\n", simulation.agents.Count);

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
            foreach (var agent in simulation.agents)
                csv.AppendFormat(ci, "{0}\t", agent.name);

            csv.AppendLine();

            foreach (var characteristic in simulationContext.relevantCharacteristcs)
            {
                csv.AppendFormat(ci, "characteristic '{0}'\t", characteristic.name);
                foreach (var agent in simulation.agents)
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

                    foreach (var agent in simulation.agents)
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

            foreach (var agent in simulation.agents)
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

        private async void runButton_Click(object sender, EventArgs e)
        {
            textBoxOutputFile.Text = string.Empty;
            try
            {
                var xmlReader = XDocument.Load(textBoxAgentsSource.Text);
                await Task.Run(() => LoadAgents(xmlReader));
                xmlReader = XDocument.Load(textBoxContextSource.Text);
                await Task.Run(() => RunContext(xmlReader, (int)numberOfRuns.Value));

                var filename = textBoxOutputFolder.Text + string.Format("\\output-{0:yyyy-MM-dd_hh-mm-ss}.csv", DateTime.Now);
                Export(filename);
                textBoxOutputFile.Text = "Output generated at '" + filename + "'";
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
            this.numberOfRuns.Value = Properties.Settings.Default.Steps;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.AgentsSource = textBoxAgentsSource.Text.Trim();
            Properties.Settings.Default.ContextSource = textBoxContextSource.Text.Trim();
            Properties.Settings.Default.OutputFolder = textBoxOutputFolder.Text.Trim();
            Properties.Settings.Default.Steps = numberOfRuns.Value;
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


        private void DisplayCharacteristicsChart()
        {
            this.chart1.Series.Clear();
            this.chart1.Legends.Clear();
            this.chart1.ChartAreas.Clear();
            this.chart1.Titles.Clear();

            var axisX = new System.Windows.Forms.DataVisualization.Charting.Axis
            {
                Interval = 10,
                Minimum = Consts.CHARACTERISTIC_MIN_VALUE,
                Maximum = Consts.CHARACTERISTIC_MAX_VALUE
            };

            var axisY = new System.Windows.Forms.DataVisualization.Charting.Axis
            {
                Interval = 10,
                Minimum = Consts.CHARACTERISTIC_MIN_VALUE,
                Maximum = Consts.CHARACTERISTIC_MAX_VALUE,
            };

            var chartArea = new System.Windows.Forms.DataVisualization.Charting.ChartArea { AxisX = axisX, AxisY = axisY };
            chartArea.AxisX.Title = simulationContext.relevantCharacteristcs[0].name;
            chartArea.AxisY.Title = simulationContext.relevantCharacteristcs[1].name;

            var charTitle = new System.Windows.Forms.DataVisualization.Charting.Title { Name = "Characteristics", Text = "Characteristics", Visible = true };
            var legends1 = new System.Windows.Forms.DataVisualization.Charting.Legend { Name = "Legenda" };

            for (int i = 0; i < simulation.agents.Count(); i++)
            {
                var series = new System.Windows.Forms.DataVisualization.Charting.Series
                {
                    Name = simulation.agents[i].name,
                    Color = Consts.COLORS[i],
                    BorderWidth = 5,
                    MarkerSize = 10,
                    IsVisibleInLegend = true,
                    ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Point
                };
                series.Points.AddXY(
                    simulation.agents[i].characteristics[simulationContext.relevantCharacteristcs[0]],
                    simulation.agents[i].characteristics[simulationContext.relevantCharacteristcs[1]]);
                this.chart1.Series.Add(series);
            }

            this.chart1.ChartAreas.Add(chartArea);
            this.chart1.Titles.Add(charTitle);
            this.chart1.Legends.Add(legends1);
            
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (comboBox1.SelectedItem != null)
                {
                    this.DisplayCharacteristicsChart();
                }
            }catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
            
        }

        private void chart1_Click(object sender, EventArgs e)
        {

        }
    }
}
