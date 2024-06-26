#!/bin/bash

if [ -d emsdk ]; then
  echo "Directory exists."
else
  echo "Cloning emsdk project"

  git clone https://github.com/emscripten-core/emsdk.git

  ./emsdk/emsdk install 1.39.5

  ./emsdk/emsdk activate 1.39.5
fi

# Activate PATH and other environment variables in the current terminal
source ./emsdk/emsdk_env.sh

rm -rf build/

mkdir build

cd build

emcmake cmake .. -DCMAKE_BUILD_TYPE=Release -DSDL2MIXER_CMD=OFF

emmake make -j 16

cd ..

# run this script with bash WebGLBuild.sh
# run the line below in dist/Emscription directory
# python3 -m http.server 8080
# webbrowser http://localhost:8080/CanisTemplate.html