include(CopyDirectoryRaw)
###NOVELRT_FIND_PACKAGE###

set(SOURCES
    main.cpp
  )

add_executable(App ${SOURCES})

###NOVELRT_ENGINE_SRC_DEPENDENCIES###
target_compile_features(App PUBLIC cxx_std_17)

target_include_directories(App
  PUBLIC
    $<BUILD_INTERFACE:${CMAKE_SOURCE_DIR}/include>
    $<INSTALL_INTERFACE:include>
)

if(MSVC)
  target_compile_options(App
    PRIVATE
      /W4
      /WX
      /wd4611
      /MP
  )
elseif(CMAKE_CXX_COMPILER_ID STREQUAL "GNU")
  target_compile_options(App
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
  target_compile_options(App
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

target_link_libraries(App
  PRIVATE
    ###NOVELRT_ENGINE_LIB###
)

set_target_properties(App PROPERTIES OUTPUT_NAME "###PROJECT_NAME###")

###NOVELRT_ENGINE_SOURCE_CBP###
