#!/usr/bin/env node

'use strict';

const fs = require('fs');
const path = require('path');

// Configuration
const PASSPORT_ROOT = './sample/Assets/Scripts/Passport';
const TUTORIALS_DIR = path.join(PASSPORT_ROOT, '_tutorials~');
const OUTPUT_DIR = './_parsed';
const OUTPUT_FILE = path.join(OUTPUT_DIR, 'passport-features.json');
const FEATURES_JSON_PATH = path.join(PASSPORT_ROOT, 'features.json');

// Ensure output directory exists
try {
  if (!fs.existsSync(OUTPUT_DIR)) {
    fs.mkdirSync(OUTPUT_DIR, { recursive: true });
  }
} catch (error) {
  console.error(`Error creating output directory: ${error.message}`);
  process.exit(1);
}

console.log('Processing Passport features metadata...');

// Load features.json to get feature groups and their features
let featureGroups = {};
try {
  const featuresContent = fs.readFileSync(FEATURES_JSON_PATH, 'utf8');
  const featuresJson = JSON.parse(featuresContent);
  featureGroups = featuresJson.features || {};
} catch (error) {
  console.error(`Error reading features.json: ${error.message}`);
  process.exit(1);
}

// Find all feature group directories in _tutorials
const findFeatureGroupDirectories = () => {
  const featureGroupDirs = [];
  
  if (!fs.existsSync(TUTORIALS_DIR)) {
    console.warn(`Tutorials directory does not exist: ${TUTORIALS_DIR}`);
    return featureGroupDirs;
    }
    
    try {
    const dirs = fs.readdirSync(TUTORIALS_DIR, { withFileTypes: true });
    
    dirs.forEach((dirent) => {
      if (dirent.isDirectory()) {
        featureGroupDirs.push(path.join(TUTORIALS_DIR, dirent.name));
        }
      });
    } catch (err) {
    console.warn(`Error reading tutorials directory ${TUTORIALS_DIR}: ${err.message}`);
    }
  
  return featureGroupDirs;
};

// Process metadata files
const processFeatureGroups = (featureGroupDirs) => {
  const featuresObject = {};
  
  featureGroupDirs.forEach((groupDir) => {
    const groupName = path.basename(groupDir);
    console.log(`Processing feature group: ${groupName}`);
    
    // Check if this group exists in features.json (case-insensitive)
    const matchingGroup = Object.keys(featureGroups).find(
      key => key.toLowerCase() === groupName.toLowerCase()
    );
    
    if (!matchingGroup) {
      console.warn(`Feature group ${groupName} not found in features.json, skipping`);
      return;
    }
    
    // Path to metadata.json in this feature group directory
    const metadataPath = path.join(groupDir, 'metadata.json');
    if (!fs.existsSync(metadataPath)) {
      console.warn(`No metadata.json found for feature group ${groupName} in ${groupDir}`);
      return;
      }
    
    // Path to tutorial.md in this feature group directory
    const tutorialPath = path.join(groupDir, 'tutorial.md');
    const tutorialExists = fs.existsSync(tutorialPath);
    
    // Use the folder name directly as the feature key
    const featureKey = groupName;
    
    if (!featureKey) {
      console.warn(`Generated empty feature key for ${groupDir}, skipping`);
      return;
    }
    
    const tutorialFile = tutorialExists ? `${featureKey}.md` : null;
    
    if (!tutorialExists) {
      console.warn(`No tutorial.md found for feature group ${groupName} in ${groupDir}`);
    }
    
    // Read and process metadata
    try {
      const metadataContent = fs.readFileSync(metadataPath, 'utf8');
      const metadata = JSON.parse(metadataContent);
      
      // Add additional fields
      metadata.title = metadata.title || matchingGroup;
      metadata.sidebar_order = metadata.sidebar_order || 0;
      metadata.deprecated = metadata.deprecated || false;
      
      // Add feature group information
      metadata.feature_group = matchingGroup;
      metadata.features = Object.keys(featureGroups[matchingGroup] || {});
      
      // Create the feature entry
      featuresObject[featureKey] = {
        tutorial: tutorialFile,
        metadata: metadata
      };
    } catch (error) {
      console.error(`Error processing metadata file ${metadataPath}: ${error.message}`);
    }
  });
  
  return featuresObject;
};

try {
  // Main execution
  const featureGroupDirs = findFeatureGroupDirectories();
  
  if (featureGroupDirs.length === 0) {
    console.warn('No feature group directories found. Output file will be empty.');
  }
  
  const features = processFeatureGroups(featureGroupDirs);

  // Create the final passport-features.json
  fs.writeFileSync(OUTPUT_FILE, JSON.stringify(features, null, 2));
  console.log(`Created ${OUTPUT_FILE}`);
} catch (error) {
  console.error(`Fatal error: ${error.message}`);
  process.exit(1);
}