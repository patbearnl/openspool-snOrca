set(_targets
  "build/cmake/modules/cotire_test/CMakeLists.txt"
  "build/cmake/modules/cotire.cmake"
)

set(_any FALSE)
foreach(_target IN LISTS _targets)
  if(NOT EXISTS "${_target}")
    message(STATUS "wxWidgets patch: '${_target}' not found; skipping")
    continue()
  endif()

  file(READ "${_target}" _contents)

  set(_patched "${_contents}")
  string(REPLACE "cmake_minimum_required(VERSION 2.8.12)" "cmake_minimum_required(VERSION 3.5)" _patched "${_patched}")
  string(REPLACE "cmake_minimum_required(VERSION 2.8)" "cmake_minimum_required(VERSION 3.5)" _patched "${_patched}")
  string(REPLACE "cmake_minimum_required(VERSION 3.0)" "cmake_minimum_required(VERSION 3.5)" _patched "${_patched}")
  string(REPLACE "cmake_minimum_required(VERSION 3.1)" "cmake_minimum_required(VERSION 3.5)" _patched "${_patched}")
  string(REPLACE "cmake_minimum_required(VERSION 3.2)" "cmake_minimum_required(VERSION 3.5)" _patched "${_patched}")
  string(REPLACE "cmake_minimum_required(VERSION 3.3)" "cmake_minimum_required(VERSION 3.5)" _patched "${_patched}")
  string(REPLACE "cmake_minimum_required(VERSION 3.4)" "cmake_minimum_required(VERSION 3.5)" _patched "${_patched}")

  if(_patched STREQUAL _contents)
    message(STATUS "wxWidgets patch: no changes needed in '${_target}'")
    continue()
  endif()

  file(WRITE "${_target}" "${_patched}")
  message(STATUS "wxWidgets patch: updated cmake_minimum_required in '${_target}'")
  set(_any TRUE)
endforeach()

if(NOT _any)
  message(STATUS "wxWidgets patch: nothing to do")
endif()

# Ensure WebView2 SDK is available without relying on CMake's libarchive extraction.
# wxWidgets' MSW webview build expects `3rdparty/webview2/build/native/include/WebView2.h`.
set(_webview2_header "3rdparty/webview2/build/native/include/WebView2.h")
set(_webview2_version "1.0.1418.22")
set(_webview2_url "https://www.nuget.org/api/v2/package/Microsoft.Web.WebView2/${_webview2_version}")
set(_webview2_sha256 "51d2ef56196e2a9d768a6843385bcb9c6baf9ed34b2603ddb074fb4995543a99")

# Also populate the build-tree package directory because wxWidgets caches WEBVIEW2_PACKAGE_DIR there.
get_filename_component(_wx_build_dir "../dep_wxWidgets-build" ABSOLUTE)
set(_webview2_build_pkg_dir "${_wx_build_dir}/packages/Microsoft.Web.WebView2.${_webview2_version}")
set(_webview2_build_header "${_webview2_build_pkg_dir}/build/native/include/WebView2.h")

if(NOT EXISTS "${_webview2_header}" OR NOT EXISTS "${_webview2_build_header}")
  set(_webview2_version "1.0.1418.22")
  set(_webview2_url "https://www.nuget.org/api/v2/package/Microsoft.Web.WebView2/${_webview2_version}")
  set(_webview2_sha256 "51d2ef56196e2a9d768a6843385bcb9c6baf9ed34b2603ddb074fb4995543a99")

  file(MAKE_DIRECTORY "3rdparty/webview2")
  set(_nuget_rel "3rdparty/webview2/webview2.zip")
  get_filename_component(_nuget "${_nuget_rel}" ABSOLUTE)
  get_filename_component(_dest "3rdparty/webview2" ABSOLUTE)

  if(NOT EXISTS "${_nuget}")
    message(STATUS "wxWidgets patch: downloading WebView2 SDK ${_webview2_version}")
    file(DOWNLOAD "${_webview2_url}" "${_nuget}" EXPECTED_HASH "SHA256=${_webview2_sha256}" SHOW_PROGRESS)
  endif()

  if(NOT EXISTS "${_webview2_header}")
    message(STATUS "wxWidgets patch: extracting WebView2 SDK into source tree with PowerShell")
    execute_process(
      COMMAND powershell -NoProfile -ExecutionPolicy Bypass -Command "Expand-Archive -Path '${_nuget}' -DestinationPath '${_dest}' -Force"
      RESULT_VARIABLE _extract_rc_src
    )
    if(NOT _extract_rc_src EQUAL 0)
      message(FATAL_ERROR "wxWidgets patch: WebView2 SDK extract to source tree failed (code ${_extract_rc_src})")
    endif()
  endif()

  if(NOT EXISTS "${_webview2_build_header}")
    file(MAKE_DIRECTORY "${_webview2_build_pkg_dir}")
    message(STATUS "wxWidgets patch: extracting WebView2 SDK into build package dir with PowerShell: ${_webview2_build_pkg_dir}")
    execute_process(
      COMMAND powershell -NoProfile -ExecutionPolicy Bypass -Command "Expand-Archive -Path '${_nuget}' -DestinationPath '${_webview2_build_pkg_dir}' -Force"
      RESULT_VARIABLE _extract_rc_build
    )
    if(NOT _extract_rc_build EQUAL 0)
      message(FATAL_ERROR "wxWidgets patch: WebView2 SDK extract to build package dir failed (code ${_extract_rc_build})")
    endif()
  endif()

  if(NOT EXISTS "${_webview2_header}")
    message(FATAL_ERROR "wxWidgets patch: WebView2 SDK header missing in source tree: '${_webview2_header}'")
  endif()
  if(NOT EXISTS "${_webview2_build_header}")
    message(FATAL_ERROR "wxWidgets patch: WebView2 SDK header missing in build package dir: '${_webview2_build_header}'")
  endif()
endif()
