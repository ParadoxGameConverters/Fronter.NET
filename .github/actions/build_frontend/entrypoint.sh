#! /bin/bash

set -eu

if [ "$BUILD_CONVERTERFRONTEND" = true ]
then
  printf "\nBuilding ConverterFrontend...\n"

  cd "${FRONTER_DIR}/Fronter.NET"
  if [ "$RUNNER_OS" = "Windows" ]
  then
    dotnet publish -c Release -p:PublishProfile=win-x64 --output:"${GITHUB_WORKSPACE}/${RELEASE_DIR}" --self-contained $SELF_CONTAINED
  
  elif [ "$RUNNER_OS" = "Linux" ]
  then
    dotnet publish -c Release -p:PublishProfile=linux-x64 --output:"${GITHUB_WORKSPACE}/${RELEASE_DIR}" --self-contained $SELF_CONTAINED
 
  elif [ "$RUNNER_OS" = "macOS" ]
  then
    dotnet publish -c Release -p:PublishProfile=osx-arm64 --output:"${GITHUB_WORKSPACE}/${RELEASE_DIR}" --self-contained $SELF_CONTAINED
    codesign --force -s - "${GITHUB_WORKSPACE}/${RELEASE_DIR}/ConverterFrontend"
    echo "Checking signature..."
    codesign -dv --verbose=4 "${GITHUB_WORKSPACE}/${RELEASE_DIR}/ConverterFrontend"
  fi
  cd "$GITHUB_WORKSPACE"

  printf "\n✔ Successfully built ConverterFrontend.\n"
fi


if [ "$BUILD_UPDATER" = true ]
then
  printf "\nBuilding updater...\n"

  cd "${FRONTER_DIR}/Updater"
  PIP_BREAK_SYSTEM_PACKAGES=1 pip3 install pip-tools
  python3 -m piptools compile -o requirements.txt pyproject.toml

  PIP_BREAK_SYSTEM_PACKAGES=1 pip3 install -r requirements.txt 
  python3 -m PyInstaller --onefile --icon=updater.ico updater.py
  mkdir -p "${GITHUB_WORKSPACE}/${RELEASE_DIR}/Updater"
  mv dist/* "${GITHUB_WORKSPACE}/${RELEASE_DIR}/Updater/"

  printf "\n✔ Successfully built updater.\n"
fi