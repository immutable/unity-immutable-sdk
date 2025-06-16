#!/bin/bash
DSTDIR="../../src/Packages/Passport/Runtime/ThirdParty/Gree/Assets/Plugins/"
rm -rf DerivedData
xcodebuild -target ImmutableWebView -configuration Release -arch x86_64 -arch arm64 build CONFIGURATION_BUILD_DIR='DerivedData' | xcbeautify
mkdir -p $DSTDIR

cp -r DerivedData/ImmutableWebView.bundle $DSTDIR
rm -rf DerivedData
