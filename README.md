<div align="center">
  <p align="center">
    <a  href="https://docs.x.immutable.com/docs">
      <img src="https://cdn.dribbble.com/users/1299339/screenshots/7133657/media/837237d447d36581ebd59ec36d30daea.gif" width="280"/>
    </a>
  </p>
</div>

---

# Immutable Unity SDK

The Immutable SDK for Unity helps you integrate your game with Immutable Passport.

# Documentation

* [Immutable X](https://docs.immutable.com/reference/unity/)
* [Immutable zkEVM](https://docs.immutable.com/reference/unity/)

## Sample Projects

This repository contains two sample projects:

- **`sample/`** - Unity 2021.3.26f1 sample project
- **`sample-unity6/`** - Unity 6 sample project

Both projects share the same Scenes, Scripts, Editor folders, and Tests via symbolic links, providing a single source of truth for the sample code. See [`sample-unity6/README.md`](sample-unity6/README.md) for setup instructions.

### First Time Setup (for contributors)

The `sample-unity6` project uses symbolic links to share Scenes, Scripts, Editor folders, and Tests with the `sample` project.

**macOS/Linux:** Symlinks are created automatically when you clone/pull - no action needed.

**Windows:** Check if symlinks were created correctly:
1. Navigate to `sample-unity6/Assets/` and `sample-unity6/`
2. Check if `Scenes`, `Scripts`, `Editor`, and `Tests` are folders (symlinks work) or small text files (symlinks didn't work)

If symlinks didn't work, run the setup script as Administrator:

```powershell
# Right-click PowerShell -> "Run as Administrator"
.\setup-symlinks.ps1
```

> **Note for Windows users**: You must run PowerShell as Administrator. Directory symbolic links (required for Unity to recognise the folders) need admin privileges on Windows. See [`sample-unity6/README.md`](sample-unity6/README.md) for details.

## Contributing

Thank you for your interest in contributing to our project! Here's a quick guide on how you can get started:

1. **Fork this Repository**: Fork the repository to your GitHub account by clicking the "Fork" button at the top right of the repository page.
2. **Create a Branch**: Once you've forked the repository, create a new branch in your forked repository where you'll be making your changes. Branch naming convention is enforced [according to patterns here](https://github.com/deepakputhraya/action-branch-name).
3. **Make Changes**: Make the necessary changes in your branch. Ensure that your changes are clear, well-documented, and aligned with the project's guidelines.
4. **Commit Changes**: Commit your changes with clear and descriptive messages following [commit message pattern here](https://github.com/conventional-changelog/commitlint?tab=readme-ov-file#what-is-commitlint). It follows [Conventional Commits specification](https://www.conventionalcommits.org/en/v1.0.0/#specification), which helps maintain a consistent and informative commit history. Read [here](https://www.conventionalcommits.org/en/v1.0.0/#why-use-conventional-commits) to learn more about the benefits of Conventional Commits.
5. **Create a Pull Request (PR)**: After you've made and committed your changes, create a PR against the original repository. Provide a clear description of the changes you've made in the PR.
6. **Example Contribution**: Refer to [this contribution](https://github.com/immutable/unity-immutable-sdk/pull/182) as an example.

## Getting Help

Immutable X is open to all to build on, with no approvals required. If you want to talk to us to learn more, or apply for developer grants, click below:

[Contact us](https://www.immutable.com/contact)

### Project Support

To get help from other developers, discuss ideas, and stay up-to-date on what's happening, become a part of our community on Discord.

[Join us on Discord](https://discord.com/invite/Dmhp398dna)

#### Still need help?

You can also apply for marketing support for your project. Or, if you need help with an issue related to what you're building with Immutable X, click below to submit an issue. Select _I have a question_ or _issue related to building on Immutable X_ as your issue type.

[Contact support](https://support.immutable.com/hc/en-us/requests/new)
