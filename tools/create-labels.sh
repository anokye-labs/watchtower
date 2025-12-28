#!/usr/bin/env bash
# Script to create semantic labels for agent signals in the WatchTower repository
# Part of anokye-labs/watchtower#69 - Nsumankwahene Flow System
#
# Prerequisites:
# - gh CLI installed and authenticated
# - Proper permissions to manage labels in the repository
#
# Usage:
#   ./tools/create-labels.sh

set -euo pipefail

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "Creating semantic labels for agent signals..."
echo ""

# Function to create a label
create_label() {
    local name="$1"
    local description="$2"
    local color="$3"
    
    echo -e "${BLUE}Creating label:${NC} ${name}"
    if gh label create "$name" --description "$description" --color "$color" 2>/dev/null; then
        echo -e "${GREEN}✓${NC} Label '${name}' created successfully"
    else
        # Check if it failed because the label already exists
        if gh label list --json name --jq '.[].name' | grep -q "^${name}$"; then
            echo -e "${YELLOW}⚠${NC} Label '${name}' already exists"
        else
            echo -e "${RED}✗${NC} Failed to create label '${name}'"
            return 1
        fi
    fi
    echo ""
}

# Create agent signal labels
echo "=== Agent Signal Labels ==="
echo ""

create_label "ɔkyeame:siesie" \
    "Agent ready - prepared for autonomous work" \
    "0E8A16"

create_label "ɔkyeame:dwuma" \
    "Agent in progress - actively being worked" \
    "1D76DB"

create_label "nnipa-gyinae-hia" \
    "Requires human decision - agent cannot proceed" \
    "D93F0B"

create_label "stale" \
    "No activity >5 days" \
    "FBCA04"

create_label "needs-fix" \
    "PR checks failed" \
    "E4E669"

create_label "asiw:external" \
    "Blocked by third-party dependency" \
    "B60205"

create_label "nsakrae:api" \
    "Breaking API change - major version bump" \
    "D93F0B"

create_label "nhwɛsoɔ-hia" \
    "Requires testing - Complexity ≥5" \
    "5319E7"

echo "=== Summary ==="
echo -e "${GREEN}✓${NC} Label creation process completed"
echo ""
echo "To view all labels, run:"
echo "  gh label list"
