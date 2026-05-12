#!/bin/bash

set -x

USER=$1

TEAMS=(
  "ped-stream-sdk-integrations-list"
  "ped-stream-blockchain-services-list"
)

IS_MEMBER=false

for TEAM in "${TEAMS[@]}"; do
  response=$(gh api \
    -H "Accept: application/vnd.github+json" \
    -H "X-GitHub-Api-Version: 2022-11-28" \
    "/orgs/immutable/teams/${TEAM}/memberships/${USER}")

  echo "$response"

  if echo "$response" | grep -q '"state":"active"'; then
    IS_MEMBER=true
    break
  fi
done

echo "$IS_MEMBER"

# Set the environment variable for the GitHub workflow
echo "IS_MEMBER=$IS_MEMBER" >> "$GITHUB_ENV"
