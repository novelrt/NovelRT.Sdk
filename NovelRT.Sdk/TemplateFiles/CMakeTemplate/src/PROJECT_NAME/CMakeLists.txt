include(CopyDirectoryRaw)

find_package(novelrt ${###PROJECT_NAME###_NOVELRT_VERSION} REQUIRED)

set(SOURCES
    main.cpp
  )

add_executable(###PROJECT_NAME###_Game ${SOURCES})

target_compile_features(###PROJECT_NAME###_Game PUBLIC cxx_std_17)

target_include_directories(###PROJECT_NAME###_Game
  PUBLIC
    $<BUILD_INTERFACE:${CMAKE_SOURCE_DIR}/include>
    $<INSTALL_INTERFACE:include>
)

if(MSVC)
  target_compile_options(###PROJECT_NAME###_Game
    PRIVATE
      /W4
      /WX
      /wd4611
      /MP
  )
elseif(CMAKE_CXX_COMPILER_ID STREQUAL "GNU")
  target_compile_options(###PROJECT_NAME###_Game
    PRIVATE
      -pedantic
      -pedantic-errors
      -Wall
      -Wextra
      -Werror
      -Wno-float-equal
      -Wno-padded
  )
else()
  target_compile_options(###PROJECT_NAME###_Game
    PRIVATE
      -pedantic
      -pedantic-errors
      -Wall
      -Wextra
      -Werror
      -Wno-c++98-compat
      -Wno-c++98-compat-pedantic
      -Wno-float-equal
      -Wno-padded
      -Wno-reserved-id-macro
  )
endif()

target_link_libraries(###PROJECT_NAME###_Game
  PRIVATE
    novelrt::novelrt
)

copy_directory_raw(###PROJECT_NAME###_Game ${CMAKE_SOURCE_DIR}/Resources)

