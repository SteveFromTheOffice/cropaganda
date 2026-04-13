# Cropaganda

## What is this?

Batch-cropping your vacation pics for Instagram? This is the tool you build when you’re too lazy to do it in Photoshop 200 times. Cropaganda: ruthlessly crops your photos to 4:5, no questions asked.

## How to build

```bash
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true src/Cropaganda/Cropaganda.csproj
```

## How to install / run

No installer. No registry. No drama. Just grab the .exe from `src/Cropaganda/bin/Release/net8.0/win-x64/publish/`, put it wherever you want, and double-click. That’s it.

## How to use

- Drag and drop a bunch of photos onto the window
- Mouse wheel to zoom in/out
- Click and drag to pan the image behind the fixed 4:5 crop box
- Hit Enter to save the crop and jump to the next image

## Output

Cropped JPEGs (quality 95) land in a `cropped\` subfolder next to your originals. Originals are untouched. Your secret is safe.
