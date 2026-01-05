file(READ "CMakeLists.txt" _tbb_cmakelists)
string(REPLACE "cmake_minimum_required(VERSION 3.1)" "cmake_minimum_required(VERSION 3.5)" _tbb_cmakelists "${_tbb_cmakelists}")
file(WRITE "CMakeLists.txt" "${_tbb_cmakelists}")

