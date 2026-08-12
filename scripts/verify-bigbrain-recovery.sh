#!/bin/sh
set -eu
base_url=${BIGBRAIN_VERIFY_BASE_URL:-http://127.0.0.1:18080}
printf 'docker_enabled=%s\n' "$(systemctl is-enabled docker.service)"
printf 'docker_active=%s\n' "$(systemctl is-active docker.service)"
printf 'bigbrain_enabled=%s\n' "$(systemctl is-enabled bigbrain.service)"
printf 'bigbrain_active=%s\n' "$(systemctl is-active bigbrain.service)"
docker compose ps --format json | jq -s '{containers:length,unhealthy:[.[]|select(.Health!="" and .Health!="healthy")|.Service]}'
curl --silent --show-error --fail "$base_url/api/v1/system/recovery" | jq '{overall,bootId,previousShutdown,recoveryCompleted,clockSynchronized,availableBytes,lowDisk,interruptedJobs,operatingMode,components}'
curl --silent --show-error --fail "$base_url/api/v1/modules/finance/observation" | jq '{provider:.provider.displayName,observations:.historicalMemory.observationCount,revisions:.retention.coveredRevisionCount,coverageFrom:.historicalMemory.coverageFrom,coverageTo:.historicalMemory.coverageTo,mode:.safety.mode}'
curl --silent --show-error --fail "$base_url/api/v1/modules/finance/robustness" | jq '{evaluations:(.evaluations|length),verdicts:[.evaluations[].verdict]}'
