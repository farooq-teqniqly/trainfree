#!/usr/bin/env bash
#
# Fails when any OpenSpec change is still active under openspec/changes/. This
# project archives a change in the same PR that ships it (see CLAUDE.md, "Pull
# requests"), so a change that is still active at merge time is an unmet merge
# precondition -- whatever state its tasks are in.
#
# There is deliberately no in-progress exemption. An earlier version skipped a
# change whose tasks.md still had unchecked items, which let add-programs-crud
# merge to main unarchived on the strength of one open manual-verification task
# (issue #12). Task checkboxes are self-reported and easy to leave stale, so
# they are not a trustworthy signal for a merge gate; presence under
# openspec/changes/ is.
#
# Work in progress therefore lives on its branch until the change is archived.
# A change that genuinely needs to span several PRs has to be archived before
# the first one merges, or split into separate changes.
#
set -euo pipefail

changes_dir="openspec/changes"
violations=()

if [[ ! -d "$changes_dir" ]]; then
  echo "No $changes_dir directory; nothing to check."
  exit 0
fi

# find, not a "$changes_dir"/*/ glob: a glob skips dot-prefixed names, so a change
# parked at openspec/changes/.wip/ would slip past a gate that claims to catch any
# change. Sorted for deterministic output; -print0 survives odd directory names.
#
# The scan writes to a temp file rather than feeding the loop from a process
# substitution, so its exit status is actually observed. A failed scan inside
# <(...) would leave violations empty and report success -- a false green on a
# required merge gate, which is the failure this whole check exists to prevent.
scan="$(mktemp)"
trap 'rm -f "$scan"' EXIT

if ! find "$changes_dir" -mindepth 1 -maxdepth 1 -type d ! -name archive -print0 |
  LC_ALL=C sort -z >"$scan"; then
  echo "Failed to scan $changes_dir for active changes; refusing to report success." >&2
  exit 1
fi

while IFS= read -r -d '' change; do
  violations+=("$(basename "$change")")
done <"$scan"

if [[ "${#violations[@]}" -ne 0 ]]; then
  echo "The following OpenSpec change(s) are still active and must be archived:"
  for v in "${violations[@]}"; do
    echo "  - $v"
  done
  echo
  echo "Archive each in this PR before merging:"
  echo "  openspec archive <change-name> -y"
  echo "(moves it under openspec/changes/archive/ and folds its deltas into the main specs)."
  exit 1
fi

echo "OpenSpec archive check passed: no active changes."
