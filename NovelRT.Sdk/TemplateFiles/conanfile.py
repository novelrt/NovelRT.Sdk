from conans import ConanFile, CMake

class ###PROJECT_NAME###Conan(ConanFile):
    settings = "os", "compiler", "build_type", "arch"
    generators = "cmake_find_package", "cmake_paths"
    requires = [
        ("novelrt/###NOVELRT_VERSION###")
    ]

    def build(self):
        cmake = CMake(self)
        cmake.configure()
        cmake.build()

    def imports(self):
        if self.settings.os == "Windows":
            self.copy("*.dll", "../deps", "bin")
        self.copy("*.spv", "../Resources/Shaders", "bin/Resources/Shaders")

