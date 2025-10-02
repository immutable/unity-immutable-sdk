# Unity 6 Sample Project

> **Note:** This Unity 6 sample is a work in progress.

This project shares Scenes, Scripts, and Editor folders with the Unity 2021 sample project located in `../sample`.

## Setup

The `Assets/Scenes`, `Assets/Scripts`, and `Assets/Editor` folders are **symbolic links** pointing to `../sample/Assets/`. This ensures a single source of truth for both Unity versions.

### First Time Setup

#### On macOS/Linux:
```bash
# From the repository root
./setup-symlinks.sh
```

#### On Windows:
1. **Enable Developer Mode** (Recommended):
   - Open Settings → Update & Security → For developers
   - Turn on "Developer Mode"

2. Run the setup script in PowerShell:
   ```powershell
   # From the repository root
   .\setup-symlinks.ps1
   ```

   Or run PowerShell as Administrator if you don't want to enable Developer Mode.

### Git Configuration (Windows Users)

If you're on Windows, you may want to enable symlink support in git:

```bash
git config core.symlinks true
```

Note: This should be done **before** cloning the repository for best results.

## How It Works

- **Source of Truth**: All Scenes, Scripts, and Editor folders are stored in `../sample/Assets/`
- **Shared Assets**: Changes made in either Unity 2021 or Unity 6 are immediately reflected in both projects
- **Separate Settings**: Each project maintains its own ProjectSettings, Library, and Unity version-specific configurations

## Opening the Project

1. Make sure symlinks are set up (see Setup section above)
2. Open Unity 6
3. Open this project (`sample-unity6` folder)
4. All scenes and scripts from the main sample will be available

## Troubleshooting

### Symlinks appear as text files
- On Windows: Enable Developer Mode or run the setup script as Administrator
- Ensure `git config core.symlinks` is set to `true`

### Unity shows missing references
- Close Unity
- Run the appropriate setup script for your platform
- Reopen Unity and let it reimport assets

