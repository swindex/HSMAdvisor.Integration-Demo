using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using HSMAdvisor.Integration.Client;
using HSMAdvisor.Integration.Protocol;
using IntegrationDemo.Tasks;

namespace IntegrationDemo
{
    public partial class MainForm : Form
    {
        private static readonly Color RunColor = Color.FromArgb(0, 122, 204);
        private static readonly Color CancelColor = Color.FromArgb(196, 43, 28);

        private readonly IntegrationSession session = new IntegrationSession();
        private List<DemoTask> allTasks;
        private DemoTask currentTask;
        private Dictionary<string, Control> parameterControls;
        private CancellationTokenSource running;
        private bool showJson;

        public MainForm()
        {
            InitializeComponent();
            parameterControls = new Dictionary<string, Control>();
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                txtOutput.Text = "=== HSMAdvisor.Integration Demo Application ===\n\n";
                txtOutput.AppendText("This demo drives the separately installed HSMAdvisor application through\n");
                txtOutput.AppendText("HSMAdvisor.Integration.dll (JSON messages over a per-user named pipe).\n");
                txtOutput.AppendText("HSMAdvisorCore.dll is never loaded into this process, so the same code works\n");
                txtOutput.AppendText("from .NET Framework 4.8 and from .NET 8/10 hosts.\n\n");

                txtOutput.AppendText($"Demo build              : {IntegrationSession.AppVersion} ({System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription})\n");
                txtOutput.AppendText($"HSMAdvisor.Integration  : {IntegrationSession.IntegrationVersion}\n");
                txtOutput.AppendText($"Minimum HSMAdvisor      : {IntegrationSession.MinimumHSMAdvisorVersion}\n\n");

                txtOutput.AppendText("Locating HSMAdvisor...\n");
                txtOutput.AppendText("  → API: HSMAdvisorLocator.FindExecutable()\n");
                string exe = HSMAdvisorLocator.FindExecutable();
                txtOutput.AppendText($"     Returns: {exe ?? "null"}\n");
                if (exe == null)
                {
                    txtOutput.AppendText(OutputFormatter.FormatWarning("HSMAdvisor.exe was not found. Install HSMAdvisor (" + HSMAdvisorClient.DownloadUrl + "),"));
                    txtOutput.AppendText("  set the " + HSMAdvisorLocator.EnvironmentVariable + " environment variable, or use \"Use a Specific HSMAdvisor.exe\".\n");
                }

                // Load all tasks
                LoadAllTasks();

                // Build tree view
                BuildTaskTree();

                txtOutput.AppendText("\nWelcome! Select a task from the left to begin.\n");
                txtOutput.AppendText("HSMAdvisor is started (without a main window) on the first request and exits\n");
                txtOutput.AppendText("a few seconds after this demo closes.\n");

                await RefreshStatusAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing application:\n{ex.Message}",
                    "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadAllTasks()
        {
            allTasks = new List<DemoTask>();

            // Connection tasks
            allTasks.Add(new LocateHSMAdvisorTask());
            allTasks.Add(new CheckRunningTask());
            allTasks.Add(new HelloTask());
            allTasks.Add(new ShowMainWindowTask());
            allTasks.Add(new SetExecutablePathTask());

            // Reference data tasks
            allTasks.Add(new ListToolTypesTask());
            allTasks.Add(new ListToolMaterialsAndCoatingsTask());
            allTasks.Add(new ListMachinesTask());
            allTasks.Add(new ListMaterialsTask());
            allTasks.Add(new FindMaterialsTask());
            allTasks.Add(new FindToolsTask());

            // Interactive HSMAdvisor dialogs
            allTasks.Add(new EditCalculationTask());
            allTasks.Add(new SelectMaterialTask());
            allTasks.Add(new SelectMachineTask());
            allTasks.Add(new SelectCutTask());
            allTasks.Add(new AddCutTask());

            // Calculation tasks
            allTasks.Add(new BasicMillingCalculationTask());
            allTasks.Add(new HSMMachiningCalculationTask());
            allTasks.Add(new SlottingCalculationTask());
            allTasks.Add(new DrillingCalculationTask());
            allTasks.Add(new MachineLimitCalculationTask());

            // Advanced tasks
            allTasks.Add(new MetricImperialConversionTask());
            allTasks.Add(new TranslateTask());
            allTasks.Add(new CancelInteractiveRequestTask());
            allTasks.Add(new ErrorResponsesTask());
            allTasks.Add(new RawProtocolTask());
        }

        private void BuildTaskTree()
        {
            treeViewTasks.Nodes.Clear();

            // Group tasks by category, in the order they were added
            var categories = allTasks.GroupBy(t => t.Category);

            foreach (var category in categories)
            {
                TreeNode categoryNode = new TreeNode(category.Key);
                categoryNode.NodeFont = new Font(treeViewTasks.Font, FontStyle.Bold);

                foreach (var task in category)
                {
                    TreeNode taskNode = new TreeNode(task.Name);
                    taskNode.Tag = task;
                    categoryNode.Nodes.Add(taskNode);
                }

                treeViewTasks.Nodes.Add(categoryNode);

                // Re-set the text once the node is in the tree, so its width is measured with the bold font
                categoryNode.Text = category.Key;
            }

            // Expand all categories
            treeViewTasks.ExpandAll();
        }

        private void treeViewTasks_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag is DemoTask task)
            {
                currentTask = task;
                DisplayTaskInfo(task);
                CreateParameterControls(task);
                btnRunTask.Enabled = true;
            }
            else
            {
                currentTask = null;
                lblTaskTitle.Text = "HSMAdvisor.Integration Demo";
                lblTaskDescription.Text = "Select a task from the list to see its description.";
                panelParameters.Controls.Clear();
                btnRunTask.Enabled = false;
            }
        }

        private void DisplayTaskInfo(DemoTask task)
        {
            lblTaskTitle.Text = task.Name;
            lblTaskDescription.Text = task.Description;

            if (task.Parameters.Count > 0)
            {
                lblTaskDescription.Text += $"\n\nThis task has {task.Parameters.Count} configurable parameter(s).";
            }
        }

        private void CreateParameterControls(DemoTask task)
        {
            panelParameters.Controls.Clear();
            parameterControls.Clear();

            if (task.Parameters.Count == 0)
            {
                Label noParamsLabel = new Label
                {
                    Text = "This task has no configurable parameters.",
                    AutoSize = true,
                    Location = new Point(10, 10),
                    ForeColor = Color.Gray
                };
                panelParameters.Controls.Add(noParamsLabel);
                return;
            }

            int yPos = 10;

            foreach (var param in task.Parameters)
            {
                // Label
                Label label = new Label
                {
                    Text = param.Label + ":",
                    Location = new Point(10, yPos),
                    AutoSize = true,
                    Width = 150
                };
                panelParameters.Controls.Add(label);

                // Input control
                Control inputControl;

                if (param.ParameterType == typeof(bool))
                {
                    CheckBox checkBox = new CheckBox
                    {
                        Location = new Point(170, yPos - 2),
                        Checked = param.DefaultValue != null && (bool)param.DefaultValue,
                        Width = 300
                    };
                    inputControl = checkBox;
                }
                else if (param.Options != null && param.Options.Count > 0)
                {
                    ComboBox comboBox = new ComboBox
                    {
                        Location = new Point(170, yPos),
                        Width = 300,
                        DropDownStyle = ComboBoxStyle.DropDownList
                    };
                    comboBox.Items.AddRange(param.Options.ToArray());
                    if (param.DefaultValue != null)
                    {
                        comboBox.SelectedItem = param.DefaultValue.ToString();
                    }
                    inputControl = comboBox;
                }
                else
                {
                    TextBox textBox = new TextBox
                    {
                        Location = new Point(170, yPos),
                        Width = 300,
                        Text = param.DefaultValue?.ToString() ?? ""
                    };
                    inputControl = textBox;
                }

                panelParameters.Controls.Add(inputControl);
                parameterControls[param.Name] = inputControl;

                // Description (smaller text)
                if (!string.IsNullOrEmpty(param.Description))
                {
                    Label descLabel = new Label
                    {
                        Text = param.Description,
                        Location = new Point(170, yPos + 25),
                        AutoSize = false,
                        Width = 500,
                        Height = 30,
                        ForeColor = Color.Gray,
                        Font = new Font(this.Font.FontFamily, 8f)
                    };
                    panelParameters.Controls.Add(descLabel);
                    yPos += 30;
                }

                yPos += 35;
            }
        }

        private async void btnRunTask_Click(object sender, EventArgs e)
        {
            // While a task runs the button is "■ Cancel": closing the pipe makes HSMAdvisor close its dialog.
            if (running != null)
            {
                btnRunTask.Enabled = false;
                btnRunTask.Text = "Cancelling...";
                running.Cancel();
                return;
            }

            if (currentTask == null)
            {
                MessageBox.Show("Please select a task first.", "No Task Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DemoTask task = currentTask;
            try
            {
                // Gather parameter values
                Dictionary<string, object> paramValues = new Dictionary<string, object>();

                foreach (var param in task.Parameters)
                {
                    if (parameterControls.ContainsKey(param.Name))
                    {
                        Control control = parameterControls[param.Name];

                        if (control is TextBox textBox)
                        {
                            paramValues[param.Name] = textBox.Text;
                        }
                        else if (control is CheckBox checkBox)
                        {
                            paramValues[param.Name] = checkBox.Checked;
                        }
                        else if (control is ComboBox comboBox)
                        {
                            paramValues[param.Name] = comboBox.SelectedItem?.ToString();
                        }
                    }
                }

                // Clear output
                txtOutput.Clear();
                txtOutput.AppendText($"Executing: {task.Name}\n");
                txtOutput.AppendText($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");
                txtOutput.AppendText(new string('-', 60) + "\n");

                // Execute task
                running = new CancellationTokenSource();
                SetRunning(true);

                var ctx = new TaskContext(session, Handle.ToInt64(), showJson, running.Token, WriteOutput);
                await task.ExecuteAsync(ctx, paramValues);

                // Display result
                txtOutput.AppendText("\n\n" + new string('=', 60) + "\n");
                txtOutput.AppendText("Task completed.\n");
            }
            catch (OperationCanceledException)
            {
                txtOutput.AppendText("\n" + OutputFormatter.FormatWarning("Cancelled. The pipe connection was closed, so HSMAdvisor closed the dialog it had opened for this request."));
            }
            catch (IntegrationException ex) when (ex.Code == HSMAdvisorClient.VersionTooOldCode)
            {
                txtOutput.AppendText("\n" + OutputFormatter.FormatError($"[{ex.Code}] {ex.Message}"));
                var answer = MessageBox.Show(this, ex.Message + "\n\nOpen the HSMAdvisor download page now?",
                    "HSMAdvisor update required", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (answer == DialogResult.Yes)
                {
                    OpenUrl(HSMAdvisorClient.DownloadUrl);
                }
            }
            catch (IntegrationException ex)
            {
                // error/busy responses, "not_installed", "not_running", ...
                txtOutput.AppendText("\n" + OutputFormatter.FormatError($"[{ex.Code}] {ex.Message}"));
            }
            catch (Exception ex)
            {
                txtOutput.AppendText("\n\n" + OutputFormatter.FormatError($"FATAL ERROR: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}"));
                MessageBox.Show($"Error executing task:\n{ex.Message}", "Execution Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                running?.Dispose();
                running = null;
                SetRunning(false);
            }

            await RefreshStatusAsync();
        }

        private void SetRunning(bool isRunning)
        {
            treeViewTasks.Enabled = !isRunning;
            panelParameters.Enabled = !isRunning;
            btnRunTask.Enabled = true;
            btnRunTask.Text = isRunning ? "■ Cancel" : "▶ Run Task";
            btnRunTask.BackColor = isRunning ? CancelColor : RunColor;
        }

        /// <summary>Appends task output; safe to call from any thread.</summary>
        private void WriteOutput(string text)
        {
            if (txtOutput.InvokeRequired)
            {
                txtOutput.BeginInvoke(new Action<string>(WriteOutput), text);
                return;
            }
            txtOutput.AppendText(text);
        }

        /// <summary>Shows whether HSMAdvisor is listening, without starting it.</summary>
        private async Task RefreshStatusAsync()
        {
            try
            {
                bool isRunning = await session.Client.IsRunningAsync(CancellationToken.None);
                var version = session.Client.ServerVersion;
                lblStatus.Text = isRunning
                    ? "● HSMAdvisor: running" + (version == null ? "" : " (v" + version + ")")
                    : "○ HSMAdvisor: not running (starts on the first request)";
                lblStatus.ForeColor = isRunning ? Color.DarkGreen : Color.DimGray;
            }
            catch (Exception ex)
            {
                lblStatus.Text = "HSMAdvisor: " + ex.Message;
                lblStatus.ForeColor = Color.DarkRed;
            }
        }

        private void btnShowJson_Click(object sender, EventArgs e)
        {
            showJson = !showJson;
            btnShowJson.Text = (showJson ? "☑" : "☐") + " Show JSON";
        }

        private void btnClearOutput_Click(object sender, EventArgs e)
        {
            txtOutput.Clear();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Close any HSMAdvisor dialog this demo still has open.
            running?.Cancel();
        }

        internal static void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open " + url + ": " + ex.Message, "HSMAdvisor", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
    }
}
