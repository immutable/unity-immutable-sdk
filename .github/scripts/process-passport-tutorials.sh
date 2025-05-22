#!/bin/bash

set -e
set -x

# Directory where docs repo is cloned
DOCS_REPO_DIR="${CLONE_DIR:-"./imx-docs"}"

# Root of the Passport features
PASSPORT_ROOT="./sample/Assets/Scripts/Passport"
TUTORIALS_DIR="${PASSPORT_ROOT}/_tutorials"

echo "Processing Passport tutorials..."

# Load features.json to get feature groups
FEATURES_JSON="${PASSPORT_ROOT}/features.json"
if [ ! -f "${FEATURES_JSON}" ]; then
  echo "Error: features.json not found at ${FEATURES_JSON}"
  exit 1
fi

# Create _tutorials directory in docs repo
DOCS_TUTORIALS_DIR="${DOCS_REPO_DIR}/docs/main/example/zkEVM/unity/passport-examples/_tutorials"
mkdir -p "${DOCS_TUTORIALS_DIR}"

# Check if _tutorials directory exists
if [ ! -d "${TUTORIALS_DIR}" ]; then
  echo "Warning: _tutorials directory not found at ${TUTORIALS_DIR}"
else
  # Process each feature group directory in _tutorials
  find "${TUTORIALS_DIR}" -mindepth 1 -maxdepth 1 -type d -print0 | while IFS= read -r -d '' GROUP_DIR; do
    echo "Processing feature group: ${GROUP_DIR}"
    
    # Extract feature group name from directory
    GROUP_NAME=$(basename "${GROUP_DIR}")
    
    # Tutorial file path
    TUTORIAL_FILE="${GROUP_DIR}/tutorial.md"
    
    if [ -f "${TUTORIAL_FILE}" ]; then
      echo "Found tutorial for ${GROUP_NAME}"
      
      # Convert feature group name to kebab-case for the destination filename
      KEBAB_NAME=$(echo "${GROUP_NAME}" | sed -E 's/([a-z])([A-Z])/\1-\2/g' | tr '[:upper:]' '[:lower:]')
      
      # Copy the tutorial file
      cp "${TUTORIAL_FILE}" "${DOCS_TUTORIALS_DIR}/${KEBAB_NAME}.md"
      echo "Copied ${TUTORIAL_FILE} to ${DOCS_TUTORIALS_DIR}/${KEBAB_NAME}.md"
    else
      echo "Warning: No tutorial.md found for feature group ${GROUP_NAME}"
    fi
  done
fi

# Copy the generated JSON file
JSON_FILE="./_parsed/passport-features.json"
if [ -f "${JSON_FILE}" ]; then
  # Create directory for JSON file if it doesn't exist
  JSON_DIR="${DOCS_REPO_DIR}/docs/main/example/zkEVM/unity/passport-examples"
  mkdir -p "${JSON_DIR}"
  
  # Copy JSON file
  cp "${JSON_FILE}" "${JSON_DIR}/passport-features.json"
  echo "Copied ${JSON_FILE} to ${JSON_DIR}/passport-features.json"
else
  echo "Warning: No passport-features.json found at ${JSON_FILE}"
fi

echo "Passport tutorial processing complete." 