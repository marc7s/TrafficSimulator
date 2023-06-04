using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace RoadGenerator
{
    public class BuildingGenerator
    {
        public void GenerateBuildings(List<BuildingWay> buildingWays, GameObject buildingPrefab, Material buildingWallMaterial, Material buildingRoofMaterial, Transform buildingContainer)
        {
            foreach (BuildingWay buildingWay in buildingWays)
            {
                GameObject building = UnityEngine.Object.Instantiate(buildingPrefab, Vector3.zero, Quaternion.identity);
                building.transform.parent = buildingContainer;
                float defaultBuildingHeight = 25;
                float height = buildingWay.Height ?? defaultBuildingHeight;

                if (buildingWay.Height == null && buildingWay.BuildingLevels != null)
                    height = buildingWay.BuildingLevels.Value * 3.5f;

                height += UnityEngine.Random.value * 0.2f;

                List<Vector3> buildingPointsBottom = buildingWay.Points;
                List<BuildingPoints> buildingPoints = new List<BuildingPoints>();
                List<Vector3> buildingPointsTop = new List<Vector3>();

                foreach (Vector3 point in buildingPointsBottom)
                    buildingPoints.Add(new BuildingPoints(point, new Vector3(point.x, height, point.z)));

                foreach (BuildingPoints point in buildingPoints)
                    buildingPointsTop.Add(point.TopPoint);

                building.name = GetBuildingName(buildingWay);

                BuildingMeshCreator.GenerateBuildingMesh(building, buildingPointsBottom, buildingPointsTop, buildingPoints, buildingWallMaterial, buildingRoofMaterial);
            }
        }

        private static string GetBuildingName(BuildingWay buildingWay)
        {
            const string defaultBuildingName = "Building";

            if (buildingWay.Name != null)
                return buildingWay.Name;
 
            if(buildingWay.StreetName == null || buildingWay.StreetAddress == null)
                return defaultBuildingName;

            return buildingWay.StreetName + " " + buildingWay.StreetAddress;
        }
    }
}