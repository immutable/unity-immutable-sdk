#!/bin/bash
# Audience SDK PlayMode test runner for Linux: in-container body.
# Runs inside the unityci/editor:ubuntu-X-linux-il2cpp-3 container.
# Caller: .github/scripts/audience/playmode-linux.sh (host-side docker wrapper).

set -uo pipefail

LOG=/github/workspace/artifacts/unity.log
ACTIVATION_LOG=/tmp/audience-unity-activation.log
RESULTS=/github/workspace/artifacts/test-results.xml
PROJECT=/github/workspace/examples/audience

test_rc=1

activate_license() {
  unity-editor -batchmode -nographics -quit \
    -username "$UNITY_EMAIL" \
    -password "$UNITY_PASSWORD" \
    -serial   "$UNITY_SERIAL" \
    -logFile - 2>&1 | tee "$ACTIVATION_LOG" || true

  if grep -qE "License activation has failed|\[Licensing::Client\] Error: Code [0-9]+" "$ACTIVATION_LOG"; then
    echo "::error::Unity license activation failed."
    exit 1
  fi
  if ! grep -qE "Successfully activated the entitlement license" "$ACTIVATION_LOG"; then
    echo "::error::Unity license activation: no success marker in log."
    exit 1
  fi
}

run_tests_with_watchdog() {
  # xvfb-run gives Unity a virtual X display. UI Toolkit needs GLX + render;
  # llvmpipe in the image provides software OpenGL so no GPU is needed.
  # -force-glcore skips the Unity 6 Vulkan init and matches the Unity 2021.3 default path.
  xvfb-run -a --server-args="-ac +extension GLX +render -noreset" -- \
    unity-editor \
      -batchmode \
      -force-glcore \
      -screen-fullscreen 0 \
      -screen-width 320 \
      -screen-height 240 \
      -projectPath "$PROJECT" \
      -runTests \
      -testPlatform StandaloneLinux64 \
      -testResults "$RESULTS" \
      -logFile "$LOG" &
  local unity_pid=$!

  # Mirror Unity log to job stdout while the editor is alive.
  tail --pid=$unity_pid -F "$LOG" 2>/dev/null &

  # Watchdog (vs fixed timeout) because per-version run length varies wildly:
  # Unity 2021.3 cells finish in ~2 min, Unity 6 in ~22 min, and Unity 6 has a
  # known post-test shutdown hang. SIGTERM 30 s after "Test run completed" so
  # each cell exits as soon as its suite finishes. 40 min hard cap as fallback.
  local deadline=$((SECONDS + 2400))
  local flush_deadline=0
  local kill_reason=""
  while kill -0 "$unity_pid" 2>/dev/null; do
    if [ "$SECONDS" -ge "$deadline" ]; then
      kill_reason="hard-cap-40m"
      break
    fi
    if [ "$flush_deadline" -eq 0 ] && grep -q "Test run completed" "$LOG" 2>/dev/null; then
      flush_deadline=$((SECONDS + 30))
      echo "[watchdog] saw \"Test run completed\" at ${SECONDS}s; SIGTERM after 30s flush window"
    fi
    if [ "$flush_deadline" -gt 0 ] && [ "$SECONDS" -ge "$flush_deadline" ]; then
      kill_reason="flush-window-elapsed"
      break
    fi
    sleep 5
  done

  if [ -n "$kill_reason" ]; then
    echo "[watchdog] sending SIGTERM to Unity (reason: $kill_reason)"
    kill -TERM "$unity_pid" 2>/dev/null || true
    # 15 s grace, then SIGKILL.
    for _ in 1 2 3; do
      kill -0 "$unity_pid" 2>/dev/null || break
      sleep 5
    done
    if kill -0 "$unity_pid" 2>/dev/null; then
      echo "[watchdog] SIGTERM not honored, sending SIGKILL"
      kill -KILL "$unity_pid" 2>/dev/null || true
    fi
  fi

  wait "$unity_pid" 2>/dev/null
  test_rc=$?
  if [ "$kill_reason" = "hard-cap-40m" ]; then
    echo "::warning::Unity hit the 40 min hard cap without logging \"Test run completed\". Inspect Player.log."
  fi
}

capture_player_log() {
  # Player runs in a separate process from the editor; copy its Player.log so
  # HTTP traces and OnError fires are captured. Glob across companies / products.
  find /root/.config/unity3d -name "Player.log" 2>/dev/null | while IFS= read -r f; do
    co=$(basename "$(dirname "$(dirname "$f")")")
    pr=$(basename "$(dirname "$f")")
    cp "$f" "/github/workspace/artifacts/Player-${co}-${pr}.log" 2>/dev/null || true
  done
}

return_license() {
  # Always return the seat to keep the activation pool from exhausting on reruns.
  unity-editor -batchmode -nographics -quit -returnlicense -logFile - 2>&1 || true
}

activate_license
run_tests_with_watchdog
capture_player_log
return_license

# Unity exits 2 on test failure or inconclusive; propagate so the step fails.
exit "$test_rc"
