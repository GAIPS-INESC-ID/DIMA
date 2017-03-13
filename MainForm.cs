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

            runButton.Enabled = false;
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

            File.WriteAllText(filePath, csv.ToString());
        }

        private async void loadButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();

            fileDialog.Filter = "Xml File (*.xml)|*.xml";
            fileDialog.RestoreDirectory = true;

            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                runButton.Enabled = false;


                var xmlReader = XDocument.Load(fileDialog.FileName);

                await Task.Run(() => LoadAgents(xmlReader));
                /*
                messageLabel.Text += " " + fileDialog.SafeFileName;
               
                loadButton.Enabled = true;*/
                runButton.Enabled = true;
            }
        }

        private async void runButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();

            fileDialog.Filter = "Xml File (*.xml)|*.xml";
            fileDialog.RestoreDirectory = true;

            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
             /*   loadButton.Enabled = false;
                runButton.Enabled = false;
                saveButton.Enabled = false;

                messageLabel.Text = "Processing Context...";

                var xmlReader = XDocument.Load(fileDialog.FileName);


                messageLabel.Text = await Task.Run(() => RunContext(xmlReader, (int)numberOfRuns.Value));

                messageLabel.Text += " " + fileDialog.SafeFileName;

                loadButton.Enabled = true;
                runButton.Enabled = true;
                saveButton.Enabled = true;*/
            }
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "Csv File (*.csv)|*.csv";

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                Export(saveDialog.FileName);

             //   messageLabel.Text = "Exported " + saveDialog.FileName;
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.textBoxAgentsSource.Text = Properties.Settings.Default.AgentsSource;
        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void textBoxAgentsSource_TextChanged(object sender, EventArgs e)
        {
            
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.AgentsSource = textBoxAgentsSource.Text.Trim();
            Properties.Settings.Default.Save();
        }
    }
}
