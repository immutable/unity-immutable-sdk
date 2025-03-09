#!/bin/bash

PATH_UNITY="/Applications/Unity/Hub/Editor/2021.3.26f1/Unity.app/Contents/MacOS/Unity"
PATH_TO_UNITY_SDK_SAMPLE_APP="./"
BUILD_METHOD="MobileBuilder.BuildForAltTester"
APPLE_TEAM_ID="54XMLXPF98"

# Define the build paths
BUILD_XCODE_PATH="$(pwd)/build/output/iOS/Xcode"
BUILD_ARCHIVE_PATH="$(pwd)/build/output/iOS/Archive"
BUILD_IPA_PATH="$(pwd)/build/output/iOS/IPA"
DERIVED_DATA_PATH="$(pwd)/build/output/iOS/DerivedData"

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
clear_directory "$DERIVED_DATA_PATH"

mkdir -p "$BUILD_XCODE_PATH"
mkdir -p "$BUILD_ARCHIVE_PATH"
mkdir -p "$BUILD_IPA_PATH"
mkdir -p "$DERIVED_DATA_PATH"

# Unity build command
UNITY_COMMAND="$PATH_UNITY -projectPath \"$PATH_TO_UNITY_SDK_SAMPLE_APP\" -executeMethod $BUILD_METHOD \
-logFile logFile.log -quit -batchmode --buildPath \"$BUILD_XCODE_PATH\" \
--platform iOS --bundleIdentifier com.immutable.Immutable-Sample-GameSDK \
--host \"127.0.0.1\" --ciBuild"
echo "Running command: $UNITY_COMMAND"

# Execute the Unity build command
eval "$UNITY_COMMAND"

# Check if the Unity build command was successful
if [ $? -ne 0 ]; then
    echo "Unity build failed. Exiting script."
    exit 1
fi

echo "Building app..."
/Applications/Xcode.app/Contents/Developer/usr/bin/xcodebuild clean build \
           -project "$(pwd)/build/output/iOS/Xcode/Unity-iPhone.xcodeproj" \
           -scheme Unity-iPhone \
           -destination "generic/platform=iOS" \
           DEVELOPMENT_TEAM="$APPLE_TEAM_ID" \
           -allowProvisioningUpdates \
           -derivedDataPath "$(pwd)/build/output/iOS/DerivedData"

mkdir -p "$(pwd)/build/output/iOS/IPA/Payload"

mv "$(pwd)/build/output/iOS/DerivedData/Build/Products/ReleaseForRunning-iphoneos/ImmutableSample.app" "$(pwd)/build/output/iOS/IPA/Payload"

pushd "$(pwd)/build/output/iOS/IPA" && zip -r Payload.zip Payload && popd

mv "$(pwd)/build/output/iOS/IPA/Payload.zip" "$(pwd)/Tests/test/ios/Payload.ipa"