#!/usr/bin/env node

const fs = require('fs');
const path = require('path');

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
    // Store both the full filename and just the filename without path
    featuresMap[scriptFile] = featureName;
    featuresMap[path.basename(scriptFile)] = featureName;
  });
} catch (error) {
  console.error(`Error reading features.json: ${error.message}`);
  process.exit(1);
}

// Platform-independent recursive file search
const findMetadataFiles = () => {
  const metadataFiles = [];
  
  const walkDir = (dir) => {
    if (!fs.existsSync(dir)) {
      console.warn(`Directory does not exist: ${dir}`);
      return;
    }
    
    try {
      const files = fs.readdirSync(dir);
      
      files.forEach(file => {
        const filePath = path.join(dir, file);
        
        try {
          const stat = fs.statSync(filePath);
          
          if (stat.isDirectory()) {
            walkDir(filePath);
          } else if (file === 'metadata.json') {
            metadataFiles.push(filePath);
          }
        } catch (err) {
          console.warn(`Error accessing file ${filePath}: ${err.message}`);
        }
      });
    } catch (err) {
      console.warn(`Error reading directory ${dir}: ${err.message}`);
    }
  };
  
  walkDir(PASSPORT_ROOT);
  return metadataFiles;
};

// Process metadata files
const processMetadataFiles = (metadataFiles) => {
  const featuresObject = {};
  
  metadataFiles.forEach(metadataFile => {
    console.log(`Processing ${metadataFile}`);
    
    // Extract feature directory
    const featureDir = path.dirname(metadataFile);
    
    // Get directory name as fallback feature name
    const dirName = path.basename(featureDir);
    
    // Try to find feature name in feature map, fallback to directory name
    let featureName = '';
    try {
      // Look for any script file in this directory
      const dirFiles = fs.readdirSync(featureDir);
      const scriptFiles = dirFiles.filter(file => file.endsWith('.cs'));
      
      // Try to match any script file to our feature map
      let found = false;
      for (const scriptFile of scriptFiles) {
        if (featuresMap[scriptFile]) {
          featureName = featuresMap[scriptFile];
          found = true;
          break;
        }
      }
      
      if (!found) {
        console.warn(`No feature found in features.json for ${featureDir}, using directory name`);
        featureName = dirName;
      }
    } catch (error) {
      console.warn(`Error processing directory ${featureDir}: ${error.message}`);
      featureName = dirName;
    }
    
    // Create feature key (kebab-case)
    const featureKey = featureName
      .replace(/([a-z])([A-Z])/g, '$1-$2')
      .replace(/\s+/g, '-')
      .replace(/[^a-z0-9-]/gi, '')
      .toLowerCase();
    
    if (!featureKey) {
      console.warn(`Generated empty feature key for ${featureDir}, skipping`);
      return;
    }
    
    // Check for tutorial.md in the same directory
    const tutorialPath = path.join(featureDir, 'tutorial.md');
    const tutorialExists = fs.existsSync(tutorialPath);
    const tutorialFile = tutorialExists ? `${featureKey}.md` : null;
    
    if (!tutorialExists) {
      console.warn(`No tutorial.md found for feature ${featureName} in ${featureDir}`);
    }
    
    // Read and process metadata
    try {
      const metadataContent = fs.readFileSync(metadataFile, 'utf8');
      const metadata = JSON.parse(metadataContent);
      
      // Add additional fields
      metadata.title = metadata.title || featureName;
      metadata.sidebar_order = metadata.sidebar_order || 0;
      metadata.deprecated = metadata.deprecated || false;
      
      // Create the feature entry
      featuresObject[featureKey] = {
        tutorial: tutorialFile,
        metadata: metadata
      };
    } catch (error) {
      console.error(`Error processing metadata file ${metadataFile}: ${error.message}`);
    }
  });
  
  return featuresObject;
};

// Main execution
const metadataFiles = findMetadataFiles();
const features = processMetadataFiles(metadataFiles);

// Create the final passport-features.json
fs.writeFileSync(OUTPUT_FILE, JSON.stringify(features, null, 2));

console.log(`Created ${OUTPUT_FILE}`);