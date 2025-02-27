using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using Microsoft.Win32;

namespace UniLine
{
    public partial class MainWindow : MetroWindow
    {
        private readonly List<ProjectInfo> projects = [];
        private string solutionDirectory = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
        }

        // Called when the user clicks "Select Solution File"
        private void BtnSelectSolution_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new()
            {
                Filter = "Solution Files (*.sln)|*.sln"
            };

            if (dialog.ShowDialog() is true)
            {
                txtSolutionPath.Text = dialog.FileName;
                solutionDirectory = Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
                LoadProjects(dialog.FileName);
            }
        }

        // Parse the .sln file to extract projects with recognized extensions.
        private void LoadProjects(string slnPath)
        {
            projects.Clear();
            stackPanelProjects.Children.Clear();
            string[] lines = File.ReadAllLines(slnPath);
            Regex regex = new(@"^Project\("".*""\)\s*=\s*""(?<name>[^""]+)"",\s*""(?<path>[^""]+)"",\s*""(?<guid>[^""]+)""");
            
            foreach (var line in lines)
            {
                Match match = regex.Match(line);
                if (match.Success)
                {
                    var projectName = match.Groups["name"].Value;
                    var projectPath = match.Groups["path"].Value;
                    var ext = Path.GetExtension(projectPath).ToLower();

                    if (ext == ".csproj" || ext == ".vcxproj" || ext == ".vbproj" || ext == ".fsproj")
                    {
                        var fullProjectPath = Path.Combine(solutionDirectory, projectPath);
                        var projectDirectory = Path.GetDirectoryName(fullProjectPath) ?? string.Empty;
                        ProjectInfo projectInfo = new() { Name = projectName, Folder = projectDirectory };
                        projects.Add(projectInfo);

                        CheckBox checkBox = new() { Content = projectName, IsChecked = true, Tag = projectInfo };
                        stackPanelProjects.Children.Add(checkBox);
                    }
                }
            }
        }

        // Called when the user clicks "Process Selected Projects"
        private void BtnProcess_Click(object sender, RoutedEventArgs e)
        {
            // Determine selected EOL style.
            var selectedEndOfLineSymbol = rbWindows.IsChecked == true ? "\r\n" : "\n";
            StringBuilder aggregatedSolution = new();
            aggregatedSolution.AppendLine("=== Aggregated Solution: " + Path.GetFileName(txtSolutionPath.Text) + " ===");

            // Overall solution statistics.
            int totalFilesAll = 0, totalWinBefore = 0, totalLinuxBefore = 0, totalUpdated = 0;

            ProcessSelectedProjects(selectedEndOfLineSymbol, aggregatedSolution, ref totalFilesAll, ref totalWinBefore, ref totalLinuxBefore, ref totalUpdated);

            FinalizeProcessing(aggregatedSolution, totalFilesAll, totalWinBefore, totalLinuxBefore, totalUpdated);
        }

        private void FinalizeProcessing(StringBuilder aggregatedSolution, int totalFilesAll, int totalWinBefore, int totalLinuxBefore, int totalUpdated)
        {
            // Write aggregated source code to "Solution.md" in the solution folder.
            string outputPath = Path.Combine(solutionDirectory, "Solution.md");
            File.WriteAllText(outputPath, aggregatedSolution.ToString(), Encoding.UTF8);

            MessageBox.Show($"Processing complete.\nSolution.md written to: {outputPath}\n\nOverall Stats:\nTotal Files: {totalFilesAll}\nWindows EOL Before: {totalWinBefore}\nLinux EOL Before: {totalLinuxBefore}\nFiles Updated: {totalUpdated}");

            txtStatus.Text = "Processing complete.";
        }

        private void ProcessSelectedProjects(string selectedEndOfLineSymbol, StringBuilder aggregatedSolution, ref int totalFilesAll, ref int totalWinBefore, ref int totalLinuxBefore, ref int totalUpdated)
        {
            // Process each selected project.
            foreach (var child in stackPanelProjects.Children)
            {
                if (child is CheckBox checkBox && checkBox.IsChecked is true)
                {
                    ProjectInfo projectInfo = (ProjectInfo)checkBox.Tag;
                    txtStatus.Text = $"Processing project: {projectInfo.Name}...";

                    ProcessProjectStatistics(selectedEndOfLineSymbol, aggregatedSolution, ref totalFilesAll, ref totalWinBefore, ref totalLinuxBefore, ref totalUpdated, projectInfo);
                }
            }
        }

        private void ProcessProjectStatistics(string selectedEol, StringBuilder aggregatedSolution, ref int totalFilesAll, ref int totalWinBefore, ref int totalLinuxBefore, ref int totalUpdated, ProjectInfo projectInfo)
        {
            var statistics = ProcessProject(projectInfo.Folder, projectInfo.Name, selectedEol, out string projectAggregate);

            aggregatedSolution.AppendLine(projectAggregate);
            totalFilesAll += statistics.TotalFiles;
            totalWinBefore += statistics.WinBefore;
            totalLinuxBefore += statistics.LinuxBefore;
            totalUpdated += statistics.UpdatedCount;
        }

        // Process a single project: convert EOLs and aggregate source code.
        private ProjectStatistics ProcessProject(string projectFolder, string projectName, string selectedEol, out string aggregatedText)
        {
            ProjectStatistics statistics = new();
            StringBuilder aggregatedLines = new();
            aggregatedLines.AppendLine("\n\n=== Project: " + projectName + " ===");

            foreach (string file in Directory.EnumerateFiles(projectFolder, "*.*", SearchOption.AllDirectories))
            {
                if (IsBinaryFile(file))
                    continue;
                statistics.TotalFiles++;

                string content;
                try
                {
                    content = File.ReadAllText(file, Encoding.UTF8);
                }
                catch
                {
                    continue;
                }

                // Count current EOL style.
                if (content.Contains("\r\n"))
                    statistics.WinBefore++;
                else if (content.Contains("\n"))
                    statistics.LinuxBefore++;
                else
                    statistics.LinuxBefore++;

                // Normalize to LF then convert to selected EOL.
                string normalized = content.Replace("\r\n", "\n").Replace("\r", "\n");
                string[] lines = normalized.Split('\n');
                string newContent = string.Join(selectedEol, lines);

                if (!newContent.EndsWith(selectedEol))
                    newContent += selectedEol;

                if (newContent != content)
                {
                    try
                    {
                        File.WriteAllText(file, newContent, new UTF8Encoding(false));
                        statistics.UpdatedCount++;
                    }
                    catch { }
                }

                aggregatedLines.AppendLine($"\n---\n### File: {file.Substring(projectFolder.Length + 1)}\n---\n");
                aggregatedLines.AppendLine(content);
            }
            aggregatedText = aggregatedLines.ToString();
            return statistics;
        }

        // Heuristically determine if a file is binary.
        private static bool IsBinaryFile(string filePath, int blockSize = 1024)
        {
            try
            {
                using (FileStream stream = File.OpenRead(filePath))
                {
                    byte[] buffer = new byte[blockSize];
                    int bytesRead = stream.Read(buffer, 0, blockSize);
                    for (int i = 0; i < bytesRead; i++)
                    {
                        if (buffer[i] == 0)
                            return true;
                    }
                }
            }
            catch { }
            return false;
        }
    }
}