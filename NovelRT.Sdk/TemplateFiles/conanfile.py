from conans import ConanFile, CMake

class ###PROJECT_NAME###Conan(ConanFile):
    settings = "os", "compiler", "build_type", "arch"
    generators = "cmake", "cmake_find_package_multi"
    requires = [
        ("novelrt/###NOVELRT_VERSION###")
    ]

    def build(self):
        cmake = CMake(self)
        cmake.configure()
        cmake.build()

    def imports(self):
        if self.settings.os == "Windows":
            self.copy("*.dll", "bin", "bin")
        self.copy("*.spv", "bin/Resources/Shaders", "bin/Resources/Shaders")
 