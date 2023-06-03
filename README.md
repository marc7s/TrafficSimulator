# TrafficSimulator
A bachelors thesis in Data Engineering and IT at [Chalmers University of Technology](https://www.chalmers.se/en/).
For more information about the project, you are welcome to read the thesis [here](https://github.com/marc7s/Traffic-Simulator-Documentation/blob/main/Project_report/Project_report.pdf).

More documentation of the development process is available in the designated [repository](https://github.com/marc7s/Traffic-Simulator-Documentation).

# Contents of this file
1. Purpose - Here the purpose and aim of the project is explained.
2. Showcase - A demonstration of some of the features of the developed simulation tool.
3. RoadGenerator - As part of the development, a custom road system generator asset was built, based on Sebastian Lague's [Path Creator](https://github.com/SebLague/Path-Creator) tool. It was extended to include many features, such as intersection generation and vehicle navigation.

# 1. Purpose
The purpose of the project is to design and construct a 3D traffic simulation tool
with high accessibility, that should provide detailed and accessible data. This allows
the user to evaluate the performance of different road networks and traffic scenarios,
and make informed decisions about urban planning and infrastructure. Data should
be presented in real-time through presentation of relevant statistics. By adjusting
the parameters of the simulation, the user should be able to witness the effect of their
tweaking, and easily see if their changes have a positive or negative impact across
relevant environmental dimensions such as congestion level or emissions.

# 2. Showcase

## Navigation
![Navigation Graph](Images/Navigation/NavigationGraph.png)
*A generated graph representing a road system*

## Road Systems
![Navigation Graph](Images/RoadGenerator/FourWayIntersection.png)
*A four way intersection*

![Navigation Graph](Images/RoadGenerator/ThreeWayIntersection.png)
*A three way intersection*

## Statistics

## Vehicles

# 3. RoadGenerator

# 4. How to build
1. Open the build settings through `File -> Build Settings`
2. Select the `StartMenu` scene as index 0 in the build settings, then add all the scenes for the scene selector to the list
3. Run `Build` or `Build And Run`
