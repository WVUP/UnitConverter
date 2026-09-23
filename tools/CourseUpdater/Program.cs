using System.Diagnostics;
using System.Text.Json;

return await CourseUpdater.RunAsync(args);

internal static class CourseUpdater
{
    private const string ManifestPath = ".course/course-updates.json";

    private static readonly TimeSpan GitTimeout = TimeSpan.FromSeconds(30);

    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            var repositoryRoot = await GetRepositoryRootAsync();
            Directory.SetCurrentDirectory(repositoryRoot);

            var manifest = await LoadManifestAsync();

            if (args.Length == 1 && args[0].Equals("--list", StringComparison.OrdinalIgnoreCase))
            {
                PrintLessonStatus(manifest);
                return 0;
            }

            if (args.Length > 1)
            {
                return Fail("Usage: dotnet run --project tools/CourseUpdater -- [lesson-number|--list]");
            }

            if (!await WorkingTreeIsCleanAsync())
            {
                return Fail("Your Git working tree is not clean. Commit or restore your changes before installing a course update.\n" +
                    "Run 'git status' to see what needs attention.");
            }

            if (!await RemoteExistsAsync(manifest.Remote))
            {
                return Fail(
                    $"The Git remote '{manifest.Remote}' is not configured.\n" +
                    "Add the instructor repository as the upstream remote before running the updater.");
            }

            var lesson = SelectLesson(manifest, args);

            if (lesson is null)
            {
                Console.WriteLine("All available lesson updates are already installed.");

                return 0;
            }

            if (!PrerequisitesInstalled(manifest, lesson))
            {
                return Fail(
                    $"{lesson.Name} cannot be installed yet because an earlier lesson update is missing.\n" +
                    "Run the updater without a lesson number to install the next lesson in sequence.");
            }

            Console.WriteLine($"Course:  {manifest.Course}");
            Console.WriteLine($"Project: {manifest.Project}");
            Console.WriteLine($"Update:  {lesson.Name}");
            Console.WriteLine();
            Console.WriteLine("Fetching instructor updates...");

            var fetch = await RunGitAsync("fetch", manifest.Remote, "--tags");

            if (fetch.TimedOut)
            {
                return Fail(
                    "Fetching instructor updates timed out.\n\n" +
                    "If your SSH private key is protected by a passphrase, " +
                    "Git may be waiting for SSH authentication.\n\n" +
                    "Make sure ssh-agent is running and your key has been " +
                    "added, then try the update again.\n\n" +
                    "You can test the connection manually with:\n" +
                    $"  git fetch {manifest.Remote} --tags\n\n" +
                    "Git command:\n" +
                    $"  {fetch.Command}");
            }

            if (fetch.ExitCode != 0)
            {
                return Fail("Unable to fetch instructor updates.\n\n" + FormatGitFailure(fetch));
            }

            if (!await TagExistsAsync(lesson.Tag))
            {
                return Fail(
                    $"The update tag '{lesson.Tag}' was not found on " +
                    $"'{manifest.Remote}'.\n \t The instructor may not have published this lesson yet.");
            }

            Console.WriteLine($"Applying {lesson.Name}...");

            var cherryPick = await RunGitAsync("cherry-pick", lesson.Tag);

            if (cherryPick.TimedOut)
            {
                await TryAbortCherryPickAsync();

                return Fail(
                    $"{lesson.Name} could not be applied because the Git " +
                    "operation timed out.\n" +
                    "Your repository has been returned to its previous " +
                    "state.\n\n" +
                    FormatGitFailure(cherryPick));
            }

            if (cherryPick.ExitCode != 0)
            {
                await TryAbortCherryPickAsync();

                return Fail(
                    $"{lesson.Name} could not be applied automatically. " +
                    "Your repository has been returned to its previous " +
                    "state.\n\n" +
                    FormatGitFailure(cherryPick) +
                    "\n\nRun 'git status' and ask for assistance before " +
                    "trying again.");
            }

            if (!File.Exists(lesson.Marker))
            {
                return Fail(
                    "The update was applied, but its marker file " +
                    $"'{lesson.Marker}' is missing.\n" +
                    "Please ask the instructor to check the lesson " +
                    "update package.");
            }

            Console.WriteLine();
            Console.WriteLine($"{lesson.Name} installed successfully.");
            Console.WriteLine();
            Console.WriteLine("Next steps:");
            Console.WriteLine("  dotnet test");
            Console.WriteLine("  git push");

            return 0;
        }
        catch (FileNotFoundException ex)
        {
            return Fail(ex.Message);
        }
        catch (JsonException ex)
        {
            return Fail($"The course update manifest is invalid: {ex.Message}");
        }
        catch (ArgumentException ex)
        {
            return Fail(ex.Message);
        }
        catch (Exception ex)
        {
            return Fail($"Course updater failed: {ex.Message}");
        }
    }

    private static async Task<string> GetRepositoryRootAsync()
    {
        var result = await RunGitAsync("rev-parse", "--show-toplevel");

        if (result.TimedOut)
        {
            throw new InvalidOperationException("Git timed out while locating the project repository.");
        }

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException("Run the course updater from inside the project Git repository.");
        }

        return result.OutputText.Trim();
    }

    private static async Task<CourseManifest> LoadManifestAsync()
    {
        if (!File.Exists(ManifestPath))
        {
            throw new FileNotFoundException($"Course update manifest not found: {ManifestPath}");
        }

        await using var stream = File.OpenRead(ManifestPath);

        var manifest = 
            await JsonSerializer.DeserializeAsync<CourseManifest>(
                stream,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

        return manifest ?? throw new JsonException("The manifest is empty.");
    }

    private static LessonUpdate? SelectLesson(CourseManifest manifest, string[] args)
    {
        if (args.Length == 0)
        {
            return manifest.Lessons
                .OrderBy(lesson => lesson.Number)
                .FirstOrDefault(
                    lesson => !File.Exists(lesson.Marker));
        }

        if (!int.TryParse(args[0], out var lessonNumber))
        {
            throw new ArgumentException("Lesson must be specified as a number, or use --list.");
        }

        var lesson = manifest.Lessons
            .SingleOrDefault(candidate => candidate.Number == lessonNumber);

        if (lesson is null)
        {
            throw new ArgumentException($"Lesson {lessonNumber} is not defined in {ManifestPath}.");
        }

        if (File.Exists(lesson.Marker))
        {
            Console.WriteLine($"{lesson.Name} is already installed.");

            return null;
        }

        return lesson;
    }

    private static bool PrerequisitesInstalled(CourseManifest manifest, LessonUpdate selectedLesson)
    {
        return manifest.Lessons
            .Where(
                lesson =>
                    lesson.Number < selectedLesson.Number)
            .All(
                lesson =>
                    File.Exists(lesson.Marker));
    }

    private static void PrintLessonStatus(CourseManifest manifest)
    {
        Console.WriteLine($"{manifest.Course} - {manifest.Project}");
        Console.WriteLine();

        foreach (var lesson in manifest.Lessons.OrderBy(lesson => lesson.Number))
        {
            var status =
                File.Exists(lesson.Marker)
                    ? "Installed"
                    : "Not installed";

            Console.WriteLine(
                $"{lesson.Number,2}. " +
                $"{lesson.Name,-30} " +
                $"{status}");
        }
    }

    private static async Task<bool> WorkingTreeIsCleanAsync()
    {
        var result = await RunGitAsync("status", "--porcelain");

        if (result.TimedOut)
        {
            throw new InvalidOperationException("Git timed out while checking the working tree.");
        }

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException("Git could not check the working tree.\n" + FormatGitFailure(result));
        }

        return string.IsNullOrWhiteSpace(result.OutputText);
    }

    private static async Task<bool> RemoteExistsAsync(string remote)
    {
        var result = await RunGitAsync("remote", "get-url", remote);

        if (result.TimedOut)
        {
            throw new InvalidOperationException("Git timed out while checking the configured remotes.");
        }

        return result.ExitCode == 0;
    }

    private static async Task<bool> TagExistsAsync(string tag)
    {
        var result = await RunGitAsync("rev-parse", "--verify", $"refs/tags/{tag}^{{commit}}");

        if (result.TimedOut)
        {
            throw new InvalidOperationException("Git timed out while checking the lesson update tag.");
        }

        return result.ExitCode == 0;
    }

    private static async Task TryAbortCherryPickAsync()
    {
        var abort = await RunGitAsync("cherry-pick", "--abort");

        if (abort.TimedOut)
        {
            Console.Error.WriteLine("Warning: Git timed out while attempting to abort the cherry-pick.");
            Console.Error.WriteLine("Run 'git status' before making additional changes.");
        }
    }

    private static async Task<GitResult> RunGitAsync(params string[] arguments)
    {
        var command = $"git {string.Join(' ', arguments)}";

        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // CourseUpdater cannot respond to an interactive Git
        // credential prompt. Fail instead of allowing Git itself
        // to wait indefinitely for terminal input.
        startInfo.Environment["GIT_TERMINAL_PROMPT"] = "0";

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process =
            new Process
            {
                StartInfo = startInfo
            };

        try
        {
            process.Start();
        }
        catch (Exception ex)
            when (ex is
                  System.ComponentModel.Win32Exception
                  or InvalidOperationException)
        {
            throw new InvalidOperationException("Git could not be started. Verify that Git is installed and available on PATH.", ex);
        }

        var outputTask = process.StandardOutput.ReadToEndAsync();

        var errorTask = process.StandardError.ReadToEndAsync();

        using var timeout = new CancellationTokenSource(GitTimeout);

        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            try
            {
                process.Kill(entireProcessTree: true);

                await process.WaitForExitAsync();
            }
            catch
            {
                // Best-effort cleanup. The timeout result below
                // still provides the caller with a useful error.
            }

            return new GitResult(
                -1,
                await outputTask,
                await errorTask,
                TimedOut: true,
                Command: command);
        }

        return new GitResult(
            process.ExitCode,
            await outputTask,
            await errorTask,
            TimedOut: false,
            Command: command);
    }

    private static string FormatGitFailure(GitResult result)
    {
        var details =
            !string.IsNullOrWhiteSpace(result.ErrorText)
                ? result.ErrorText.Trim()
                : !string.IsNullOrWhiteSpace(result.OutputText)
                    ? result.OutputText.Trim()
                    : $"Git exited with code {result.ExitCode}.";

        return
            $"Git command:\n" +
            $"  {result.Command}\n\n" +
            $"{details}";
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine();
        Console.Error.WriteLine("Course update not applied.");
        Console.Error.WriteLine(message.Trim());

        return 1;
    }
}

internal sealed record CourseManifest(
    string Course,
    string Project,
    string Remote,
    IReadOnlyList<LessonUpdate> Lessons);

internal sealed record LessonUpdate(
    int Number,
    string Name,
    string Tag,
    string Marker);

internal sealed record GitResult(
    int ExitCode,
    string OutputText,
    string ErrorText,
    bool TimedOut = false,
    string? Command = null);
