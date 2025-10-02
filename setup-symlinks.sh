#!/bin/bash
# shellcheck shell=bash

# Setup script for creating symlinks between sample and sample-unity6
# Works on macOS and Linux

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
SAMPLE_UNITY6_ASSETS="$SCRIPT_DIR/sample-unity6/Assets"

echo "Setting up symlinks for sample-unity6..."

# Remove existing directories/symlinks if they exist
cd "$SAMPLE_UNITY6_ASSETS" || exit
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
for item in Scenes Scripts; do
    if [ -e "$SAMPLE_UNITY6_ASSETS/$item" ]; then
        ls -la "$SAMPLE_UNITY6_ASSETS/$item"
    fi
done

