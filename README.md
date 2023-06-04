# TrafficSimulator
A bachelors thesis in Data Engineering and IT at [Chalmers University of Technology](https://www.chalmers.se/en/).
The project was a suggestion by the five members and was completed over a period of 4,5 months. For more information about the project, you are welcome to read the thesis [here](https://github.com/marc7s/Traffic-Simulator-Documentation/blob/main/Project_report/Project_report.pdf).

More documentation of the development process is available in the designated [repository](https://github.com/marc7s/Traffic-Simulator-Documentation).

# Contents of this file
1. Purpose - Here the purpose and aim of the project is explained.
2. Showcase - A demonstration of some of the features of the developed simulation tool.
3. RoadGenerator - As part of the development, a custom road system generator asset was built, based on Sebastian Lague's [Path Creator](https://github.com/SebLague/Path-Creator) tool. It was extended to include many features, such as intersection generation and vehicle navigation.
4. How to build - A short tutorial of how to build the simulation tool.

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

## UI Overview
The User Interface, or UI for short, is the means with which the user interacts with the simulator. Therefore, effort was put into making it easy to use, following common looks and practices in order to make it intuitive. This section will showcase the different parts of the UI:
1. The main menu, where settings can be changed and from which the simulation is started.
2. The simulation UI, which is an overlay with which the user can interact during the simulation. This contains the statistics, which are presented in the following section.

### Main menu
![Main Menu](Images/UI/MainMenu.png)<br>
*The main menu*<br><br><br>

![Scene Selection](Images/UI/SceneSelection.png)<br>
*The scene selection menu, where you can choose the road system to simulate*<br><br><br>

![Settings](Images/UI/Settings.png)<br>
*The settings menu*<br><br><br>

### Simulation UI
![UI Overview](Images/UI/Overview.png)<br>
*An overview of the UI*<br><br><br>

![Selected Vehicle Info](Images/UI/SelectedVehicleInfo.png)<br>
*A selected vehicle, with its current navigation path and information*<br><br><br>

## Statistics
During the simulation, data is collected and presented in real-time to the user through the statistics panel. Here, the user can see details for specific vehicles, or statistics for the entire road network. The fuel consumption gathers the total fuel consumed by all vehicles over time, while the congestion ratio is an indication of how congested the network is. It represents the ratio of currently congested roads, and can be used to easily identify bottlenecks or heavily trafficated roads.

![Fuel Consumption](Images/Statistics/FuelConsumption.png)<br>
*Gathered statistics over total fuel consumption over time*<br><br><br>

![Congestion Ratio](Images/Statistics/Congestion.png)<br>
*Gathered statistics over ratio of congested roads over time*<br><br><br>

![Road Colour](Images/Statistics/RoadColour.png)<br>
*Each Road can be coloured according to the current congestion or fuel consumption*<br><br><br>

## Cameras
Three different cameras are available during the simulation and are presented in this section.

![Freecam](Images/Cameras/Freecam.png)<br>
*The freecam allows the user to freely roam around*<br><br><br>

![Follow Camera](Images/Cameras/FollowCamera.png)<br>
*The follow camera follows the selected vehicle*<br><br><br>

![Driver Camera](Images/Cameras/DriverCamera.png)<br>
*The driver camera places the user behind the wheel of a simulated vehicle, enabling further immersion*<br><br><br>

## Navigation
In order for the vehicles to navigate the road system, each vehicle drives as an individual agent. The agents follow the roads - more specifically the nodes generated along the roads - which can be seen in the later section titled "RoadGenerator".

Furthermore, they need to find their way through the road system, driving through intersections and planning their route. For this, a graph is generated representing the road system. The nodes are intersections, road end points and POIs and any other point the vehicles need to find. The edges are the roads between these. Using the road system graph, an A* path finding algorithm is used by each vehicle to plan their route, which can be seen in the following gif.

![Navigation Graph](Images/Navigation/NavigationGraph.png)<br>
*A generated graph representing a road system*<br><br><br>

![Vehicle Navigation](Images/Navigation/VehicleNavigation.gif)<br>
*A vehicle navigating a RoadSystem, driving to its target*<br><br><br>

## Manual Driving
A user can at any time enter the manual driving mode, where they will gain control of their own vehicle. This allows the user to drive around the road system, gaining a good understanding of what it is like to drive around the road network. This facilitates a unique opportunity to evaluate changes to the system by placing yourself in the driver's seat of a road user, offering an intuitive way to understand whether it is easy for the drivers to find their way around or not.

The manual vehicle will interact with the other vehicles in the road systems, which will queue up behind the manually driven vehicle as any other vehicle. This integrates the manual car with the traffic.

![Manual Car](Images/ManualCar/ManualCar.png)<br>
*A car being driven manually, able to interact with the traffic*<br><br><br>

## OpenStreetMap - OSM
While it is possible to manually create road systems through the developed RoadGenerator tool in the Unity Editor (only available with the project open in Unity), an integration with OpenStreetMap allows for automatic generation of road systems. The integration contains a parser that can read OSM map data and generate roads, intersections, buildings, bus stops, parkings and more according to the real world data. It also configurates the roads to mirror the real world, matching settings such as road width, speed limits and one way roads.

For this project, a map of Masthugget - a district of Gothenburg, Sweden - was exported from OSM and used to generate an entire environment which can be simulated. The following section contains some images of the generated version of Masthugget.

![Comparison](Images/OSM/OSMTransition.gif)<br>
*A comparison between the OpenStreetMap version and the generated environment*<br><br><br>

![Masthugget Overview](Images/OSM/MasthuggetOverview.gif)<br>
*An overview of the Masthugget scene*<br><br><br>

![Masthugget FPV](Images/OSM/MasthuggetFPV.gif)<br>
*A first person view of the generated Masthugget*<br><br><br>

![Masthugget Simulation](Images/OSM/MasthuggetSimulation.png)<br>
*Vehicles simulated in Masthugget*<br><br><br>

![Ferry View](Images/OSM/FerryView.png)<br>
*A view of Masthugget from the river Göta älv*<br><br><br>

## Vehicles
An important part of the simulation is the vehicles. To tie it together, a range of realistic looking models are simulated. These are showcased in this section.

![Vehicle Types](Images/Vehicles/VehicleTypes.png)<br>
*A number of different vehicle types are simulated for a realistic feel*<br><br><br>

![Vehicle Closeup](Images/Vehicles/VehicleCloseup.png)<br>
*A closer look at one of the vehicles*<br><br><br>

## Points of Interest - POIs
A few different points of interest can be added to the road systems, such as bus stops or parkings. The simulated vehicles can interact with the POIs. As an example, they can navigate to parking POIs and park as well as leave the parking. Simulated buses can follow a specified bus route, stopping at every bus stop along the route which simulates public transport.

![Bus Stop](Images/POIs/BusStop.png)<br>
*A closeup of a generated bus stop, complete with its name tag*<br><br><br>

![Bus Route](Images/POIs/BusRoute.png)<br>
*A bus with its displayed navigation path, visiting all its related bus stops in order*<br><br><br>

![Roadside Parking](Images/POIs/RoadsideParking.png)<br>
*Vehicles parked at a roadside parking*<br><br><br>

![Parking Lot](Images/POIs/ParkingLot.png)<br>
*Vehicles parked at a parking lot*<br><br><br>

# 3. RoadGenerator
As part of the project, an asset called RoadGenerator was developed to allow the creation of road systems. RoadGenerator contains many different features such as automatic intersection generation and an integration with OpenStreetMap to create virtual twins of real life locations to be simulated. Some of these features will be shown in the following section, while others have already been demonstrated in the previous section. Note that there are many additional features to RoadGenerator, and that this is a simplification since it is only a showcase and not documentation.

RoadGenerator is based on Sebastian Lague's [Path Creator](https://github.com/SebLague/Path-Creator) asset, which enables the creation and editing of Bézier path. A Bézier path is a chained sequence of [Bézier curves](https://pomax.github.io/bezierinfo/), which are mathematical definitions of curves. They have many use cases and form the basis of the roads in RoadGenerator.

![Bezier Path](Images/RoadGenerator/BezierPath.png)<br>
*A Bézier Path, used to describe the path of every Road*<br><br><br>

Nodes, called RoadNodes, are generated along the Bézier curve of every Road and are used by the navigation among others. They also contain information used by many parts of the simulation, such as the vehicles and the intersection yielding algorithms.

![Bezier Path RoadNodes](Images/RoadGenerator/BezierPathWithRoadNodes.png)<br>
*RoadNodes generated along a Bézier path*<br><br><br>

![Road Mesh](Images/RoadGenerator/RoadWithRoadNodes.png)<br>
*A mesh is generated along each Bézier curve, forming the Road*<br><br><br>

![Intersection Generation](Images/RoadGenerator/GeneratedIntersection.png)<br>
*At every intersecting point between Roads, an Intersection is automatically generated*<br><br><br>

There is support for two different intersection types - three and four way intersections. The type of intersection is determined automatically during the generation. Each intersection can also be configured, such as switching between traffic lights or yield signs.

![Four Way Intersection](Images/RoadGenerator/FourWayIntersection.png)<br>
*A four way intersection*<br><br><br>

![Three Way Intersection](Images/RoadGenerator/ThreeWayIntersection.png)<br>
*A three way intersection*<br><br><br>

The RoadGenerator asset can be used to easily manually create and edit road systems. The following is a sped up gif of a small road system being created. For larger road systems, the OSM integration is suitable as creating road networks is time consuming. This also has the benefit of replicating a real life location.

![Timelapse](Images/RoadGenerator/RoadCreationTimelapse.gif)<br>
*A timelapse of a RoadSystem being created by hand*<br><br><br>

# 4. How to build
Before building, make sure you have all assets specified in `ASSETS.md` and that they are placed inside the `Unity Assets` folder, as well as all plugins specified in `PLUGINS.md` and that they are placed inside the `Plugins` folder.

1. Open the build settings through `File -> Build Settings`
2. Select the `StartMenu` scene as index 0 in the build settings, then add all the scenes for the scene selector to the list
3. Run `Build` or `Build And Run`
