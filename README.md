# Unit Converter Starter Project

This repository provides the starter project for the **Unit Converter** ASP.NET Core assignment.

You will fork this repository into your own GitHub account and use your fork throughout the project. Your Git repository will serve as the working history of your project and will also be used to create the ZIP file you submit to Blackboard.

> **Important:** Do not download the repository as a ZIP file to begin the assignment. Fork the repository on GitHub and then clone your fork as described below.

## Current Project Status

| Lesson | Status |
| --- | --- |
| Starter Project | ![Starter Project Tests](../../actions/workflows/starter-tests.yml/badge.svg) |
| Lesson 1 — First Razor Page | ![Lesson 1 Tests](../../actions/workflows/lesson-01-tests.yml/badge.svg) |
| Lesson 2 — Route Parameters and Multiple Conversions | ![Lesson 2 Tests](../../actions/workflows/lesson-02-tests.yml/badge.svg) |

Each badge shows the current status of the automated tests for that stage of the project. A new lesson's badge may initially be failing; it should become green as you complete that lesson's requirements.

## Project Environment

This project uses the following development environment:

- JetBrains Rider
- .NET 10 SDK
- Git
- GitHub
- ASP.NET Core with Kestrel
- xUnit for automated tests

The repository already contains the basic ASP.NET Core application and test-project structure. You should use the provided solution rather than creating a new ASP.NET Core project.

## Repository Structure

The important parts of the repository are:

```text
UnitConverter/
├── .course/                    Course update configuration
├── .github/workflows/          Automated GitHub Actions tests
├── src/
│   └── UnitConverter/          ASP.NET Core application
├── tests/
│   └── UnitConverter.Tests/    Automated tests
├── tools/
│   └── CourseUpdater/          Installs lesson updates
├── UnitConverter.slnx          Rider/.NET solution
├── global.json                 Required .NET SDK version
├── Directory.Build.props       Project-wide build settings
└── README.md                   These instructions
```

You will primarily work in the `src/UnitConverter` project. Automated tests belong in `tests/UnitConverter.Tests`.

> **Important:** Do not modify files in `.course/`. They are used by the course updater to track lesson releases.

---

# 1. Git and GitHub Configuration

You will use Git and GitHub throughout this assignment. Git keeps a history of the changes you make to your project, while GitHub stores a copy of your Git repository online.

## Configure Git

If you have not previously configured Git on your computer, verify your configuration from a terminal:

```bash
git config --global user.name
git config --global user.email
```

These commands should display your name and the email address associated with your GitHub account.

If they do not, configure them with:

```bash
git config --global user.name "Your Name"
git config --global user.email "your-email@example.com"
```

## Configure GitHub Authentication

> If you do not yet have a GitHub account, sign up for one using your school email address.  Select a professonal username, you will likely use this account to showcase your work when applying for jobs in the future.

You should configure SSH authentication between your computer and GitHub.

Follow GitHub's instructions using *Git Bash* (on Windows) at [Connecting to GitHub with SSH](https://docs.github.com/en/authentication/connecting-to-github-with-ssh) for:

1. Creating an SSH key.
2. Adding the key to your GitHub account.
3. Testing your SSH connection.

Once configured, your account on this computer will use the keys to authenticate you with any of your repositories on GitHub.  You normally will not need to enter your GitHub username and password when pushing your work.

You will need to do this once on each computer you use Git and GitHub on.

---

# 2. Fork and Clone the Starter Repository

## Fork the Repository

Open the starter repository on GitHub and select **Fork** in the upper-right corner of the repository page.

This creates your own copy of the repository under your GitHub account.

You will make your changes in **your fork**, not in the original class repository.

## Clone Your Fork

On the GitHub page for **your fork**, select **Code → SSH** and copy the SSH repository address.

In a command-line terminal such as Git Bash, change to the directory where you keep your class projects with the cd command:

In Git Bash:
```bash
cd $USERPROFILE/RiderProjects/
```

In Windows Terminal:
```bash
cd $env:USERPROFILE\RiderProjects
```

In cmd.exe:
```bash
cd %USERPROFILE%\RiderProjects
```

Then run:

```bash
git clone <your-repository-address>
```

For example:

```bash
git clone git@github.com:yourusername/UnitConverter.git
```

Then enter the repository:

```bash
cd UnitConverter
```

## Verify Your Repository

Run:

```bash
git status
```

You should see a message similar to:

```text
On branch main
Your branch is up to date with 'origin/main'.

nothing to commit, working tree clean
```

Next, check which GitHub repository your local project is connected to:

```bash
git remote -v
```

The `origin` addresses should point to your GitHub fork. For example:

```text
origin  git@github.com:yourusername/UnitConverter.git (fetch)
origin  git@github.com:yourusername/UnitConverter.git (push)
```

## Add the Instructor Repository

Your fork is called `origin`. You will also add the instructor's starter repository as a second remote called `upstream`.

You only need to do this once:

```bash
git remote add upstream git@github.com:WVUP/UnitConverter.git
```

Verify the configuration:

```bash
git remote -v
```

You should now see both `origin` and `upstream`.

---

# 3. Open and Verify the Starter Project

Open `UnitConverter.slnx` in JetBrains Rider.

Allow Rider to restore the project's NuGet packages if necessary.

Before changing any code, verify that the starter project builds successfully.

From Rider, use **Build → Build Solution**.

You can also build from a terminal in the repository root:

```bash
dotnet build
```

Then run the automated tests:

```bash
dotnet test
```

Finally, run the ASP.NET Core application from Rider and verify that it opens successfully in your browser.

Do not begin the assignment until the starter project builds, the tests run, and the application starts successfully.

---

# 4. Working on the Assignment

You will continue using this repository as you develop the Unit Converter application across several lessons.

Do not wait until the assignment is finished to use Git.

A good development cycle is:

1. Check the repository and pull your GitHub changes:

   ```bash
   git status
   git pull
   ```

2. Make a small group of related changes.
3. Build and test your application.
4. Commit the working changes:

   ```bash
   git add .
   git commit -m "Describe the changes you made"
   ```

5. Continue to the next part of the assignment.
6. Push your commits to GitHub regularly:

   ```bash
   git push
   ```

## Before Starting Work

At the beginning of each work session, open a terminal in the repository and run:

```bash
git status
git pull
```

`git status` tells you whether you have uncommitted local changes.

`git pull` retrieves changes that are on your GitHub fork but not yet on your computer.

Getting into the habit of doing this **before you start editing files** will prevent many common Git problems.

---

# 5. Saving Your Work with Git

Git commits are checkpoints in the history of your project.

You should commit your work regularly rather than making one large commit at the end of the assignment.

## Check Your Changes

Before committing, run:

```bash
git status
```

This shows which files have changed.

You can also see the actual changes with:

```bash
git diff
```

## Stage Your Changes

To stage all changed files:

```bash
git add .
```

Then check again:

```bash
git status
```

Git will show the files that are ready to be committed.

## Commit Your Changes

Create a commit with a short message describing what you accomplished:

```bash
git commit -m "Add length conversion form"
```

Good commit messages describe the completed change:

```text
Add temperature conversion service
Create unit selection controls
Add tests for length conversions
Fix validation for empty input
Update converter page layout
```

Avoid messages such as:

```text
stuff
changes
assignment
worked on project
final
```

If you need to stop working before reaching a major milestone, a temporary work-in-progress commit is acceptable:

```bash
git commit -m "WIP: begin temperature conversion"
```

The important goal is to avoid having hours of work that exist only on your computer.

## Push Your Work to GitHub

After making one or more commits, send them to GitHub:

```bash
git push
```

A commit saves a checkpoint **on your computer**.

A push sends your commits **to GitHub**.

Your work is not backed up on GitHub until you push it.

---

# 6. Checking Your Repository Before You Stop

Before ending a work session, run:

```bash
git status
```

Ideally, you should see:

```text
nothing to commit, working tree clean
```

Then run:

```bash
git push
```

Check your repository on GitHub and make sure your recent commits appear there.

A good rule for this class is:

> **Pull when you begin. Commit while you work. Push before you leave.**

---

# 7. Receiving Lesson Updates

New lesson requirements, automated tests, GitHub Actions workflows, and README updates are released through the course updater.

The updater receives instructor materials from `upstream` while leaving your normal `origin` workflow unchanged.

## Install the Next Lesson

Before installing a lesson update, make sure your current work is committed:

```bash
git status
```

Your working tree should be clean.

Then run:

```bash
dotnet run --project tools/CourseUpdater
```

The updater will:

1. Verify that your working tree is clean.
2. Fetch the available instructor lesson releases.
3. Determine the next lesson you need.
4. Install that lesson's tests, GitHub Actions workflow, README changes, and other instructor-provided files.

After the update is installed, run:

```bash
dotnet test
```

A newly released lesson may contain tests that fail at first. **This is expected.** Those tests describe behavior you will implement during the lesson.

Push the installed lesson update to your GitHub fork:

```bash
git push
```

## Check Lesson Status

To see which lesson updates have already been installed:

```bash
dotnet run --project tools/CourseUpdater -- --list
```

If the updater cannot install a lesson automatically, it will return the repository to its previous state. Run:

```bash
git status
```

and ask for assistance before trying to force the update.

---

# 8. Troubleshooting Git

## "I changed something and want to see what I did."

Run:

```bash
git diff
```

## "I changed a file but haven't committed it, and I want the original version back."

First inspect your changes:

```bash
git diff
```

If you are certain you want to discard them:

```bash
git restore <filename>
```

**Warning:** `git restore` permanently discards uncommitted changes to that file.

## "I forgot to pull before I started working."

Do **not** delete your project and do **not** immediately run destructive Git commands.

First:

```bash
git status
```

Commit your current work if it is in a reasonable state:

```bash
git add .
git commit -m "WIP: save local changes before synchronization"
```

Then:

```bash
git pull
```

Git may merge the changes automatically. If it reports a conflict, stop and resolve the conflict before continuing.

## "My project worked before, but now it doesn't."

Use:

```bash
git log --oneline
```

This shows the checkpoints you have created.

You can also inspect the changes you have made since your last commit:

```bash
git diff
```

One of the major advantages of making frequent commits is that you have a history you can use to determine what changed.

If you are unsure how to recover your project, ask for assistance **before using `git reset --hard` or deleting files**.

---

# 9. Before Submitting

Before creating your submission, make sure the project is in a working state.

Run:

```bash
dotnet build
```

Then:

```bash
dotnet test
```

Run the application and verify its behavior in your browser.

Next, check Git:

```bash
git status
```

You should see:

```text
nothing to commit, working tree clean
```

If you have changes that should be part of your assignment, commit them before continuing.

Finally:

```bash
git push
```

Verify on GitHub that your most recent commit appears in your repository.

---

# 10. Submitting the Project to Blackboard

Your Blackboard submission should contain the **committed version** of your Git project.

From the root directory of the repository, run:

```bash
git archive HEAD -o <yourname>-UnitConverter.zip
```

For example:

```bash
git archive HEAD -o JaneSmith-UnitConverter.zip
```

This creates a ZIP archive from the most recent Git commit.

Because the archive is created by Git, files that are not tracked by the repository—such as build output and IDE-specific files—will not be included.

> **Important:** `git archive` includes only committed files. If you forgot to commit a change, that change will **not** be included in your submission.

After creating the archive, open the ZIP file and verify that it contains:

```text
src/
tests/
tools/
.course/
UnitConverter.slnx
global.json
README.md
```

Submit the resulting ZIP file to Blackboard.

---

# Git Command Quick Reference

| Command | Purpose |
| --- | --- |
| `git status` | See the current state of your repository |
| `git diff` | See your uncommitted changes |
| `git pull` | Get changes from your GitHub fork |
| `git add .` | Stage your changes for a commit |
| `git commit -m "message"` | Create a checkpoint |
| `git push` | Send your commits to GitHub |
| `git log --oneline` | View your commit history |
| `dotnet run --project tools/CourseUpdater` | Install the next lesson update |
| `dotnet run --project tools/CourseUpdater -- --list` | Show installed lesson updates |
| `git restore <file>` | Discard uncommitted changes to a file |

## The Workflow to Remember

```text
START WORK
    ↓
git status
    ↓
git pull
    ↓
Make a small set of changes
    ↓
Build and test
    ↓
git add .
    ↓
git commit
    ↓
Continue working
    ↓
git push
    ↓
CHECK GITHUB
```

**Pull when you begin. Commit while you work. Push before you leave.**
