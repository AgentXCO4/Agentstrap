# Agentstrap

Agentstrap is a Windows Roblox bootstrapper built with **C#, WPF, XAML, and .NET 10**. It provides an installer and launcher plus settings for Roblox startup, appearance, mods, integrations, and other client-side options.

This is an independent project based on Bloxstrap. The original MIT license and copyright notice are retained in [LICENSE](LICENSE).

## Download

Download releases from the [Agentstrap GitHub Releases](https://github.com/AgentXCO4/Agentstrap/releases). Verify that downloads come from this repository before running them. The release workflow requires an Agentstrap-owned SignPath project and GitHub secret before tagged releases can be signed and published; its inherited signing identifiers must be replaced by the repository owner first.

The app is published as a self-contained x64 Windows executable, so users do not need to install the .NET runtime separately.

## Features

- Install, update, and launch Roblox Player or Roblox Studio.
- Configure launch behavior, including process priority.
- Manage supported Roblox content mods and the custom skybox.
- Configure appearance, integrations, shortcuts, and deployment options.
- Use Agentstrap Matchmaker to find and open a public Roblox server through Roblox's regular launch flow.
- Use the optional command console to send allowlisted chat commands through normal Windows clipboard and keyboard interaction.
- Track Roblox activity and expose configured integrations such as Discord Rich Presence.

Agentstrap does not inject code into Roblox or modify Roblox process memory. Follow Roblox's terms and applicable platform policies when using client features.

## Requirements

- Windows 10 or Windows 11, x64.
- An internet connection for installation, Roblox updates, and online services.

The published app bundles its .NET runtime. Building from source requires the .NET 10 SDK and .NET 6 SDK because the checked-in WPF UI dependency still targets .NET 6.

## Build from source

Clone with submodules so the WPF UI dependency is present:

```powershell
git clone --recurse-submodules https://github.com/AgentXCO4/Agentstrap.git
cd Agentstrap
git submodule update --init --recursive
dotnet restore .\Agentstrap.sln
dotnet build .\Agentstrap.sln -c Release --no-restore
```

To publish a self-contained x64 executable:

```powershell
dotnet publish .\Agentstrap\Agentstrap.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true
```

The executable is written to:

```text
Agentstrap\bin\Release\net10.0-windows\win-x64\publish\Agentstrap.exe
```

The application source and built product are named Agentstrap.

## Fork and contribute

1. Use GitHub's **Fork** button to create a copy under your account.
2. Clone your fork with submodules: `git clone --recurse-submodules https://github.com/YOUR-ACCOUNT/Agentstrap.git`.
3. Create a branch for your change: `git switch -c my-change`.
4. Restore and build using the commands above. Keep the existing `wpfui` submodule initialized.
5. Commit and push your branch, then open a pull request against the repository you intend to contribute to.

Before publishing your own fork, verify the repository, support, download, and update endpoints in `Agentstrap/App.xaml.cs` and the GitHub workflows. For tagged releases, configure an Agentstrap-owned SignPath project in the repository's Actions variables: `SIGNPATH_ORGANIZATION_ID`, `SIGNPATH_PROJECT_SLUG`, `SIGNPATH_SIGNING_POLICY`, and `SIGNPATH_ARTIFACT_CONFIGURATION`. Store its API token as the `SIGNPATH_API_TOKEN` secret. Do not reuse another project's credentials. Update the version in `Agentstrap/Agentstrap.csproj` for each release. The workflow builds on `v*` tags and creates a draft GitHub release after signing succeeds.

## Report a bug

[Open an Agentstrap issue](https://github.com/AgentXCO4/Agentstrap/issues/new) and include:

- Agentstrap version and Windows version/build.
- What you expected and what happened instead.
- Reproduction steps, including the Roblox launch path used.
- Relevant Agentstrap and Roblox logs, if available.
- Screenshots or a short recording when they clarify the issue.

Review logs and screenshots before posting. Remove Roblox cookies, authentication tokens, account details, and other private information. Please do not report Roblox account credentials or authentication data in a public issue.

For help using inherited features, the upstream [Bloxstrap help pages](https://bloxstraplabs.com/wiki/) remain available; they may describe behavior that differs from Agentstrap.

## Third-party components

Agentstrap uses the [WPF UI](https://github.com/lepoco/wpfui) controls through the `wpfui` submodule. See [LICENSE](LICENSE) and the licenses included with dependencies for applicable terms.
