using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace FreeGLBA.NugetClientPublisher;

internal class Program
{
    private static NuGetConfig _config = new();
    private static bool _dryRun = true;

    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║       FreeGLBA.Client NuGet Package Publisher                ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // Load configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddUserSecrets<Program>(optional: true)
            .AddCommandLine(args)
            .Build();

        configuration.GetSection("NuGet").Bind(_config);

        // Allow command line overrides
        if (args.Length > 0 && args[0] == "--version" && args.Length > 1)
        {
            _config.Version = args[1];
        }

        try
        {
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine("                       CONFIGURATION                           ");
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine($"  Package ID:    {_config.PackageId}");
            Console.WriteLine($"  Version:       {_config.Version}");
            Console.WriteLine($"  Configuration: {_config.Configuration}");
            Console.WriteLine($"  Source:        {_config.Source}");
            Console.WriteLine($"  Project:       {_config.ProjectPath}");
            Console.WriteLine($"  API Key:       {(string.IsNullOrWhiteSpace(_config.ApiKey) ? "❌ NOT CONFIGURED" : "✓ Configured (hidden)")}");
            Console.WriteLine();

            // Validate project path
            var projectPath = ResolveProjectPath();
            if (projectPath == null)
            {
                return 1;
            }

            // Show menu
            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                DisplayModeHeader();
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                Console.WriteLine("  1. View current configuration - READ ONLY");
                Console.WriteLine("  2. Verify project builds successfully - READ ONLY");
                Console.WriteLine("  3. Pack NuGet package (build .nupkg)");
                Console.WriteLine("  4. Push to NuGet.org");
                Console.WriteLine("  5. Full publish (Clean → Build → Pack → Push)");
                Console.WriteLine();
                Console.WriteLine("  V. Change version number");
                Console.WriteLine("  D. Toggle DRY RUN mode");
                Console.WriteLine("  0. Exit");
                Console.WriteLine();
                Console.Write("Select option: ");

                var key = Console.ReadKey();
                Console.WriteLine();
                Console.WriteLine();

                switch (char.ToUpper(key.KeyChar))
                {
                    case '1':
                        await ViewConfiguration();
                        break;
                    case '2':
                        await VerifyBuild();
                        break;
                    case '3':
                        await PackNuGet();
                        break;
                    case '4':
                        await PushToNuGet();
                        break;
                    case '5':
                        await FullPublish();
                        break;
                    case 'V':
                        ChangeVersion();
                        break;
                    case 'D':
                        ToggleDryRunMode();
                        break;
                    case '0':
                        Console.WriteLine("Exiting...");
                        return 0;
                    default:
                        Console.WriteLine("Invalid option. Please try again.");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner: {ex.InnerException.Message}");
            }
            Console.WriteLine();
            Console.WriteLine("Stack Trace:");
            Console.WriteLine(ex.StackTrace);
            Console.ResetColor();
            return 1;
        }
    }

    #region Menu Display Helpers

    private static void DisplayModeHeader()
    {
        if (_dryRun)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("              MENU - 🔒 DRY RUN MODE (No writes)              ");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("              MENU - ⚠️  LIVE MODE (Will publish!)             ");
            Console.ResetColor();
        }
    }

    private static void ToggleDryRunMode()
    {
        _dryRun = !_dryRun;
        Console.WriteLine();
        if (_dryRun)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("🔒 DRY RUN MODE ENABLED - No packages will be pushed.");
            Console.WriteLine("   Operations will show what WOULD happen.");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("⚠️  LIVE MODE ENABLED - Packages WILL be pushed to NuGet.org!");
            Console.WriteLine("   Are you sure? Press 'D' again to switch back to Dry Run.");
            Console.ResetColor();
        }
    }

    private static void ChangeVersion()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("                      CHANGE VERSION                           ");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine($"  Current version: {_config.Version}");
        Console.WriteLine();
        Console.Write("  Enter new version (e.g., 1.0.1): ");
        var newVersion = Console.ReadLine()?.Trim();

        if (!string.IsNullOrWhiteSpace(newVersion))
        {
            _config.Version = newVersion;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ✓ Version updated to: {_config.Version}");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  Version unchanged.");
            Console.ResetColor();
        }
    }

    #endregion

    #region Menu Actions

    private static async Task ViewConfiguration()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("                    CURRENT CONFIGURATION                      ");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();

        var projectPath = ResolveProjectPath();

        Console.WriteLine("  ┌──────────────────────┬──────────────────────────────────────────────┐");
        Console.WriteLine("  │ Setting              │ Value                                        │");
        Console.WriteLine("  ├──────────────────────┼──────────────────────────────────────────────┤");
        Console.WriteLine($"  │ Package ID           │ {_config.PackageId.PadRight(44)} │");
        Console.WriteLine($"  │ Version              │ {_config.Version.PadRight(44)} │");
        Console.WriteLine($"  │ Configuration        │ {_config.Configuration.PadRight(44)} │");
        Console.WriteLine($"  │ Source               │ {TruncateString(_config.Source, 44).PadRight(44)} │");
        Console.WriteLine($"  │ Skip Duplicate       │ {_config.SkipDuplicate.ToString().PadRight(44)} │");
        Console.WriteLine($"  │ Include Symbols      │ {_config.IncludeSymbols.ToString().PadRight(44)} │");
        Console.WriteLine("  ├──────────────────────┼──────────────────────────────────────────────┤");

        if (string.IsNullOrWhiteSpace(_config.ApiKey))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  │ API Key              │ {"❌ NOT CONFIGURED".PadRight(44)} │");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  │ API Key              │ {"✓ Configured (hidden)".PadRight(44)} │");
            Console.ResetColor();
        }

        Console.WriteLine("  └──────────────────────┴──────────────────────────────────────────────┘");
        Console.WriteLine();

        Console.WriteLine("  PROJECT PATH:");
        if (projectPath != null)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"    ✓ {projectPath}");
            Console.ResetColor();

            // Check if package already exists
            var projectDir = Path.GetDirectoryName(projectPath)!;
            var outputDir = Path.Combine(projectDir, "bin", _config.Configuration);
            var packageFileName = $"{_config.PackageId}.{_config.Version}.nupkg";
            var existingPackage = FindPackage(outputDir, packageFileName);
            
            if (existingPackage != null)
            {
                Console.WriteLine();
                Console.WriteLine("  EXISTING PACKAGE:");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"    ⚠ {existingPackage}");
                Console.WriteLine($"      Size: {new FileInfo(existingPackage).Length / 1024.0:F1} KB");
                Console.WriteLine($"      Modified: {File.GetLastWriteTime(existingPackage):yyyy-MM-dd HH:mm:ss}");
                Console.ResetColor();
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"    ✗ Project not found: {_config.ProjectPath}");
            Console.ResetColor();
        }

        if (string.IsNullOrWhiteSpace(_config.ApiKey))
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  ⚠ API KEY NOT SET!");
            Console.WriteLine("    Run: dotnet user-secrets set \"NuGet:ApiKey\" \"your-key-here\"");
            Console.ResetColor();
        }

        await Task.CompletedTask;
    }

    private static async Task VerifyBuild()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("                      VERIFY BUILD                             ");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();

        var projectPath = ResolveProjectPath();
        if (projectPath == null) return;

        var projectDir = Path.GetDirectoryName(projectPath)!;

        Console.WriteLine("  Step 1: Restoring packages...");
        if (!await RunCommandAsync("dotnet", $"restore \"{projectPath}\"", projectDir))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✗ Restore failed");
            Console.ResetColor();
            return;
        }
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  ✓ Restore completed");
        Console.ResetColor();

        Console.WriteLine();
        Console.WriteLine("  Step 2: Building project...");
        if (!await RunCommandAsync("dotnet", $"build \"{projectPath}\" -c {_config.Configuration} --no-restore", projectDir))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✗ Build failed");
            Console.ResetColor();
            return;
        }
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  ✓ Build completed successfully!");
        Console.ResetColor();
    }

    private static async Task PackNuGet()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        if (_dryRun)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("              PACK NUGET (DRY RUN - Preview only)              ");
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine("                      PACK NUGET PACKAGE                       ");
        }
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();

        var projectPath = ResolveProjectPath();
        if (projectPath == null) return;

        var projectDir = Path.GetDirectoryName(projectPath)!;
        var outputDir = Path.Combine(projectDir, "bin", _config.Configuration);

        Console.WriteLine($"  Package: {_config.PackageId}");
        Console.WriteLine($"  Version: {_config.Version}");
        Console.WriteLine();

        if (_dryRun)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  [DRY RUN] Would execute:");
            Console.WriteLine($"    dotnet clean \"{projectPath}\" -c {_config.Configuration}");
            Console.WriteLine($"    dotnet restore \"{projectPath}\"");
            Console.WriteLine($"    dotnet pack \"{projectPath}\" -c {_config.Configuration} -p:Version={_config.Version}");
            Console.WriteLine();
            Console.WriteLine("  ✓ Dry run complete - no changes made.");
            Console.ResetColor();
            return;
        }

        // Clean
        Console.WriteLine("  Step 1: Cleaning...");
        await RunCommandAsync("dotnet", $"clean \"{projectPath}\" -c {_config.Configuration}", projectDir);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  ✓ Clean completed");
        Console.ResetColor();

        // Restore
        Console.WriteLine();
        Console.WriteLine("  Step 2: Restoring...");
        if (!await RunCommandAsync("dotnet", $"restore \"{projectPath}\"", projectDir))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✗ Restore failed");
            Console.ResetColor();
            return;
        }
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  ✓ Restore completed");
        Console.ResetColor();

        // Build
        Console.WriteLine();
        Console.WriteLine("  Step 3: Building...");
        if (!await RunCommandAsync("dotnet", $"build \"{projectPath}\" -c {_config.Configuration} --no-restore", projectDir))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✗ Build failed");
            Console.ResetColor();
            return;
        }
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  ✓ Build completed");
        Console.ResetColor();

        // Pack
        Console.WriteLine();
        Console.WriteLine("  Step 4: Packing...");
        var packArgs = $"pack \"{projectPath}\" -c {_config.Configuration} -p:Version={_config.Version} --no-build";
        if (_config.IncludeSymbols)
        {
            packArgs += " --include-symbols -p:SymbolPackageFormat=snupkg";
        }
        if (!await RunCommandAsync("dotnet", packArgs, projectDir, hideOutput: false))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✗ Pack failed");
            Console.ResetColor();
            return;
        }

        // Show result
        var packageFileName = $"{_config.PackageId}.{_config.Version}.nupkg";
        var packagePath = FindPackage(outputDir, packageFileName);
        if (packagePath != null)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ✓ Package created: {packagePath}");
            Console.WriteLine($"    Size: {new FileInfo(packagePath).Length / 1024.0:F1} KB");
            Console.ResetColor();
        }
    }

    private static async Task PushToNuGet()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        if (_dryRun)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("              PUSH TO NUGET (DRY RUN - Preview only)           ");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("                    PUSH TO NUGET.ORG                          ");
            Console.ResetColor();
        }
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();

        // Validate API key
        if (string.IsNullOrWhiteSpace(_config.ApiKey))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✗ ERROR: NuGet API key not configured!");
            Console.WriteLine();
            Console.WriteLine("  Please set the API key using user secrets:");
            Console.WriteLine("    dotnet user-secrets set \"NuGet:ApiKey\" \"your-api-key-here\"");
            Console.ResetColor();
            return;
        }

        var projectPath = ResolveProjectPath();
        if (projectPath == null) return;

        var projectDir = Path.GetDirectoryName(projectPath)!;
        var outputDir = Path.Combine(projectDir, "bin", _config.Configuration);
        var packageFileName = $"{_config.PackageId}.{_config.Version}.nupkg";
        var packagePath = FindPackage(outputDir, packageFileName);

        if (packagePath == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ✗ Package not found: {packageFileName}");
            Console.WriteLine("    Run option 3 (Pack) first to create the package.");
            Console.ResetColor();
            return;
        }

        Console.WriteLine($"  Package: {packagePath}");
        Console.WriteLine($"  Size: {new FileInfo(packagePath).Length / 1024.0:F1} KB");
        Console.WriteLine($"  Destination: {_config.Source}");
        Console.WriteLine();

        if (_dryRun)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  [DRY RUN] Would execute:");
            Console.WriteLine($"    dotnet nuget push \"{packagePath}\"");
            Console.WriteLine($"      --api-key ***API-KEY***");
            Console.WriteLine($"      --source {_config.Source}");
            if (_config.SkipDuplicate) Console.WriteLine($"      --skip-duplicate");
            Console.WriteLine();
            Console.WriteLine("  ✓ Dry run complete - no packages pushed.");
            Console.ResetColor();
            return;
        }

        // Confirm before pushing
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("  ⚠ This will publish to NuGet.org. Continue? (y/N): ");
        Console.ResetColor();
        var confirm = Console.ReadKey();
        Console.WriteLine();

        if (char.ToUpper(confirm.KeyChar) != 'Y')
        {
            Console.WriteLine("  Cancelled.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("  Pushing to NuGet.org...");

        var pushArgs = $"nuget push \"{packagePath}\" --api-key {_config.ApiKey} --source {_config.Source}";
        if (_config.SkipDuplicate)
        {
            pushArgs += " --skip-duplicate";
        }

        if (!await RunCommandAsync("dotnet", pushArgs, projectDir, hideOutput: false))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✗ Push failed");
            Console.ResetColor();
            return;
        }

        // Push symbols if they exist
        if (_config.IncludeSymbols)
        {
            var symbolsFileName = $"{_config.PackageId}.{_config.Version}.snupkg";
            var symbolsPath = FindPackage(outputDir, symbolsFileName);
            if (symbolsPath != null)
            {
                Console.WriteLine();
                Console.WriteLine("  Pushing symbols package...");
                var symbolsPushArgs = $"nuget push \"{symbolsPath}\" --api-key {_config.ApiKey} --source {_config.Source}";
                if (_config.SkipDuplicate)
                {
                    symbolsPushArgs += " --skip-duplicate";
                }
                await RunCommandAsync("dotnet", symbolsPushArgs, projectDir, hideOutput: false);
            }
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                         SUCCESS!                             ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine($"  Package {_config.PackageId} v{_config.Version} published to NuGet.org!");
        Console.WriteLine($"  View at: https://www.nuget.org/packages/{_config.PackageId}/{_config.Version}");
        Console.WriteLine();
        Console.WriteLine("  Note: It may take a few minutes for the package to be indexed.");
    }

    private static async Task FullPublish()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        if (_dryRun)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("           FULL PUBLISH (DRY RUN - Preview only)               ");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("                      FULL PUBLISH                             ");
            Console.ResetColor();
        }
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();

        Console.WriteLine($"  This will: Clean → Build → Pack → Push");
        Console.WriteLine($"  Package: {_config.PackageId} v{_config.Version}");
        Console.WriteLine();

        if (!_dryRun)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("  ⚠ This will publish to NuGet.org. Continue? (y/N): ");
            Console.ResetColor();
            var confirm = Console.ReadKey();
            Console.WriteLine();

            if (char.ToUpper(confirm.KeyChar) != 'Y')
            {
                Console.WriteLine("  Cancelled.");
                return;
            }
            Console.WriteLine();
        }

        // Validate API key first
        if (string.IsNullOrWhiteSpace(_config.ApiKey))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✗ ERROR: NuGet API key not configured!");
            Console.ResetColor();
            return;
        }

        var projectPath = ResolveProjectPath();
        if (projectPath == null) return;

        var projectDir = Path.GetDirectoryName(projectPath)!;
        var outputDir = Path.Combine(projectDir, "bin", _config.Configuration);

        if (_dryRun)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  [DRY RUN] Would execute the following steps:");
            Console.WriteLine();
            Console.WriteLine($"    1. dotnet clean \"{projectPath}\" -c {_config.Configuration}");
            Console.WriteLine($"    2. dotnet restore \"{projectPath}\"");
            Console.WriteLine($"    3. dotnet build \"{projectPath}\" -c {_config.Configuration}");
            Console.WriteLine($"    4. dotnet pack \"{projectPath}\" -c {_config.Configuration} -p:Version={_config.Version} --no-build");
            Console.WriteLine($"    5. dotnet nuget push ... --api-key ***API-KEY*** --source {_config.Source}");
            Console.WriteLine();
            Console.WriteLine("  ✓ Dry run complete - no changes made.");
            Console.ResetColor();
            return;
        }

        // Step 1: Clean
        Console.WriteLine("  Step 1/5: Cleaning...");
        await RunCommandAsync("dotnet", $"clean \"{projectPath}\" -c {_config.Configuration}", projectDir);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  ✓ Clean completed");
        Console.ResetColor();

        // Step 2: Restore
        Console.WriteLine();
        Console.WriteLine("  Step 2/5: Restoring...");
        if (!await RunCommandAsync("dotnet", $"restore \"{projectPath}\"", projectDir))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✗ Restore failed - aborting");
            Console.ResetColor();
            return;
        }
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  ✓ Restore completed");
        Console.ResetColor();

        // Step 3: Build
        Console.WriteLine();
        Console.WriteLine("  Step 3/5: Building...");
        if (!await RunCommandAsync("dotnet", $"build \"{projectPath}\" -c {_config.Configuration} --no-restore", projectDir))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✗ Build failed - aborting");
            Console.ResetColor();
            return;
        }
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  ✓ Build completed");
        Console.ResetColor();

        // Step 4: Pack
        Console.WriteLine();
        Console.WriteLine("  Step 4/5: Packing...");
        var packArgs = $"pack \"{projectPath}\" -c {_config.Configuration} -p:Version={_config.Version} --no-build";
        if (_config.IncludeSymbols)
        {
            packArgs += " --include-symbols -p:SymbolPackageFormat=snupkg";
        }
        if (!await RunCommandAsync("dotnet", packArgs, projectDir, hideOutput: false))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✗ Pack failed - aborting");
            Console.ResetColor();
            return;
        }
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  ✓ Pack completed");
        Console.ResetColor();

        // Find package
        var packageFileName = $"{_config.PackageId}.{_config.Version}.nupkg";
        var packagePath = FindPackage(outputDir, packageFileName);
        if (packagePath == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ✗ Package not found: {packageFileName}");
            Console.ResetColor();
            return;
        }

        // Step 5: Push
        Console.WriteLine();
        Console.WriteLine("  Step 5/5: Pushing to NuGet.org...");
        var pushArgs = $"nuget push \"{packagePath}\" --api-key {_config.ApiKey} --source {_config.Source}";
        if (_config.SkipDuplicate)
        {
            pushArgs += " --skip-duplicate";
        }
        if (!await RunCommandAsync("dotnet", pushArgs, projectDir, hideOutput: false))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✗ Push failed");
            Console.ResetColor();
            return;
        }

        // Push symbols
        if (_config.IncludeSymbols)
        {
            var symbolsFileName = $"{_config.PackageId}.{_config.Version}.snupkg";
            var symbolsPath = FindPackage(outputDir, symbolsFileName);
            if (symbolsPath != null)
            {
                Console.WriteLine("  Pushing symbols...");
                var symbolsPushArgs = $"nuget push \"{symbolsPath}\" --api-key {_config.ApiKey} --source {_config.Source}";
                if (_config.SkipDuplicate) symbolsPushArgs += " --skip-duplicate";
                await RunCommandAsync("dotnet", symbolsPushArgs, projectDir, hideOutput: false);
            }
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                         SUCCESS!                             ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine($"  Package {_config.PackageId} v{_config.Version} published to NuGet.org!");
        Console.WriteLine($"  View at: https://www.nuget.org/packages/{_config.PackageId}/{_config.Version}");
        Console.WriteLine();
        Console.WriteLine("  Note: It may take a few minutes for the package to be indexed.");
    }

    #endregion

    #region Helper Methods

    private static string? _solutionRoot = null;

    /// <summary>
    /// Gets the solution root directory - either from config or by auto-detection
    /// </summary>
    private static string? GetSolutionRoot()
    {
        if (_solutionRoot != null) return _solutionRoot;

        // First, check if explicitly configured
        if (!string.IsNullOrWhiteSpace(_config.SolutionRoot) && Directory.Exists(_config.SolutionRoot))
        {
            _solutionRoot = _config.SolutionRoot;
            return _solutionRoot;
        }

        // Try to auto-detect by walking up from current directory
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            if (dir.GetFiles("*.sln").Any())
            {
                _solutionRoot = dir.FullName;
                return _solutionRoot;
            }
            dir = dir.Parent;
        }

        // If running from bin\Debug\net10.0, go up 4 levels to solution root
        var currentDir = Directory.GetCurrentDirectory();
        if (currentDir.Contains(Path.Combine("bin", "Debug")) || currentDir.Contains(Path.Combine("bin", "Release")))
        {
            // bin\Debug\net10.0 -> project -> solution
            var candidate = Path.GetFullPath(Path.Combine(currentDir, "..", "..", "..", ".."));
            if (Directory.Exists(candidate) && Directory.GetFiles(candidate, "*.sln").Any())
            {
                _solutionRoot = candidate;
                return _solutionRoot;
            }
        }

        return null;
    }

    private static string? ResolveProjectPath()
    {
        var solutionRoot = GetSolutionRoot();
        
        if (solutionRoot == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✗ ERROR: Could not determine solution root!");
            Console.WriteLine();
            Console.WriteLine("  Please set 'SolutionRoot' in appsettings.json to your solution folder:");
            Console.WriteLine("    \"SolutionRoot\": \"C:\\\\Users\\\\pepkad\\\\source\\\\repos\\\\FreeGLBA\"");
            Console.ResetColor();
            return null;
        }

        Console.WriteLine($"  Solution root: {solutionRoot}");
        
        var projectPath = Path.GetFullPath(Path.Combine(solutionRoot, _config.ProjectPath));

        if (!File.Exists(projectPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ✗ ERROR: Project file not found: {projectPath}");
            Console.WriteLine();
            Console.WriteLine("  Check that 'ProjectPath' in appsettings.json is correct.");
            Console.WriteLine($"    Current value: {_config.ProjectPath}");
            Console.ResetColor();
            return null;
        }
        
        Console.WriteLine($"  Project path:  {projectPath}");
        return projectPath;
    }

    private static string TruncateString(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength - 3) + "...";
    }

    private static string? FindPackage(string directory, string fileName)
    {
        var searchDirs = new[] { directory, Path.Combine(directory, "net10.0") };

        foreach (var dir in searchDirs)
        {
            if (Directory.Exists(dir))
            {
                var path = Path.Combine(dir, fileName);
                if (File.Exists(path))
                {
                    return path;
                }
            }
        }

        if (Directory.Exists(directory))
        {
            var files = Directory.GetFiles(directory, fileName, SearchOption.AllDirectories);
            if (files.Length > 0)
            {
                return files[0];
            }
        }

        return null;
    }

    private static async Task<bool> RunCommandAsync(string command, string arguments, string workingDirectory, bool hideOutput = true)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"    Failed to start process: {command}");
                Console.ResetColor();
                return false;
            }

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            var output = await outputTask;
            var error = await errorTask;

            // Show output if not hiding, or if command failed
            bool showOutput = !hideOutput || process.ExitCode != 0;
            
            if (showOutput && !string.IsNullOrWhiteSpace(output))
            {
                foreach (var line in output.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)))
                {
                    // Highlight errors in red
                    if (line.Contains("error", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"    {line.TrimEnd()}");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.WriteLine($"    {line.TrimEnd()}");
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                foreach (var line in error.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)))
                {
                    Console.WriteLine($"    {line.TrimEnd()}");
                }
                Console.ResetColor();
            }

            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"    Error: {ex.Message}");
            Console.ResetColor();
            return false;
        }
    }

    #endregion
}

/// <summary>
/// Configuration for NuGet publishing
/// </summary>
public class NuGetConfig
{
    public string ApiKey { get; set; } = "";
    public string Source { get; set; } = "https://api.nuget.org/v3/index.json";
    public string PackageId { get; set; } = "FreeGLBA.Client";
    public string Version { get; set; } = "1.0.0";
    public string SolutionRoot { get; set; } = "";
    public string ProjectPath { get; set; } = "FreeGLBA.NugetClient\\FreeGLBA.NugetClient.csproj";
    public string Configuration { get; set; } = "Release";
    public bool SkipDuplicate { get; set; } = true;
    public bool IncludeSymbols { get; set; } = true;
}
