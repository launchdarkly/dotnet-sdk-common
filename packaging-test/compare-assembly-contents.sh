#!/bin/bash

# Run in CI to verify that both LaunchDarkly.CommonSdk and LaunchDarkly.CommonSdk.StrongName can be
# built for a single target framework, and that they contain the same types.

set -eu

FRAMEWORK=$1

COMMONSDK_DLL="$(pwd)/src/LaunchDarkly.CommonSdk/bin/Debug/${FRAMEWORK}/LaunchDarkly.CommonSdk.dll"
COMMONSDK_STRONGNAME_DLL="$(pwd)/src/LaunchDarkly.CommonSdk.StrongName/bin/Debug/${FRAMEWORK}/LaunchDarkly.CommonSdk.StrongName.dll"

if [ ! -f "${COMMONSDK_DLL}" ]; then
  dotnet build src/LaunchDarkly.CommonSdk -f "${FRAMEWORK}"
fi
if [ ! -f "${COMMONSDK_STRONGNAME_DLL}" ]; then
  dotnet build src/LaunchDarkly.CommonSdk.StrongName -f "${FRAMEWORK}"
fi

TEST_PROJ=packaging-test/DumpDllTypes.csproj
TEST_APP=packaging-test/bin/Debug/netcoreapp2.0/DumpDllTypes.dll

dotnet build "${TEST_PROJ}" -f netcoreapp2.0

TEMP_DIR=$(mktemp -d -t dotnet-sdk-packaging-test.XXXXXX)
trap "rm -rf ${TEMP_DIR}" EXIT

TYPES_LIST_1="${TEMP_DIR}/LaunchDarkly.CommonSdk_types"
TYPES_LIST_2="${TEMP_DIR}/LaunchDarkly.CommonSdk.StrongName_types"

dotnet "${TEST_APP}" $COMMONSDK_DLL > "${TYPES_LIST_1}"
dotnet "${TEST_APP}" $COMMONSDK_STRONGNAME_DLL > "${TYPES_LIST_2}"

diff "${TYPES_LIST_1}" "${TYPES_LIST_2}" || ( \
  echo "";
  echo "LaunchDarkly.CommonSdk and LaunchDarkly.CommonSdk.StrongName do not contain the same types!";
  exit 1;
)
