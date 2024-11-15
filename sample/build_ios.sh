#!/bin/bash

PATH_UNITY="/Applications/Unity/Unity.app/Contents/MacOS/Unity"
PATH_TO_UNITY_SDK_SAMPLE_APP="./"
BUILD_METHOD="MobileBuilder.BuildForAltTester"
APPLE_TEAM_ID=""

# Define the build paths
BUILD_XCODE_PATH="$(pwd)/build/output/iOS/Xcode"
BUILD_ARCHIVE_PATH="$(pwd)/build/output/iOS/Archive"
BUILD_IPA_PATH="$(pwd)/build/output/iOS/IPA"

# Function to clear a directory
clear_directory() {
    local dir_path=$1
    if [ -d "$dir_path" ]; then
        echo "Clearing contents of $dir_path."
        rm -rf "$dir_path"/*
    else
        echo "Directory not found at $dir_path"
    fi
}

# Clear all specified directories
clear_directory "$BUILD_XCODE_PATH"
clear_directory "$BUILD_ARCHIVE_PATH"
clear_directory "$BUILD_IPA_PATH"

# Unity build command
UNITY_COMMAND="$PATH_UNITY -projectPath \"$PATH_TO_UNITY_SDK_SAMPLE_APP\" -executeMethod $BUILD_METHOD -logFile logFile.log -quit -batchmode --buildPath \"$BUILD_XCODE_PATH\" --platform iOS"
echo "Running command: $UNITY_COMMAND"

# Execute the Unity build command
eval "$UNITY_COMMAND"

# Check if the Unity build command was successful
if [ $? -ne 0 ]; then
    echo "Unity build failed. Exiting script."
    exit 1
fi

# Build and archive project
xcodebuild -project "$(pwd)/build/output/iOS/Xcode/Unity-iPhone.xcodeproj" \
           -scheme Unity-iPhone \
           -archivePath "$(pwd)/build/output/iOS/Archive/Unity-iPhone.xcarchive" \
           -configuration Release \
           DEVELOPMENT_TEAM="$APPLE_TEAM_ID" \
           CODE_SIGN_STYLE=Automatic \
           archive

# Create ExportOptions.plist with the correct APPLE_TEAM_ID
EXPORT_OPTIONS_PATH="$(pwd)/build/output/iOS/Archive/ExportOptions.plist"

cat <<EOF > "$EXPORT_OPTIONS_PATH"
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>method</key>
    <string>development</string> <!-- Use 'ad-hoc' or 'app-store' as needed -->
    <key>teamID</key>
    <string>$APPLE_TEAM_ID</string>
    <key>signingStyle</key>
    <string>automatic</string> <!-- Use automatic signing -->
    <key>compileBitcode</key>
    <false/>
    <key>thinning</key>
    <string>&lt;none&gt;</string>
</dict>
</plist>
EOF

# Generate .ipa file
xcodebuild -exportArchive \
           -archivePath "$(pwd)/build/output/iOS/Archive/Unity-iPhone.xcarchive" \
           -exportPath "$(pwd)/build/output/iOS/IPA" \
           -exportOptionsPlist "$EXPORT_OPTIONS_PATH"
