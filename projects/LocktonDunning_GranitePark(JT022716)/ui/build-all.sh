#!/bin/bash

# Create a global builds directory if it doesn't exist
mkdir -p ./builds

# Compiles all projects in the html/ folder (excluding _template)
for dir in ./html/*/; do
    dir=${dir%*/}
    foldername=${dir##*/}
    
    if [ "$foldername" == "_template" ]; then
        continue
    fi

    # Create a safe slug for the project name (no spaces/special chars)
    # This helps the ch5-cli avoid issues with long/complex folder names
    SLUG=$(echo "$foldername" | tr '[:upper:]' '[:lower:]' | sed 's/[^a-z0-9]/_/g' | sed 's/__*/_/g')

    echo "------------------------------------------------"
    echo "Building project: $foldername ($SLUG)..."
    echo "------------------------------------------------"
    
    cd "$dir"
    # Clean up any old local artifacts
    rm -rf ./temp
    rm -f ./*.ch5z
    
    # Use npx for better reliability across environments
    # Use the safe SLUG for the project name (-p)
    # Output to the root builds folder
    npx ch5-cli archive -p "$SLUG" -d . -o "../../builds"
    
    # Final cleanup of the tool's temp folder
    rm -rf ./temp
    cd ../..
done

echo "------------------------------------------------"
echo "Build complete. Archives are in the /builds folder."
echo "------------------------------------------------"
