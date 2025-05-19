#!/bin/bash

set -e
set -x

# Directory where docs repo is cloned
DOCS_REPO_DIR=${CLONE_DIR:-"./imx-docs"}

# Root of the Passport features
PASSPORT_ROOT="./sample/Assets/Scripts/Passport"

echo "Processing Passport tutorials..."

# Load features.json to map script files to feature names
FEATURES_JSON="$PASSPORT_ROOT/features.json"
if [ ! -f "$FEATURES_JSON" ]; then
  echo "Error: features.json not found at $FEATURES_JSON"
  exit 1
fi

# Create _tutorials directory in docs repo
TUTORIALS_DIR="$DOCS_REPO_DIR/docs/main/example/zkEVM/passport-examples/_tutorials"
mkdir -p "$TUTORIALS_DIR"

# Find all tutorial.md files
TUTORIAL_FILES=$(find "$PASSPORT_ROOT" -name "tutorial.md" -type f)

for TUTORIAL_FILE in $TUTORIAL_FILES; do
  echo "Processing $TUTORIAL_FILE"
  
  # Extract feature directory
  FEATURE_DIR=$(dirname "$TUTORIAL_FILE")
  
  # Try to find script file in this directory
  SCRIPT_FILE=$(find "$FEATURE_DIR" -name "*.cs" -type f | head -n 1)
  if [ -z "$SCRIPT_FILE" ]; then
    echo "Warning: No script file found in $FEATURE_DIR, using directory name"
    FEATURE_NAME=$(basename "$FEATURE_DIR")
  else
    # Extract script filename
    SCRIPT_FILENAME=$(basename "$SCRIPT_FILE")
    
    # Look up the feature name in features.json
    FEATURE_NAME=$(jq -r ".features[] | to_entries[] | select(.value == \"$SCRIPT_FILENAME\") | .key" "$FEATURES_JSON")
    
    # If not found in features.json, fallback to directory name
    if [ -z "$FEATURE_NAME" ] || [ "$FEATURE_NAME" == "null" ]; then
      echo "Warning: Feature for script $SCRIPT_FILENAME not found in features.json, using directory name"
      FEATURE_NAME=$(basename "$FEATURE_DIR")
    fi
  fi
  
  echo "Feature name: $FEATURE_NAME"
  
  # Copy and rename tutorial file
  cp "$TUTORIAL_FILE" "$TUTORIALS_DIR/${FEATURE_NAME}.md"
  echo "Copied $TUTORIAL_FILE to $TUTORIALS_DIR/${FEATURE_NAME}.md"
done

# Copy the generated JSON file
JSON_FILE="./_parsed/passport-features.json"
if [ -f "$JSON_FILE" ]; then
  # Create directory for JSON file if it doesn't exist
  JSON_DIR="$DOCS_REPO_DIR/docs/main/example/zkEVM/passport-examples"
  mkdir -p "$JSON_DIR"
  
  # Copy JSON file
  cp "$JSON_FILE" "$JSON_DIR/passport-features.json"
  echo "Copied $JSON_FILE to $JSON_DIR/passport-features.json"
else
  echo "Warning: No passport-features.json found at $JSON_FILE"
fi

echo "Passport tutorial processing complete." 