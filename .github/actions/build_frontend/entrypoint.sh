#! /bin/bash

set -eu

# Self-hosted runners have far more RAM/CPUs than GitHub-hosted ones;
# MSBuild uses all of it by default, which can starve the machine.
if [ "${RUNNER_ENVIRONMENT:-}" = "self-hosted" ]
then
  dotnet_build_args="-m:16"
else
  dotnet_build_args=""
fi

if [ "$BUILD_CONVERTERFRONTEND" = true ]
then
  if ! command -v dotnet >/dev/null 2>&1; then
    for candidate in "$HOME/.dotnet" "/home/runner/.dotnet" "/home/runner2/.dotnet" "/usr/share/dotnet" "/usr/local/share/dotnet" "/opt/dotnet"; do
      if [ -x "$candidate/dotnet" ]; then
        export DOTNET_ROOT="$candidate"
        export PATH="$candidate:$PATH"
        break
      fi
    done
  fi

  if ! command -v dotnet >/dev/null 2>&1; then
    echo "dotnet was not found in PATH or common installation directories." >&2
    exit 1
  fi

  printf "\nBuilding ConverterFrontend...\n"

  cd "${FRONTER_DIR}/Fronter.NET"
  if [ "$RUNNER_OS" = "Windows" ]
  then
    dotnet publish -c Release -p:PublishProfile=win-x64 --output:"${GITHUB_WORKSPACE}/${RELEASE_DIR}" --self-contained $SELF_CONTAINED $dotnet_build_args

  elif [ "$RUNNER_OS" = "Linux" ]
  then
    dotnet publish -c Release -p:PublishProfile=linux-x64 --output:"${GITHUB_WORKSPACE}/${RELEASE_DIR}" --self-contained $SELF_CONTAINED $dotnet_build_args

  elif [ "$RUNNER_OS" = "macOS" ]
  then
    dotnet publish -c Release -p:PublishProfile=osx-arm64 --output:"${GITHUB_WORKSPACE}/${RELEASE_DIR}" --self-contained $SELF_CONTAINED $dotnet_build_args
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
  python3 -m venv venv
  if [ "$RUNNER_OS" == "Windows" ]; then
    source venv/Scripts/activate
  else
    source venv/bin/activate
  fi
  python3 -m pip install --upgrade "pip<25.3" "pip-tools==7.4.0"
  python3 -m piptools compile -o requirements.txt pyproject.toml

  python3 -m pip install -r requirements.txt
  python3 -m PyInstaller --onefile --icon=updater.ico updater.py
  mkdir -p "${GITHUB_WORKSPACE}/${RELEASE_DIR}/Updater"
  mv dist/* "${GITHUB_WORKSPACE}/${RELEASE_DIR}/Updater/"

  printf "\n✔ Successfully built updater.\n"
fi
