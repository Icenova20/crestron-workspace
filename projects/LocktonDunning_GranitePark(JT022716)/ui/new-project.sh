#!/bin/bash

# Check if project name is provided
if [ -z "$1" ]; then
    echo "Usage: ./new-project.sh \"Official Project Name (ID)\""
    exit 1
fi

PROJECT_NAME="$1"
HTML_DIR="./html"
TEMPLATE_DIR="$HTML_DIR/_template"
TARGET_DIR="$HTML_DIR/$PROJECT_NAME"

# 1. Create directory from template
if [ -d "$TARGET_DIR" ]; then
    echo "Error: Project already exists at $TARGET_DIR"
    exit 1
fi

echo "Cloning Template into: $PROJECT_NAME..."
mkdir -p "$TARGET_DIR"
cp -r "$TEMPLATE_DIR/." "$TARGET_DIR/"

# 2. Clean up build artifacts
rm -f "$TARGET_DIR"/*.ch5z

# 3. Update internal configurations
# Create a slug for the .ch5z filename
SLUG=$(echo "$PROJECT_NAME" | tr '[:upper:]' '[:lower:]' | sed 's/[^a-z0-9]/_/g' | sed 's/__*/_/g')

sed -i "s/lockton_dunning/$SLUG/g" "$TARGET_DIR/package.json"
sed -i "s/lockton_dunning_html/$SLUG/g" "$TARGET_DIR/project-config.json"
sed -i "s/lockton_dunning.ch5z/$SLUG.ch5z/g" "$TARGET_DIR/project-config.json"

echo "------------------------------------------------"
echo "Success! Project created."
echo "Directory: $TARGET_DIR"
echo "URL: http://localhost:8001/$(echo $PROJECT_NAME | sed 's/ /%20/g')/"
echo "------------------------------------------------"
