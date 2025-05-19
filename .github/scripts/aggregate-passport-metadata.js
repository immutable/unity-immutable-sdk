#!/usr/bin/env node

const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

// Configuration
const PASSPORT_ROOT = './sample/Assets/Scripts/Passport';
const OUTPUT_DIR = './_parsed';
const OUTPUT_FILE = path.join(OUTPUT_DIR, 'passport-features.json');
const FEATURES_JSON_PATH = path.join(PASSPORT_ROOT, 'features.json');

// Ensure output directory exists
if (!fs.existsSync(OUTPUT_DIR)) {
  fs.mkdirSync(OUTPUT_DIR, { recursive: true });
}

console.log('Processing Passport features metadata...');

// Load features.json to map script files to feature names
let featuresMap = {};
try {
  const featuresContent = fs.readFileSync(FEATURES_JSON_PATH, 'utf8');
  const featuresJson = JSON.parse(featuresContent);
  
  // Create mapping of script filename to feature name
  featuresJson.features.forEach(feature => {
    const [featureName, scriptFile] = Object.entries(feature)[0];
    featuresMap[scriptFile] = featureName;
  });
} catch (error) {
  console.error(`Error reading features.json: ${error.message}`);
  process.exit(1);
}

// Find all metadata.json files
const findMetadataFiles = () => {
  const result = execSync(`find "${PASSPORT_ROOT}" -name "metadata.json" -type f`).toString().trim();
  return result.split('\n').filter(Boolean);
};

// Process metadata files
const processMetadataFiles = (metadataFiles) => {
  const features = [];
  
  metadataFiles.forEach(metadataFile => {
    console.log(`Processing ${metadataFile}`);
    
    // Extract feature directory
    const featureDir = path.dirname(metadataFile);
    
    // Find script file in this directory
    let featureName = '';
    try {
      const dirFiles = fs.readdirSync(featureDir);
      const scriptFiles = dirFiles.filter(file => file.endsWith('.cs'));
      
      if (scriptFiles.length > 0) {
        // Look up the feature name in featuresMap
        const scriptFile = scriptFiles[0];
        featureName = featuresMap[scriptFile] || '';
      }
      
      // If not found in features.json, fallback to directory name
      if (!featureName) {
        console.warn(`Feature for script in ${featureDir} not found in features.json, using directory name`);
        featureName = path.basename(featureDir);
      }
    } catch (error) {
      console.warn(`Error processing directory ${featureDir}: ${error.message}`);
      featureName = path.basename(featureDir);
    }
    
    console.log(`Feature name: ${featureName}`);
    
    // Read and process metadata
    try {
      const metadataContent = fs.readFileSync(metadataFile, 'utf8');
      const metadata = JSON.parse(metadataContent);
      
      // Add feature name to metadata
      metadata.name = featureName;
      features.push(metadata);
    } catch (error) {
      console.error(`Error processing metadata file ${metadataFile}: ${error.message}`);
    }
  });
  
  return features;
};

// Main execution
const metadataFiles = findMetadataFiles();
const features = processMetadataFiles(metadataFiles);

// Create the final passport-features.json
const passportFeatures = { features };
fs.writeFileSync(OUTPUT_FILE, JSON.stringify(passportFeatures, null, 2));

console.log(`Created ${OUTPUT_FILE}`); 