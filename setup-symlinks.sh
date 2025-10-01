---
#!/bin/bash

# Setup script for creating symlinks between sample and sample-unity6
# Works on macOS and Linux

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
SAMPLE_UNITY6_ASSETS="$SCRIPT_DIR/sample-unity6/Assets"

echo "Setting up symlinks for sample-unity6..."

# Remove existing directories/symlinks if they exist
cd "$SAMPLE_UNITY6_ASSETS"
rm -rf Scenes Scripts
rm -f Scenes.meta Scripts.meta

# Create symlinks
ln -s ../../sample/Assets/Scenes Scenes
ln -s ../../sample/Assets/Scripts Scripts
ln -s ../../sample/Assets/Scenes.meta Scenes.meta
ln -s ../../sample/Assets/Scripts.meta Scripts.meta

echo "✅ Symlinks created successfully!"
echo ""
echo "Scenes and Scripts in sample-unity6 now point to sample/Assets"
ls -la "$SAMPLE_UNITY6_ASSETS" | grep -E "(Scenes|Scripts)"

