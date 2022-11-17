#! /bin/bash

set -eu

if [ "$BUILD_CONVERTERFRONTEND" = true ]
then
  printf "\nBuilding ConverterFrontend...\n"

  cd "${FRONTER_DIR}/Fronter.NET"
  if [ "$RUNNER_OS" = "Windows" ]
  then
    dotnet publish -p:PublishProfile=win-x64 --output:"${GITHUB_WORKSPACE}/${RELEASE_DIR}"
  
  elif [ "$RUNNER_OS" = "Linux" ]
  then
    dotnet publish -p:PublishProfile=linux-x64 --output:"${GITHUB_WORKSPACE}/${RELEASE_DIR}"
 
  elif [ "$RUNNER_OS" = "macOS" ]
  then
    dotnet publish -p:PublishProfile=osx-x64 --output:"${GITHUB_WORKSPACE}/${RELEASE_DIR}"
  
  fi
  cd "$GITHUB_WORKSPACE"

  printf "\n✔ Successfully built ConverterFrontend.\n"
fi


if [ "$BUILD_UPDATER" = true ]
then
  printf "\nBuilding updater...\n"

  cd "${FRONTER_DIR}/Updater"
  pip3 install -r requirements.txt
  python3 -m PyInstaller --icon=updater.ico updater.py
  mkdir -p "${GITHUB_WORKSPACE}/${RELEASE_DIR}/Updater"
  mv dist/updater/* "${GITHUB_WORKSPACE}/${RELEASE_DIR}/Updater/"

  printf "\n✔ Successfully built updater.\n"
fi