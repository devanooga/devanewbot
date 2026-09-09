#!/usr/bin/env bash
set -euo pipefail

: "${SLACK_APP_ID:?Set SLACK_APP_ID to the app id shown on api.slack.com/apps}"
: "${SLACK_CONFIG_TOKEN:?Generate one under \"Your App Configuration Tokens\" on api.slack.com/apps}"

manifest="$(cd "$(dirname "$0")/.." && pwd)/slack-manifest.json"

call() {
    local method="$1"
    shift
    curl -s -X POST "https://slack.com/api/$method" \
        -H "Authorization: Bearer $SLACK_CONFIG_TOKEN" \
        --data-urlencode "app_id=$SLACK_APP_ID" \
        "$@"
}

check() {
    if ! jq -e '.ok' >/dev/null <<<"$1"; then
        jq -r '[.error] + [(.errors // [])[].message] | join("\n")' <<<"$1" >&2
        exit 1
    fi
}

case "${1:-}" in
    export)
        response="$(call apps.manifest.export)"
        check "$response"
        jq '.manifest' <<<"$response" >"$manifest"
        echo "Wrote $manifest"
        ;;
    validate | apply)
        method="apps.manifest.validate"
        [ "$1" = "apply" ] && method="apps.manifest.update"
        response="$(call "$method" --data-urlencode "manifest=$(cat "$manifest")")"
        check "$response"
        echo "$1 ok"
        ;;
    *)
        echo "Usage: $0 export|validate|apply" >&2
        exit 1
        ;;
esac
