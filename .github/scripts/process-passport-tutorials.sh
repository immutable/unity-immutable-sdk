#!/bin/bash

set -e
set -x

# Directory where docs repo is cloned
DOCS_REPO_DIR="${CLONE_DIR:-"./imx-docs"}"

# Root of the Passport features
PASSPORT_ROOT="./sample/Assets/Scripts/Passport"
TUTORIALS_DIR="${PASSPORT_ROOT}/_tutorials~"

echo "Processing Passport tutorials..."

FEATURES_JSON="${PASSPORT_ROOT}/features.json"
if [ ! -f "${FEATURES_JSON}" ]; then
  echo "Error: features.json not found at ${FEATURES_JSON}"
  exit 1
fi

# Base directory for usage guides in the docs repo
DOCS_USAGE_GUIDES_DIR="${DOCS_REPO_DIR}/docs/main/build/unity/usage-guides"

# Check if _tutorials~ directory exists
if [ ! -d "${TUTORIALS_DIR}" ]; then
  echo "Warning: _tutorials~ directory not found at ${TUTORIALS_DIR}"
else
  # Process each feature group directory in _tutorials~
  find "${TUTORIALS_DIR}" -mindepth 1 -maxdepth 1 -type d -print0 | while IFS= read -r -d '' GROUP_DIR; do
    echo "Processing feature group: ${GROUP_DIR}"
    
    # Extract feature group name from directory
    GROUP_NAME=$(basename "${GROUP_DIR}")
    
    # Tutorial file path
    TUTORIAL_FILE="${GROUP_DIR}/tutorial.md"
    
    if [ -f "${TUTORIAL_FILE}" ]; then
      echo "Found tutorial for ${GROUP_NAME}"
      
      # Define the destination directory for this feature group
      DEST_GROUP_DIR="${DOCS_USAGE_GUIDES_DIR}/${GROUP_NAME}"
      mkdir -p "${DEST_GROUP_DIR}"
      
      # Use the folder name directly for the destination filename
      OUTPUT_FILENAME="${GROUP_NAME}.md"
      
      # Copy the tutorial file to its new group directory
      cp "${TUTORIAL_FILE}" "${DEST_GROUP_DIR}/${OUTPUT_FILENAME}"
      echo "Copied ${TUTORIAL_FILE} to ${DEST_GROUP_DIR}/${OUTPUT_FILENAME}"
    else
      echo "Warning: No tutorial.md found for feature group ${GROUP_NAME}"
    fi
  done
fi

# Copy the generated JSON file
JSON_FILE="./_parsed/passport-features.json"
if [ -f "${JSON_FILE}" ]; then
  # Create directory for JSON file if it doesn't exist
  JSON_DIR="${DOCS_REPO_DIR}/docs/main/build/unity/usage-guides"
  mkdir -p "${JSON_DIR}"
  
  # Copy JSON file
  cp "${JSON_FILE}" "${JSON_DIR}/passport-features.json"
  echo "Copied ${JSON_FILE} to ${JSON_DIR}/passport-features.json"
else
  echo "Warning: No passport-features.json found at ${JSON_FILE}"
fi

echo "Passport tutorial processing complete." 