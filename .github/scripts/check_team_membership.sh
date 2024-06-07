#!/bin/bash

set -x

USER=$1
TOKEN=$2

response=$(curl -L -H "Authorization: Bearer $TOKEN" -H "Accept: application/vnd.github+json" -H "X-GitHub-Api-Version: 2022-11-28" "https://api.github.com/orgs/immutable/teams/sdk/memberships/codeschwert")

echo "$response"

if echo "$response" | grep -q '"state": "active"'; then
  echo "true"
else
  echo "false"
fi
