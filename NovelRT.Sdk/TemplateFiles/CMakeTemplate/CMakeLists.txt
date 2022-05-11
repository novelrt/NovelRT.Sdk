cmake_minimum_required(VERSION 3.19 FATAL_ERROR)

cmake_policy(SET CMP0091 NEW)

project(###PROJECT_NAME###
  VERSION ###PROJECT_VERSION###
  DESCRIPTION "###PROJECT_DESCRIPTION###"
  LANGUAGES C CXX
)

if(MSVC)
  # CMake by default appends /W3, so we need to strip it to prevent warnings (D9025)
  string(REGEX REPLACE " /W[0-4]" "" CMAKE_C_FLAGS "${CMAKE_C_FLAGS}")
  string(REGEX REPLACE " /W[0-4]" "" CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS}")
  string(REGEX REPLACE " /M[TD]d?" "" CMAKE_C_FLAGS "${CMAKE_C_FLAGS}")
  string(REGEX REPLACE " /M[TD]d?" "" CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS}")
endif()

if(WIN32)
  set(CMAKE_WINDOWS_EXPORT_ALL_SYMBOLS ON)
  set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreadedDLL")
endif()

#set(###PROJECT_NAME###_NOVELRT_VERSION ###NOVELRT_VERSION### CACHE STRING "NovelRT version")

list(APPEND CMAKE_MODULE_PATH "${CMAKE_SOURCE_DIR}/cmake")
set(GAME_RESOURCES "${CMAKE_SOURCE_DIR}/Resources")


include(${CMAKE_SOURCE_DIR}/cmake/CopyBuildProducts.cmake)
include(${CMAKE_BINARY_DIR}/conan_paths.cmake)
###NOVELRT_ENGINE_SUBDIR###

add_subdirectory(src)
