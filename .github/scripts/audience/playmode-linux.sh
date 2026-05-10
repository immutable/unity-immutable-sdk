#!/bin/bash
# Audience SDK PlayMode test runner for Linux: host-side docker wrapper.
# Runs unityci/editor with the env and volume mounts the inner
# playmode-linux-container.sh expects. Lives outside the container so the
# workflow can launch all 3 desktop platforms from one matrix-shared.json entry.
#
# Manual docker run because game-ci/unity-test-runner@v4 hardcodes
# -nographics. Without a virtual display every PlayMode test comes back
# inconclusive, and the action's USE_EXIT_CODE=false suppresses Unity
# exit 2, so cells went silently green.
#
# Workflow caller: .github/workflows/test-audience-sample-app.yml (playmode job).
# Inputs (env): UNITY_VERSION, UNITY_EMAIL, UNITY_PASSWORD, UNITY_SERIAL,
#               AUDIENCE_TEST_PUBLISHABLE_KEY, AUDIENCE_SCRIPTING_BACKEND.

set -uo pipefail
mkdir -p artifacts

docker run --rm \
  --workdir /github/workspace \
  --env UNITY_EMAIL --env UNITY_PASSWORD --env UNITY_SERIAL \
  --env AUDIENCE_TEST_PUBLISHABLE_KEY --env AUDIENCE_SCRIPTING_BACKEND \
  --env AUDIENCE_TEST_RUN_ID --env AUDIENCE_TEST_CELL_ID --env AUDIENCE_TEST_JOB_ID \
  --volume "$PWD":/github/workspace:z \
  --cpus=8 --memory=30487m \
  "unityci/editor:ubuntu-${UNITY_VERSION}-linux-il2cpp-3" \
  bash /github/workspace/.github/scripts/audience/playmode-linux-container.sh
