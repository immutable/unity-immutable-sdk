#!/bin/bash

# Auth0 Management API Script to Update Google Connection
# This adds the Web Client ID to the allowed mobile client IDs

# IMPORTANT: Get your Management API token from:
# Auth0 Dashboard → Applications → Management API → API Explorer → Get Token

echo "Auth0 Google Connection Update Script"
echo "======================================"
echo ""
echo "This script will add the Web Client ID to Auth0's Google connection"
echo ""

# Configuration
AUTH0_DOMAIN="prod.immutable.auth0app.com"
CONNECTION_ID="google-oauth2"  # or "google" depending on your setup

# Client IDs to add
WEB_CLIENT_ID="410239185541-kgflh9f9g1a0r2vrs7ilto5f8521od77.apps.googleusercontent.com"
ANDROID_CLIENT_ID="410239185541-hkielganvnnvgmd40iep6c630d15bfr4.apps.googleusercontent.com"

echo "Step 1: Get your Management API token"
echo "---------------------------------------"
echo "1. Go to: https://manage.auth0.com/dashboard/us/${AUTH0_DOMAIN}/applications"
echo "2. Click 'Auth0 Management API'"
echo "3. Click 'API Explorer' tab"
echo "4. Click 'Create & Authorize Test Application'"
echo "5. Copy the token that appears"
echo ""
read -p "Paste your Management API token here: " MGMT_TOKEN
echo ""

# Get current connection configuration
echo "Step 2: Fetching current Google connection configuration..."
echo "------------------------------------------------------------"

RESPONSE=$(curl -s -X GET \
  "https://${AUTH0_DOMAIN}/api/v2/connections?name=${CONNECTION_ID}" \
  -H "Authorization: Bearer ${MGMT_TOKEN}" \
  -H "Content-Type: application/json")

echo "Current configuration:"
echo "$RESPONSE" | jq '.'
echo ""

# Extract connection ID
CONNECTION_ID=$(echo "$RESPONSE" | jq -r '.[0].id')
echo "Connection ID: $CONNECTION_ID"
echo ""

# Update with both client IDs
echo "Step 3: Updating connection with both client IDs..."
echo "----------------------------------------------------"

UPDATE_BODY=$(cat <<EOF
{
  "options": {
    "allowed_audiences": [
      "${WEB_CLIENT_ID}",
      "${ANDROID_CLIENT_ID}"
    ],
    "upstream_params": {
      "android_client_id": "${ANDROID_CLIENT_ID}"
    }
  }
}
EOF
)

echo "Sending update request..."
UPDATE_RESPONSE=$(curl -s -X PATCH \
  "https://${AUTH0_DOMAIN}/api/v2/connections/${CONNECTION_ID}" \
  -H "Authorization: Bearer ${MGMT_TOKEN}" \
  -H "Content-Type: application/json" \
  -d "$UPDATE_BODY")

echo "Update response:"
echo "$UPDATE_RESPONSE" | jq '.'
echo ""

echo "Step 4: Verification"
echo "--------------------"
echo "✅ Configuration updated!"
echo ""
echo "Wait 5-10 minutes for changes to propagate, then test your app."
echo ""
echo "Expected client IDs in Auth0:"
echo "  - Web:     ${WEB_CLIENT_ID}"
echo "  - Android: ${ANDROID_CLIENT_ID}"
