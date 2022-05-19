from conans import ConanFile, CMake

class ###PROJECT_NAME###Conan(ConanFile):
    name = "###PROJECT_NAME###"
    version = "0.1"
    settings = "os", "compiler", "build_type", "arch"
    generators = "cmake_find_package", "cmake_paths"
    requires = [
        ("freetype/2.10.1"),
        ("glfw/3.3.6"),
        ("glm/0.9.9.7"),
        ("gtest/1.10.0"),
        ("libsndfile/1.0.30"),
        ("ms-gsl/3.1.0"),
        ("openal/1.21.1"),
        ("onetbb/2021.3.0"),
        ("spdlog/1.8.2"),
        ("vulkan-loader/1.2.198.0"),
        #("vulkan-memory-allocator/2.3.0")
    ]
    options = {
        "engineBuild": [True, False],
        "verbose": [True, False],
        "config": ["Release", "Debug", "MinSizeRel", "RelWithDebInfo"]
    }
    default_options = {
        "freetype:shared":True,
        "glfw:shared":True,
        "libsndfile:shared":True,
        "openal:shared":True,
        "PNG:shared":True,
        "BZip2:shared":True,
        "flac:shared":True,
        "fmt:shared":True,
        "Opus:shared":True,
        "Ogg:shared":True,
        "Vorbis:shared":True,
        "vulkan-loader:shared":True,
        "spdlog:header_only":True,
        "engineBuild":False,
        "verbose": False,
        "config": "Debug",
    }

    def requirements(self):
        if self.settings.os == "Macos":
            self.requires("moltenvk/1.1.6")
            self.options["moltenvk"].shared = True
            self.output.info("Generating for MacOS with MoltenVK support")
            
    def cmake_configure(self):
        cmake = CMake(self)
        cmake.verbose = self.options.verbose
        if self.options.engineBuild:
            cmake.definitions["NOVELRT_BUILD_SAMPLES"] = "Off"
            cmake.definitions["NOVELRT_BUILD_DOCUMENTATION"] = "Off"
        cmake.configure(args=["--no-warn-unused-cli"])
        return cmake

    def build(self):
        cmake = self.cmake_configure()
        cmake.build(args=[f'--config {self.options.config}'])