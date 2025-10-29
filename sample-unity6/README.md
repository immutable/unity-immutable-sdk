# Unity 6 Sample Project

This project shares Scenes, Scripts, Editor folders, and Tests with the Unity 2021 sample project located in `../sample`.

## Setup

The `Assets/Scenes`, `Assets/Scripts`, `Assets/Editor` folders, and `Tests` are **symbolic links** pointing to `../sample/Assets/` and `../sample/Tests` respectively. This ensures a single source of truth for both Unity versions.

### First Time Setup

#### On macOS/Linux:
```bash
# From the repository root
./setup-symlinks.sh
```

#### On Windows:
1. **Run PowerShell as Administrator**:
   - Right-click on PowerShell in the Start menu
   - Select "Run as Administrator"

2. Run the setup script:
   ```powershell
   # From the repository root
   .\setup-symlinks.ps1
   ```

> **Note**: Administrator privileges are required because the script creates directory symbolic links (`mklink /D`) which Unity on Windows needs to properly recognise the linked folders. Developer Mode alone is not sufficient for directory symlinks.

### Git Configuration (Windows Users)

If you're on Windows, you may want to enable symlink support in git:

```bash
git config core.symlinks true
```

Note: This should be done **before** cloning the repository for best results.

## How It Works

- **Source of Truth**: All Scenes, Scripts, Editor folders, and Tests are stored in `../sample/Assets/` and `../sample/Tests`
- **Shared Assets**: Changes made in either Unity 2021 or Unity 6 are immediately reflected in both projects
- **Separate Settings**: Each project maintains its own ProjectSettings, Library, and Unity version-specific configurations

## Opening the Project

1. Make sure symlinks are set up (see Setup section above)
2. Open Unity 6
3. Open this project (`sample-unity6` folder)
4. All scenes and scripts from the main sample will be available

## Troubleshooting

### Symlinks appear as text files (on Windows)
- You must run the setup script as Administrator
- Right-click PowerShell → "Run as Administrator", then run `.\setup-symlinks.ps1`
- Ensure `git config core.symlinks` is set to `true`

### Folders don't appear in Unity's Project window (on Windows)
- The script must create directory symbolic links (`mklink /D`) not junctions
- Close Unity completely
- Delete the existing links and run setup script as Administrator
- Reopen Unity and let it reimport assets

### Unity shows missing references
- Close Unity
- Run the appropriate setup script for your platform
- Reopen Unity and let it reimport assets

