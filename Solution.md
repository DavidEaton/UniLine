=== Aggregated Solution: UniLine.sln ===


=== Project: UniLine ===

---
### File: App.xaml
---
```xaml

<Application x:Class="UniLine.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             StartupUri="MainWindow.xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <!-- MahApps.Metro resource dictionaries. Make sure that all file names are Case Sensitive! -->
                <ResourceDictionary Source="pack://application:,,,/MahApps.Metro;component/Styles/Controls.xaml" />
                <ResourceDictionary Source="pack://application:,,,/MahApps.Metro;component/Styles/Fonts.xaml" />
                <!-- Theme setting -->
                <ResourceDictionary Source="pack://application:,,,/MahApps.Metro;component/Styles/Themes/Light.Blue.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>


```

---
### File: App.xaml.cs
---
```cs

using System.Windows;

namespace UniLine;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
}



```

---
### File: AssemblyInfo.cs
---
```cs

using System.Windows;

[assembly:ThemeInfo(
    ResourceDictionaryLocation.None,            //where theme specific resource dictionaries are located
                                                //(used if a resource is not found in the page,
                                                // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly   //where the generic resource dictionary is located
                                                //(used if a resource is not found in the page,
                                                // app, or any theme specific resource dictionaries)
)]


```

---
### File: MainWindow.xaml
---
```xaml

<mah:MetroWindow x:Class="UniLine.MainWindow"
                 xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                 xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                 xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
                 xmlns:mah="clr-namespace:MahApps.Metro.Controls;assembly=MahApps.Metro"
                 xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
                 Title="UniLine"
                 Width="800"
                 Height="450"
                 WindowStartupLocation="CenterScreen"
                 mc:Ignorable="d">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        <!-- Top panel: Solution file selection -->
        <StackPanel Orientation="Horizontal" Margin="10">
            <Button Name="btnSelectSolution" Content="Select Solution File" Click="BtnSelectSolution_Click" Margin="0,0,10,0"/>
            <TextBlock Name="txtSolutionPath" VerticalAlignment="Center" Text="No solution selected" TextWrapping="Wrap" Width="600"/>
        </StackPanel>
        <!-- EOL selection -->
        <StackPanel Orientation="Horizontal" Margin="10" Grid.Row="1">
            <TextBlock Text="Select EOL Style:" VerticalAlignment="Center" Margin="0,0,10,0"/>
            <RadioButton Name="rbWindows" Content="Windows (CRLF)" IsChecked="True" Margin="0,0,10,0"/>
            <RadioButton Name="rbLinux" Content="Linux (LF)" Margin="0,0,10,0"/>
        </StackPanel>
        <!-- Projects list -->
        <GroupBox Header="Select Projects to Include" Margin="10" Grid.Row="2">
            <ScrollViewer>
                <StackPanel Name="stackPanelProjects"/>
            </ScrollViewer>
        </GroupBox>
        <!-- Process button and status -->
        <StackPanel Orientation="Horizontal" Margin="10" Grid.Row="3">
            <Button Name="btnProcess" Content="Process Selected Projects" Click="BtnProcess_Click" Width="200" Margin="0,0,10,0"/>
            <TextBlock Name="txtStatus" VerticalAlignment="Center" />
        </StackPanel>
    </Grid>
</mah:MetroWindow>


```

---
### File: MainWindow.xaml.cs
---
```cs

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
        private static ProjectStatistics ProcessProject(string projectFolder, string projectName, string selectedEol, out string aggregatedText)
        {
            ProjectStatistics statistics = new();
            StringBuilder aggregatedLines = new();
            aggregatedLines.AppendLine("\n\n=== Project: " + projectName + " ===");

            foreach (string file in Directory.EnumerateFiles(projectFolder, "*.*", SearchOption.AllDirectories))
            {
                string relativeFilePath = file.Substring(projectFolder.Length + 1);
                if (relativeFilePath.StartsWith("bin/") || relativeFilePath.StartsWith("obj/"))
                    continue;

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
                else if (content.Contains('\n'))
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

                aggregatedLines.AppendLine($"\n---\n### File: {relativeFilePath}\n---");
                string fileExtension = Path.GetExtension(file).TrimStart('.');
                aggregatedLines.AppendLine($"```{fileExtension}\n");
                aggregatedLines.AppendLine(content);
                aggregatedLines.AppendLine("\n```");
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


```

---
### File: ProjectInfo.cs
---
```cs

namespace UniLine
{
    public class ProjectInfo
    {
        public required string Name { get; set; }
        public required string Folder { get; set; }
    }
}


```

---
### File: ProjectStatistics.cs
---
```cs

namespace UniLine
{
    public class ProjectStatistics
    {
        public int TotalFiles { get; set; }
        public int WinBefore { get; set; }
        public int LinuxBefore { get; set; }
        public int UpdatedCount { get; set; }
    }
}


```

---
### File: UniLine.csproj
---
```csproj

<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="MahApps.Metro" Version="2.4.10" />
  </ItemGroup>

</Project>


```

---
### File: UniLine.csproj.user
---
```user

<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <_LastSelectedProfileId>C:\Users\DavidEaton\source\repos\UniLine\UniLine\Properties\PublishProfiles\FolderProfile.pubxml</_LastSelectedProfileId>
  </PropertyGroup>
</Project>


```

---
### File: obj\project.assets.json
---
```json

{
  "version": 3,
  "targets": {
    "net8.0-windows7.0": {
      "ControlzEx/4.4.0": {
        "type": "package",
        "dependencies": {
          "Microsoft.Xaml.Behaviors.Wpf": "1.1.19",
          "System.Text.Json": "4.7.2"
        },
        "compile": {
          "lib/netcoreapp3.1/ControlzEx.dll": {
            "related": ".pdb;.xml"
          }
        },
        "runtime": {
          "lib/netcoreapp3.1/ControlzEx.dll": {
            "related": ".pdb;.xml"
          }
        },
        "frameworkReferences": [
          "Microsoft.WindowsDesktop.App.WPF"
        ]
      },
      "MahApps.Metro/2.4.10": {
        "type": "package",
        "dependencies": {
          "ControlzEx": "[4.4.0, 6.0.0)"
        },
        "compile": {
          "lib/netcoreapp3.1/MahApps.Metro.dll": {
            "related": ".pdb;.xml"
          }
        },
        "runtime": {
          "lib/netcoreapp3.1/MahApps.Metro.dll": {
            "related": ".pdb;.xml"
          }
        },
        "frameworkReferences": [
          "Microsoft.WindowsDesktop.App.WPF"
        ],
        "resource": {
          "lib/netcoreapp3.1/de/MahApps.Metro.resources.dll": {
            "locale": "de"
          }
        }
      },
      "Microsoft.Xaml.Behaviors.Wpf/1.1.19": {
        "type": "package",
        "compile": {
          "lib/netcoreapp3.0/Microsoft.Xaml.Behaviors.dll": {
            "related": ".pdb;.xml"
          }
        },
        "runtime": {
          "lib/netcoreapp3.0/Microsoft.Xaml.Behaviors.dll": {
            "related": ".pdb;.xml"
          }
        },
        "frameworkReferences": [
          "Microsoft.WindowsDesktop.App.WPF"
        ]
      },
      "System.Text.Json/4.7.2": {
        "type": "package",
        "compile": {
          "lib/netcoreapp3.0/System.Text.Json.dll": {
            "related": ".xml"
          }
        },
        "runtime": {
          "lib/netcoreapp3.0/System.Text.Json.dll": {
            "related": ".xml"
          }
        }
      }
    }
  },
  "libraries": {
    "ControlzEx/4.4.0": {
      "sha512": "pZ5z4hYWwE4R13UMCVs6vII//nL7hz+Nwn4oJlnsZJRGqJNy6Z9KnJiTZfly6lKFu0pMc1aWBZpx+VqFTQKP1Q==",
      "type": "package",
      "path": "controlzex/4.4.0",
      "files": [
        ".nupkg.metadata",
        ".signature.p7s",
        "controlzex.4.4.0.nupkg.sha512",
        "controlzex.nuspec",
        "lib/net45/ControlzEx.dll",
        "lib/net45/ControlzEx.pdb",
        "lib/net45/ControlzEx.xml",
        "lib/net462/ControlzEx.dll",
        "lib/net462/ControlzEx.pdb",
        "lib/net462/ControlzEx.xml",
        "lib/netcoreapp3.0/ControlzEx.dll",
        "lib/netcoreapp3.0/ControlzEx.pdb",
        "lib/netcoreapp3.0/ControlzEx.xml",
        "lib/netcoreapp3.1/ControlzEx.dll",
        "lib/netcoreapp3.1/ControlzEx.pdb",
        "lib/netcoreapp3.1/ControlzEx.xml",
        "logo-mini.png"
      ]
    },
    "MahApps.Metro/2.4.10": {
      "sha512": "45exHKJCVYaD1/rNr3ekZPECEBM4uHOt6aYp6yNaJbliFMUo+d3z8Gi1xG+qEkbiHKITX+dlz+BW1FOsjAbl/w==",
      "type": "package",
      "path": "mahapps.metro/2.4.10",
      "hasTools": true,
      "files": [
        ".nupkg.metadata",
        ".signature.p7s",
        "lib/net452/MahApps.Metro.dll",
        "lib/net452/MahApps.Metro.pdb",
        "lib/net452/MahApps.Metro.xml",
        "lib/net452/de/MahApps.Metro.resources.dll",
        "lib/net46/MahApps.Metro.dll",
        "lib/net46/MahApps.Metro.pdb",
        "lib/net46/MahApps.Metro.xml",
        "lib/net46/de/MahApps.Metro.resources.dll",
        "lib/net47/MahApps.Metro.dll",
        "lib/net47/MahApps.Metro.pdb",
        "lib/net47/MahApps.Metro.xml",
        "lib/net47/de/MahApps.Metro.resources.dll",
        "lib/netcoreapp3.0/MahApps.Metro.dll",
        "lib/netcoreapp3.0/MahApps.Metro.pdb",
        "lib/netcoreapp3.0/MahApps.Metro.xml",
        "lib/netcoreapp3.0/de/MahApps.Metro.resources.dll",
        "lib/netcoreapp3.1/MahApps.Metro.dll",
        "lib/netcoreapp3.1/MahApps.Metro.pdb",
        "lib/netcoreapp3.1/MahApps.Metro.xml",
        "lib/netcoreapp3.1/de/MahApps.Metro.resources.dll",
        "mahapps.metro.2.4.10.nupkg.sha512",
        "mahapps.metro.logo.png",
        "mahapps.metro.nuspec",
        "tools/VisualStudioToolsManifest.xml"
      ]
    },
    "Microsoft.Xaml.Behaviors.Wpf/1.1.19": {
      "sha512": "5sPWkbqImc2t1aQwIfJcKsUo7tOg1Tr8+6xVzZJB56Nzt4u9NlpcLofgdX/aRYpPKdWDA3U23Akw1KQzU5e82g==",
      "type": "package",
      "path": "microsoft.xaml.behaviors.wpf/1.1.19",
      "hasTools": true,
      "files": [
        ".nupkg.metadata",
        ".signature.p7s",
        "lib/net45/Design/Microsoft.Xaml.Behaviors.Design.dll",
        "lib/net45/Microsoft.Xaml.Behaviors.dll",
        "lib/net45/Microsoft.Xaml.Behaviors.pdb",
        "lib/net45/Microsoft.Xaml.Behaviors.xml",
        "lib/netcoreapp3.0/Design/Microsoft.Xaml.Behaviors.DesignTools.dll",
        "lib/netcoreapp3.0/Microsoft.Xaml.Behaviors.dll",
        "lib/netcoreapp3.0/Microsoft.Xaml.Behaviors.pdb",
        "lib/netcoreapp3.0/Microsoft.Xaml.Behaviors.xml",
        "microsoft.xaml.behaviors.wpf.1.1.19.nupkg.sha512",
        "microsoft.xaml.behaviors.wpf.nuspec",
        "tools/Install.ps1"
      ]
    },
    "System.Text.Json/4.7.2": {
      "sha512": "TcMd95wcrubm9nHvJEQs70rC0H/8omiSGGpU4FQ/ZA1URIqD4pjmFJh2Mfv1yH1eHgJDWTi2hMDXwTET+zOOyg==",
      "type": "package",
      "path": "system.text.json/4.7.2",
      "files": [
        ".nupkg.metadata",
        ".signature.p7s",
        "Icon.png",
        "LICENSE.TXT",
        "THIRD-PARTY-NOTICES.TXT",
        "lib/net461/System.Text.Json.dll",
        "lib/net461/System.Text.Json.xml",
        "lib/netcoreapp3.0/System.Text.Json.dll",
        "lib/netcoreapp3.0/System.Text.Json.xml",
        "lib/netstandard2.0/System.Text.Json.dll",
        "lib/netstandard2.0/System.Text.Json.xml",
        "system.text.json.4.7.2.nupkg.sha512",
        "system.text.json.nuspec",
        "useSharedDesignerContext.txt",
        "version.txt"
      ]
    }
  },
  "projectFileDependencyGroups": {
    "net8.0-windows7.0": [
      "MahApps.Metro >= 2.4.10"
    ]
  },
  "packageFolders": {
    "C:\\Users\\DavidEaton\\.nuget\\packages\\": {},
    "C:\\Program Files (x86)\\Microsoft Visual Studio\\Shared\\NuGetPackages": {}
  },
  "project": {
    "version": "1.0.0",
    "restore": {
      "projectUniqueName": "C:\\Users\\DavidEaton\\source\\repos\\UniLine\\UniLine\\UniLine.csproj",
      "projectName": "UniLine",
      "projectPath": "C:\\Users\\DavidEaton\\source\\repos\\UniLine\\UniLine\\UniLine.csproj",
      "packagesPath": "C:\\Users\\DavidEaton\\.nuget\\packages\\",
      "outputPath": "C:\\Users\\DavidEaton\\source\\repos\\UniLine\\UniLine\\obj\\",
      "projectStyle": "PackageReference",
      "fallbackFolders": [
        "C:\\Program Files (x86)\\Microsoft Visual Studio\\Shared\\NuGetPackages"
      ],
      "configFilePaths": [
        "C:\\Users\\DavidEaton\\AppData\\Roaming\\NuGet\\NuGet.Config",
        "C:\\Program Files (x86)\\NuGet\\Config\\Microsoft.VisualStudio.FallbackLocation.config",
        "C:\\Program Files (x86)\\NuGet\\Config\\Microsoft.VisualStudio.Offline.config"
      ],
      "originalTargetFrameworks": [
        "net8.0-windows"
      ],
      "sources": {
        "C:\\Program Files (x86)\\Microsoft SDKs\\NuGetPackages\\": {},
        "https://api.nuget.org/v3/index.json": {}
      },
      "frameworks": {
        "net8.0-windows7.0": {
          "targetAlias": "net8.0-windows",
          "projectReferences": {}
        }
      },
      "warningProperties": {
        "warnAsError": [
          "NU1605"
        ]
      },
      "restoreAuditProperties": {
        "enableAudit": "true",
        "auditLevel": "low",
        "auditMode": "direct"
      },
      "SdkAnalysisLevel": "9.0.200"
    },
    "frameworks": {
      "net8.0-windows7.0": {
        "targetAlias": "net8.0-windows",
        "dependencies": {
          "MahApps.Metro": {
            "target": "Package",
            "version": "[2.4.10, )"
          }
        },
        "imports": [
          "net461",
          "net462",
          "net47",
          "net471",
          "net472",
          "net48",
          "net481"
        ],
        "assetTargetFallback": true,
        "warn": true,
        "frameworkReferences": {
          "Microsoft.NETCore.App": {
            "privateAssets": "all"
          },
          "Microsoft.WindowsDesktop.App.WPF": {
            "privateAssets": "none"
          }
        },
        "runtimeIdentifierGraphPath": "C:\\Program Files\\dotnet\\sdk\\9.0.200/PortableRuntimeIdentifierGraph.json"
      }
    }
  }
}


```

---
### File: obj\project.nuget.cache
---
```cache

{
  "version": 2,
  "dgSpecHash": "pkyxHwYkgCw=",
  "success": true,
  "projectFilePath": "C:\\Users\\DavidEaton\\source\\repos\\UniLine\\UniLine\\UniLine.csproj",
  "expectedPackageFiles": [
    "C:\\Users\\DavidEaton\\.nuget\\packages\\controlzex\\4.4.0\\controlzex.4.4.0.nupkg.sha512",
    "C:\\Users\\DavidEaton\\.nuget\\packages\\mahapps.metro\\2.4.10\\mahapps.metro.2.4.10.nupkg.sha512",
    "C:\\Users\\DavidEaton\\.nuget\\packages\\microsoft.xaml.behaviors.wpf\\1.1.19\\microsoft.xaml.behaviors.wpf.1.1.19.nupkg.sha512",
    "C:\\Users\\DavidEaton\\.nuget\\packages\\system.text.json\\4.7.2\\system.text.json.4.7.2.nupkg.sha512"
  ],
  "logs": []
}


```

---
### File: obj\UniLine.csproj.nuget.dgspec.json
---
```json

{
  "format": 1,
  "restore": {
    "C:\\Users\\DavidEaton\\source\\repos\\UniLine\\UniLine\\UniLine.csproj": {}
  },
  "projects": {
    "C:\\Users\\DavidEaton\\source\\repos\\UniLine\\UniLine\\UniLine.csproj": {
      "version": "1.0.0",
      "restore": {
        "projectUniqueName": "C:\\Users\\DavidEaton\\source\\repos\\UniLine\\UniLine\\UniLine.csproj",
        "projectName": "UniLine",
        "projectPath": "C:\\Users\\DavidEaton\\source\\repos\\UniLine\\UniLine\\UniLine.csproj",
        "packagesPath": "C:\\Users\\DavidEaton\\.nuget\\packages\\",
        "outputPath": "C:\\Users\\DavidEaton\\source\\repos\\UniLine\\UniLine\\obj\\",
        "projectStyle": "PackageReference",
        "fallbackFolders": [
          "C:\\Program Files (x86)\\Microsoft Visual Studio\\Shared\\NuGetPackages"
        ],
        "configFilePaths": [
          "C:\\Users\\DavidEaton\\AppData\\Roaming\\NuGet\\NuGet.Config",
          "C:\\Program Files (x86)\\NuGet\\Config\\Microsoft.VisualStudio.FallbackLocation.config",
          "C:\\Program Files (x86)\\NuGet\\Config\\Microsoft.VisualStudio.Offline.config"
        ],
        "originalTargetFrameworks": [
          "net8.0-windows"
        ],
        "sources": {
          "C:\\Program Files (x86)\\Microsoft SDKs\\NuGetPackages\\": {},
          "https://api.nuget.org/v3/index.json": {}
        },
        "frameworks": {
          "net8.0-windows7.0": {
            "targetAlias": "net8.0-windows",
            "projectReferences": {}
          }
        },
        "warningProperties": {
          "warnAsError": [
            "NU1605"
          ]
        },
        "restoreAuditProperties": {
          "enableAudit": "true",
          "auditLevel": "low",
          "auditMode": "direct"
        },
        "SdkAnalysisLevel": "9.0.200"
      },
      "frameworks": {
        "net8.0-windows7.0": {
          "targetAlias": "net8.0-windows",
          "dependencies": {
            "MahApps.Metro": {
              "target": "Package",
              "version": "[2.4.10, )"
            }
          },
          "imports": [
            "net461",
            "net462",
            "net47",
            "net471",
            "net472",
            "net48",
            "net481"
          ],
          "assetTargetFallback": true,
          "warn": true,
          "frameworkReferences": {
            "Microsoft.NETCore.App": {
              "privateAssets": "all"
            },
            "Microsoft.WindowsDesktop.App.WPF": {
              "privateAssets": "none"
            }
          },
          "runtimeIdentifierGraphPath": "C:\\Program Files\\dotnet\\sdk\\9.0.200/PortableRuntimeIdentifierGraph.json"
        }
      }
    }
  }
}


```

---
### File: obj\UniLine.csproj.nuget.g.props
---
```props

<?xml version="1.0" encoding="utf-8" standalone="no"?>
<Project ToolsVersion="14.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup Condition=" '$(ExcludeRestorePackageImports)' != 'true' ">
    <RestoreSuccess Condition=" '$(RestoreSuccess)' == '' ">True</RestoreSuccess>
    <RestoreTool Condition=" '$(RestoreTool)' == '' ">NuGet</RestoreTool>
    <ProjectAssetsFile Condition=" '$(ProjectAssetsFile)' == '' ">$(MSBuildThisFileDirectory)project.assets.json</ProjectAssetsFile>
    <NuGetPackageRoot Condition=" '$(NuGetPackageRoot)' == '' ">$(UserProfile)\.nuget\packages\</NuGetPackageRoot>
    <NuGetPackageFolders Condition=" '$(NuGetPackageFolders)' == '' ">C:\Users\DavidEaton\.nuget\packages\;C:\Program Files (x86)\Microsoft Visual Studio\Shared\NuGetPackages</NuGetPackageFolders>
    <NuGetProjectStyle Condition=" '$(NuGetProjectStyle)' == '' ">PackageReference</NuGetProjectStyle>
    <NuGetToolVersion Condition=" '$(NuGetToolVersion)' == '' ">6.13.2</NuGetToolVersion>
  </PropertyGroup>
  <ItemGroup Condition=" '$(ExcludeRestorePackageImports)' != 'true' ">
    <SourceRoot Include="C:\Users\DavidEaton\.nuget\packages\" />
    <SourceRoot Include="C:\Program Files (x86)\Microsoft Visual Studio\Shared\NuGetPackages\" />
  </ItemGroup>
  <PropertyGroup Condition=" '$(ExcludeRestorePackageImports)' != 'true' ">
    <PkgMicrosoft_Xaml_Behaviors_Wpf Condition=" '$(PkgMicrosoft_Xaml_Behaviors_Wpf)' == '' ">C:\Users\DavidEaton\.nuget\packages\microsoft.xaml.behaviors.wpf\1.1.19</PkgMicrosoft_Xaml_Behaviors_Wpf>
    <PkgMahApps_Metro Condition=" '$(PkgMahApps_Metro)' == '' ">C:\Users\DavidEaton\.nuget\packages\mahapps.metro\2.4.10</PkgMahApps_Metro>
  </PropertyGroup>
</Project>


```

---
### File: obj\UniLine.csproj.nuget.g.targets
---
```targets

<?xml version="1.0" encoding="utf-8" standalone="no"?>
<Project ToolsVersion="14.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003" />


```

---
### File: Properties\PublishProfiles\FolderProfile.pubxml
---
```pubxml

<?xml version="1.0" encoding="utf-8"?>
<!-- https://go.microsoft.com/fwlink/?LinkID=208121. -->
<Project>
  <PropertyGroup>
    <Configuration>Release</Configuration>
    <Platform>Any CPU</Platform>
    <PublishDir>C:\Users\DavidEaton\OneDrive - CyberSecure IPS\Desktop\Apps</PublishDir>
    <PublishProtocol>FileSystem</PublishProtocol>
    <_TargetId>Folder</_TargetId>
  </PropertyGroup>
</Project>


```

---
### File: Properties\PublishProfiles\FolderProfile.pubxml.user
---
```user

<?xml version="1.0" encoding="utf-8"?>
<!-- https://go.microsoft.com/fwlink/?LinkID=208121. -->
<Project>
  <PropertyGroup>
    <History>True|2025-02-27T21:55:40.4616749Z||;True|2025-02-27T15:23:19.8774369-05:00||;</History>
    <LastFailureDetails />
  </PropertyGroup>
</Project>

```

---
### File: bin\Debug\net8.0-windows\UniLine.deps.json
---
```json

{
  "runtimeTarget": {
    "name": ".NETCoreApp,Version=v8.0",
    "signature": ""
  },
  "compilationOptions": {},
  "targets": {
    ".NETCoreApp,Version=v8.0": {
      "UniLine/1.0.0": {
        "dependencies": {
          "MahApps.Metro": "2.4.10"
        },
        "runtime": {
          "UniLine.dll": {}
        }
      },
      "ControlzEx/4.4.0": {
        "dependencies": {
          "Microsoft.Xaml.Behaviors.Wpf": "1.1.19",
          "System.Text.Json": "4.7.2"
        },
        "runtime": {
          "lib/netcoreapp3.1/ControlzEx.dll": {
            "assemblyVersion": "4.0.0.0",
            "fileVersion": "4.4.0.50"
          }
        }
      },
      "MahApps.Metro/2.4.10": {
        "dependencies": {
          "ControlzEx": "4.4.0"
        },
        "runtime": {
          "lib/netcoreapp3.1/MahApps.Metro.dll": {
            "assemblyVersion": "2.0.0.0",
            "fileVersion": "2.4.10.1"
          }
        },
        "resources": {
          "lib/netcoreapp3.1/de/MahApps.Metro.resources.dll": {
            "locale": "de"
          }
        }
      },
      "Microsoft.Xaml.Behaviors.Wpf/1.1.19": {
        "runtime": {
          "lib/netcoreapp3.0/Microsoft.Xaml.Behaviors.dll": {
            "assemblyVersion": "1.1.0.0",
            "fileVersion": "1.1.19.35512"
          }
        }
      },
      "System.Text.Json/4.7.2": {}
    }
  },
  "libraries": {
    "UniLine/1.0.0": {
      "type": "project",
      "serviceable": false,
      "sha512": ""
    },
    "ControlzEx/4.4.0": {
      "type": "package",
      "serviceable": true,
      "sha512": "sha512-pZ5z4hYWwE4R13UMCVs6vII//nL7hz+Nwn4oJlnsZJRGqJNy6Z9KnJiTZfly6lKFu0pMc1aWBZpx+VqFTQKP1Q==",
      "path": "controlzex/4.4.0",
      "hashPath": "controlzex.4.4.0.nupkg.sha512"
    },
    "MahApps.Metro/2.4.10": {
      "type": "package",
      "serviceable": true,
      "sha512": "sha512-45exHKJCVYaD1/rNr3ekZPECEBM4uHOt6aYp6yNaJbliFMUo+d3z8Gi1xG+qEkbiHKITX+dlz+BW1FOsjAbl/w==",
      "path": "mahapps.metro/2.4.10",
      "hashPath": "mahapps.metro.2.4.10.nupkg.sha512"
    },
    "Microsoft.Xaml.Behaviors.Wpf/1.1.19": {
      "type": "package",
      "serviceable": true,
      "sha512": "sha512-5sPWkbqImc2t1aQwIfJcKsUo7tOg1Tr8+6xVzZJB56Nzt4u9NlpcLofgdX/aRYpPKdWDA3U23Akw1KQzU5e82g==",
      "path": "microsoft.xaml.behaviors.wpf/1.1.19",
      "hashPath": "microsoft.xaml.behaviors.wpf.1.1.19.nupkg.sha512"
    },
    "System.Text.Json/4.7.2": {
      "type": "package",
      "serviceable": true,
      "sha512": "sha512-TcMd95wcrubm9nHvJEQs70rC0H/8omiSGGpU4FQ/ZA1URIqD4pjmFJh2Mfv1yH1eHgJDWTi2hMDXwTET+zOOyg==",
      "path": "system.text.json/4.7.2",
      "hashPath": "system.text.json.4.7.2.nupkg.sha512"
    }
  }
}

```

---
### File: bin\Debug\net8.0-windows\UniLine.runtimeconfig.json
---
```json

{
  "runtimeOptions": {
    "tfm": "net8.0",
    "frameworks": [
      {
        "name": "Microsoft.NETCore.App",
        "version": "8.0.0"
      },
      {
        "name": "Microsoft.WindowsDesktop.App",
        "version": "8.0.0"
      }
    ],
    "configProperties": {
      "System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization": true,
      "CSWINRT_USE_WINDOWS_UI_XAML_PROJECTIONS": false
    }
  }
}

```

---
### File: bin\Release\net8.0-windows\UniLine.deps.json
---
```json

{
  "runtimeTarget": {
    "name": ".NETCoreApp,Version=v8.0",
    "signature": ""
  },
  "compilationOptions": {},
  "targets": {
    ".NETCoreApp,Version=v8.0": {
      "UniLine/1.0.0": {
        "dependencies": {
          "MahApps.Metro": "2.4.10"
        },
        "runtime": {
          "UniLine.dll": {}
        }
      },
      "ControlzEx/4.4.0": {
        "dependencies": {
          "Microsoft.Xaml.Behaviors.Wpf": "1.1.19",
          "System.Text.Json": "4.7.2"
        },
        "runtime": {
          "lib/netcoreapp3.1/ControlzEx.dll": {
            "assemblyVersion": "4.0.0.0",
            "fileVersion": "4.4.0.50"
          }
        }
      },
      "MahApps.Metro/2.4.10": {
        "dependencies": {
          "ControlzEx": "4.4.0"
        },
        "runtime": {
          "lib/netcoreapp3.1/MahApps.Metro.dll": {
            "assemblyVersion": "2.0.0.0",
            "fileVersion": "2.4.10.1"
          }
        },
        "resources": {
          "lib/netcoreapp3.1/de/MahApps.Metro.resources.dll": {
            "locale": "de"
          }
        }
      },
      "Microsoft.Xaml.Behaviors.Wpf/1.1.19": {
        "runtime": {
          "lib/netcoreapp3.0/Microsoft.Xaml.Behaviors.dll": {
            "assemblyVersion": "1.1.0.0",
            "fileVersion": "1.1.19.35512"
          }
        }
      },
      "System.Text.Json/4.7.2": {}
    }
  },
  "libraries": {
    "UniLine/1.0.0": {
      "type": "project",
      "serviceable": false,
      "sha512": ""
    },
    "ControlzEx/4.4.0": {
      "type": "package",
      "serviceable": true,
      "sha512": "sha512-pZ5z4hYWwE4R13UMCVs6vII//nL7hz+Nwn4oJlnsZJRGqJNy6Z9KnJiTZfly6lKFu0pMc1aWBZpx+VqFTQKP1Q==",
      "path": "controlzex/4.4.0",
      "hashPath": "controlzex.4.4.0.nupkg.sha512"
    },
    "MahApps.Metro/2.4.10": {
      "type": "package",
      "serviceable": true,
      "sha512": "sha512-45exHKJCVYaD1/rNr3ekZPECEBM4uHOt6aYp6yNaJbliFMUo+d3z8Gi1xG+qEkbiHKITX+dlz+BW1FOsjAbl/w==",
      "path": "mahapps.metro/2.4.10",
      "hashPath": "mahapps.metro.2.4.10.nupkg.sha512"
    },
    "Microsoft.Xaml.Behaviors.Wpf/1.1.19": {
      "type": "package",
      "serviceable": true,
      "sha512": "sha512-5sPWkbqImc2t1aQwIfJcKsUo7tOg1Tr8+6xVzZJB56Nzt4u9NlpcLofgdX/aRYpPKdWDA3U23Akw1KQzU5e82g==",
      "path": "microsoft.xaml.behaviors.wpf/1.1.19",
      "hashPath": "microsoft.xaml.behaviors.wpf.1.1.19.nupkg.sha512"
    },
    "System.Text.Json/4.7.2": {
      "type": "package",
      "serviceable": true,
      "sha512": "sha512-TcMd95wcrubm9nHvJEQs70rC0H/8omiSGGpU4FQ/ZA1URIqD4pjmFJh2Mfv1yH1eHgJDWTi2hMDXwTET+zOOyg==",
      "path": "system.text.json/4.7.2",
      "hashPath": "system.text.json.4.7.2.nupkg.sha512"
    }
  }
}

```

---
### File: bin\Release\net8.0-windows\UniLine.runtimeconfig.json
---
```json

{
  "runtimeOptions": {
    "tfm": "net8.0",
    "frameworks": [
      {
        "name": "Microsoft.NETCore.App",
        "version": "8.0.0"
      },
      {
        "name": "Microsoft.WindowsDesktop.App",
        "version": "8.0.0"
      }
    ],
    "configProperties": {
      "System.Reflection.Metadata.MetadataUpdater.IsSupported": false,
      "System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization": true,
      "CSWINRT_USE_WINDOWS_UI_XAML_PROJECTIONS": false
    }
  }
}

```

---
### File: obj\Debug\net8.0-windows\.NETCoreApp,Version=v8.0.AssemblyAttributes.cs
---
```cs

// <autogenerated />
using System;
using System.Reflection;
[assembly: global::System.Runtime.Versioning.TargetFrameworkAttribute(".NETCoreApp,Version=v8.0", FrameworkDisplayName = ".NET 8.0")]


```

---
### File: obj\Debug\net8.0-windows\App.g.cs
---
```cs

#pragma checksum "..\..\..\App.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "15322AEDF07C97260D2FC12D8B30A73C4810D8E3"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace UniLine {
    
    
    /// <summary>
    /// App
    /// </summary>
    public partial class App : System.Windows.Application {
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "9.0.2.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            
            #line 4 "..\..\..\App.xaml"
            this.StartupUri = new System.Uri("MainWindow.xaml", System.UriKind.Relative);
            
            #line default
            #line hidden
            System.Uri resourceLocater = new System.Uri("/UniLine;component/app.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\App.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        /// <summary>
        /// Application Entry Point.
        /// </summary>
        [System.STAThreadAttribute()]
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "9.0.2.0")]
        public static void Main() {
            UniLine.App app = new UniLine.App();
            app.InitializeComponent();
            app.Run();
        }
    }
}



```

---
### File: obj\Debug\net8.0-windows\App.g.i.cs
---
```cs

#pragma checksum "..\..\..\App.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "15322AEDF07C97260D2FC12D8B30A73C4810D8E3"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace UniLine {
    
    
    /// <summary>
    /// App
    /// </summary>
    public partial class App : System.Windows.Application {
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "9.0.2.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            
            #line 4 "..\..\..\App.xaml"
            this.StartupUri = new System.Uri("MainWindow.xaml", System.UriKind.Relative);
            
            #line default
            #line hidden
            System.Uri resourceLocater = new System.Uri("/UniLine;component/app.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\App.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        /// <summary>
        /// Application Entry Point.
        /// </summary>
        [System.STAThreadAttribute()]
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "9.0.2.0")]
        public static void Main() {
            UniLine.App app = new UniLine.App();
            app.InitializeComponent();
            app.Run();
        }
    }
}



```

---
### File: obj\Debug\net8.0-windows\MainWindow.g.cs
---
```cs

#pragma checksum "..\..\..\MainWindow.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "370276349CFECE735DF0FDDFE4F2B50DD6BD2782"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using MahApps.Metro.Controls;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace UniLine {
    
    
    /// <summary>
    /// MainWindow
    /// </summary>
    public partial class MainWindow : MahApps.Metro.Controls.MetroWindow, System.Windows.Markup.IComponentConnector {
        
        
        #line 21 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button btnSelectSolution;
        
        #line default
        #line hidden
        
        
        #line 22 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock txtSolutionPath;
        
        #line default
        #line hidden
        
        
        #line 27 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.RadioButton rbWindows;
        
        #line default
        #line hidden
        
        
        #line 28 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.RadioButton rbLinux;
        
        #line default
        #line hidden
        
        
        #line 33 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.StackPanel stackPanelProjects;
        
        #line default
        #line hidden
        
        
        #line 38 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button btnProcess;
        
        #line default
        #line hidden
        
        
        #line 39 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock txtStatus;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "9.0.2.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/UniLine;component/mainwindow.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\MainWindow.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "9.0.2.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            this.btnSelectSolution = ((System.Windows.Controls.Button)(target));
            
            #line 21 "..\..\..\MainWindow.xaml"
            this.btnSelectSolution.Click += new System.Windows.RoutedEventHandler(this.BtnSelectSolution_Click);
            
            #line default
            #line hidden
            return;
            case 2:
            this.txtSolutionPath = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 3:
            this.rbWindows = ((System.Windows.Controls.RadioButton)(target));
            return;
            case 4:
            this.rbLinux = ((System.Windows.Controls.RadioButton)(target));
            return;
            case 5:
            this.stackPanelProjects = ((System.Windows.Controls.StackPanel)(target));
            return;
            case 6:
            this.btnProcess = ((System.Windows.Controls.Button)(target));
            
            #line 38 "..\..\..\MainWindow.xaml"
            this.btnProcess.Click += new System.Windows.RoutedEventHandler(this.BtnProcess_Click);
            
            #line default
            #line hidden
            return;
            case 7:
            this.txtStatus = ((System.Windows.Controls.TextBlock)(target));
            return;
            }
            this._contentLoaded = true;
        }
    }
}



```

---
### File: obj\Debug\net8.0-windows\MainWindow.g.i.cs
---
```cs

#pragma checksum "..\..\..\MainWindow.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "370276349CFECE735DF0FDDFE4F2B50DD6BD2782"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using MahApps.Metro.Controls;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace UniLine {
    
    
    /// <summary>
    /// MainWindow
    /// </summary>
    public partial class MainWindow : MahApps.Metro.Controls.MetroWindow, System.Windows.Markup.IComponentConnector {
        
        
        #line 21 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button btnSelectSolution;
        
        #line default
        #line hidden
        
        
        #line 22 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock txtSolutionPath;
        
        #line default
        #line hidden
        
        
        #line 27 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.RadioButton rbWindows;
        
        #line default
        #line hidden
        
        
        #line 28 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.RadioButton rbLinux;
        
        #line default
        #line hidden
        
        
        #line 33 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.StackPanel stackPanelProjects;
        
        #line default
        #line hidden
        
        
        #line 38 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button btnProcess;
        
        #line default
        #line hidden
        
        
        #line 39 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock txtStatus;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "9.0.2.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/UniLine;V1.0.0.0;component/mainwindow.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\MainWindow.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "9.0.2.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            this.btnSelectSolution = ((System.Windows.Controls.Button)(target));
            
            #line 21 "..\..\..\MainWindow.xaml"
            this.btnSelectSolution.Click += new System.Windows.RoutedEventHandler(this.BtnSelectSolution_Click);
            
            #line default
            #line hidden
            return;
            case 2:
            this.txtSolutionPath = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 3:
            this.rbWindows = ((System.Windows.Controls.RadioButton)(target));
            return;
            case 4:
            this.rbLinux = ((System.Windows.Controls.RadioButton)(target));
            return;
            case 5:
            this.stackPanelProjects = ((System.Windows.Controls.StackPanel)(target));
            return;
            case 6:
            this.btnProcess = ((System.Windows.Controls.Button)(target));
            
            #line 38 "..\..\..\MainWindow.xaml"
            this.btnProcess.Click += new System.Windows.RoutedEventHandler(this.BtnProcess_Click);
            
            #line default
            #line hidden
            return;
            case 7:
            this.txtStatus = ((System.Windows.Controls.TextBlock)(target));
            return;
            }
            this._contentLoaded = true;
        }
    }
}



```

---
### File: obj\Debug\net8.0-windows\UniLine.AssemblyInfo.cs
---
```cs

//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Reflection;

[assembly: System.Reflection.AssemblyCompanyAttribute("UniLine")]
[assembly: System.Reflection.AssemblyConfigurationAttribute("Debug")]
[assembly: System.Reflection.AssemblyFileVersionAttribute("1.0.0.0")]
[assembly: System.Reflection.AssemblyInformationalVersionAttribute("1.0.0+e77bd89d731f27fb65929a4fc557dc50d9d7a1a5")]
[assembly: System.Reflection.AssemblyProductAttribute("UniLine")]
[assembly: System.Reflection.AssemblyTitleAttribute("UniLine")]
[assembly: System.Reflection.AssemblyVersionAttribute("1.0.0.0")]
[assembly: System.Runtime.Versioning.TargetPlatformAttribute("Windows7.0")]
[assembly: System.Runtime.Versioning.SupportedOSPlatformAttribute("Windows7.0")]

// Generated by the MSBuild WriteCodeFragment class.



```

---
### File: obj\Debug\net8.0-windows\UniLine.AssemblyInfoInputs.cache
---
```cache

fe416f0e751ffe7bbe6e34bbf22fa6a9ed9ec1d86d28db1785e227a7f0129815


```

---
### File: obj\Debug\net8.0-windows\UniLine.csproj.BuildWithSkipAnalyzers
---
```BuildWithSkipAnalyzers




```

---
### File: obj\Debug\net8.0-windows\UniLine.csproj.CoreCompileInputs.cache
---
```cache

efcde17720575ee9335cfd6946a81e960e0c72e7cdbd12ebe566003ccb856bd0


```

---
### File: obj\Debug\net8.0-windows\UniLine.csproj.FileListAbsolute.txt
---
```txt

C:\Users\DavidEaton\source\repos\UniLine\UniLine\bin\Debug\net8.0-windows\UniLine.exe
C:\Users\DavidEaton\source\repos\UniLine\UniLine\bin\Debug\net8.0-windows\UniLine.deps.json
C:\Users\DavidEaton\source\repos\UniLine\UniLine\bin\Debug\net8.0-windows\UniLine.runtimeconfig.json
C:\Users\DavidEaton\source\repos\UniLine\UniLine\bin\Debug\net8.0-windows\UniLine.dll
C:\Users\DavidEaton\source\repos\UniLine\UniLine\bin\Debug\net8.0-windows\UniLine.pdb
C:\Users\DavidEaton\source\repos\UniLine\UniLine\bin\Debug\net8.0-windows\ControlzEx.dll
C:\Users\DavidEaton\source\repos\UniLine\UniLine\bin\Debug\net8.0-windows\MahApps.Metro.dll
C:\Users\DavidEaton\source\repos\UniLine\UniLine\bin\Debug\net8.0-windows\Microsoft.Xaml.Behaviors.dll
C:\Users\DavidEaton\source\repos\UniLine\UniLine\bin\Debug\net8.0-windows\de\MahApps.Metro.resources.dll
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Debug\net8.0-windows\UniLine.csproj.AssemblyReference.cache
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Debug\net8.0-windows\MainWindow.baml
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Debug\net8.0-windows\App.baml
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Debug\net8.0-windows\MainWindow.g.cs
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Debug\net8.0-windows\App.g.cs
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Debug\net8.0-windows\UniLine_MarkupCompile.cache
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Debug\net8.0-windows\UniLine.g.resources
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Debug\net8.0-windows\UniLine.GeneratedMSBuildEditorConfig.editorconfig
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Debug\net8.0-windows\UniLine.AssemblyInfoInputs.cache
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Debug\net8.0-windows\UniLine.AssemblyInfo.cs
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Debug\net8.0-windows\UniLine.csproj.CoreCompileInputs.cache
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Debug\net8.0-windows\UniLine.sourcelink.json
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Debug\net8.0-windows\UniLine.csproj.Up2Date
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Debug\net8.0-windows\UniLine.dll
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Debug\net8.0-windows\refint\UniLine.dll
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Debug\net8.0-windows\UniLine.pdb
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Debug\net8.0-windows\UniLine.genruntimeconfig.cache
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Debug\net8.0-windows\ref\UniLine.dll


```

---
### File: obj\Debug\net8.0-windows\UniLine.csproj.Up2Date
---
```Up2Date




```

---
### File: obj\Debug\net8.0-windows\UniLine.designer.deps.json
---
```json

{
  "runtimeTarget": {
    "name": ".NETCoreApp,Version=v8.0",
    "signature": ""
  },
  "compilationOptions": {},
  "targets": {
    ".NETCoreApp,Version=v8.0": {
      "ControlzEx/4.4.0": {
        "dependencies": {
          "Microsoft.Xaml.Behaviors.Wpf": "1.1.19",
          "System.Text.Json": "4.7.2"
        },
        "runtime": {
          "lib/netcoreapp3.1/ControlzEx.dll": {
            "assemblyVersion": "4.0.0.0",
            "fileVersion": "4.4.0.50"
          }
        }
      },
      "MahApps.Metro/2.4.10": {
        "dependencies": {
          "ControlzEx": "4.4.0"
        },
        "runtime": {
          "lib/netcoreapp3.1/MahApps.Metro.dll": {
            "assemblyVersion": "2.0.0.0",
            "fileVersion": "2.4.10.1"
          }
        },
        "resources": {
          "lib/netcoreapp3.1/de/MahApps.Metro.resources.dll": {
            "locale": "de"
          }
        }
      },
      "Microsoft.Xaml.Behaviors.Wpf/1.1.19": {
        "runtime": {
          "lib/netcoreapp3.0/Microsoft.Xaml.Behaviors.dll": {
            "assemblyVersion": "1.1.0.0",
            "fileVersion": "1.1.19.35512"
          }
        }
      },
      "System.Text.Json/4.7.2": {
        "runtime": {
          "lib/netcoreapp3.0/System.Text.Json.dll": {
            "assemblyVersion": "4.0.1.2",
            "fileVersion": "4.700.20.21406"
          }
        }
      }
    }
  },
  "libraries": {
    "ControlzEx/4.4.0": {
      "type": "package",
      "serviceable": true,
      "sha512": "sha512-pZ5z4hYWwE4R13UMCVs6vII//nL7hz+Nwn4oJlnsZJRGqJNy6Z9KnJiTZfly6lKFu0pMc1aWBZpx+VqFTQKP1Q==",
      "path": "controlzex/4.4.0",
      "hashPath": "controlzex.4.4.0.nupkg.sha512"
    },
    "MahApps.Metro/2.4.10": {
      "type": "package",
      "serviceable": true,
      "sha512": "sha512-45exHKJCVYaD1/rNr3ekZPECEBM4uHOt6aYp6yNaJbliFMUo+d3z8Gi1xG+qEkbiHKITX+dlz+BW1FOsjAbl/w==",
      "path": "mahapps.metro/2.4.10",
      "hashPath": "mahapps.metro.2.4.10.nupkg.sha512"
    },
    "Microsoft.Xaml.Behaviors.Wpf/1.1.19": {
      "type": "package",
      "serviceable": true,
      "sha512": "sha512-5sPWkbqImc2t1aQwIfJcKsUo7tOg1Tr8+6xVzZJB56Nzt4u9NlpcLofgdX/aRYpPKdWDA3U23Akw1KQzU5e82g==",
      "path": "microsoft.xaml.behaviors.wpf/1.1.19",
      "hashPath": "microsoft.xaml.behaviors.wpf.1.1.19.nupkg.sha512"
    },
    "System.Text.Json/4.7.2": {
      "type": "package",
      "serviceable": true,
      "sha512": "sha512-TcMd95wcrubm9nHvJEQs70rC0H/8omiSGGpU4FQ/ZA1URIqD4pjmFJh2Mfv1yH1eHgJDWTi2hMDXwTET+zOOyg==",
      "path": "system.text.json/4.7.2",
      "hashPath": "system.text.json.4.7.2.nupkg.sha512"
    }
  }
}


```

---
### File: obj\Debug\net8.0-windows\UniLine.designer.runtimeconfig.json
---
```json

{
  "runtimeOptions": {
    "tfm": "net8.0",
    "frameworks": [
      {
        "name": "Microsoft.NETCore.App",
        "version": "8.0.0"
      },
      {
        "name": "Microsoft.WindowsDesktop.App",
        "version": "8.0.0"
      }
    ],
    "additionalProbingPaths": [
      "C:\\Users\\DavidEaton\\.dotnet\\store\\|arch|\\|tfm|",
      "C:\\Users\\DavidEaton\\.nuget\\packages",
      "C:\\Program Files (x86)\\Microsoft Visual Studio\\Shared\\NuGetPackages"
    ],
    "configProperties": {
      "System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization": true,
      "CSWINRT_USE_WINDOWS_UI_XAML_PROJECTIONS": false,
      "Microsoft.NETCore.DotNetHostPolicy.SetAppPaths": true
    }
  }
}


```

---
### File: obj\Debug\net8.0-windows\UniLine.GeneratedMSBuildEditorConfig.editorconfig
---
```editorconfig

is_global = true
build_property.TargetFramework = net8.0-windows
build_property.TargetPlatformMinVersion = 7.0
build_property.UsingMicrosoftNETSdkWeb = 
build_property.ProjectTypeGuids = 
build_property.InvariantGlobalization = 
build_property.PlatformNeutralAssembly = 
build_property.EnforceExtendedAnalyzerRules = 
build_property._SupportedPlatformList = Linux,macOS,Windows
build_property.RootNamespace = UniLine
build_property.ProjectDir = C:\Users\DavidEaton\source\repos\UniLine\UniLine\
build_property.EnableComHosting = 
build_property.EnableGeneratedComInterfaceComImportInterop = 
build_property.CsWinRTUseWindowsUIXamlProjections = false
build_property.EffectiveAnalysisLevelStyle = 8.0
build_property.EnableCodeStyleSeverity = 


```

---
### File: obj\Debug\net8.0-windows\UniLine.genruntimeconfig.cache
---
```cache

c95b359aa9778feb8b9dfa9fd759b3c057d63441f3961eb2083a51dfa90aa3c8


```

---
### File: obj\Debug\net8.0-windows\UniLine.GlobalUsings.g.cs
---
```cs

// <auto-generated/>
global using global::System;
global using global::System.Collections.Generic;
global using global::System.Linq;
global using global::System.Threading;
global using global::System.Threading.Tasks;


```

---
### File: obj\Debug\net8.0-windows\UniLine.sourcelink.json
---
```json

{"documents":{"C:\\Users\\DavidEaton\\source\\repos\\UniLine\\*":"https://raw.githubusercontent.com/DavidEaton/UniLine/e77bd89d731f27fb65929a4fc557dc50d9d7a1a5/*"}}

```

---
### File: obj\Debug\net8.0-windows\UniLine_MarkupCompile.cache
---
```cache

UniLine


winexe
C#
.cs
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Debug\net8.0-windows\
UniLine
none
false
TRACE;DEBUG;NET;NET8_0;NETCOREAPP
C:\Users\DavidEaton\source\repos\UniLine\UniLine\App.xaml
11407045341

6-394260729
201-257809130
MainWindow.xaml;

False



```

---
### File: obj\Debug\net8.0-windows\UniLine_MarkupCompile.i.cache
---
```cache

UniLine
1.0.0.0

winexe
C#
.cs
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Debug\net8.0-windows\
UniLine
none
false
TRACE;DEBUG;NET;NET8_0;NETCOREAPP;WINDOWS;WINDOWS7_0;NET5_0_OR_GREATER;NET6_0_OR_GREATER;NET7_0_OR_GREATER;NET8_0_OR_GREATER;NETCOREAPP3_0_OR_GREATER;NETCOREAPP3_1_OR_GREATER;WINDOWS7_0_OR_GREATER
C:\Users\DavidEaton\source\repos\UniLine\UniLine\App.xaml
11407045341

8-179248736
201-257809130
MainWindow.xaml;

False



```

---
### File: obj\Release\net8.0-windows\.NETCoreApp,Version=v8.0.AssemblyAttributes.cs
---
```cs

// <autogenerated />
using System;
using System.Reflection;
[assembly: global::System.Runtime.Versioning.TargetFrameworkAttribute(".NETCoreApp,Version=v8.0", FrameworkDisplayName = ".NET 8.0")]


```

---
### File: obj\Release\net8.0-windows\App.g.cs
---
```cs

#pragma checksum "..\..\..\App.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "15322AEDF07C97260D2FC12D8B30A73C4810D8E3"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace UniLine {
    
    
    /// <summary>
    /// App
    /// </summary>
    public partial class App : System.Windows.Application {
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "9.0.2.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            
            #line 4 "..\..\..\App.xaml"
            this.StartupUri = new System.Uri("MainWindow.xaml", System.UriKind.Relative);
            
            #line default
            #line hidden
            System.Uri resourceLocater = new System.Uri("/UniLine;component/app.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\App.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        /// <summary>
        /// Application Entry Point.
        /// </summary>
        [System.STAThreadAttribute()]
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "9.0.2.0")]
        public static void Main() {
            UniLine.App app = new UniLine.App();
            app.InitializeComponent();
            app.Run();
        }
    }
}



```

---
### File: obj\Release\net8.0-windows\App.g.i.cs
---
```cs

#pragma checksum "..\..\..\App.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "15322AEDF07C97260D2FC12D8B30A73C4810D8E3"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace UniLine {
    
    
    /// <summary>
    /// App
    /// </summary>
    public partial class App : System.Windows.Application {
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "9.0.2.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            
            #line 4 "..\..\..\App.xaml"
            this.StartupUri = new System.Uri("MainWindow.xaml", System.UriKind.Relative);
            
            #line default
            #line hidden
            System.Uri resourceLocater = new System.Uri("/UniLine;component/app.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\App.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        /// <summary>
        /// Application Entry Point.
        /// </summary>
        [System.STAThreadAttribute()]
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "9.0.2.0")]
        public static void Main() {
            UniLine.App app = new UniLine.App();
            app.InitializeComponent();
            app.Run();
        }
    }
}



```

---
### File: obj\Release\net8.0-windows\MainWindow.g.cs
---
```cs

#pragma checksum "..\..\..\MainWindow.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "370276349CFECE735DF0FDDFE4F2B50DD6BD2782"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using MahApps.Metro.Controls;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace UniLine {
    
    
    /// <summary>
    /// MainWindow
    /// </summary>
    public partial class MainWindow : MahApps.Metro.Controls.MetroWindow, System.Windows.Markup.IComponentConnector {
        
        
        #line 21 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button btnSelectSolution;
        
        #line default
        #line hidden
        
        
        #line 22 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock txtSolutionPath;
        
        #line default
        #line hidden
        
        
        #line 27 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.RadioButton rbWindows;
        
        #line default
        #line hidden
        
        
        #line 28 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.RadioButton rbLinux;
        
        #line default
        #line hidden
        
        
        #line 33 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.StackPanel stackPanelProjects;
        
        #line default
        #line hidden
        
        
        #line 38 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button btnProcess;
        
        #line default
        #line hidden
        
        
        #line 39 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock txtStatus;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "9.0.2.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/UniLine;component/mainwindow.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\MainWindow.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "9.0.2.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            this.btnSelectSolution = ((System.Windows.Controls.Button)(target));
            
            #line 21 "..\..\..\MainWindow.xaml"
            this.btnSelectSolution.Click += new System.Windows.RoutedEventHandler(this.BtnSelectSolution_Click);
            
            #line default
            #line hidden
            return;
            case 2:
            this.txtSolutionPath = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 3:
            this.rbWindows = ((System.Windows.Controls.RadioButton)(target));
            return;
            case 4:
            this.rbLinux = ((System.Windows.Controls.RadioButton)(target));
            return;
            case 5:
            this.stackPanelProjects = ((System.Windows.Controls.StackPanel)(target));
            return;
            case 6:
            this.btnProcess = ((System.Windows.Controls.Button)(target));
            
            #line 38 "..\..\..\MainWindow.xaml"
            this.btnProcess.Click += new System.Windows.RoutedEventHandler(this.BtnProcess_Click);
            
            #line default
            #line hidden
            return;
            case 7:
            this.txtStatus = ((System.Windows.Controls.TextBlock)(target));
            return;
            }
            this._contentLoaded = true;
        }
    }
}



```

---
### File: obj\Release\net8.0-windows\MainWindow.g.i.cs
---
```cs

#pragma checksum "..\..\..\MainWindow.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "370276349CFECE735DF0FDDFE4F2B50DD6BD2782"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using MahApps.Metro.Controls;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace UniLine {
    
    
    /// <summary>
    /// MainWindow
    /// </summary>
    public partial class MainWindow : MahApps.Metro.Controls.MetroWindow, System.Windows.Markup.IComponentConnector {
        
        
        #line 21 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button btnSelectSolution;
        
        #line default
        #line hidden
        
        
        #line 22 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock txtSolutionPath;
        
        #line default
        #line hidden
        
        
        #line 27 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.RadioButton rbWindows;
        
        #line default
        #line hidden
        
        
        #line 28 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.RadioButton rbLinux;
        
        #line default
        #line hidden
        
        
        #line 33 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.StackPanel stackPanelProjects;
        
        #line default
        #line hidden
        
        
        #line 38 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button btnProcess;
        
        #line default
        #line hidden
        
        
        #line 39 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock txtStatus;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "9.0.2.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/UniLine;component/mainwindow.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\MainWindow.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "9.0.2.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            this.btnSelectSolution = ((System.Windows.Controls.Button)(target));
            
            #line 21 "..\..\..\MainWindow.xaml"
            this.btnSelectSolution.Click += new System.Windows.RoutedEventHandler(this.BtnSelectSolution_Click);
            
            #line default
            #line hidden
            return;
            case 2:
            this.txtSolutionPath = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 3:
            this.rbWindows = ((System.Windows.Controls.RadioButton)(target));
            return;
            case 4:
            this.rbLinux = ((System.Windows.Controls.RadioButton)(target));
            return;
            case 5:
            this.stackPanelProjects = ((System.Windows.Controls.StackPanel)(target));
            return;
            case 6:
            this.btnProcess = ((System.Windows.Controls.Button)(target));
            
            #line 38 "..\..\..\MainWindow.xaml"
            this.btnProcess.Click += new System.Windows.RoutedEventHandler(this.BtnProcess_Click);
            
            #line default
            #line hidden
            return;
            case 7:
            this.txtStatus = ((System.Windows.Controls.TextBlock)(target));
            return;
            }
            this._contentLoaded = true;
        }
    }
}



```

---
### File: obj\Release\net8.0-windows\PublishOutputs.7c39a2cc6f.txt
---
```txt

C:\Users\DavidEaton\OneDrive - CyberSecure IPS\Desktop\Apps\UniLine.exe
C:\Users\DavidEaton\OneDrive - CyberSecure IPS\Desktop\Apps\UniLine.dll
C:\Users\DavidEaton\OneDrive - CyberSecure IPS\Desktop\Apps\UniLine.deps.json
C:\Users\DavidEaton\OneDrive - CyberSecure IPS\Desktop\Apps\UniLine.runtimeconfig.json
C:\Users\DavidEaton\OneDrive - CyberSecure IPS\Desktop\Apps\UniLine.pdb
C:\Users\DavidEaton\OneDrive - CyberSecure IPS\Desktop\Apps\ControlzEx.dll
C:\Users\DavidEaton\OneDrive - CyberSecure IPS\Desktop\Apps\MahApps.Metro.dll
C:\Users\DavidEaton\OneDrive - CyberSecure IPS\Desktop\Apps\Microsoft.Xaml.Behaviors.dll
C:\Users\DavidEaton\OneDrive - CyberSecure IPS\Desktop\Apps\de\MahApps.Metro.resources.dll


```

---
### File: obj\Release\net8.0-windows\UniLine.AssemblyInfo.cs
---
```cs

//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Reflection;

[assembly: System.Reflection.AssemblyCompanyAttribute("UniLine")]
[assembly: System.Reflection.AssemblyConfigurationAttribute("Release")]
[assembly: System.Reflection.AssemblyFileVersionAttribute("1.0.0.0")]
[assembly: System.Reflection.AssemblyInformationalVersionAttribute("1.0.0+e77bd89d731f27fb65929a4fc557dc50d9d7a1a5")]
[assembly: System.Reflection.AssemblyProductAttribute("UniLine")]
[assembly: System.Reflection.AssemblyTitleAttribute("UniLine")]
[assembly: System.Reflection.AssemblyVersionAttribute("1.0.0.0")]
[assembly: System.Runtime.Versioning.TargetPlatformAttribute("Windows7.0")]
[assembly: System.Runtime.Versioning.SupportedOSPlatformAttribute("Windows7.0")]

// Generated by the MSBuild WriteCodeFragment class.



```

---
### File: obj\Release\net8.0-windows\UniLine.AssemblyInfoInputs.cache
---
```cache

8e307a8f6ba9d8cc638329b38893f992958a0ae335a4835f0fbe38fd967a82be


```

---
### File: obj\Release\net8.0-windows\UniLine.csproj.BuildWithSkipAnalyzers
---
```BuildWithSkipAnalyzers



```

---
### File: obj\Release\net8.0-windows\UniLine.csproj.CoreCompileInputs.cache
---
```cache

03f3f8e821fed1232ea477e7fd6bfd0c314599fdc454ade9784ac67d20c8edd6


```

---
### File: obj\Release\net8.0-windows\UniLine.csproj.FileListAbsolute.txt
---
```txt

C:\Users\DavidEaton\source\repos\UniLine\UniLine\bin\Release\net8.0-windows\UniLine.exe
C:\Users\DavidEaton\source\repos\UniLine\UniLine\bin\Release\net8.0-windows\UniLine.deps.json
C:\Users\DavidEaton\source\repos\UniLine\UniLine\bin\Release\net8.0-windows\UniLine.runtimeconfig.json
C:\Users\DavidEaton\source\repos\UniLine\UniLine\bin\Release\net8.0-windows\UniLine.dll
C:\Users\DavidEaton\source\repos\UniLine\UniLine\bin\Release\net8.0-windows\UniLine.pdb
C:\Users\DavidEaton\source\repos\UniLine\UniLine\bin\Release\net8.0-windows\ControlzEx.dll
C:\Users\DavidEaton\source\repos\UniLine\UniLine\bin\Release\net8.0-windows\MahApps.Metro.dll
C:\Users\DavidEaton\source\repos\UniLine\UniLine\bin\Release\net8.0-windows\Microsoft.Xaml.Behaviors.dll
C:\Users\DavidEaton\source\repos\UniLine\UniLine\bin\Release\net8.0-windows\de\MahApps.Metro.resources.dll
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Release\net8.0-windows\UniLine.csproj.AssemblyReference.cache
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Release\net8.0-windows\MainWindow.baml
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Release\net8.0-windows\App.baml
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Release\net8.0-windows\MainWindow.g.cs
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Release\net8.0-windows\App.g.cs
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Release\net8.0-windows\UniLine_MarkupCompile.cache
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Release\net8.0-windows\UniLine.g.resources
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Release\net8.0-windows\UniLine.GeneratedMSBuildEditorConfig.editorconfig
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Release\net8.0-windows\UniLine.AssemblyInfoInputs.cache
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Release\net8.0-windows\UniLine.AssemblyInfo.cs
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Release\net8.0-windows\UniLine.csproj.CoreCompileInputs.cache
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Release\net8.0-windows\UniLine.sourcelink.json
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Release\net8.0-windows\UniLine.csproj.Up2Date
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Release\net8.0-windows\UniLine.dll
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Release\net8.0-windows\refint\UniLine.dll
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Release\net8.0-windows\UniLine.pdb
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Release\net8.0-windows\UniLine.genruntimeconfig.cache
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Release\net8.0-windows\ref\UniLine.dll


```

---
### File: obj\Release\net8.0-windows\UniLine.csproj.Up2Date
---
```Up2Date




```

---
### File: obj\Release\net8.0-windows\UniLine.designer.deps.json
---
```json

{
  "runtimeTarget": {
    "name": ".NETCoreApp,Version=v8.0",
    "signature": ""
  },
  "compilationOptions": {},
  "targets": {
    ".NETCoreApp,Version=v8.0": {
      "ControlzEx/4.4.0": {
        "dependencies": {
          "Microsoft.Xaml.Behaviors.Wpf": "1.1.19",
          "System.Text.Json": "4.7.2"
        },
        "runtime": {
          "lib/netcoreapp3.1/ControlzEx.dll": {
            "assemblyVersion": "4.0.0.0",
            "fileVersion": "4.4.0.50"
          }
        }
      },
      "MahApps.Metro/2.4.10": {
        "dependencies": {
          "ControlzEx": "4.4.0"
        },
        "runtime": {
          "lib/netcoreapp3.1/MahApps.Metro.dll": {
            "assemblyVersion": "2.0.0.0",
            "fileVersion": "2.4.10.1"
          }
        },
        "resources": {
          "lib/netcoreapp3.1/de/MahApps.Metro.resources.dll": {
            "locale": "de"
          }
        }
      },
      "Microsoft.Xaml.Behaviors.Wpf/1.1.19": {
        "runtime": {
          "lib/netcoreapp3.0/Microsoft.Xaml.Behaviors.dll": {
            "assemblyVersion": "1.1.0.0",
            "fileVersion": "1.1.19.35512"
          }
        }
      },
      "System.Text.Json/4.7.2": {
        "runtime": {
          "lib/netcoreapp3.0/System.Text.Json.dll": {
            "assemblyVersion": "4.0.1.2",
            "fileVersion": "4.700.20.21406"
          }
        }
      }
    }
  },
  "libraries": {
    "ControlzEx/4.4.0": {
      "type": "package",
      "serviceable": true,
      "sha512": "sha512-pZ5z4hYWwE4R13UMCVs6vII//nL7hz+Nwn4oJlnsZJRGqJNy6Z9KnJiTZfly6lKFu0pMc1aWBZpx+VqFTQKP1Q==",
      "path": "controlzex/4.4.0",
      "hashPath": "controlzex.4.4.0.nupkg.sha512"
    },
    "MahApps.Metro/2.4.10": {
      "type": "package",
      "serviceable": true,
      "sha512": "sha512-45exHKJCVYaD1/rNr3ekZPECEBM4uHOt6aYp6yNaJbliFMUo+d3z8Gi1xG+qEkbiHKITX+dlz+BW1FOsjAbl/w==",
      "path": "mahapps.metro/2.4.10",
      "hashPath": "mahapps.metro.2.4.10.nupkg.sha512"
    },
    "Microsoft.Xaml.Behaviors.Wpf/1.1.19": {
      "type": "package",
      "serviceable": true,
      "sha512": "sha512-5sPWkbqImc2t1aQwIfJcKsUo7tOg1Tr8+6xVzZJB56Nzt4u9NlpcLofgdX/aRYpPKdWDA3U23Akw1KQzU5e82g==",
      "path": "microsoft.xaml.behaviors.wpf/1.1.19",
      "hashPath": "microsoft.xaml.behaviors.wpf.1.1.19.nupkg.sha512"
    },
    "System.Text.Json/4.7.2": {
      "type": "package",
      "serviceable": true,
      "sha512": "sha512-TcMd95wcrubm9nHvJEQs70rC0H/8omiSGGpU4FQ/ZA1URIqD4pjmFJh2Mfv1yH1eHgJDWTi2hMDXwTET+zOOyg==",
      "path": "system.text.json/4.7.2",
      "hashPath": "system.text.json.4.7.2.nupkg.sha512"
    }
  }
}


```

---
### File: obj\Release\net8.0-windows\UniLine.designer.runtimeconfig.json
---
```json

{
  "runtimeOptions": {
    "tfm": "net8.0",
    "frameworks": [
      {
        "name": "Microsoft.NETCore.App",
        "version": "8.0.0"
      },
      {
        "name": "Microsoft.WindowsDesktop.App",
        "version": "8.0.0"
      }
    ],
    "additionalProbingPaths": [
      "C:\\Users\\DavidEaton\\.dotnet\\store\\|arch|\\|tfm|",
      "C:\\Users\\DavidEaton\\.nuget\\packages",
      "C:\\Program Files (x86)\\Microsoft Visual Studio\\Shared\\NuGetPackages"
    ],
    "configProperties": {
      "System.Reflection.Metadata.MetadataUpdater.IsSupported": false,
      "System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization": true,
      "CSWINRT_USE_WINDOWS_UI_XAML_PROJECTIONS": false,
      "Microsoft.NETCore.DotNetHostPolicy.SetAppPaths": true
    }
  }
}


```

---
### File: obj\Release\net8.0-windows\UniLine.GeneratedMSBuildEditorConfig.editorconfig
---
```editorconfig

is_global = true
build_property.TargetFramework = net8.0-windows
build_property.TargetPlatformMinVersion = 7.0
build_property.UsingMicrosoftNETSdkWeb = 
build_property.ProjectTypeGuids = 
build_property.InvariantGlobalization = 
build_property.PlatformNeutralAssembly = 
build_property.EnforceExtendedAnalyzerRules = 
build_property._SupportedPlatformList = Linux,macOS,Windows
build_property.RootNamespace = UniLine
build_property.ProjectDir = C:\Users\DavidEaton\source\repos\UniLine\UniLine\
build_property.EnableComHosting = 
build_property.EnableGeneratedComInterfaceComImportInterop = 
build_property.CsWinRTUseWindowsUIXamlProjections = false
build_property.EffectiveAnalysisLevelStyle = 8.0
build_property.EnableCodeStyleSeverity = 


```

---
### File: obj\Release\net8.0-windows\UniLine.genruntimeconfig.cache
---
```cache

bba47e7ba2b3aebd831190582868bcfa402d4543e885ff32bb8e69660590139e


```

---
### File: obj\Release\net8.0-windows\UniLine.GlobalUsings.g.cs
---
```cs

// <auto-generated/>
global using global::System;
global using global::System.Collections.Generic;
global using global::System.Linq;
global using global::System.Threading;
global using global::System.Threading.Tasks;


```

---
### File: obj\Release\net8.0-windows\UniLine.sourcelink.json
---
```json

{"documents":{"C:\\Users\\DavidEaton\\source\\repos\\UniLine\\*":"https://raw.githubusercontent.com/DavidEaton/UniLine/e77bd89d731f27fb65929a4fc557dc50d9d7a1a5/*"}}

```

---
### File: obj\Release\net8.0-windows\UniLine_MarkupCompile.cache
---
```cache

UniLine


winexe
C#
.cs
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Release\net8.0-windows\
UniLine
none
false
TRACE;RELEASE;NET;NET8_0;NETCOREAPP
C:\Users\DavidEaton\source\repos\UniLine\UniLine\App.xaml
11407045341

61864337350
201-257809130
MainWindow.xaml;

False



```

---
### File: obj\Release\net8.0-windows\UniLine_MarkupCompile.i.cache
---
```cache

UniLine
1.0.0.0

winexe
C#
.cs
C:\Users\DavidEaton\source\repos\UniLine\UniLine\obj\Release\net8.0-windows\
UniLine
none
false
TRACE;RELEASE;NET;NET8_0;NETCOREAPP;WINDOWS;WINDOWS7_0;NET5_0_OR_GREATER;NET6_0_OR_GREATER;NET7_0_OR_GREATER;NET8_0_OR_GREATER;NETCOREAPP3_0_OR_GREATER;NETCOREAPP3_1_OR_GREATER;WINDOWS7_0_OR_GREATER
C:\Users\DavidEaton\source\repos\UniLine\UniLine\App.xaml
11407045341

8497932427
201-257809130
MainWindow.xaml;

False



```

