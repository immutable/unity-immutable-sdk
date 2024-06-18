#!/bin/bash

set -x

USER=$1
TOKEN=$2

echo "$(gh api \
  -H "Accept: application/vnd.github+json" \
  -H "X-GitHub-Api-Version: 2022-11-28" \
  /orgs/immutable/teams)"

echo "$(gh api \
  -H "Accept: application/vnd.github+json" \
  -H "X-GitHub-Api-Version: 2022-11-28" \
  /orgs/immutable/teams/sdk/members)"

response=$(gh api \
  -H "Accept: application/vnd.github+json" \
  -H "X-GitHub-Api-Version: 2022-11-28" \
  /orgs/immutable/teams/sdk/memberships/codeschwert)

echo "$response"

if echo "$response" | grep -q '"state": "active"'; then
  echo "true"
else
  echo "false"
fi
