function(copy_directory_raw target directory)
  
  if (WIN32)
    add_custom_command(TARGET ${target} POST_BUILD COMMAND Xcopy /E /I /Y
        \"${directory}\" 
        \"$<TARGET_FILE_DIR:${target}>\")
  else() 
    add_custom_command(TARGET ${target} POST_BUILD COMMAND cp -r
        \"${directory}\"
        \"$<TARGET_FILE_DIR:${target}>\")
  endif()
  
endfunction()
 