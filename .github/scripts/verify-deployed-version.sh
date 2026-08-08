#!/usr/bin/env bash
#
# Asserts that the deployed Worker reports the build stamp it was deployed with.
#
# The stamp the app header shows comes from GET /api/version -- the Blazor client renders
# its own compiled-in stamp and compares it against this response, so a Worker deployed
# with the wrong (or no) APP_VERSION/APP_COMMIT var makes the stale-version banner fire for
# every visitor on every load. Before this check that was only discoverable by opening the
# site (see issue #18).
#
# The whole origin sits behind Cloudflare Access, so this needs an Access *service token*
# -- a Client ID/Secret pair from Zero Trust > Access > Service Auth, with a Service Auth
# policy on the application admitting it. That is a different credential from
# CLOUDFLARE_API_TOKEN, which only speaks to the management API and is rejected by Access.
#
# Environment:
#   BASE_URL                 deployed origin, e.g. https://trainfree.example.workers.dev
#   CF_ACCESS_CLIENT_ID      whole header line: "CF-Access-Client-Id: <value>"
#   CF_ACCESS_CLIENT_SECRET  whole header line: "CF-Access-Client-Secret: <value>"
#   EXPECTED_VERSION         git tag, e.g. v0.0.9
#   EXPECTED_COMMIT          short SHA, e.g. abc1234
#
# Both secrets carry their own header name so curl passes them through verbatim -- do not
# prepend a header name at the call site. Runnable outside CI: set the five variables and
# run it. The ::error:: prefixes are GitHub annotations and are harmless elsewhere.

set -euo pipefail

readonly ATTEMPTS=6
readonly RETRY_SECONDS=10
# curl caps neither total transfer time nor (usefully) connect time by default -- its
# stock connect timeout is 300s and there is no --max-time at all, so one hung connection
# would stall the step until the job's own limit hours later. These bound the whole check
# at roughly ATTEMPTS * (MAX_SECONDS + RETRY_SECONDS), about four minutes. /api/version is
# a fixed-size JSON response off a no-store route, so a slow one is a failure, not a big
# download being cut off.
readonly CONNECT_TIMEOUT_SECONDS=10
readonly MAX_SECONDS=30

if [ -z "${BASE_URL:-}" ]; then
  echo "::error::No URL to verify. Set the APP_BASE_URL repository variable to the deployed origin (e.g. https://trainfree.example.com)."
  exit 1
fi
if [ -z "${CF_ACCESS_CLIENT_ID:-}" ] || [ -z "${CF_ACCESS_CLIENT_SECRET:-}" ]; then
  echo "::error::CF_ACCESS_CLIENT_ID / CF_ACCESS_CLIENT_SECRET are not set. Access returns its login page to unauthenticated callers, so the version check cannot run without a service token."
  exit 1
fi
if [ -z "${EXPECTED_VERSION:-}" ] || [ -z "${EXPECTED_COMMIT:-}" ]; then
  echo "::error::EXPECTED_VERSION / EXPECTED_COMMIT are not set, so there is nothing to compare the deployed stamp against."
  exit 1
fi

url="${BASE_URL%/}/api/version"
expected="${EXPECTED_VERSION}+${EXPECTED_COMMIT}"

# A fresh deploy takes a few seconds to reach every colo, so poll rather than failing the
# first time the previous stamp answers.
for attempt in $(seq 1 "$ATTEMPTS"); do
  # Status and body together, and deliberately no --location: an unauthenticated caller
  # gets a 302 to <team>.cloudflareaccess.com, and following it would both hide the
  # rejection behind a 200 login page and resend the service token to another host.
  response=$(curl --silent --show-error --write-out '\n%{http_code}' \
    --connect-timeout "$CONNECT_TIMEOUT_SECONDS" \
    --max-time "$MAX_SECONDS" \
    --header "${CF_ACCESS_CLIENT_ID}" \
    --header "${CF_ACCESS_CLIENT_SECRET}" \
    "$url") || response=""
  status="${response##*$'\n'}"
  body="${response%$'\n'*}"

  case "$status" in
    30*)
      echo "::error::$url redirected to the Access login page (HTTP $status). The service token was not accepted -- check that the application has a Service Auth policy including this token."
      exit 1
      ;;
    403)
      echo "::error::$url returned 403. The service token authenticated but is not admitted by any policy on the application."
      exit 1
      ;;
  esac

  if [ "$status" = "200" ] && [ -n "$body" ]; then
    # Anything non-JSON at 200 is the app misbehaving rather than Access, which the cases
    # above have already ruled out -- quote it so the log says what.
    if ! printf '%s' "$body" | jq -e 'type == "object" and has("version")' >/dev/null 2>&1; then
      echo "::error::$url returned 200 but not version JSON. Response began: $(printf '%s' "$body" | head -c 200)"
      exit 1
    fi

    actual="$(printf '%s' "$body" | jq -r '.version')+$(printf '%s' "$body" | jq -r '.commit')"
    if [ "$actual" = "$expected" ]; then
      echo "Deployed version verified: $actual"
      exit 0
    fi
    echo "Attempt $attempt: got $actual, want $expected"
  else
    echo "Attempt $attempt: no usable response from $url (HTTP ${status:-none})"
  fi

  sleep "$RETRY_SECONDS"
done

echo "::error::$url never reported $expected. The app header will show a stale-version banner on every load."
exit 1
