#!/bin/bash
# shellcheck shell=bash

# Setup script for creating symlinks between sample and sample-unity6
# Works on macOS and Linux

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
SAMPLE_UNITY6_ASSETS="$SCRIPT_DIR/sample-unity6/Assets"
SAMPLE_UNITY6="$SCRIPT_DIR/sample-unity6"

echo "Setting up symlinks for sample-unity6..."

# Remove existing directories/symlinks if they exist in Assets
cd "$SAMPLE_UNITY6_ASSETS" || exit
rm -rf Scenes Scripts Editor
rm -f Scenes.meta Scripts.meta Editor.meta

# Create symlinks for Assets
ln -s ../../sample/Assets/Scenes Scenes
ln -s ../../sample/Assets/Scripts Scripts
ln -s ../../sample/Assets/Editor Editor
ln -s ../../sample/Assets/Scenes.meta Scenes.meta
ln -s ../../sample/Assets/Scripts.meta Scripts.meta
ln -s ../../sample/Assets/Editor.meta Editor.meta

echo "✅ Asset symlinks created successfully!"
echo ""
echo "Scenes, Scripts, and Editor in sample-unity6 now point to sample/Assets"
for item in Scenes Scripts Editor; do
    if [ -e "$SAMPLE_UNITY6_ASSETS/$item" ]; then
        ls -la "$SAMPLE_UNITY6_ASSETS/$item"
    fi
done

# Create symlink for Tests
cd "$SAMPLE_UNITY6" || exit
rm -rf Tests
ln -s ../sample/Tests Tests

echo ""
echo "✅ Tests symlink created successfully!"
echo "Tests in sample-unity6 now points to sample/Tests"
if [ -e "$SAMPLE_UNITY6/Tests" ]; then
    ls -la "$SAMPLE_UNITY6/Tests"
fi

