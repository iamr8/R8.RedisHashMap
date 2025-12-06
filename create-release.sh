#!/bin/bash

# R8.RedisHashMap Release Script
# This script helps you create a new release

set -e

# Check if version argument is provided
if [ -z "$1" ]; then
    echo "Usage: ./create-release.sh <version>"
    echo "Example: ./create-release.sh 1.0.0"
    exit 1
fi

VERSION=$1

# Validate version format (basic check)
if ! [[ $VERSION =~ ^[0-9]+\.[0-9]+\.[0-9]+(-[a-zA-Z0-9]+)?$ ]]; then
    echo "Error: Invalid version format. Expected format: X.Y.Z or X.Y.Z-suffix"
    echo "Examples: 1.0.0, 2.1.3, 1.0.0-beta"
    exit 1
fi

echo "Creating release v$VERSION..."

# Ensure we're on the main branch and up to date
echo "Checking git status..."
if [ -n "$(git status --porcelain)" ]; then
    echo "Warning: You have uncommitted changes. Please commit or stash them first."
    read -p "Do you want to continue anyway? (y/N) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi

# Create and push the tag
echo "Creating tag v$VERSION..."
git tag -a "v$VERSION" -m "Release version $VERSION"

echo "Pushing tag to origin..."
git push origin "v$VERSION"

echo ""
echo "✅ Release tag v$VERSION has been created and pushed!"
echo ""
echo "Next steps:"
echo "1. Go to https://github.com/iamr8/R8.RedisHashMap/actions"
echo "2. Watch the 'Release NuGet Package' workflow"
echo "3. Once complete, the release will be available at:"
echo "   https://github.com/iamr8/R8.RedisHashMap/releases/tag/v$VERSION"
echo ""
echo "If you need to delete this tag:"
echo "  git tag -d v$VERSION"
echo "  git push origin :refs/tags/v$VERSION"

