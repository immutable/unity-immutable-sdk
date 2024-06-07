#!/bin/bash

USER=$1
TOKEN=$2

response=$(curl -s -H "Authorization: token $TOKEN" "https://api.github.com/orgs/immutable/teams/sdk/memberships/$USER")

if echo "$response" | grep -q '"state": "active"'; then
  echo "true"
else
  echo "false"
fi
