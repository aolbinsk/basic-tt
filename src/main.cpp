#include "../include/infrastructure/game/GameLoop.h"
#include <iostream>
#include <exception>

int main(int argc, char* argv[]) {
    std::cout << "==================================================" << std::endl;
    std::cout << "  BasicTT - Table Tennis VR Simulation (OpenXR)  " << std::endl;
    std::cout << "==================================================" << std::endl;
    std::cout << std::endl;

    try {
        BasicTT::GameLoop game;

        if (!game.Initialize()) {
            std::cerr << "Failed to initialize game" << std::endl;
            return 1;
        }

        game.Run();
        game.Shutdown();

        std::cout << "\nThank you for playing BasicTT!" << std::endl;
        return 0;

    } catch (const std::exception& e) {
        std::cerr << "Exception: " << e.what() << std::endl;
        return 1;
    } catch (...) {
        std::cerr << "Unknown exception occurred" << std::endl;
        return 1;
    }
}
